# P2-08 - Document management API endpoints

- ID: P2-08
- Title: Document management API endpoints
- Type: User Story

## Description
As a knowledge-base operator, I need an HTTP API to add, replace, list, and delete documents so that the corpus is available for queries without manually manipulating local files.

## Objective
Expose document use cases through HTTP endpoints with DTO contracts, status codes, and consistent error handling.

## Technical Context
The Project 2 API must follow the Minimal API pattern and separate transport, dependency composition, and input/output contracts. Endpoints must map HTTP requests to Application use cases without exposing Infrastructure or SQLite details.

## Scope
- Define endpoints for `PUT /api/documents/{documentId}` and `DELETE /api/documents/{documentId}`.
- Define queries for listing documents and retrieving their status.
- Validate HTTP input at the transport boundary and keep business logic in Application.
- Define error codes for validation, not found, invalid content, and technical failures.
- Ensure diagnostics, errors, and logs do not reveal sensitive content.

## Verifiable Acceptance Criteria
1. The API exposes HTTP contracts only and does not directly use Infrastructure types.
2. Each operation returns a clear result: success, validation failure, missing resource, or unexpected error.
3. A document with invalid content is not indexed and does not replace the previous state.
4. Listing and deletion operations reflect the actual corpus state.
5. The transport uses the Application layer as its logical interface and contains no persistence logic.

## Affected Components
- `src/DevOpsAI.Rag.Api/Endpoints/`
- `src/DevOpsAI.Rag.Api/Contracts/`
- `src/DevOpsAI.Rag.Api/ErrorHandling/`
- `src/DevOpsAI.Rag.Api/Program.cs`
- `src/DevOpsAI.Rag.Api/DependencyInjection.cs`

## Dependencies
- Depends on `P2-03`, `P2-04`, and `P2-05`.
- Requires document status and validation decisions to keep the API consistent.

## Expected Tests
- Endpoint integration tests using TestHost.
- HTTP error cases for validation, missing resources, and infrastructure failures.
- Verification of document responses before and after replacement.

## Definition of Done
- Document endpoints are implemented and documented by contract.
- The API reflects the actual local corpus state without exposing storage details.
- Error responses and statuses are clearly defined for critical operations.

## Tasks / Subtasks
- [x] Añadir pruebas TestHost para creación, reemplazo, listado, borrado y errores de validación/técnicos.
- [x] Implementar contratos HTTP, endpoints Minimal API, registro de dependencias y mapeo seguro de errores.
- [x] Ejecutar build, pruebas, analizadores, formatter y verificación de límites; registrar los resultados.

## Dev Agent Record

### Implementation Plan

Se conectaron los casos de uso existentes de Application con endpoints Minimal API para carga/reemplazo, listado y borrado. Se añadieron contratos HTTP independientes, mapeo explícito de estados y errores seguros, además de pruebas TestHost con SQLite temporal y un generador de embeddings falso.

### Decisions

- El estado de cada documento se expone en `GET /api/documents` y en la respuesta de `PUT`; no se agregó un GET individual porque el contrato REST de la arquitectura define el GET de colección.
- Se añadió `ChunkCount` al resumen de Application y a la consulta de Infrastructure para devolver metadatos reales del corpus sin lógica de persistencia en API.
- Los errores de validación devuelven 400/413/415, la ausencia 404, fallos conocidos de dependencia 503 y fallos técnicos inesperados 500. Las respuestas y logs incluyen código/trace ID, nunca contenido ni detalles de excepción.
- Las pruebas usan SQLite temporal y un fake local; no requieren Ollama, red ni credenciales.

### Completion Notes

Implementados `PUT /api/documents/{documentId}`, `GET /api/documents` y `DELETE /api/documents/{documentId}` con contratos OpenAPI y ejemplos `.http`. La creación devuelve 201 con `Location`, el reemplazo 200, el borrado 204 o 404. La validación fallida y los errores de embedding no reemplazan la versión activa.

Verificación final:
- `dotnet build devops-ai-assistant.sln --no-restore`: correcto, 0 advertencias y 0 errores.
- `dotnet test devops-ai-assistant.sln --no-build --no-restore`: 184 pruebas aprobadas.
- `RagProjectBoundaryTests`: 5 pruebas aprobadas.
- `dotnet format` con inclusión de todos los archivos C# modificados: correcto.
- `git diff --check`: correcto.
- `dotnet format` de toda la solución sigue reportando 2.313 diagnósticos `ENDOFLine` en archivos preexistentes; no reporta diagnósticos de formato en los archivos modificados.
- No existe `sprint-status.yaml`; el estado se registra en esta historia.

### Debug Log

Las primeras ejecuciones detectaron que TestHost reutilizaba el connection string de la configuración de aplicación aunque el test sobrescribiera `IConfiguration`. Se aisló la persistencia reemplazando el registro de `RagDbContext` por SQLite temporal en `ConfigureTestServices`; las pruebas finales validan la ruta real y pasan.

## File List

- `_bmad-output/planning-artifacts/project-2/P2-08-document-management-api.md`
- `src/DevOpsAI.Rag.Api/Program.cs`
- `src/DevOpsAI.Rag.Api/Contracts/ApiErrorResponse.cs`
- `src/DevOpsAI.Rag.Api/Contracts/DocumentListResponse.cs`
- `src/DevOpsAI.Rag.Api/Contracts/DocumentResponse.cs`
- `src/DevOpsAI.Rag.Api/DependencyInjection.cs`
- `src/DevOpsAI.Rag.Api/Endpoints/DocumentEndpoints.cs`
- `src/DevOpsAI.Rag.Api/ErrorHandling/RagApiExceptionHandler.cs`
- `src/DevOpsAI.Rag.Api/documents.http`
- `src/DevOpsAI.Rag.Application/Documents/DeleteRunbookDocument.cs`
- `src/DevOpsAI.Rag.Application/Documents/ReplaceRunbookDocument.cs`
- `src/DevOpsAI.Rag.Application/Documents/RunbookDocumentSummary.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/SqliteRunbookDocumentStore.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/Documents/RunbookDocumentLifecycleTests.cs`
- `tests/DevOpsAI.Rag.IntegrationTests/DocumentEndpointsTests.cs`

## Change Log

- 2026-11-25: Implementada la API de gestión documental, los contratos y el manejo seguro de errores; añadidas pruebas de integración offline y actualizado el estado a `review`.

## Status
review
