# Impact Analysis: MILESTONE_REOPENED

This document is a pre-implementation change impact analysis. It records facts
observed in the current implementation, recommended design decisions, and
decisions that require human approval. No implementation change is made by
this document.

## 1. Change Summary

The requested scope introduces a new milestone lifecycle event named
`MILESTONE_REOPENED`. A successful reopen must:

- produce an immutable audit entry;
- notify the relevant team members; and
- capture the acting user's IP address in the audit record.

The change affects both sides of the existing synchronous Project API to
Notification & Audit Service integration. It is not only a new status command:
the event must be authorized and tenant-scoped in the Project API, represented
in the lifecycle contract, accepted and persisted by the Notification & Audit
Service, and handled idempotently with notification creation.

The approved event name is `MILESTONE_REOPENED` at the external contract
boundary. Existing event strings such as `MilestoneCreated` and
`MilestoneStatusUpdated` remain unchanged for backward compatibility.

## 2. Current Architecture Impact

### Facts discovered

The repository uses two service boundaries:

- The Project API is under `src/projects/TaskBridge.Api`.
- The Notification & Audit Service is under `src/notifications`.
- Tests are under `tests/TaskBridge.Tests`.

The relevant current components are:

| Area | Existing component | Likely impact |
|---|---|---|
| Milestone domain | `Models/Milestone.cs`, `MilestoneStatus` | Define eligible source states and resulting state; no new enum value should be added until approved. |
| Milestone contracts | `Models/MilestoneDtos.cs` | Add a reopen request/response contract only if the endpoint needs one; preserve concurrency-token behavior. |
| Milestone business logic | `Services/MilestoneService.cs`, `IMilestoneService` | Add the authorized state transition, snapshot creation, persistence, recipient selection, and event publication. |
| Milestone HTTP API | `Controllers/MilestonesController.cs` | Add a dedicated command route or extend the status route, with the existing tenant authorization policy. |
| Lifecycle contract | `Contracts/LifecycleEventContracts.cs` | Carry the new event type and actor IP data through the integration contract. |
| Event publisher | `Services/LifecycleEventPublisher.cs` | Serialize the added contract data and ensure the authenticated internal token carries or binds the trusted actor IP. |
| Tenant context | `Security/ITenantContext.cs`, `Security/TenantContext.cs` | Provide trusted organization and actor identity; likely extend the abstraction for request IP resolution or add a focused request-context abstraction. |
| Authentication | `Security/AuthenticationExtensions.cs`, `Program.cs` | Protect the new route and configure trusted proxy/forwarded-header behavior before IP resolution. |
| Project persistence | `Data/TaskBridgeDbContext.cs` | Continue organization-filtered milestone/project/team queries and concurrency handling; do not change persisted project/milestone models in this analysis step. |
| Notification request DTO | `src/notifications/DTOs/AuditDtos.cs` | Accept the new event and actor IP field from the authenticated internal integration. |
| Audit entity | `src/notifications/Models/AuditEntry.cs` | Add nullable actor IP storage in the implementation phase if approved. |
| Audit persistence | `Repositories/IAuditEntryRepository.cs`, `Repositories/EfRepositories.cs` | Preserve insert/read-only audit behavior and tenant predicates; no update/delete path should be introduced. |
| Audit business logic | `Services/AuditService.cs` | Allow-list the event, validate its snapshots and IP representation, include IP in idempotency matching, and create notifications. |
| Audit HTTP API | `Controllers/AuditController.cs` | The existing internal `POST /audit` is the likely ingestion surface; its policy must continue to require the Project API service identity. |
| Notification business logic | `Services/NotificationService.cs` | Existing read/mark-read tenant and recipient ownership rules should remain unchanged. |
| Notification persistence | `Models/Notification.cs`, `Data/NotificationDbContext.cs` | Message generation and uniqueness should support the new event; no notification schema change appears necessary. |
| Database and migrations | `NotificationDbContext` and EF migration tooling | Add an audit column and migration if `ActorIpAddress` is approved; no migration files currently exist in the repository. |
| Error handling | Both `Middleware/ExceptionHandlingMiddleware.cs` files | Reuse existing validation, authorization, conflict, not-found, and integration-failure mappings. |
| Tests | `MilestoneServiceTests.cs`, `NotificationServiceTests.cs`, tenant tests | Add focused unit/integration coverage after the contract and product decisions are approved. |
| Documentation | `SPEC.md`, `README.md`, and this analysis | Update the specification and operational documentation during implementation, not during this analysis step. |

