# TaskBridge API

TaskBridge is a B2B SaaS platform supporting project 
collaboration, 
notifications, and immutable audit logging.

## Technology Stack

- C#
- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- xUnit
- Github Copilot

The Project API includes tenant-isolated milestone creation, status updates,
reopening of completed milestones, and deletion. Reopening changes
`Completed` to `InProgress` and requires the current concurrency token.
Successful milestone lifecycle operations publish typed, idempotent events
synchronously to the Notification & Audit Service. Team members are selected
from the tenant-safe project team membership relationship; audit and
notification dispatch failures are reported rather than swallowed.

The reopen event captures a nullable, normalized actor IP resolved from the
server request connection. Client request data and untrusted forwarded headers
cannot set the audit IP. The notification database requires an additive,
nullable `ActorIpAddress` column before deploying the updated notification
service; this repository has no existing EF migration artifacts.
