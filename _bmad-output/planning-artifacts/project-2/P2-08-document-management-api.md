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