### Relevant current behavior

`MilestoneService` currently:

1. obtains organization and actor IDs from `ITenantContext`;
2. loads milestones and projects with organization predicates;
3. uses `ConcurrencyToken` for status updates;
4. saves a successful mutation before publishing its lifecycle event;
5. selects distinct team-member user IDs through the project team and tenant;
6. publishes synchronously through `ILifecycleEventPublisher`; and
7. reports notification integration failures as `503` while leaving the
   already-committed milestone change in place.

The Notification & Audit Service currently allow-lists six event strings,
stores JSON snapshots as `jsonb`, creates an immutable audit entry and
notifications in one persistence operation, and deduplicates recipients with
`Distinct()`. `SourceEventId` has a unique index and duplicate delivery returns
the existing audit result without creating more notifications.

## 3. Functional Impact

### Existing lifecycle facts

The current `MilestoneStatus` values are `Planned`, `InProgress`, `Completed`,
and `Cancelled`. There is no `Reopened` status. Existing updates replace the
status, refresh the concurrency token and update timestamp, then publish a
full milestone snapshot before and after the change.

### Recommended behavior

Treat reopening as a dedicated command with an explicit domain rule rather
than as an arbitrary status update. The command should:

- load the milestone by ID and trusted organization ID;
- require the current concurrency token to prevent stale reopen operations;
- reject states not explicitly eligible for reopening;
- set the approved target status;
- create a previous snapshot containing the complete persisted milestone state,
  including the old status;
- create a new snapshot containing the committed state, including the target
  status; and
- publish `MILESTONE_REOPENED` only after the milestone save succeeds.

The notification recipient source can reuse the existing tenant-safe project
team query. It should select distinct relevant team members, and should include
the actor only if that is the established product policy. The Notification &
Audit Service should continue to deduplicate at both service and database
levels.

### Decisions not safely inferable from the code

The following lifecycle details are not defined by the current implementation:

- whether `Completed`, `Cancelled`, or both can be reopened;
- whether reopening results in `Planned` or `InProgress`;
- whether a milestone can be reopened repeatedly after it has been reopened;
- whether a reopened milestone may be reopened from any later status transition;
- whether the snapshots should include only status or the full milestone
  snapshot; and
- whether assignees, project members, team members, or a combination are
  notification recipients.

The recommended default is to use a dedicated command, preserve the existing
status vocabulary, and record full before/after snapshots. Product must choose
the eligible states, target status, repeat behavior, and recipient policy.

## 4. API Impact

### Recommendation

Add a dedicated endpoint:

`POST /api/milestones/{id}/reopen`

This is the simplest API design consistent with a command that has a distinct
business rule and event. It avoids allowing a general status update caller to
bypass reopen-specific authorization or lifecycle validation. It also makes
the audit event unambiguous. The existing `PATCH /api/milestones/{id}/status`
should not silently acquire reopen semantics.

### Proposed contract

The request should contain the current `ConcurrencyToken`; it should not
contain organization ID, actor ID, actor IP, or an arbitrary target status.
The target status should be determined by the approved domain rule.

The response should be `200 OK` with the existing `MilestoneResponse`, including
the new status and refreshed concurrency token. No client-supplied IP address
should appear in the request or be echoed as authoritative data.

The endpoint must require authentication and the existing tenant access policy.
The service must derive organization and actor identity from trusted claims and
must query the milestone and containing project within that organization. A
milestone belonging to another organization should be indistinguishable from
missing and return the existing not-found behavior.

Expected behavior is:

- `400 Bad Request` for a missing/empty concurrency token or invalid request;
- `401 Unauthorized` for missing authenticated organization or actor context;
- `403 Forbidden` for an authenticated user lacking the approved reopen
  permission;
- `404 Not Found` for a milestone outside the caller's tenant or absent;
- `409 Conflict` for a stale concurrency token or conflicting lifecycle state,
  subject to the existing error taxonomy; and
