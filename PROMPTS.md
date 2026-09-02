# GitHub Copilot Prompt Log

## Prompt 1 — Initial Project Service Generation

### Exact Prompt

Generate a Project model and a Project service with create, update status, get by team, and delete functions. Use a database.

### Copilot Feature

GitHub Copilot Chat — Agent Mode

### Prompting Technique

Specificity

### Rationale

The prompt explicitly defined the main artifacts to generate—a Project model and
Project service—and specified the required operations: create, update status, get
by team, and delete, along with a database requirement. Implementation details
were intentionally left unspecified to simulate a low-effort AI-generated
implementation that would later be reviewed and remediated.

### Result

GitHub Copilot generated a broader implementation than explicitly requested,
including an ASP.NET Core API project, controller, Entity Framework Core DbContext,
Project model, Project service, test project, and solution structure.

### Post-Generation Corrections

None at this stage. The generated output was preserved without modification for
subsequent code review.

## Prompt 2 — AI-Assisted Code Review

### Exact Prompt

Act as a senior software engineer reviewing this AI-generated Project Service for a multi-tenant B2B SaaS application.

Review the Project model, ProjectService, ProjectsController, DbContext, and Program.cs.

Identify security, multi-tenant isolation, authorization, architecture, validation, error handling, performance, and coding-standard issues.

For each issue, provide:
1. The file and code location
2. Severity
3. Why it is a problem
4. Its impact in a multi-tenant B2B SaaS environment
5. A recommended fix

Do not modify any code. Review only.

### Copilot Feature

GitHub Copilot Chat — Ask Mode

### Prompting Technique

Role-based + Specificity + Constraint

### Rationale

A role-based approach was used to ask Copilot to review the code from the
perspective of a senior software engineer. Specific review categories were
provided to focus the analysis on the requirements of a multi-tenant B2B SaaS
application. A constraint was included to prevent Copilot from modifying the
initial AI-generated code because the original output needed to be preserved
as evidence before remediation.

### Result

Copilot identified nine primary issues, including missing authentication and
authorization, missing tenant isolation, trusting client-supplied tenant
identifiers, insufficient validation, inconsistent error handling, hard
deletion without an audit trail, missing pagination, indexing concerns, and
direct Entity Framework access from the service layer.

### Post-Generation Corrections

No code modifications were made at this stage. The output was preserved for
comparison with human review before remediation.

## Prompt 3 — Remediation Architecture Planning

### Exact Prompt

Act as a senior .NET solution architect.

Based on the current TaskBridge Project Service implementation and the issues documented in REVIEW.md, propose a remediation plan before making any code changes.

The intended repository structure is:

taskbridge-api/
├── src/
│   ├── projects/
│   └── notifications/
└── tests/

GitHub Copilot initially generated TaskBridge.Api and TaskBridge.Tests at the repository root, leaving the intended src/projects and src/notifications folders unused.

The remediation plan should address both the project structure and the following concerns:

1. Aligning the generated implementation with the intended src/projects and src/notifications structure.
2. Authentication and authorization.
3. Tenant isolation using a trusted tenant context.
4. Preventing clients from controlling OrganizationId.
5. Request and response DTOs.
6. Repository and service separation.
7. Validation.
8. Centralized exception handling.
9. Pagination.
10. Optimistic concurrency.
11. Audit integration for important project lifecycle events.

Do not modify any files.

For each proposed change, identify:
- Files or folders to create, move, or modify.
- The responsibility of each component.
- Which REVIEW.md findings the change addresses.
- Dependencies or implementation considerations.

Provide the changes in a recommended implementation order.

### Copilot Feature

GitHub Copilot Chat — Ask Mode

### Prompting Technique

Role-based + Decomposition + Constraint

### Rationale

A role-based prompt was used to ask Copilot to act as a senior .NET solution
architect. The remediation work was decomposed into specific architectural and
security concerns to ensure that each review finding was considered
systematically. Copilot was explicitly constrained not to modify files because
the remediation design needed to be reviewed before implementation.

### Result

Copilot proposed a comprehensive remediation plan covering repository structure,
authentication and authorization, trusted tenant context, DTO contracts,
repository and service separation, validation, centralized exception handling,
tenant isolation, pagination, optimistic concurrency, audit integration, and
security testing.

The plan recommended reorganizing the root-level generated projects into the
intended `src/projects`, `src/notifications`, and `tests` repository boundaries.

During human review of the remediation plan, the proposed architecture was
simplified. Copilot suggested separate API, Application, Domain, and
Infrastructure projects for both the Project and Notification services.
Although technically valid, this was considered unnecessarily complex for the
scope of the assignment.

The selected approach will retain clear Project and Notification service
boundaries while using a simpler project structure with Controllers, Contracts,
Models, Repositories, Services, Data, and Middleware organized within the
relevant service project.

## Prompt 4 — Remediation Architecture Planning

