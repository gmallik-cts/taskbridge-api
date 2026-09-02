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

## Prompt 6 — DTOs, Validation, and Tenant Isolation

### Exact Prompt

Act as a senior .NET API engineer.

Implement the next security and API remediation step for the TaskBridge Project API.

Review the current implementation, REVIEW.md, and the existing authentication and
tenant context implementation before making changes.

The Project API is located at:

src/projects/TaskBridge.Api

Implement the following related improvements together:

1. Replace direct client binding to the Project entity with request and response DTOs.
2. Add appropriate validation for Project create and update requests.
3. Ensure OrganizationId cannot be supplied or controlled by the client.
4. Apply the trusted ITenantContext to all Project CRUD operations.
5. Ensure Project data is isolated by OrganizationId.

Requirements:

DTOs:

1. Create appropriate request DTOs for creating and updating a Project.
2. Create an appropriate response DTO for returning Project data.
3. Do not expose database entities directly through the API where avoidable.
4. OrganizationId must not be accepted from the client in create or update request DTOs.

Tenant isolation:

1. When creating a Project, OrganizationId must come from ITenantContext.
2. Getting a Project by ID must only return the Project if it belongs to the
   authenticated user's OrganizationId.
3. Updating a Project must only update a Project belonging to the authenticated
   user's OrganizationId.
4. Deleting a Project must only delete a Project belonging to the authenticated
   user's OrganizationId.
5. Get-by-team and other collection queries must return only Projects belonging
   to the authenticated user's OrganizationId.
6. A user from one Organization must never be able to access another
   Organization's Project by guessing or supplying its ID.

Validation:

1. Validate required fields.
2. Validate string lengths where appropriate based on the existing Project model.
3. Reject invalid input with appropriate validation responses.
4. Do not rely only on database errors for request validation.

Architecture constraints:

1. Keep the current project structure.
2. Reuse the existing ProjectService unless a small modification is necessary.
3. Do not introduce unnecessary architectural patterns.
4. Do not implement notifications or audit functionality.
5. Do not implement pagination in this step.
6. Do not implement optimistic concurrency in this step.
7. Do not remove existing API functionality unless necessary to enforce security.

Testing:

Add or update tests for:

1. Valid Project creation using OrganizationId from ITenantContext.
2. Client inability to control OrganizationId through create or update requests.
3. Tenant isolation when retrieving a Project.
4. Tenant isolation when updating a Project.
5. Tenant isolation when deleting a Project.
6. Validation failures.

After implementation:

1. List every file created or modified.
2. Explain how OrganizationId is assigned during Project creation.
3. Explain how tenant isolation is enforced for read, update, and delete operations.
4. Explain the DTOs and validation approach.
5. Build the solution.
6. Run all tests and report the results.

### Copilot Feature

GitHub Copilot Chat — Agent Mode

### Prompting Technique

Role-Based + Decomposition + Specificity + Constraint

### Rationale

Several related security findings were combined into one focused implementation
step to reduce unnecessary iterations.

DTOs, request validation, prevention of client-controlled OrganizationId, and
tenant isolation are closely related because they define the secure flow of
data from an API request to the database.

Specific requirements ensured that OrganizationId is obtained only from the
trusted ITenantContext and that all Project CRUD operations enforce the current
tenant boundary.

Constraints prevented unrelated changes such as notifications, audit
functionality, pagination, optimistic concurrency, and unnecessary architectural
patterns.

Agent Mode was selected because the implementation required coordinated changes
across DTOs, services, controllers, and tests.

### Result

Copilot implemented the Project API remediation.

Files changed:

- ProjectDtos.cs
- ProjectService.cs
- ProjectsController.cs
- ProjectServiceTests.cs

Key changes:

- Added create, update, and response DTOs.
- Excluded OrganizationId from request DTOs.
- Assigned OrganizationId exclusively from ITenantContext.
- Added tenant filtering to get, collection, update, and delete operations.
- Added GET /api/projects/{id}.
- Added full PUT update support.
- Added required-field, length, GUID, and enum validation.
- Added tenant isolation tests.
- Added validation tests.

