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