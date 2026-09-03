# TaskBridge Architecture

TaskBridge is implemented as two independently deployable ASP.NET Core services.

The Project API owns project and milestone lifecycle state, authorization, tenant context,
and the primary project database.

The Notification & Audit Service owns immutable audit records and persisted user
notifications and maintains its own PostgreSQL database.

A lifecycle change is initiated through the Project API and, after the relevant state
operation succeeds, is represented as a lifecycle event containing the event type, entity
identity, tenant, actor, timestamp, and before/after snapshots.

The Project API publishes the lifecycle event to the Notification & Audit Service using
synchronous HTTP and a stable source event ID.

The Notification & Audit Service validates the trusted tenant and actor information,
persists the immutable audit entry, and creates notifications for distinct relevant team
members.

Both services independently enforce tenant isolation rather than trusting a client-supplied
organization identifier.

The Project API uses controller → service → repository/data-access responsibilities,
while the Notification service separates controllers, application services, repositories,
and EF Core persistence.

For the milestone reopen change, the Project API exposes a dedicated reopen endpoint and
requires the current concurrency token to prevent stale updates.

Audit entries are immutable and include the actor IP address for the approved
MILESTONE_REOPENED change.

Synchronous HTTP was deliberately selected to keep the implementation simple for the
assessment; an outbox/message broker could improve delivery reliability in a future
iteration.