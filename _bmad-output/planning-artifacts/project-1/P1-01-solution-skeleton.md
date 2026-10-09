# P1-01 — Solution skeleton

## Summary

Create the base .NET solution structure for Project 1, defining project boundaries, layer references, and the initial build, test, and code-quality configuration.

## Type

Story / initialization ticket

## Status

Approved

## Dependencies

- ARCHITECTURE.md
- PROJECT-1-ARCHITECTURE.md
- AGENTS.md

## Objective

Establish the solution and folder structure needed for the project to grow without breaking dependency rules or introducing unsupported technologies.

## Scope

### Included

- .NET solution creation
- Domain, Application, Infrastructure, and API projects
- test project(s) according to the architecture
- layer reference configuration
- nullable enablement
- analyzers and style rules
- central package management
- editorconfig and build properties
- baseline for local/CI compilation

### Excluded

- business logic for the use case
- real incident prompts
- AI provider implementation
- functional domain endpoints
- any incident-specific business rules

## Acceptance criteria

1. The solution builds successfully.
2. The dependency from Domain to Infrastructure is impossible in the project graph.
3. The API depends on Application and not on concrete infrastructure.
4. The test projects exist and are runnable.
5. The repository configures `Nullable` and code analysis rules.
6. Repository packages are managed centrally.
7. The solution is ready to continue with P1-02 without major structural changes.

## Work to do

- Create the solution at the repository root.
- Create the projects listed in the architecture.
- Configure correct references by layer.
- Add `Directory.Build.props` and `Directory.Packages.props` if they do not already exist.
- Define `.editorconfig` with style and test rules.
- Add quality-analysis thresholds and warnings as repository policy.
- Create the base test projects with xUnit and correct dependencies.
- Leave the structure ready for the next story without implementing real logic.

## Technical constraints

- No technology outside the defined stack should be introduced.
- No business classes or AI adapters should be created in this ticket.
- Keep the architecture clean and explicit from the start.

## Verification

- Run the repository build.
- Run the base project tests.
- Validate the reference structure in the solution graph.

## Risks / notes

- Structural changes at this point can affect the rest of the evolution.
- Do not add unnecessary infrastructure before the actual need is known.

## Definition of done

The delivery is complete when the repository structure is valid, the solution builds, and the test base is ready for P1-02.

## Changes made

- Created the .NET solution and project skeleton for Domain, Application, Infrastructure, API, and test projects.
- Configured layer references: API -> Application; Infrastructure -> Application + Domain; Application -> Domain.
- Added repository-level package management, analyzers, and editorconfig rules.
- Added smoke tests for the domain/application assemblies and the API root endpoint.
- Validated the repository build and test execution successfully.

## Code review

### Senior Developer Review (AI)

- The story scope is satisfied for the P1-01 skeleton target.
- Build quality gate tightened by treating warnings as errors in the repository configuration.
- Added architecture boundary smoke tests to prevent Domain/Application/API regressions.
- Removed placeholder scaffold files to keep the base clean and explicit.
- Verification: `dotnet test --nologo` passed with 6/6 tests successful.

## Implementation notes

- Reuse the structure defined by the shared architecture.
- Keep folder and project names clear.
- Ensure the separation of layers is respected from the beginning.