### Validation

- Solution build: succeeded.
- Tests: 13 passed, 0 failed.

## Prompt 7 — Reliability, Validation, Pagination, and Optimistic Concurrency

### Exact Prompt

Act as a senior .NET API engineer.

Implement the next and final major reliability remediation step for the
TaskBridge Project API.

Review the current implementation, REVIEW.md, and the existing Project API
before making changes.

The Project API is located at:

src/projects/TaskBridge.Api

Implement the following improvements while keeping the implementation simple
and avoiding unnecessary architectural changes.

1. Centralized error handling
2. Business validation for tenant-scoped Team references
3. Pagination for Project collection queries
4. Optimistic concurrency for Project updates

Centralized error handling:

1. Introduce centralized exception handling.
2. Remove unnecessary duplicated controller-level exception handling where the
   centralized mechanism can handle the same failure.
3. Return consistent HTTP responses for:
   - validation failures,
   - not found resources,
   - unauthorized or forbidden access,
   - concurrency conflicts,
   - unexpected errors.
4. Do not expose internal exception details to API clients.

Business validation:

1. When a Project is created or updated with a TeamId, ensure the referenced
   Team belongs to the authenticated OrganizationId.
2. Do not allow a user to associate a Project with a Team from another tenant.
3. Continue using the trusted ITenantContext as the source of OrganizationId.

Pagination:

1. Add pagination to Project collection queries.
2. Accept page number and page size parameters.
3. Validate pagination parameters.
4. Enforce a reasonable maximum page size.
5. Return pagination metadata where appropriate.
6. Do not introduce unnecessary generic pagination frameworks.

Optimistic concurrency:

1. Add an appropriate EF Core optimistic concurrency mechanism to Project.
2. Ensure update requests contain the information required to detect stale
   updates.
3. Detect concurrency conflicts and return an appropriate HTTP conflict response.
4. Do not silently overwrite another user's newer update.
5. Keep the implementation simple and appropriate for the existing application.

Constraints:

1. Keep the existing project structure.
2. Do not implement notification functionality in this step.
3. Do not implement audit functionality in this step.
4. Do not introduce CQRS, MediatR, event buses, factories, or unnecessary
   architectural patterns.
5. Do not introduce a repository layer in this step.
6. Preserve the authentication, authorization, DTO, and tenant isolation work
   already implemented.
7. Do not remove existing functionality unless required to fix one of the issues
   described above.

Testing:

Add or update tests for:

1. Invalid or cross-tenant TeamId validation.
2. Pagination parameter validation.
3. Pagination behavior.
4. Optimistic concurrency conflicts.
5. Appropriate error responses where practical.

After implementation:

1. List every file created or modified.
2. Explain the centralized error handling approach.
3. Explain how Team tenant validation is enforced.
4. Explain the pagination design and maximum page size.
5. Explain the optimistic concurrency strategy.
6. Build the solution.
7. Run all tests and report the results.

### Copilot Feature

GitHub Copilot Chat — Agent Mode

### Prompting Technique

Role-Based + Decomposition + Specificity + Constraint

### Rationale

This prompt combined several related reliability and data integrity improvements
into one implementation step to reduce unnecessary prompt iterations.

Decomposition was used to separate the requirements into centralized error
handling, tenant-scoped Team validation, pagination, and optimistic concurrency
while allowing them to be implemented together.

Specificity defined the expected HTTP behavior, pagination constraints, tenant
boundary requirements, and concurrency conflict behavior.

Constraints prevented unrelated architectural expansion such as CQRS, MediatR,
event buses, factories, repository abstractions, notification functionality,
and audit functionality.

Agent Mode was selected because the implementation required coordinated changes
across the Project model, DTOs, service, controller, database context,
middleware, and tests.

### Result

Copilot implemented the final reliability remediation for the Project API.

Files modified:

