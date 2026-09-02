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
and deletion. Successful milestone lifecycle operations publish typed,
idempotent events synchronously to the Notification & Audit Service. Team
members are selected from the tenant-safe project team membership relationship;
audit and notification dispatch failures are reported rather than swallowed.
