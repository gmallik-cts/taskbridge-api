# Notification & Audit Service Specification

## 1. Purpose and Scope

TaskBridge is a multi-tenant B2B SaaS platform. The Notification & Audit Service records durable evidence of important project and milestone actions and delivers user-facing notifications to the people affected by those actions.

The service is responsible for:

- Accepting authenticated lifecycle events from the Project API.
- Creating immutable audit entries containing actor, tenant, entity, time, and state information.
- Creating notifications for the relevant team members.
- Allowing authorized users to read their tenant's project audit history.
- Allowing users to read and mark their own notifications as read.
- Enforcing tenant isolation independently of the Project API.

The Project API remains responsible for project and milestone ownership, validation of project and milestone business rules, team membership and permissions, persistence of project and milestone state, optimistic concurrency, and deciding when a lifecycle operation has successfully committed. It must not delegate project or milestone authorization to the Notification & Audit Service.

The Notification & Audit Service does not own project or milestone state, update projects or milestones, manage team membership, or provide general-purpose application logging. Operational logs remain diagnostic data and are separate from immutable audit history.

The initial implementation is intentionally small:

- One ASP.NET Core service under `src/notifications/`.
- Entity Framework Core with PostgreSQL.
- A repository and service layer behind controllers.
- Synchronous HTTP event delivery from the Project API to the notification service.
- In-process notification creation, represented as persisted database records rather than email, push, or queue infrastructure.
- Audit reads filtered by project, optional UTC date range, and optional event type.

The Project API now owns a minimal milestone entity and team-member recipient source so milestone events can be emitted reliably. This specification defines the contract implemented by that integration.

## 2. Architecture and Service Boundaries

The repository will use these top-level boundaries:

```text
src/
  projects/       Existing Project API and future milestone lifecycle owner
  notifications/  Notification & Audit Service
```

Each service follows the repository's simple layered structure:

```text
Controller / Route
        -> Service
        -> Repository
        -> Entity Framework Core
        -> PostgreSQL
```

The Project API is the event producer. After a project or milestone operation is successfully committed, it sends a lifecycle event to the Notification & Audit Service over an authenticated internal HTTP client. The Notification & Audit Service validates the event, verifies the trusted tenant context, writes an audit entry, and creates notifications for the supplied relevant recipients.

The integration contract is explicit and versionable. The event request includes an event type, entity identity, actor identity, organization identity, UTC timestamp, previous and new state snapshots, and the recipient user IDs. The notification service does not query the Project API or infer recipients from an untrusted project ID. Recipient selection remains a Project API/domain responsibility because it owns team and milestone membership.

The initial flow is:

1. An authenticated user calls the Project API.
2. The Project API obtains `OrganizationId` and `ActorUserId` from trusted authentication claims and validates the operation within that organization.
3. The Project API commits the project or milestone change.
4. The Project API posts the committed lifecycle event to the Notification & Audit Service using service authentication and the same organization boundary.
5. The Notification & Audit Service validates that the event tenant and actor are authorized by the integration identity, stores one immutable audit record, and creates one notification per distinct relevant recipient in that tenant.
6. The Project API treats a rejected event as an integration failure and logs it for retry/operational handling. The assignment does not require a message broker or a distributed transaction.

This synchronous approach is simple to implement and easy to test. Its trade-off is that a network failure can leave project state committed while its audit/notification event is pending. The implementation should use a stable event ID for idempotency and a bounded retry or explicit failure handling policy. A transactional outbox may be considered later if delivery guarantees become a requirement, but it is outside the initial scope.

All service read paths apply organization filtering in the repository query. Internal event ingestion must also authenticate the Project API and reject a caller that attempts to submit an organization different from the trusted integration context. The service must never use a client-supplied `OrganizationId` as proof of access.

## 3. Data Models

All identifiers are `Guid`. All persisted timestamps are UTC `DateTime` values or an equivalent PostgreSQL `timestamp with time zone` mapping. JSON snapshots are stored as PostgreSQL `jsonb` and exposed as JSON objects or serialized JSON according to the chosen EF Core mapping.

