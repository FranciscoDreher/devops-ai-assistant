# P2-04 - SQLite persistence and document metadata storage

- ID: P2-04
- Title: SQLite persistence and document metadata storage
- Type: User Story

## Description
As a local RAG system, we need a reliable persistence layer that retains documents, chunks, metadata, and references so that corpus state survives process restarts and does not rely on temporary memory.

## Objective
Implement the Infrastructure persistence layer for document metadata, chunk relationships, and index state, enabling consistent updates, queries, and deletions.

## Technical Context
The project adopted SQLite as local storage for a simple, durable MVP. The Infrastructure layer must handle persistence entities, migrations, and clear read/write access while keeping EF Core details out of application logic.

## Scope
- Define `RagDbContext` and persistence entities for documents, chunks, and references.
- Configure SQLite with migrations or an initial schema compatible with the MVP.
- Expose a concrete implementation of the storage port for documents and chunks.
- Ensure consistent updates and deletion of document headers, chunks, and metadata.
- Prepare support for rebuilding the index after a process restart.

## Verifiable Acceptance Criteria
1. Persistent storage retains corpus state across restarts.
2. Infrastructure entities have a clear mapping to domain models.
3. Deleting a document invalidates or removes its associated chunks and references.
4. The persistence context contains no business rules or user-response logic.
5. Write operations provide explicit, consistent, and atomic replacement semantics.

## Affected Components
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`
- `src/DevOpsAI.Rag.Infrastructure/DependencyInjection.cs`
- `src/DevOpsAI.Rag.Application/Ports/`
- `tests/DevOpsAI.Rag.IntegrationTests/`

## Dependencies
- Depends on `P2-02` and `P2-03`.
- Requires the definition of ports and document states for mapping to SQLite.

## Expected Tests
- Persistence tests for creating, updating, and deleting documents.
- Integrity tests between documents and chunks.
- Process-reopen tests to confirm that data persists correctly.

## Definition of Done
- SQLite persistence is implemented with a stable data structure.
- Corpus state is durable and consistent after restart.
- The Application layer can consume persistence without knowing the underlying database.
