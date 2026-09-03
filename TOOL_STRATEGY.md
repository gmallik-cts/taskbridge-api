# Copilot Tool Strategy

## Feature Usage Log

| # | Copilot Feature | Why This Feature | What Happened |
|---|---|---|---|
| 1 | Agent Mode | Used for multi-file implementation where the service boundary and related files had to be created together. | Generated the Notification & Audit Service structure and supporting models, repositories, services, controllers and configuration. |
| 2 | Ask Mode | Used to understand inherited Project Service behavior before changing it. | Helped identify architectural, security, validation and persistence concerns. |
| 3 | Edit Mode | Used for targeted remediation of existing Project API code. | Applied focused changes for DTOs, validation, tenant handling, pagination and concurrency. |
| 4 | Inline Chat | Used for focused changes to individual methods and implementation details. | Helped refine lifecycle and service logic without redesigning unrelated code. |
| 5 | Test generation | Used after implementation to increase automated coverage. | Generated and refined tests covering tenant isolation, concurrency, audit and notification behavior. |
| 6 | Documentation assistance | Used to structure specification, impact analysis and engineering documentation. | Helped organize implementation decisions and validation points while human review determined the final content. |

## Scenario Responses

### 1. Understanding a complex legacy service

I would use **Ask Mode** with the relevant files and `@workspace` context.

Ask Mode is appropriate because I first want explanation and dependency understanding rather
than immediate code changes. I would validate the explanation against the actual code before
making architectural decisions.

### 2. Consistent validation middleware across many handlers

I would use **Agent Mode** because the change spans multiple related files and handlers.

The prompt would define the validation rules and expected error contract, while the agent
could identify and update the affected handlers consistently.

### 3. Verifying JWT expiry and signature tampering

I would use **Ask Mode** to review the authentication configuration and then use generated
tests to verify expiry and invalid-signature behavior.

This separates explanation/review from implementation and gives an executable verification
of the security behavior.

### 4. Enforcing commit quality automatically

I would use **GitHub Actions** rather than Copilot itself for the enforcement mechanism.

Copilot can help create the workflow, but CI is the appropriate tool for automatically
blocking merges when linting, tests or coverage requirements fail.

### 5. Reviewing an AI-generated service for security vulnerabilities

I would use **Ask Mode** with a security-focused review prompt.

I would explicitly ask Copilot to review authentication, authorization, tenant isolation,
input validation, data access and information exposure, followed by manual engineering
validation.

### 6. Maintaining tenant isolation consistently

I would use **custom Copilot instructions** together with `@workspace` context.

The instructions establish tenant-isolation rules consistently across developers and
sessions, while workspace context allows Copilot to apply those rules to the existing
architecture.

## Limitations Encountered

### Limitation 1 — Authentication was incomplete for manual testing

The implementation correctly enforced JWT authentication, but there was no login/token
issuing endpoint available for manual testing.

I detected this when an unauthenticated milestone request returned 401. I validated the
authentication configuration and attempted to create a temporary test token using the
existing project configuration rather than adding a new application.

### Limitation 2 — Local EF tooling version mismatch

The project used EF Core 9 packages while the globally installed `dotnet-ef` version was
older.

I detected this while preparing migrations. Rather than changing the application package
versions, I installed a local EF tool version matching the project's EF Core version.

### Limitation 3 — Generated implementation required human validation

Some generated behavior did not fully satisfy the architectural and security requirements,
including tenant isolation, concurrency, pagination, audit integration and trusted actor
information.

These issues were identified through review and tests and were corrected before final
validation.

## Human Judgment

Copilot was used as an implementation accelerator rather than as an authority.

Security boundaries, tenant isolation, lifecycle semantics, migration decisions,
integration trade-offs and production risks were validated by reviewing the specification,
generated code, tests and runtime behavior.