### Exact Prompt

Act as a senior .NET engineer.

Reorganize the existing TaskBridge solution to align with the intended repository structure.

Current relevant structure:

taskbridge-api/
├── TaskBridge.Api/
├── TaskBridge.Tests/
├── TaskBridge.sln
├── src/
│   ├── projects/
│   └── notifications/
└── tests/

Target structure:

taskbridge-api/
├── src/
│   ├── projects/
│   │   └── TaskBridge.Api/
│   └── notifications/
│
├── tests/
│   └── TaskBridge.Tests/
│
├── TaskBridge.sln
├── PROMPTS.md
├── REVIEW.md
└── README.md

Move the existing Project API project into `src/projects/TaskBridge.Api`.

Move the existing test project into `tests/TaskBridge.Tests`.

Update the solution and any project references required after the move.

Constraints:

1. Do not change business logic.
2. Do not refactor existing classes.
3. Do not change namespaces unless required for the project to build after the move.
4. Do not implement any REVIEW.md remediation items yet.
5. Do not create the Notification service yet.
6. Do not delete any existing implementation files.
7. Preserve the existing code behaviour.

After completing the changes:

1. Show the final relevant repository structure.
2. List every file or project moved or modified.
3. Explain any solution or project-reference changes made.
4. Build the solution and report the result.

### Copilot Feature

GitHub Copilot Chat — Agent Mode

### Prompting Technique

Role-based + Decomposition + Constraint

### Rationale

A role-based prompt was used to ask Copilot to act as a senior .NET engineer.
The requested structural changes were specified explicitly to reduce ambiguity.
Strict constraints were included to prevent Copilot from changing business logic,
refactoring existing classes, implementing review findings, or creating the
Notification service prematurely.

Agent Mode was selected because the task required moving project folders and
updating solution and project references.


### Result

Copilot moved the existing API project into `src/projects/TaskBridge.Api` and
moved the existing test project into `tests/TaskBridge.Tests`.

Copilot updated `TaskBridge.sln` to use the new project paths and restored the
test build configuration mappings. `TaskBridge.Tests.csproj` was updated so that
its project reference points to the API project at its new location.

No business logic, namespaces, classes, remediation items, or Notification
service implementation were changed.

Validation succeeded. The solution built successfully and all tests passed:
5 passed and 0 failed.

The repository structure is now aligned with the intended `src/projects`,
`src/notifications`, and `tests` boundaries.

## Prompt 5 — Authentication, Authorization, and Trusted Tenant Context

### Exact Prompt

Act as a senior .NET security engineer.

Implement the security foundation for the TaskBridge Project API.

The solution has already been reorganized. The Project API is located at:

src/projects/TaskBridge.Api

Review the current implementation and REVIEW.md before making changes.

Implement:

1. Authentication for the API.
2. Authorization for Project API endpoints.
3. A trusted tenant context abstraction that provides the current authenticated
   user's OrganizationId.
4. Dependency injection registration for the authentication and tenant context
   components.

For the current assignment implementation, use JWT Bearer authentication.

The tenant context must obtain OrganizationId from an authenticated JWT claim.
Do not obtain OrganizationId from request body parameters, query parameters,
route values, or other client-controlled input.

Create clear abstractions where appropriate, for example:

- `ITenantContext`
- `TenantContext`

Requirements:

1. Project API endpoints must require authenticated users.
2. The tenant context must only expose tenant information after authentication.
3. If the authenticated user does not contain a valid OrganizationId claim,
   tenant access must fail safely.
4. Use configuration for JWT settings and do not hard-code secrets in source code.
5. Register all required services through dependency injection.
6. Follow the existing repository structure and keep security-related code
   organized appropriately within the Project API.
7. Do not yet modify Project entity properties.
8. Do not yet implement DTOs.
9. Do not yet refactor repositories or ProjectService.
10. Do not yet implement Notification or Audit functionality.
11. Do not remove existing functionality unless required to enforce security.

After implementation:

1. List every file created or modified.
2. Explain how authentication works.
3. Explain how OrganizationId is obtained and why it is trusted.
4. Explain what happens when the OrganizationId claim is missing or invalid.
5. Build the solution.
6. Run the existing tests and report the result.

### Copilot Feature

GitHub Copilot Chat — Agent Mode

### Prompting Technique

Role-based + Specificity + Decomposition + Constraint

### Rationale

A role-based prompt was used to position Copilot as a senior .NET security
engineer. The security work was decomposed from later remediation tasks so that
authentication and trusted tenant identity could be implemented and validated
independently.

Specific requirements defined JWT Bearer authentication and required
OrganizationId to come only from a validated authenticated JWT claim.
Constraints prevented unrelated changes such as DTO implementation, repository
refactoring, Project entity changes, and Notification or Audit implementation.

Agent Mode was selected because the task required creating security-related
classes, modifying application configuration, registering services, and updating
the API pipeline.

