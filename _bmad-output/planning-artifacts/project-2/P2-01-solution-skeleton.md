# P2-01 - Solution skeleton and project boundaries

- ID: P2-01
- Title: Solution skeleton and project boundaries
- Type: User Story

## Description
As the project's engineering team, we need a clear solution structure and project boundaries for Project 2 so that we can develop the Runbook Chatbot module without coupling it to Project 1 code or introducing unplanned dependencies.

## Objective
Create the minimum foundation for the RAG module: projects, references, shared configuration, conventions, and separation of responsibilities among Domain, Application, Infrastructure, and API.

## Technical Context
Project 2 must live in the current .NET solution and follow the Clean Architecture defined in `ARCHITECTURE.md` and `PROJECT-2-ARCHITECTURE.md`. The foundation must separate the domain, use cases, ports, infrastructure, and API without changing Project 1 or creating a premature reuse pattern.

## Scope
- Create the `DevOpsAI.Rag.Domain`, `DevOpsAI.Rag.Application`, `DevOpsAI.Rag.Infrastructure`, and `DevOpsAI.Rag.Api` projects in the solution.
- Configure references to follow `Api -> Application -> Domain` and `Infrastructure -> Application/Domain`.
- Keep Project 1 intact and free of cross-project references.
- Ensure that the API project is a minimal ASP.NET Core host with basic configuration.
- Prepare the foundation for xUnit/NSubstitute and module integration tests.
- Define the essential package set and repository formatting/analyzer configuration.

## Verifiable Acceptance Criteria
1. The solution includes all four Project 2 projects and registers them in `devops-ai-assistant.sln`.
2. Project 2 has no references to Project 1, and Domain or Application has no references to provider-specific SDKs.
3. The API project builds with minimal configuration and no business logic.
4. The Infrastructure project is ready to implement persistence and LLM/RAG ports, with no infrastructure dependencies mixed into Domain.
5. The module's unit/integration test projects have an initial structure and compile without external providers.

## Affected Components
- `src/DevOpsAI.Rag.Domain/`
- `src/DevOpsAI.Rag.Application/`
- `src/DevOpsAI.Rag.Infrastructure/`
- `src/DevOpsAI.Rag.Api/`
- `tests/DevOpsAI.Rag.UnitTests/`
- `tests/DevOpsAI.Rag.ApplicationTests/`
- `tests/DevOpsAI.Rag.IntegrationTests/`
- `devops-ai-assistant.sln`
- `Directory.Build.props` / `Directory.Packages.props` (only if the package structure requires changes)

## Dependencies
- Depends on approval of the project scope defined in `ARCHITECTURE.md` and `PROJECT-2-ARCHITECTURE.md`.
- Requires the repository's .NET 10 foundation and coding conventions.

## Expected Tests
- Run `dotnet build` on the solution.
- Run `dotnet test` for empty projects or projects with bootstrapping tests.
- Verify that no prohibited cross-project references exist.

## Definition of Done
- The module structure exists and builds.
- Layers are separated according to the agreed architecture.
- The API and Infrastructure are ready to receive real use cases without introducing business logic.
- Configuration and reference files are documented and consistent with the repository.

## Tasks / Subtasks

- [x] Create the four Project 2 production projects with the approved Clean Architecture references and add them to the solution.
- [x] Add a minimal, locally configured ASP.NET Core API host without business logic.
- [x] Add the three offline xUnit/NSubstitute test projects and tests for project boundaries and API startup.
- [x] Build, test, format, verify dependency boundaries, and update the story record.

## Dev Agent Record

### Implementation Plan

Create only the Project 2 project structure, references, minimal host configuration, and test scaffolding defined by P2-01. Reuse central packages and repository build/format settings; defer all RAG behavior and provider/persistence packages to later stories.

### Completion Notes

Created the four isolated Project 2 projects, registered their Clean Architecture references, and added all projects to the solution. The API is a minimal ASP.NET Core host with OpenAPI and a loopback-configured health endpoint. Added xUnit/NSubstitute test projects with project-boundary checks and an offline API smoke test. No persistence, RAG, provider SDK, or business logic was added; shared build/package configuration and Project 1 sources remain unchanged.

Validation used .NET SDK 10.0.404 installed outside the repository. `dotnet build devops-ai-assistant.sln` passed with zero warnings/errors; `dotnet test devops-ai-assistant.sln` passed all 55 tests. `dotnet format` verification passed for all changed C# files. Full-solution formatter verification still reports 1,910 pre-existing line-ending diagnostics across 9 untouched Project 1 files; no Project 2 files are reported.

Review follow-up strengthened the project-boundary tests to inspect transitive project references and added an integration assertion that the configured API URL binds to loopback. With these changes, `dotnet build devops-ai-assistant.sln` passes with zero warnings/errors, all 57 tests pass, and formatting verification passes for the modified C# tests.

## File List

- `devops-ai-assistant.sln`
- `src/DevOpsAI.Rag.Api/DevOpsAI.Rag.Api.csproj`
- `src/DevOpsAI.Rag.Api/Program.cs`
- `src/DevOpsAI.Rag.Api/appsettings.json`
- `src/DevOpsAI.Rag.Api/Properties/launchSettings.json`
- `src/DevOpsAI.Rag.Application/DevOpsAI.Rag.Application.csproj`
- `src/DevOpsAI.Rag.Application/ProjectMarker.cs`
- `src/DevOpsAI.Rag.Domain/DevOpsAI.Rag.Domain.csproj`
- `src/DevOpsAI.Rag.Domain/ProjectMarker.cs`
- `src/DevOpsAI.Rag.Infrastructure/DevOpsAI.Rag.Infrastructure.csproj`
- `src/DevOpsAI.Rag.Infrastructure/ProjectMarker.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/ApplicationAssemblySmokeTests.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/DevOpsAI.Rag.ApplicationTests.csproj`
- `tests/DevOpsAI.Rag.IntegrationTests/DevOpsAI.Rag.IntegrationTests.csproj`
- `tests/DevOpsAI.Rag.IntegrationTests/RagApiSmokeTests.cs`
- `tests/DevOpsAI.Rag.UnitTests/DevOpsAI.Rag.UnitTests.csproj`
- `tests/DevOpsAI.Rag.UnitTests/RagProjectBoundaryTests.cs`
- `_bmad-output/planning-artifacts/project-2/P2-01-solution-skeleton.md`

## Change Log

- 2026-11-12: Started implementation of P2-01.
- 2026-11-12: Added the Project 2 solution skeleton and offline architecture/API tests; build and tests pass.
- 2026-11-12: Strengthened transitive dependency-boundary and loopback configuration tests; translated Project 2 planning artifacts to English.

## Status

review