### AuditEntry

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Required primary key; generated by the service or supplied as a stable event ID for idempotent ingestion. |
| `EventType` | `string` or enum | Required; allow-listed values such as `ProjectCreated`, `ProjectUpdated`, `ProjectDeleted`, `MilestoneCreated`, `MilestoneStatusUpdated`, and `MilestoneDeleted`. |
| `EntityType` | `string` or enum | Required; initially `Project` or `Milestone`. |
| `EntityId` | `Guid` | Required and non-empty. |
| `ProjectId` | `Guid` | Required containing project identifier; allows project-scoped audit queries for both project and milestone events. |
| `MilestoneId` | `Guid?` | Set for milestone events; null for project-only events. |
| `ActorUserId` | `Guid` | Required and non-empty; taken from trusted actor identity. |
| `ActorOrganizationId` | `Guid` | Required and non-empty; must equal the trusted tenant context. |
| `PreviousStateSnapshot` | JSON object, nullable | Null for creation events; required for updates and deletions where prior state is available. |
| `NewStateSnapshot` | JSON object, nullable | Required for creation and updates; null for deletion events. |
| `Timestamp` | `DateTime` | Required UTC event time; reject non-UTC or out-of-range values. |
| `CreatedAt` | `DateTime` | Required UTC persistence time; set by the service. |
| `SourceEventId` | `Guid` | Required unique integration event ID; duplicate submissions return the original result without duplicate notifications. |

An audit entry is immutable. The repository exposes create and read operations only. There are no update or delete routes. EF Core should use a separate write path that never attaches audit entities for modification, and the database account used by the service should have insert/select permissions for the audit table but no update/delete permissions. A database trigger or equivalent constraint can provide an additional defense in depth. `SourceEventId` has a unique index.

### Notification

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Required primary key. |
| `RecipientUserId` | `Guid` | Required and non-empty. |
| `OrganizationId` | `Guid` | Required tenant key; derived from the trusted event tenant. |
| `EventType` | `string` or enum | Required and allow-listed. |
| `ProjectId` | `Guid` | Required for the initial project/milestone scope; for milestone events this is the containing project. |
| `MilestoneId` | `Guid?` | Set for milestone events; null for project-only events. |
| `AuditEntryId` | `Guid` | Required foreign-key/reference to the audit event that caused the notification. |
| `Message` | `string` | Required, bounded in length, and generated or validated by the service rather than treated as executable content. |
| `IsRead` | `bool` | Required; defaults to `false`. |
| `CreatedAt` | `DateTime` | Required UTC persistence time. |
| `ReadAt` | `DateTime?` | Set when the notification is marked read. |

A uniqueness rule on `(AuditEntryId, RecipientUserId)` prevents duplicate notifications when the same event is retried. Indexes should support `(OrganizationId, ProjectId)`, `(OrganizationId, RecipientUserId, CreatedAt)`, and audit filtering by `(ActorOrganizationId, EntityId, Timestamp, EventType)`.

## 4. API Contracts

