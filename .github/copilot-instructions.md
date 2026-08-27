# TaskBridge GitHub Copilot Instructions

## 1. Technology Stack

- Language: C#
- Framework: .NET 8 / ASP.NET Core Web API
- ORM: Entity Framework Core
- Database: PostgreSQL
- Testing: xUnit
- API style: RESTful HTTP APIs
- Dependency Injection: ASP.NET Core built-in dependency injection

## 2. Architecture

Use a layered architecture for each service:

Controller/Route
    ↓
Service
    ↓
Repository
    ↓
Entity Framework Core
    ↓
Database

Responsibilities:

- Controllers handle HTTP concerns, request validation, response mapping,
  and authorization boundaries.
- Services contain business logic and must not contain direct database access.
- Repositories handle persistence and database queries.
- Models/entities represent persisted domain data.
- DTOs must be used for API request and response contracts where appropriate.

Do not place business logic in controllers.

Do not access the database directly from controllers.

Do not place SQL or Entity Framework queries in business services.

Keep the Project Service and Notification & Audit Service independently
structured while keeping their integration contract explicit.

## 3. Coding Standards

- Use PascalCase for classes, methods, properties, and public members.
- Use camelCase for local variables and parameters.
- Use meaningful and descriptive names.
- Enable and respect nullable reference types.
- Use explicit types where they improve readability and type safety.
- Use async/await for database and other I/O operations.
- Pass CancellationToken through asynchronous application and data-access
  operations where appropriate.
- Use dependency injection instead of manually creating dependencies.
- Keep methods focused on a single responsibility.
- Avoid unnecessary duplication.
- Prefer immutable data where practical.
- Public methods and classes should have XML documentation where appropriate.
- Use structured logging through Microsoft.Extensions.Logging.
- Do not use Console.WriteLine for application logging.

## 4. Security and Multi-Tenancy

TaskBridge is a multi-tenant B2B SaaS application.

- Never trust organisation IDs, user IDs, or other tenant identifiers supplied
  by clients.
- Tenant identity must come from the authenticated request context.
- Every tenant-scoped database query must enforce organisation isolation.
- A user from one organisation must never be able to access another
  organisation's projects, audit entries, or notifications.
- Validate and authorize every protected operation.
- Follow least-privilege principles.
- Do not expose internal database entities directly through APIs when a DTO
  is more appropriate.
- Do not expose secrets, credentials, tokens, or sensitive internal data.
- Never hardcode secrets, API keys, passwords, or connection credentials.
- Validate all external input.
- Return appropriate HTTP status codes without leaking internal exception
  details.
- Audit data must not be updateable or deletable after creation.
- Treat actor information and audit information as sensitive data.

## 5. Database and Data Access

- Use Entity Framework Core as the ORM.
- Do not use raw database drivers for normal application persistence.
- Avoid raw SQL unless there is a documented and justified requirement.
- Use parameterized queries when raw SQL is unavoidable.
- Use appropriate database indexes for frequently queried fields.
- Use decimal rather than floating-point types for monetary values.
- Keep database access inside repositories.
- Use asynchronous EF Core APIs.
- Handle database exceptions appropriately at the application boundary.
- Use migrations for schema changes.

## 6. Testing Expectations

All significant business logic must have automated tests.

Tests should cover:

- Happy paths
- Validation failures
- Error scenarios
- Boundary and edge cases
- Authorization failures
- Multi-tenant isolation
- Audit immutability
- Date and event-type filtering
- Notification dispatch behavior

Use xUnit for unit tests.

Tests should be deterministic and independent.

Do not rely on external services for unit tests unless explicitly required.

Every Copilot-generated test must be reviewed for correctness before
accepting it.

## 7. GitHub Copilot Usage

GitHub Copilot suggestions must be reviewed before being accepted.

When generating or modifying code:

- Follow these project instructions.
- Prefer small, understandable changes.
- Do not assume generated code is correct.
- Explain or investigate unfamiliar generated code before accepting it.
- Consider security, tenant isolation, validation, error handling,
  performance, and maintainability when reviewing generated code.