- `503 Service Unavailable` when the synchronous lifecycle integration rejects
  the committed event, matching the current design.

Whether authorization is team membership, project permission, a dedicated
milestone permission, or an administrative permission is a human decision.

## 5. Audit Model Impact

### Recommended model change

Add `ActorIpAddress` to `AuditEntry` as a nullable string, and expose it through
the audit DTO/response where audit readers are permitted to see it. Nullable is
required for historical rows created before IP capture and for any approved
non-HTTP ingestion path that has no meaningful source IP.

The value should be a normalized textual representation produced by the
platform's IP address parser, supporting both IPv4 and IPv6. A bounded database
string is appropriate; the implementation should define a maximum length and
canonicalization policy. Storing the parsed address rather than an arbitrary
header string prevents malformed values and preserves a consistent format.

An index on actor IP is not required for the requested lifecycle behavior and
would increase sensitive-data queryability. Add one only if a documented
incident-investigation or compliance query requires it. Existing event/project
and tenant indexes remain the primary audit read indexes.

### Trusted resolution and propagation

The Project API is the service that receives the user request, so it should
resolve the actor IP from the server request context after trusted forwarded
header middleware has been configured. A suitable abstraction should expose a
parsed address to `MilestoneService`; the service must not read arbitrary
request headers directly.

The resolved value should be included in the internal lifecycle event and sent
to the Notification & Audit Service over the authenticated service-to-service
channel. To prevent tampering, the integration should bind the value to the
authenticated Project API request, preferably by carrying it in a signed
service-token claim and checking that it matches the contract payload, or by
using an equivalently authenticated contract mechanism. The Notification &
Audit Service must treat this as trusted only because the authenticated
Project API is trusted, not because the body field is client-controlled.

The external milestone request must not contain `ActorIpAddress`. A client
cannot select, override, or replay an arbitrary IP address as the actor's
address. The Notification & Audit Service should reject malformed IP values
and should reject mismatches between authenticated service context and event
payload where the chosen integration design provides both values.

### Reverse proxies and privacy

`RemoteIpAddress` may be the proxy address in a deployed topology. Forwarded
headers can identify the original client only when they come through explicitly
configured, trusted proxies or load balancers. The application must configure
the ASP.NET Core forwarded-headers middleware with an allow-list of known proxy
addresses/networks and an appropriate forward limit. It must not trust an
unvalidated `X-Forwarded-For` or similar header from a direct client.

IP addresses are personal data in many jurisdictions. Retention, access
control, encryption/backups, masking in diagnostic logs, incident access, and
deletion/retention policy must be reviewed. The IP should not be written into
ordinary application logs or error messages unnecessarily.

## 6. Security Impact

The new operation increases the value of the lifecycle integration payload and
adds a potentially sensitive attribute. Required controls are:

- derive tenant identity from the authenticated Project API context and enforce
  it on milestone, project, team-member, and event queries;
- derive actor identity from trusted authentication claims, never request JSON;
- resolve IP only from a trusted server/proxy context;
- authenticate the Project API as the event sender and validate its service
  authorization in `AuditIngestion`;
- bind organization, actor, event, and IP data to the authenticated internal
  request to prevent service-token/payload substitution;
- reject cross-organization milestone IDs and never use a supplied organization
  ID to widen access;
- ensure recipient selection is tenant-scoped and cannot include users from
  another organization; and
- authorize reopening independently of Notification & Audit Service
  authorization, because the Project API owns milestone permissions.

New risks include spoofed forwarded headers, a compromised or over-privileged
Project API integration credential, replay of a valid event, leakage of IP data
through audit reads, and authorization bypass if reopen is implemented as a
generic status update. `SourceEventId` idempotency and short-lived authenticated
service tokens reduce replay impact but do not replace authorization or
tenant checks.

## 7. Data and Migration Impact

Adding `ActorIpAddress` requires an EF Core model change in the Notification &
Audit Service and a PostgreSQL migration adding a nullable bounded text/varchar
column to the audit table. Existing audit rows must remain valid with
`NULL` IPs. Historical values should not be fabricated from logs or inferred
from unrelated requests.

The migration should be additive and deployable before or alongside the
application version that writes the field. Read paths should tolerate nulls
during rollout. The audit insert-only policy, database permissions, and any
future trigger must continue to prevent updates and deletes.