All routes require HTTPS and bearer authentication. The service uses the same trusted organization claim convention as the Project API, represented by `ITenantContext` in the .NET implementation. Error bodies use one consistent problem-details shape, for example:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more request values are invalid",
  "errors": { "eventType": ["Unsupported event type"] }
}
```

The exact `type` URI may be local, but `title`, `status`, `detail`, and field errors must be consistent.

### POST /audit

This is the internal event-ingestion endpoint. It is not a general client endpoint. It requires an authenticated Project API service identity and a valid tenant context; the integration policy must authorize event submission.

Request body:

```json
{
  "sourceEventId": "guid",
  "eventType": "MilestoneStatusUpdated",
  "entityType": "Milestone",
  "entityId": "guid",
  "projectId": "guid",
  "milestoneId": "guid",
  "actorUserId": "guid",
  "organizationId": "guid",
  "timestamp": "2026-09-02T12:00:00Z",
  "previousStateSnapshot": { "status": "InProgress" },
  "newStateSnapshot": { "status": "Completed" },
  "recipients": ["guid", "guid"]
}
```

`organizationId` is present for contract correlation but is accepted only when it equals the organization resolved from the authenticated integration context. `recipients` must be non-empty for notification-producing events, contain valid distinct user IDs, and be limited to the relevant team/milestone members supplied by the Project API. The service generates the audit ID, notification messages, and persistence timestamps.

A successful new event returns `201 Created` with the audit entry summary and notification count. A repeated `sourceEventId` returns `200 OK` with the existing audit entry and does not create duplicates. The response does not expose internal persistence details beyond the contract fields.

Expected errors: `400 Bad Request` for malformed IDs, missing fields, invalid snapshots, unsupported event/entity combinations, invalid timestamp, or invalid recipients; `401 Unauthorized` for missing/invalid authentication; `403 Forbidden` for an unauthorized integration identity or tenant mismatch; `409 Conflict` for a conflicting reuse of `sourceEventId`; `500 Internal Server Error` for an unexpected failure.

### GET /audit/{projectId}

Returns the authenticated user's organization's audit entries for the specified project, ordered newest first. Query parameters:

- `from`: optional inclusive UTC ISO-8601 timestamp.
- `to`: optional exclusive UTC ISO-8601 timestamp.
- `eventType`: optional allow-listed event type.
- `pageNumber`: optional positive integer, default `1`.
- `pageSize`: optional positive integer, default `20`, maximum `100`.

Response `200 OK`:

```json
{
  "items": [
    {
      "id": "guid",
      "eventType": "MilestoneStatusUpdated",
      "entityType": "Milestone",
      "entityId": "guid",
      "actorUserId": "guid",
      "actorOrganizationId": "guid",
      "previousStateSnapshot": { "status": "InProgress" },
      "newStateSnapshot": { "status": "Completed" },
      "timestamp": "2026-09-02T12:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

Authorization permits only an authenticated user with a valid tenant claim and the appropriate audit-read permission. The repository query must include both `ProjectId`/entity relationship and the trusted `OrganizationId`; a project from another organization is not disclosed. Return `400` for an invalid project ID, query values, date range, event type, or pagination; `401` for missing authentication; `403` for insufficient permission; `404` if the project is not visible in the tenant (the implementation may use `404` to avoid resource enumeration); and `500` for unexpected failures.

### GET /notifications/{userId}

Returns notifications for the authenticated user in the authenticated organization, newest first. In the initial scope, `{userId}` must equal the authenticated `ActorUserId` unless an explicitly authorized administrative notification-read permission is introduced.

Query parameters may include `isRead`, `pageNumber`, and `pageSize` with the same positive and maximum bounds as audit pagination. A successful response is `200 OK` with notification fields `id`, `recipientUserId`, `eventType`, `projectId`, optional `milestoneId`, `message`, `isRead`, `createdAt`, and `readAt`, plus pagination metadata.

The service filters by both `RecipientUserId` and trusted `OrganizationId`. A valid ID belonging to another user is `403 Forbidden` rather than a cross-tenant data lookup. An invalid or empty route ID is `400`; missing authentication is `401`; a nonexistent visible user may return `404` where user existence is part of the contract, but must not reveal another tenant's user; invalid filters return `400`; unexpected failures return `500`.

### PATCH /notifications/{id}/read

Marks one notification as read. The request body is empty, or may contain `{ "isRead": true }`; the service only supports the transition to read in the initial scope. The response is `200 OK` with the updated notification, or `204 No Content` if the implementation chooses a command-only response.

The caller must be authenticated and must own the notification as the same `RecipientUserId` in the trusted organization. The update query must include `Id`, `RecipientUserId`, and `OrganizationId`. Return `400` for an invalid ID or unsupported body/value, `401` for missing authentication, `403` for a notification owned by another user or organization, `404` if not found in the caller's visible scope, and `500` for unexpected failures. Marking an already-read notification is idempotent and returns success.

## 5. Integration with Project Service

The Project API must publish an event only after the corresponding project or milestone transaction has committed. The event contract must include:

- A unique `SourceEventId` generated for the lifecycle action.
- `EventType`, `EntityType`, `EntityId`, containing `ProjectId`, and optional `MilestoneId`.
- The actor user ID and organization ID from the authenticated context, never from an untrusted request body.
- The committed event timestamp in UTC.
- A previous state snapshot and new state snapshot sufficient to explain the change.
- The relevant team member user IDs, excluding duplicates and normally including the actor only if product policy requires it.

Initial events:

| Event | Required audit information | Notification recipients and content |
|---|---|---|
| `ProjectCreated` | Project ID, team ID/name as appropriate, actor, organization, null previous snapshot, new project snapshot. | Relevant team members; message identifies the project and creator. |
| `MilestoneCreated` | Milestone ID, containing project ID, actor, organization, null previous snapshot, new milestone snapshot. | Members of the project/team and any milestone assignees; message identifies the milestone and project. |
| `MilestoneStatusUpdated` | Milestone ID, project ID, actor, organization, old status and new status, plus any required state fields. | Relevant team members and assignees; message identifies the status transition. |
| `MilestoneDeleted` | Milestone ID, project ID, actor, organization, prior milestone snapshot, null new snapshot. | Relevant team members and assignees known before deletion; message identifies the deleted milestone. |

Project status update and project deletion can use the same contract when the Project API exposes them as in-scope lifecycle events. A project deletion event must be sent before the project data becomes unavailable for recipient lookup, or must include all recipients in the event payload.

The simple synchronous HTTP client avoids adding Kafka, RabbitMQ, Azure Service Bus, or another infrastructure dependency. The Project API should configure a typed `HttpClient`, a service-to-service credential, a request timeout, and structured failure logging. The Notification service should be idempotent by `SourceEventId`; the Project API may retry transient failures. A future durable outbox can improve reliability without changing the event payload contract.

## 6. Multi-Tenant Security

`OrganizationId` is obtained from the authenticated request context. In the current Project API this is represented by a validated JWT organization claim and `ITenantContext`; the Notification service must implement the same trusted-claim pattern or a trusted service-token equivalent. The user ID comes from the authenticated subject/user claim, not from a client-provided actor field.

Every tenant-scoped repository query includes the trusted organization predicate. This applies to audit creation, audit reads, notification reads, notification updates, duplicate detection, and any lookup used to validate recipient or project relationships. The service should use a tenant-aware authorization policy and reject requests with a missing, invalid, or empty organization claim.

For internal event ingestion, the service authenticates the Project API separately from the end user and verifies that the token is allowed to submit events. The submitted organization and actor are checked against trusted claims or a signed service contract. The event sender is responsible for proving that recipients belong to the affected project/team; the notification service must not widen the recipient set based only on client input.

Cross-organization access must fail closed. A query for another organization's project audit history returns no data and normally `404` to avoid resource enumeration. Attempts to mark another organization's notification read are denied. Tenant identifiers supplied directly by clients cannot be trusted because a caller could substitute another organization's GUID and otherwise bypass a filter or create data under the wrong tenant.

## 7. Immutability

Audit entries are evidence of what happened. Allowing them to be edited or deleted would destroy the historical record, weaken accountability, and make incident investigation unreliable.

Allowed operations are creation through the authenticated event-ingestion contract and read-only retrieval through authorized audit queries. Replaying the same event is allowed only as an idempotent no-op that returns the existing entry when all immutable identifying fields match.

The service must not expose `PUT`, `PATCH`, or `DELETE` endpoints for audit entries. It must not provide repository methods that update or remove audit entries, cascade-delete audit rows when projects are deleted, or allow snapshots, actor, tenant, event type, or timestamp to be changed after insertion. Database permissions, unique constraints, and optionally an insert-only trigger provide defense in depth beyond controller behavior. Corrections are represented by a new compensating audit event, never by rewriting history.

## 8. Validation and Error Handling

The service validates at the API boundary and again in the service layer where the rule protects data integrity. Required checks include:

- Request body presence and required fields.
- Non-empty, well-formed GUIDs for all identifiers.
- Maximum lengths for event types, messages, and serialized snapshots.
- Allow-listed event types and valid event/entity combinations.
- Required snapshot rules: no previous snapshot for creation, prior state for deletion, and both states for updates.
- UTC timestamps and reasonable clock/skew bounds defined by configuration.
- `from < to`; valid ISO-8601 date values; bounded date intervals to prevent unbounded expensive queries.
- Positive page number and page size no greater than `100`.
- Notification recipient count limits, unique IDs, and non-empty recipients for events that notify users.
- Notification ownership and organization predicates on every read-status update.
- Stable idempotency behavior for duplicate `SourceEventId` values.

Use `400 Bad Request` for malformed identifiers, invalid query/body values, invalid date ranges, unsupported event types, and validation failures. Use `401 Unauthorized` when authentication is absent or invalid. Use `403 Forbidden` when the caller is authenticated but lacks the required permission, attempts another user's notification, or presents an organization that does not match the trusted context. Use `404 Not Found` for a resource that is absent from the caller's tenant scope, without confirming that it exists elsewhere. Use `409 Conflict` for conflicting idempotency reuse or a persistence conflict that cannot safely be treated as a retry. Unexpected errors are logged with structured context and returned as `500 Internal Server Error` without stack traces, tokens, snapshots containing secrets, or database details.

## 9. Testing Requirements

The implementation must include deterministic xUnit tests for at least these scenarios:

1. Notifications are dispatched to all relevant team members, with duplicate recipients producing only one notification each.
2. An audit entry is created when a milestone is updated, including the previous and new status snapshots.
3. An audit entry cannot be deleted or overwritten through the service or API.
4. Audit history is filtered correctly by an inclusive `from` and exclusive `to` date range.
5. Audit history is filtered correctly by event type.
6. A cross-organization user cannot access another organization's audit log.

Additional high-value tests:

- Missing or invalid organization claim is rejected.
- A client-supplied organization ID cannot change the trusted tenant.
- A cross-organization notification cannot be read or marked read.
- A user cannot read another user's notifications without an explicit administrative permission.
- Invalid GUIDs, unsupported event/entity combinations, empty recipients, oversized payloads, and invalid pagination return consistent `400` responses.
- `from >= to` and dates outside the configured range are rejected.
- Duplicate event delivery is idempotent and does not duplicate the audit entry or notifications.
- A conflicting reuse of `SourceEventId` returns `409`.
- Creation and deletion snapshot rules are enforced.
- Notification read marking is idempotent.
- A milestone deletion still notifies recipients included before the milestone is removed.
- Audit rows remain available after the related project or milestone is deleted.
- Repository queries cannot return records from another organization even when IDs are known.

## 10. Implementation Sequence

1. Confirm the milestone domain fields, lifecycle operations, team membership source, authenticated user claim, and event recipient rules in the Project API.
2. Create the `src/notifications/` service project with authentication, tenant context, centralized problem-details error handling, PostgreSQL EF Core configuration, and migrations.
3. Add immutable `AuditEntry` and `Notification` models, indexes, unique constraints, and repository interfaces. Verify that audit persistence has no update/delete path.
4. Implement the event-ingestion service and `POST /audit` with allow-listed events, snapshot validation, idempotency, and recipient notification creation.
5. Implement tenant-scoped audit and notification read services and the three read endpoints, including pagination and filters.
6. Implement `PATCH /notifications/{id}/read` with recipient and tenant predicates.
7. Add focused unit/integration tests for validation, tenant isolation, immutability, filtering, idempotency, and dispatch.
8. Add a typed Project API HTTP client and emit events only after successful lifecycle commits. Add retry/failure logging without changing existing Project API behavior until the contract and tests are reviewed.
9. Run the solution build and all tests, then review generated migrations, authorization policies, database permissions, and the event payload with a human reviewer.

This order keeps the existing Project API unchanged during this documentation step and makes the Notification service independently testable before integration is enabled.

## 11. Copilot and Human Judgment

### Where Copilot Helped

GitHub Copilot was used for initial design assistance by organizing the assignment requirements into service boundaries, data responsibilities, contracts, and a low-complexity implementation sequence. It assisted with contract and model scaffolding concepts, including audit snapshots, notification fields, tenant keys, pagination, and idempotent event ingestion. It also helped plan tests covering lifecycle events, immutable history, date/event filters, notification dispatch, validation, and cross-organization access.

### Where Human Judgment Was Applied

Human review is required for the security and operational decisions that generated design suggestions cannot safely settle on their own. In particular, the organization boundary must be derived from trusted authentication context and applied to every query; audit records must be insert-only and must not be deleted as a side effect of project deletion; and the Project API remains authoritative for project state, membership, authorization, and recipient selection.

Human judgment also selected synchronous HTTP with a stable event ID instead of introducing Kafka, RabbitMQ, Azure Service Bus, or another unnecessary infrastructure dependency. The design explicitly accepts the initial delivery trade-off and leaves an outbox as a future option. Before implementation, a human reviewer must validate the generated assumptions about milestone fields, user claims, team membership, event ordering, snapshot contents, retry behavior, database permissions, and the exact permissions granted to internal event ingestion.