### Result

Copilot implemented the security foundation for the Project API.

The implementation added JWT Bearer authentication and authorization and
protected the Project API endpoints using the `TenantAccess` authorization
policy.

A trusted tenant abstraction was created using `ITenantContext` and
`TenantContext`. The tenant context obtains OrganizationId from the authenticated
user's JWT claim rather than from request body data, query parameters, route
values, headers, or other client-controlled input.

The implementation created:

- `JwtOptions.cs`
- `ITenantContext.cs`
- `TenantContext.cs`
- `AuthenticationExtensions.cs`
- `TenantContextTests.cs`

The implementation modified:

- `Program.cs`
- `ProjectsController.cs`
- `TaskBridge.Api.csproj`
- `appsettings.json`
- `appsettings.Development.json`

The application pipeline now includes authentication before authorization.

Validation succeeded using fresh commands:

- `dotnet build TaskBridge.sln --nologo` — succeeded
- `dotnet test --nologo --verbosity minimal` — 7 succeeded, 0 failed, 0 skipped

A follow-up human review of the authorization policy is required to verify that
an OrganizationId claim containing an invalid GUID cannot pass authorization
merely because the claim exists.

## Prompt 5A — Validate OrganizationId Claim in TenantAccess Policy

### Exact Prompt

Act as a senior .NET security engineer.

Perform a focused security refinement of the existing TenantAccess authorization
policy.

During human review, the current policy was found to require that the user is
authenticated and that the OrganizationId claim exists, but it does not verify
that the OrganizationId claim value is a valid GUID.

The current policy is conceptually:

policy.RequireAuthenticatedUser()
      .RequireClaim(jwtOptions.OrganizationIdClaimType);

Update the authorization implementation so that TenantAccess is granted only
when all of the following are true:

1. The user is authenticated.
2. The configured OrganizationId claim exists.
3. The OrganizationId claim value is a valid Guid.

The implementation must fail closed. A missing, empty, or invalid OrganizationId
claim must not satisfy TenantAccess.

Requirements:

1. Reuse the configured OrganizationId claim type from JwtOptions.
2. Do not hard-code the claim name.
3. Keep the existing JWT authentication configuration unchanged unless required
   for this specific fix.
4. Do not modify Project business logic.
5. Do not implement DTOs, repository changes, validation changes, audit
   functionality, pagination, or concurrency changes.
6. Add or update tests covering:
   - a valid OrganizationId claim,
   - a missing OrganizationId claim,
   - an invalid OrganizationId claim.
7. Build the solution and run all tests.

After completing the change:

1. Explain exactly how the policy now validates the claim.
2. List every file modified.
3. Report the build and test results.

### Copilot Feature

GitHub Copilot Chat — Agent Mode

### Prompting Technique

Iterative Refinement + Specificity + Constraint + Role-Based Prompting

### Rationale

This prompt was created after human review identified a security gap in the
initial TenantAccess authorization policy. The original implementation verified
that the OrganizationId claim existed but did not verify that its value was a
valid GUID.

Iterative refinement was used to correct only the identified issue rather than
regenerating the complete security implementation.

Specific requirements defined exactly when authorization should succeed:
the user must be authenticated, the configured OrganizationId claim must exist,
and the claim value must be a valid GUID.

Constraints were included to prevent unrelated changes to Project business logic,
DTOs, repositories, validation, audit functionality, pagination, or concurrency.

A role-based prompt was used to focus Copilot on secure .NET authorization
practices.

Agent Mode was selected because the task required modifying the existing
authorization implementation and updating or adding tests.

### Result

The TenantAccess authorization policy was refined following human review.

The policy now requires:

1. An authenticated user.
2. The configured OrganizationId claim to exist.
3. The OrganizationId claim value to be successfully parsed as a Guid.

The implementation uses the configured claim type from JwtOptions and does not
hard-code the OrganizationId claim name.

The authorization policy now fails closed:

- Missing OrganizationId claim → access denied.
- Empty OrganizationId claim → access denied.
- Invalid GUID OrganizationId claim → access denied.
- Valid GUID OrganizationId claim → authorization can succeed.

The validation was added using an authorization assertion that retrieves the
configured OrganizationId claim and verifies it with Guid.TryParse.

This refinement addressed the security gap identified during human review of
Prompt #5.

### Human Review Finding

The initial TenantAccess policy required authentication and the presence of an
OrganizationId claim but did not validate that the claim value was a valid GUID.

Human review identified that an invalid claim value could potentially satisfy
RequireClaim because the claim existed, even though the TenantContext could not
successfully parse the value.

A focused iterative refinement was therefore performed to ensure invalid tenant
identifiers fail authorization before access is granted.

### Validation

Build and test results should be recorded here using the results reported by
Copilot after Prompt #5A.

- Build: succeeded
- Tests: all tests passed