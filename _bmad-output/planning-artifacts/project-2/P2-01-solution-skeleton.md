# P2-01 - Solution skeleton and project boundaries

- ID: P2-01
- Título: Solution skeleton and project boundaries
- Tipo: User Story

## Descripción
Como equipo de ingeniería del proyecto, necesitamos una estructura de solución y límites de proyecto claros para Project 2 para poder desarrollar el módulo Runbook Chatbot sin acoplarlo al código de Project 1 ni introducir dependencias no previstas.

## Objetivo
Crear la base mínima del módulo RAG: proyectos, referencias, configuración compartida, convenciones y separación de responsabilidades entre Domain, Application, Infrastructure y API.

## Contexto técnico
Project 2 debe vivir dentro de la solución actual de .NET y seguir la arquitectura Clean Architecture definida en `ARCHITECTURE.md` y `PROJECT-2-ARCHITECTURE.md`. La base debe separar dominio, casos de uso, puertos, infraestructura y API sin tocar Project 1 ni crear un patrón de reutilización prematuro.

## Alcance específico
- Crear los proyectos de `DevOpsAI.Rag.Domain`, `DevOpsAI.Rag.Application`, `DevOpsAI.Rag.Infrastructure` y `DevOpsAI.Rag.Api` dentro de la solución.
- Configurar referencias correctas siguiendo la dirección `Api -> Application -> Domain` y `Infrastructure -> Application/Domain`.
- Mantener `Project 1` intacto y sin referencias cruzadas.
- Asegurar que el proyecto API sea un host mínimo vía ASP.NET Core con configuración básica.
- Preparar la base para xUnit/NSubstitute y pruebas de integración del módulo.
- Definir el conjunto de paquetes esenciales y configuraciones de formato/análisis del repositorio.

## Criterios de aceptación verificables
1. La solución incluye los cuatro proyectos de Project 2 y todos se agregan a `devops-ai-assistant.sln`.
2. No existen referencias de Project 2 hacia Project 1 ni a SDKs específicos de proveedor desde el Domain o Application.
3. El proyecto API compila con una configuración mínima sin lógica de negocio.
4. El proyecto Infrastructure queda preparado para implementar puertos de persistencia y LLM/RAG y no se mezclan dependencias de infraestructura con Domain.
5. Las pruebas unitarias/integración del módulo cuentan con estructura inicial y ejecutan compilación sin requerir proveedores externos.

## Componentes afectados
- `src/DevOpsAI.Rag.Domain/`
- `src/DevOpsAI.Rag.Application/`
- `src/DevOpsAI.Rag.Infrastructure/`
- `src/DevOpsAI.Rag.Api/`
- `tests/DevOpsAI.Rag.UnitTests/`
- `tests/DevOpsAI.Rag.ApplicationTests/`
- `tests/DevOpsAI.Rag.IntegrationTests/`
- `devops-ai-assistant.sln`
- `Directory.Build.props` / `Directory.Packages.props` (solo si la estructura de paquetes exige ajuste)

## Dependencias
- Depende de la aprobación del alcance del proyecto definido en `ARCHITECTURE.md` y `PROJECT-2-ARCHITECTURE.md`.
- Requiere la existencia de la base del repositorio con .NET 10 y convenios de estilo.

## Tests esperados
- `dotnet build` sobre la solución.
- `dotnet test` para proyectos vacíos o con pruebas de bootstrapping.
- Verificación de que no haya referencias cruzadas no permitidas.

## Definition of Done
- La estructura del módulo existe y se compila.
- Las capas quedan separadas según la arquitectura acordada.
- La API y la infraestructura están listas para recibir casos de uso reales sin introducir lógica de negocio.
- Los archivos de configuración y referencias están documentados y consistentes con el repositorio.

## Tasks / Subtasks

- [x] Create the four Project 2 production projects with the approved Clean Architecture references and add them to the solution.
- [x] Add a minimal, locally configured ASP.NET Core API host without business logic.
- [x] Add the three offline xUnit/NSubstitute test projects and tests for project boundaries and API startup.
- [x] Build, test, format, verify dependency boundaries, and update the story record.

## Dev Agent Record

### Implementation Plan

Create only the Project 2 project structure, references, minimal host configuration, and test scaffolding defined by P2-01. Reuse central packages and repository build/format settings; defer all RAG behavior and provider/persistence packages to later stories.

### Completion Notes

Created the four isolated Project 2 projects, registered their Clean Architecture references and added all projects to the solution. The API is a minimal ASP.NET Core host with OpenAPI and a loopback-configured health endpoint. Added xUnit/NSubstitute test projects with project-boundary checks and an offline API smoke test. No persistence, RAG, provider SDK or business logic was added; shared build/package configuration and Project 1 sources remain unchanged.

Validation used .NET SDK 10.0.404 installed outside the repository. `dotnet build devops-ai-assistant.sln` passed with zero warnings/errors; `dotnet test devops-ai-assistant.sln` passed all 55 tests. `dotnet format` verification passed for all changed C# files. Full-solution formatter verification still reports 1,910 pre-existing line-ending diagnostics across 9 untouched Project 1 files; no Project 2 files are reported.

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

## Status

review