- ProjectsController.cs
- TaskBridgeDbContext.cs
- Project.cs
- ProjectDtos.cs
- Program.cs
- ProjectService.cs
- ProjectServiceTests.cs

Files created:

- ExceptionHandlingMiddleware.cs
- ProjectExceptions.cs
- Team.cs

Key changes:

- Added centralized exception handling middleware.
- Added consistent ProblemDetails responses for validation, authentication,
  forbidden access, not-found resources, concurrency conflicts, and unexpected
  errors.
- Prevented internal exception details from being exposed to API clients.
- Removed duplicated controller-level exception handling where centralized
  middleware could handle the failure.
- Added validation to ensure Team references belong to the authenticated tenant.
- Missing Teams result in validation errors.
- Cross-tenant Team references result in 403 Forbidden.
- Added pagination to Project collection queries.
- Pagination defaults to pageNumber 1 and pageSize 20.
- Maximum pageSize is 100.
- Pagination metadata is returned with collection results.
- Added an EF Core Guid concurrency token to Project.
- Update requests must provide the concurrency token.
- The concurrency token is rotated after successful updates.
- Stale updates result in 409 Conflict responses.
- Added tests for cross-tenant Team validation, pagination validation and
  behavior, and concurrency conflicts.

### Validation

- Build: succeeded.
- Tests: 17 passed, 0 failed.

### Deployment / Migration Note

The implementation added a Team table and a Project concurrency column.

A database migration will be required before deploying this version against an
existing PostgreSQL database.

This migration requirement was identified as part of the implementation review
and should be documented in the final README and deployment instructions.

## Prompt 8 — Notification & Audit Service Specification

### Exact Prompt

Act as a senior software architect and .NET API designer.

Before implementing the Notification & Audit Service, create a specification
document named SPEC.md in the repository root.

Review the existing Project API implementation, the assignment requirements,
README.md, REVIEW.md, and PROMPTS.md before writing the specification.

Do not implement the Notification & Audit Service yet.
Do not modify the existing Project API in this step.
This step is documentation and design only.

The specification must describe a new Notification & Audit Service located at:

src/notifications/

The service will integrate with the existing Project API and support Project
milestone lifecycle events.

SPEC.md must include the following sections.

1. Purpose and Scope

Explain:

- The purpose of the Notification & Audit Service.
- Its responsibilities.
- What functionality belongs to the Project API versus the Notification &
  Audit Service.
- The scope of the initial implementation.

2. Architecture and Service Boundaries

Describe:

- The relationship between the Project API and Notification & Audit Service.
- The integration contract between the services.
- How Project lifecycle events will result in audit entries and notifications.
- How the design supports multi-tenant isolation.

Keep the design simple and appropriate for the assignment.

3. Data Models

Define the proposed data models and field types.

Audit entry must capture at minimum:

- Id
- EventType
- EntityType
- EntityId
- ActorUserId
- ActorOrganizationId
- PreviousStateSnapshot
- NewStateSnapshot
- Timestamp

Also define how audit entries are made immutable.

Notification must capture at minimum:

- Id
- RecipientUserId
- EventType
- ProjectId
- Message
- IsRead
- CreatedAt

Include any additional fields required for multi-tenant isolation if appropriate.

4. API Contracts

Define the contracts for:

- POST /audit
- GET /audit/{projectId}
- GET /notifications/{userId}
- PATCH /notifications/{id}/read

For each endpoint specify:

- Request data
- Response data
- Authentication and authorization expectations
- Tenant isolation requirements
- Validation behavior
- Expected error responses.

5. Integration with Project Service

Describe how the Project API will communicate lifecycle events to the Notification
& Audit Service.

The initial milestone events must support:

- Project or milestone creation where applicable
- Milestone status update
- Milestone deletion

For each event, describe what information must be provided to create:

- An audit entry
- Notifications for relevant team members

Do not introduce Kafka, RabbitMQ, Azure Service Bus, or other infrastructure
unless it is required by the assignment.

Choose the simplest reasonable integration approach for this repository and
explain the trade-off.

6. Multi-Tenant Security

