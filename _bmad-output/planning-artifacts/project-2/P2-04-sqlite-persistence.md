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

## Tasks / Subtasks
- [x] Define the SQLite context and entities for document metadata, chunks, and citation references.
- [x] Implement the Infrastructure document-store adapter with atomic replacement, deletion, and domain mapping.
- [x] Register and initialize SQLite through Infrastructure and configure the local database path.
- [x] Add temporary-file SQLite integration tests for persistence across reopen, replacement, rollback, and deletion integrity.
- [x] Run build, tests, analyzers, formatter, and dependency-boundary verification.

## Dev Agent Record

### Implementation Plan

Added an EF Core SQLite persistence model and Infrastructure adapter behind the existing Application document-store port. Extended the port to accept a document snapshot with its chunks while retaining the existing document-only operation for the P2-03 lifecycle use case. Persisted citation coordinates with each chunk, and used transactions so replacement/deletion cannot expose stale derived records.

### Decisions

- Database-location alternatives considered: a platform-specific LocalApplicationData default or a configurable connection string. Chose `ConnectionStrings:RagDatabase`, defaulting to `runbook-rag.db` in the process working directory, because it is easy to override across local environments and supports custom absolute paths. The working-directory behavior and `ConnectionStrings__RagDatabase` override are documented in the README.
- Schema alternatives considered: EF Core migrations or an MVP initial schema. Chose `EnsureCreatedAsync` for this first SQLite schema to avoid adding design-time tooling before schema evolution is required; migrations remain a future schema-evolution mechanism.
- Stored citation references as one-to-one rows keyed by chunk ID and cascading from the chunk. This maps the existing `CitationReference` domain concept and ensures replacement/deletion removes stale citation data.
- Embedding vectors are not added ahead of the P2-06 embedding/retrieval contract. P2-04 persists source content, chunks, versions, and citation locations needed to retain/rebuild corpus state; no P2-05/P2-06 indexing behavior was implemented.

### Completion Notes

Implemented `RagDbContext`, document/chunk/citation persistence entities, domain reconstitution mapping, and `SqliteRunbookDocumentStore`. Replacements enforce the next document version and run in a transaction that removes prior chunks/references and writes the new snapshot; failures roll back to the prior snapshot. Deletion persists a content-free tombstone and removes chunks/references atomically. SQLite is registered through Infrastructure, initialized at API startup, and uses foreign-key enforcement. The local database filename is ignored by Git.

Added SQLite integration tests for create/reopen mapping, replacement, stale-write rejection, rollback, deletion and related-record integrity. Final validation with temporary .NET SDK 10.0.100: `dotnet build` succeeded with 0 warnings/errors; all 117 solution tests passed; the 5 Project 2 architecture-boundary tests passed; scoped `dotnet format --verify-no-changes` passed for all changed C# files. Repository-wide formatter verification still fails on existing line-ending diagnostics in unrelated files, with no diagnostics in changed C# files. No `sprint-status.yaml` exists, so the story file is the progress record.

### File List

- `.gitignore`
- `Directory.Packages.props`
- `README.md`
- `src/DevOpsAI.Rag.Api/Program.cs`
- `src/DevOpsAI.Rag.Api/appsettings.json`
- `src/DevOpsAI.Rag.Application/Ports/IRunbookDocumentStore.cs`
- `src/DevOpsAI.Rag.Domain/Documents/KnowledgeDocument.cs`
- `src/DevOpsAI.Rag.Infrastructure/DevOpsAI.Rag.Infrastructure.csproj`
- `src/DevOpsAI.Rag.Infrastructure/DependencyInjection.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/Entities/RunbookChunkEntity.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/Entities/RunbookCitationReferenceEntity.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/Entities/RunbookDocumentEntity.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/RagDbContext.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/SqliteRunbookDocumentStore.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/RunbookDocumentStoreContractTests.cs`
- `tests/DevOpsAI.Rag.IntegrationTests/Persistence/SqliteRunbookDocumentStoreTests.cs`
- `_bmad-output/planning-artifacts/project-2/P2-04-sqlite-persistence.md`

### Change Log

- 2026-11-19: Implemented SQLite persistence and atomic document/chunk/reference lifecycle; added offline persistence and rollback tests.

## Status
review