No change to the `SourceEventId` uniqueness rule is needed. The new event must
use a new stable ID per lifecycle action. Duplicate delivery with the same
event ID must return the original audit result and must not create duplicate
notifications. Idempotency matching should include all immutable identifying
fields, including the approved IP field if a changed IP on the same event ID
is considered a conflicting reuse rather than an idempotent retry.

An IP index is not recommended by default because the requested reads are
tenant/project/event oriented and IP is sensitive. Existing audit indexes may
need review only if the new event materially changes audit query patterns.

## 8. Notification Impact

The existing recipient selection in `MilestoneService.CreateEventAsync` can be
reused: it filters team membership through the containing project's team and
organization, then applies `Distinct()`. The reopen operation should use the
same source and preserve the existing requirement that a notification-producing
event has at least one recipient.

`MILESTONE_REOPENED` requires an allow-list entry in `AuditService` and a
message template or message-generation branch. The message should identify
the milestone/project and the reopen action without exposing the actor IP.

No notification table change appears necessary. The existing unique
`(AuditEntryId, RecipientUserId)` constraint and `SourceEventId` idempotency
behavior should prevent duplicates during retries. Tests must verify both
duplicate input recipient IDs and repeated event delivery.

Whether assignees must be added to the current team-member recipient set is a
product decision; the Notification & Audit Service should not infer recipients
from an untrusted milestone ID.

## 9. Testing Impact

The implementation should add deterministic tests covering at least:

1. A valid milestone reopen creates an audit entry.
2. The reopen audit entry contains the correct previous and new state snapshots.
3. Relevant distinct team members receive notifications.
4. A cross-organization milestone reopen is prevented.
5. An invalid lifecycle state cannot be reopened.
6. Duplicate team members do not receive duplicate notifications.
7. The actor IP is captured from the trusted request context.
8. A client-supplied fake IP is not treated as authoritative.
9. Existing audit records remain compatible with a null IP.
10. Integration failure behavior remains consistent with the existing lifecycle
    design, including committed milestone state and a reported integration
    failure.

Additional high-value coverage should verify:

- stale concurrency tokens are rejected;
- unauthorized users cannot reopen a milestone;
- malformed, non-canonical, IPv4, and IPv6 values follow the chosen IP policy;
- untrusted forwarded headers cannot override the resolved address;
- a service-token/payload IP mismatch is rejected if both are carried;
- duplicate `SourceEventId` delivery returns the original result without new
  notifications;
- conflicting reuse of `SourceEventId` returns a conflict;
- the new event is accepted by ingestion and audit event filtering;
- audit reads remain tenant-isolated and IP visibility follows authorization;
- audit entries remain immutable; and
- the endpoint returns the documented status codes for missing, foreign, and
  invalid milestones.

## 10. Recommended Implementation Plan

1. **Domain and lifecycle decision.** Obtain approval for eligible source
   states, target status, repeat behavior, recipient policy, and reopen
   authorization. Do not add an enum or constant until the event/status
   vocabulary is approved.
2. **Audit model and DTO changes.** Add nullable `ActorIpAddress` to the audit
   entity, create request, and response as appropriate; define normalization,
   maximum length, null, and idempotency rules.
3. **Database migration.** Generate and review an additive PostgreSQL migration
   for the nullable audit column. Confirm insert-only permissions and rollback/
   deployment sequencing.
4. **Trusted IP resolution.** Configure trusted forwarded headers and introduce
   a request-context abstraction that returns a parsed IP. Add tests proving
   untrusted headers and client JSON cannot override it.
5. **Lifecycle event contract changes.** Add the approved event and IP field to
   `LifecycleEvent`, the publisher payload, the internal authentication binding,
   and Notification & Audit Service validation.
6. **Milestone API/service changes.** Add `POST /api/milestones/{id}/reopen`,
   concurrency validation, authorization, tenant-safe lookup, state mutation,
   complete snapshots, and post-commit event publication.
7. **Notification behavior.** Add the event allow-list and message handling;
   reuse tenant-safe distinct recipient selection and existing idempotency
   constraints.
8. **Tests.** Implement the required unit and integration tests, including
   tenant isolation, proxy trust, fake IP rejection, null historical IPs,
   idempotency, immutability, and integration failure behavior.
