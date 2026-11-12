# P2-02 - Domain models and core invariants

- ID: P2-02
- Title: Domain models and core invariants
- Type: User Story

## Description
As a developer of the RAG module, we need to model the domain concepts for documents, chunks, and references so that we can define core business rules before implementing persistence and use cases.

## Objective
Define the domain types that represent documents, retrievable chunks, citations, and query results while keeping the domain independent of SQLite, ASP.NET Core, and AI providers.

## Technical Context
The project must support Markdown/TXT documents, deterministic chunking, source traceability, and answers with verifiable citations. Domain models must encapsulate invariants such as identifier, hash, version, and valid content without depending on infrastructure or serialization details.

## Scope
- Define the `KnowledgeDocument` model with an ID, filename, content, hash, version, and minimal metadata.
- Define `DocumentChunk` with its document relationship, location, text, and source metadata.
- Define `RetrievalCandidate` and `CitationReference` with identifiers and relevance/support information.
- Define `GroundedAnswer` for results that accept only verifiable sources.
- Ensure the domain contains no access to HTTP APIs, databases, SQLite, EF Core, or model SDKs.

## Verifiable Acceptance Criteria
1. Domain contains only business entities and contracts, with no infrastructure or transport references.
2. Each entity defines at least one clear invariant, such as a non-null document, a chunk with valid content, or an answer with verifiable citations.
3. Models can represent a replaced or deleted document without losing traceability to its prior content.
4. Application consumes the domain types, and Infrastructure maps to them rather than the reverse.

## Affected Components
- `src/DevOpsAI.Rag.Domain/Documents/`
- `src/DevOpsAI.Rag.Domain/Chat/`
- `src/DevOpsAI.Rag.Application/Ports/`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`

## Dependencies
- Depends on `P2-01`.
- Requires a decision on which document metadata must be persisted and how citations are represented.

## Expected Tests
- Unit tests for domain invariants.
- Entity validation tests for empty documents, invalid chunks, answers without references, and answers with invalid references.
- Verification that no infrastructure dependencies are introduced.

## Definition of Done
- The domain model is defined and documented.
- Critical rules are represented as invariants or deterministic validation.
- The rest of the project can consume these types without accessing infrastructure details.

## Tasks / Subtasks

- [x] Define immutable document snapshots with deterministic content hashes, version replacement, and traceable deletion state.
- [x] Define chunks, retrieval candidates, citations, and grounded answers with deterministic identity, location, score, and citation-membership invariants.
- [x] Add an Application-owned document-store port that consumes Domain types without exposing persistence details.
- [x] Add offline tests for invalid values, replacement/deletion traceability, citation verification, and architectural boundaries.
- [x] Build, test, format, verify dependency direction, and update the story record.

## Dev Agent Record

### Implementation Plan

Implement the P2-02 domain contracts and their invariants, add only the Application port required to consume the document model, and verify all behavior without adding persistence or provider dependencies.

### Decisions

- A replacement creates a new immutable snapshot with the same stable ID and an incremented version; the original snapshot remains unchanged.
- Deletion creates a deleted snapshot retaining the original content and SHA-256 hash so that previous references remain traceable.
- Citations bind document ID, version, chunk ID, heading, and source line range; `GroundedAnswer` accepts citations only when all those values match retrieved candidates.
- A concrete Infrastructure persistence mapper is deferred to P2-04, where the persistence representation is introduced. P2-02 establishes the dependency direction through the Application port and existing Infrastructure-to-Domain project reference without introducing premature persistence entities.

### Completion Notes

Added `KnowledgeDocument`, `DocumentChunk`, `RetrievalCandidate`, `CitationReference`, and `GroundedAnswer` with immutable values and deterministic validation. Added `IRunbookDocumentStore` as an Application-owned port using Domain types and cancellation-aware async methods. Removed the now-unused Domain `ProjectMarker` from the empty skeleton so that the layer contains only domain models/contracts. Tests verify hashes, invalid/empty values, snapshot replacement and deletion, chunk locations, normalized scores, citation membership and document-version traceability, and Domain dependency boundaries.

Validation completed with .NET SDK 10.0.404: `dotnet build devops-ai-assistant.sln --no-restore` passed with 0 warnings and 0 errors; `dotnet test devops-ai-assistant.sln --no-restore` passed all 85 tests; `dotnet format devops-ai-assistant.sln --verify-no-changes --no-restore --include ...` passed for all changed C# files. No Project 1 source or dependencies were changed.

## File List

- `src/DevOpsAI.Rag.Domain/Documents/KnowledgeDocumentStatus.cs`
- `src/DevOpsAI.Rag.Domain/Documents/KnowledgeDocument.cs`
- `src/DevOpsAI.Rag.Domain/Documents/DocumentChunk.cs`
- `src/DevOpsAI.Rag.Domain/ProjectMarker.cs` (removed)
- `src/DevOpsAI.Rag.Domain/Chat/RetrievalCandidate.cs`
- `src/DevOpsAI.Rag.Domain/Chat/CitationReference.cs`
- `src/DevOpsAI.Rag.Domain/Chat/GroundedAnswer.cs`
- `src/DevOpsAI.Rag.Application/Ports/IRunbookDocumentStore.cs`
- `tests/DevOpsAI.Rag.UnitTests/KnowledgeDocumentTests.cs`
- `tests/DevOpsAI.Rag.UnitTests/DocumentChunkTests.cs`
- `tests/DevOpsAI.Rag.UnitTests/GroundedAnswerTests.cs`
- `tests/DevOpsAI.Rag.UnitTests/RagProjectBoundaryTests.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/RunbookDocumentStoreContractTests.cs`
- `_bmad-output/planning-artifacts/project-2/P2-02-domain-models.md`

## Change Log

- 2026-11-12: Added Project 2 domain models, lifecycle and citation invariants, an Application-owned document-store port, and offline tests; removed the obsolete empty Domain marker; solution build, tests, and scoped format verification pass.

## Status

review
