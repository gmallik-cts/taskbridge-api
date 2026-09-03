# Pull Request Description

## Summary

Implemented the TaskBridge Notification & Audit Service and integrated it with the
Project API milestone lifecycle.

The implementation provides immutable audit entries, tenant-isolated notification
records, lifecycle event ingestion, milestone lifecycle events, milestone reopening,
optimistic concurrency handling, and trusted actor information.

## What Was Built

### Project API

- JWT authentication and tenant-aware authorization.
- Trusted organization and actor context.
- Project DTOs and validation.
- Tenant-isolated project operations.
- Pagination.
- Optimistic concurrency using concurrency tokens.
- Milestone lifecycle endpoints.
- Dedicated milestone reopen operation.
- Synchronous lifecycle event publishing.

### Notification & Audit Service

- Independent ASP.NET Core service.
- Independent PostgreSQL persistence.
- Audit repository/service/controller layers.
- Immutable audit records.
- Stable source event ID/idempotency protection.
- Tenant-isolated audit queries.
- Persisted notifications.
- Duplicate recipient prevention.
- Notification read/mark-read support.
- Actor IP address persistence for milestone reopen events.

## AI Tool Disclosure

GitHub Copilot was used throughout the implementation for code generation, architectural
exploration, remediation, test generation, documentation assistance, and iterative fixes.

Copilot output was treated as a starting point rather than automatically accepted.
Generated code was reviewed against the specification, security requirements, tenant
isolation requirements, lifecycle rules, concurrency requirements, and database behavior.

Examples of human validation and correction included authentication and authorization,
tenant isolation, pagination, concurrency handling, repository boundaries, notification
recipient handling, lifecycle event behavior, actor IP trust considerations, and
migration requirements.

Estimated contribution:
- Approximately 70% AI-assisted/generated
- Approximately 30% human-authored, corrected, validated, or refactored

## Service Integration

The Project API is the lifecycle event producer.

The Notification & Audit Service is the event consumer.

Lifecycle events contain:

- Source event ID
- Event type
- Organization ID
- Actor user ID
- UTC timestamp
- Entity identity
- Before snapshot
- After snapshot
- Relevant notification recipients
- Trusted actor IP address where applicable

The integration currently uses synchronous HTTP.

## Testing

The final solution was validated with automated tests covering authentication context,
tenant isolation, validation, project behavior, concurrency, notification behavior,
audit behavior, lifecycle events, and milestone reopening.

Final validation result:

- 35 tests passed
- 0 failed
- 0 skipped

Both service databases were also validated using EF Core migrations.

## Known Gaps

The synchronous HTTP integration does not provide the durability guarantees of a
transactional outbox/message broker.

Production secret management and deployment-specific configuration are outside the
local assessment environment.

The current implementation uses database-backed persisted notifications rather than
an external push notification provider.

## Risk / Trade-off

The main architectural trade-off is synchronous lifecycle event delivery. It keeps the
assessment implementation simple and avoids unnecessary messaging infrastructure, but
a downstream service outage can cause lifecycle operations to report an integration
failure.

A future production implementation could use an outbox pattern and asynchronous message
delivery.

## Self-Review Checklist

- [x] Authentication and authorization reviewed
- [x] Tenant isolation reviewed
- [x] Client-controlled organization IDs removed from request contracts
- [x] Input validation reviewed
- [x] Error handling reviewed
- [x] Pagination implemented
- [x] Optimistic concurrency implemented
- [x] Audit records made immutable
- [x] Notification recipient duplication prevented
- [x] Lifecycle event integration reviewed
- [x] MILESTONE_REOPENED implemented
- [x] Actor IP address handling reviewed
- [x] EF Core migrations created and applied locally
- [x] Automated tests executed
- [x] Build verified
- [x] Secrets excluded from submission evidence

## Peer Review Simulation

| # | Location | Review Comment | Why |
|---|---|---|---|
| 1 | `MilestoneService.ReopenAsync` | Keep the concurrency-token check and return a clear conflict when a stale token is supplied. | Prevents one user's stale update from overwriting another user's lifecycle change. |
| 2 | `LifecycleEventPublisher` | Consider an outbox pattern for production deployments instead of relying solely on synchronous HTTP delivery. | Prevents downstream availability from creating a lifecycle-event delivery gap. |
| 3 | `TenantContext.ActorIpAddress` | Ensure forwarded client IP headers are trusted only when requests originate through configured trusted proxies. | An AI-generated implementation may capture an address correctly syntactically while still trusting spoofable forwarding information in a production deployment. |