9. **Documentation.** Update `SPEC.md`, `README.md`, API examples, retention
   guidance, proxy configuration, and operational runbooks after the design is
   approved. This analysis file should remain as the change-impact record.
10. **Verification.** Run the full solution build and test suite; review the
    generated migration, authorization policy, forwarded-header configuration,
    service-token claims, audit response exposure, database permissions, and
    event payload with a human reviewer.

This order preserves the current behavior until the domain and security
decisions are explicit, and keeps the Project API and Notification & Audit
Service contract changes coordinated.

## 11. Risks and Trade-offs

- **Privacy:** IP addresses can be personal data and increase the sensitivity
  of audit history. Retention and reader permissions must be deliberate.
- **Proxy trust:** Incorrect forwarded-header configuration can record a proxy
  address, accept a spoofed address, or create false audit evidence.
- **Synchronous integration:** The current design can commit a milestone and
  then fail to deliver audit/notification data, producing a reported `503` and
  an operational retry obligation. An outbox would improve durability but is
  outside this scope.
- **Service boundary consistency:** The Project API and Notification & Audit
  Service must deploy compatible event contracts and authentication rules.
  A partial rollout can reject the new event or lose IP data.
- **Backward compatibility:** Existing audit records have no IP and must remain
  readable as null. Existing consumers may need to tolerate the new response
  field.
- **Migration risk:** The additive schema change, provider mapping, database
  permissions, and deployment order require review; no migration directory is
  currently present.
- **Scope expansion:** Reopen authorization, assignee notification rules,
  retention, proxy infrastructure, and durable event delivery can expand the
  change beyond the requested lifecycle event.
- **Audit integrity:** Treating an event body or forwarded header as trusted
  without authenticating its source could create false actor/IP evidence.

## 11A. How Copilot Assisted This Analysis

GitHub Copilot was used in Agent Mode to inspect the existing specification,
Project API, Milestone implementation, Notification & Audit Service, lifecycle
integration contracts, and tests before making any implementation changes.

The analysis prompt explicitly constrained Copilot to perform impact analysis
only and prohibited changes to production source code, database models, enums,
tests, SPEC.md, and README.md.

Copilot assisted by:

- identifying the files and architectural layers affected by
  `MILESTONE_REOPENED`;
- tracing the existing milestone lifecycle and lifecycle event publishing flow;
- identifying the impact on audit DTOs, `AuditEntry`, repositories, services,
  and database migration requirements;
- identifying that actor IP address introduces a new trust boundary involving
  reverse proxies and forwarded headers;
- identifying testing requirements for tenant isolation, notification
  deduplication, audit snapshots, IP spoofing, and integration failures; and
- documenting the impact of the existing synchronous integration consistency
  model.

Human judgment was required to validate and avoid assumptions about:

- which milestone states are eligible for reopening;
- the target status after reopening;
- who is authorized to perform a reopen operation;
- which users should receive notifications;
- whether the actor should receive a notification;
- which reverse proxies can be trusted for forwarded IP information;
- IP address retention and audit-read access policy; and
- whether the existing synchronous consistency trade-off is acceptable.

Copilot's recommendations were treated as implementation options rather than
product decisions. Lifecycle rules, authorization policy, IP trust boundaries,
and privacy/retention requirements require explicit human approval before the
scope change is implemented.

## 12. Human Decisions Required

Copilot should not assume the following:

1. Which milestone states are eligible for reopening: `Completed`, `Cancelled`,
   both, or another explicitly approved set.
2. What status reopening produces: `Planned`, `InProgress`, or a new status
   that would require a separate domain change.
3. Whether reopening is allowed more than once and which transitions follow it.
4. Who is authorized to reopen a milestone and whether that differs from
   ordinary status-update permissions.
5. Whether recipients are all project team members, milestone assignees,
   both, or another defined audience, and whether the actor is included.
6. Whether the external event name is exactly `MILESTONE_REOPENED` or follows
   the current PascalCase names such as `MilestoneStatusUpdated`.
7. Whether `ActorIpAddress` is nullable for all non-HTTP/historical records and
   whether historical records must remain null.