Describe:

- How OrganizationId is obtained.
- How tenant boundaries are enforced.
- How cross-organization access to audit logs and notifications is prevented.
- Why tenant identifiers must not be trusted when supplied directly by clients.

7. Immutability

Clearly explain:

- Why audit entries must be immutable.
- Which operations are allowed.
- Which operations must not exist.
- How the API and service design prevent modification or deletion of audit
  history.

8. Validation and Error Handling

Describe:

- Required validation.
- Invalid identifier handling.
- Date range validation.
- Event type validation.
- Not-found behavior.
- Forbidden cross-tenant access.
- Consistent error response expectations.

9. Testing Requirements

Explicitly include at least these required scenarios:

1. Notifications are dispatched to all relevant team members.
2. An audit entry is created when a milestone is updated.
3. An audit entry cannot be deleted or overwritten.
4. Audit history is filtered correctly by date range.
5. Audit history is filtered correctly by event type.
6. A cross-organization user cannot access another organization's audit log.

Also identify any additional high-value tenant isolation or validation tests.

10. Implementation Sequence

Provide a short recommended implementation order that minimizes risk and avoids
breaking the existing Project API.

11. Copilot and Human Judgment

Include two subsections:

### Where Copilot Helped

Describe how GitHub Copilot was used for:

- Initial design assistance
- Contract and model scaffolding
- Test planning

### Where Human Judgment Was Applied

Describe decisions requiring human review, including:

- Tenant security boundaries
- Audit immutability
- Service integration simplicity
- Avoiding unnecessary infrastructure
- Validation of generated design assumptions

Constraints:

1. Create only SPEC.md in this step unless a minor documentation reference is
   necessary.
2. Do not create the Notification & Audit Service implementation yet.
3. Do not modify the Project API implementation.
4. Keep the architecture simple.
5. Do not introduce unnecessary microservice infrastructure.
6. Make the specification concrete enough that the next implementation prompt
   can be executed without guessing.

After creating SPEC.md:

1. Summarize the design decisions.
2. List every file modified.
3. Identify assumptions or decisions that should be reviewed before implementation.

### Copilot Feature

GitHub Copilot Chat — Agent Mode

### Prompting Technique

Role-Based + Decomposition + Specificity + Constraint

### Rationale

A specification was intentionally created before implementation because the
assignment requires the Notification & Audit Service design to be documented
before code is generated.

Role-based prompting asked Copilot to approach the task as a senior software
architect and .NET API designer.

Decomposition was used to break the specification into clear areas including
service boundaries, data models, API contracts, integration, multi-tenant
security, immutability, validation, testing, and implementation sequencing.

Specificity ensured that all mandatory assignment requirements were explicitly
included in the specification rather than leaving implementation details for
Copilot to infer.

Constraints prevented implementation work from starting before the specification
was completed and kept the design focused on the assignment without introducing
unnecessary infrastructure such as message brokers or event buses.

Agent Mode was selected because Copilot needed to inspect the existing
repository and create a new repository-level specification document.

### Result

Copilot created SPEC.md in the repository root.

The specification defines the planned Notification & Audit Service located under:

src/notifications/

It documents:

- The service purpose and scope.
- Architecture and service boundaries.
- Integration with the Project API.
- Audit and Notification data models.
- Required API contracts.
- Multi-tenant security requirements.
- Audit immutability requirements.
- Validation and error handling expectations.
- Required testing scenarios.
- Recommended implementation sequence.
- Areas where Copilot assisted.
- Areas requiring human judgment.

The implementation of the Notification & Audit Service was intentionally not
started during this step.

### Human Review

The specification was reviewed before proceeding to implementation to ensure:

- Required audit fields were included.
- Required notification fields were included.
- Organization boundaries were enforced.
- Audit entries were designed as immutable.
- Required API endpoints were included.
- Required test scenarios were planned.
- The integration approach did not introduce unnecessary infrastructure.

### Validation

- SPEC.md created successfully.
- No Project API implementation changes were made in this step.