8. Which canonical IP format and maximum length should be stored, including
   IPv4-mapped IPv6 handling.
9. Which reverse proxies/load balancers are trusted and how their addresses,
   forward limits, and deployment configuration are maintained.
10. Whether the IP is included in audit read responses, who may view it, and
    how long it is retained, backed up, or deleted.
11. Whether IP must be duplicated in the signed service-token claim and event
    body, and what mismatch behavior is required.
12. Whether the current synchronous failure semantics are acceptable or an
    outbox/retry change is required before enabling the event.

The highest-risk decisions are the reopen authorization/state transition, the
trust boundary for forwarded IP information, the privacy/retention policy, and
the compatibility and failure behavior of the cross-service contract.

## Scope Confirmation

Files created by this analysis:

- `IMPACT_ANALYSIS.md`

No production source code was changed. No database model, enum, constant, test,
`SPEC.md`, or `README.md` was modified. No `MILESTONE_REOPENED` implementation
was added.

### Human architectural recommendation

Actor IP should be carried once in the authenticated lifecycle event contract.

The Notification & Audit Service should trust the value only when the request is
received from the authenticated Project API service identity. Duplicating the IP
address in both a service JWT claim and the request payload is not recommended
for the current assessment implementation because it introduces redundant data
and mismatch handling without materially improving the trust model.

## 13. Human Decisions Approved Before Implementation

After reviewing the impact analysis, the following decisions were made by the
human reviewer before implementation.

### 1. Eligible source state for reopening

Only milestones in the `Completed` state can be reopened.

Attempts to reopen milestones in `Planned` or `InProgress` states must be
rejected.

Milestones in other terminal states, such as `Cancelled` if such states exist
in the implementation, must also be rejected unless future product
requirements explicitly define reopen behavior for them.

### 2. Target state after reopening

Reopening a completed milestone changes its status from:

`Completed` → `InProgress`

This allows a milestone to resume active work using the existing lifecycle
model without introducing an additional status.

### 3. Repeat reopening behavior

A milestone may be reopened again after it has subsequently been completed.

For example:

`InProgress` → `Completed` → `InProgress` → `Completed` → `InProgress`

Each successful reopen operation must create its own lifecycle event and audit
entry.

### 4. API design

Reopening will use a dedicated command endpoint:

`POST /api/milestones/{id}/reopen`

The request should include only information required by the existing concurrency
design, such as the concurrency token.

The client must not provide:

- OrganizationId
- ActorUserId
- ActorIpAddress
- Arbitrary target status

### 5. Notification recipients

Notifications will be sent to all distinct members of the milestone's
project team using the existing tenant-safe recipient selection approach.

Duplicate team membership data must not result in duplicate notifications for
the same lifecycle event.

### 6. Actor notification behavior

The actor will receive a notification if the actor is also a relevant member of
the milestone's project team.

No special self-notification suppression will be introduced for this
assessment.

### 7. Actor IP address storage

Actor IP address will be stored as a nullable audit field.

Existing audit records will remain compatible and will have a null IP address
where historical IP information is unavailable.

Both IPv4 and IPv6 addresses must be supported.

### 8. Actor IP address trust boundary

The Project API will resolve the actor IP address from trusted server-side
request context.

Clients must not be allowed to provide an authoritative ActorIpAddress value in
request bodies, query parameters, or arbitrary headers.

Forwarded headers must only be trusted when reverse proxy infrastructure has
been explicitly configured as trusted.

### 9. Actor IP address propagation

Actor IP address will be included once in the authenticated lifecycle event
contract sent from the Project API to the Notification & Audit Service.

The Notification & Audit Service will accept the value only from the
authenticated Project API service identity.

The IP address will not be unnecessarily duplicated in both a service JWT claim
and the lifecycle event payload.

### 10. IP address retention

A production IP retention and deletion policy is outside the scope of this
assessment.

The implementation will store the IP address as part of the audit record, while
the privacy and retention policy should be defined before a production
deployment.

### 11. Integration consistency

The existing synchronous lifecycle integration behavior will be preserved for
this assessment.

The implementation will continue to document the known consistency trade-off
between Project API persistence and Notification & Audit Service event
publication.

Production-grade outbox, durable retry, and asynchronous messaging mechanisms
remain outside the current scope.