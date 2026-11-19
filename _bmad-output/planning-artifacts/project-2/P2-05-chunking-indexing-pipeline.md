# P2-05 - Deterministic chunking and indexing pipeline

- ID: P2-05
- Title: Deterministic chunking and indexing pipeline
- Type: User Story

## Description
As a document retrieval system, we need to split documents into reusable chunks and index them with metadata that enables contextual search, retrieval, and precise references.

## Objective
Create the ingestion and indexing pipeline that converts validated documents into deterministic chunks with source references and clean update support.

## Technical Context
The project requires a deterministic chunker for Markdown/TXT that can split by headings, paragraphs, and a suggested maximum size. The goal is to keep chunks useful for semantic retrieval and citations traceable to documents and relevant lines or sections.

## Scope
- Define basic text extraction for Markdown/TXT without executing content.
- Establish chunking and overlap strategies based on maximum size and document structure.
- Create the indexing flow that associates each chunk with a document, its location, and a content hash.
- Integrate the pipeline with persistence and indexing ports.
- Prepare the logic for clean replacement and deletion of old chunks.

## Verifiable Acceptance Criteria
1. The pipeline processes valid documents into deterministic chunks without relying on model content or external providers.
2. Each chunk retains enough source metadata to construct verifiable citations.
3. Replacing a document removes or invalidates old chunks before accepting new queries.
4. The process does not interpret or execute file content; it only extracts plain text and splits it into chunks.

## Affected Components
- `src/DevOpsAI.Rag.Application/Documents/`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`
- `src/DevOpsAI.Rag.Infrastructure/DependencyInjection.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/`

## Dependencies
- Depends on `P2-03` and `P2-04`.
- Requires stabilized document and persistence models.

## Expected Tests
- Unit tests for deterministic chunking across text with different structures.
- Verification that chunks retain source metadata.
- Replacement tests to ensure old chunks are removed.

## Definition of Done
- Documents are converted into chunks and stored consistently.
- The local index supports later retrieval without depending on a generation model.
- The mapping from citations in results to locations in source documents is clear.

## Tasks / Subtasks

- [x] Implement deterministic Markdown/TXT chunking with heading and paragraph boundaries, configurable maximum size, overlap, and source line metadata.
- [x] Integrate chunk generation into document replacement and persist complete snapshots through the existing atomic document-store port.
- [x] Register chunking options and pipeline dependencies in the RAG API composition root.
- [x] Add application tests for deterministic output, text structure, source metadata, overlap, and invalid configuration.
- [x] Add SQLite pipeline coverage proving replacement removes old chunks and citation references.
- [x] Run build, tests, analyzers, scoped formatting, and Clean Architecture boundary verification.

## Dev Agent Record

### Implementation Plan

Use the already validated UTF-8 document content as plain text; parse Markdown headings without rendering or executing document content; preserve TXT text as-is. Chunk by section/paragraph with configured maximum size and overlap, then persist the document and its chunks in the same store operation so the existing SQLite transaction replaces stale searchable data atomically.

### Decisions

- Reused `IRunbookDocumentStore.SaveAsync(document, chunks, cancellationToken)` and its existing SQLite transaction/deletion of old chunks and citation rows; no new persistence abstraction or external provider was needed.
- Added `RunbookChunkingOptions` with the architecture defaults of 1,000 characters maximum and 150 characters overlap. Overlap is applied to size-driven splits; paragraph-boundary splits start cleanly at the next paragraph rather than repeating short fragments.
- Kept fenced Markdown content as source text and suppressed heading parsing inside fences. TXT files are not interpreted as Markdown.
- Registered the chunker and replacement use case at the API composition root. The Infrastructure registration and persistence adapter required no changes because the existing port already supports atomic document/chunk replacement.

### Completion Notes

Implemented deterministic chunk IDs derived from document ID, version, and ordinal; chunk metadata includes document/version association, heading, and inclusive source line range. Replacement now creates chunks before saving the active snapshot and forwards cancellation to persistence. Tests cover Markdown and TXT handling, headings, paragraph boundaries, size limits, overlap, line citations, heading-only Markdown, configuration validation, and replacement cleanup of old chunks/citations.

Validation: `dotnet build devops-ai-assistant.sln --no-restore` succeeded with 0 warnings/errors; `dotnet test devops-ai-assistant.sln --no-build --no-restore` passed all 128 tests. `dotnet format` verification passed for all changed C# files. Full-solution formatter verification reports pre-existing line-ending diagnostics in untouched files; none are in the changed RAG files. Architecture boundary tests passed as part of the full suite. No sprint-status file exists; this story file tracks progress.

### Review Follow-up

Addressed review findings in the chunker: Markdown Setext headings now retain heading and source metadata, heading-only chunks respect the configured size, and chunk boundaries preserve UTF-16 surrogate pairs. Added focused application tests for all three cases.

Post-fix validation was attempted with the installed SDK, but build and tests are blocked because the repository targets `net10.0` and only .NET 6/9 SDKs are installed. Scoped `dotnet format --verify-no-changes` could not load project references for the same environment limitation. `git diff --check` passes.

## File List

- `src/DevOpsAI.Rag.Api/Program.cs`
- `src/DevOpsAI.Rag.Api/appsettings.json`
- `src/DevOpsAI.Rag.Application/Configuration/RunbookChunkingOptions.cs`
- `src/DevOpsAI.Rag.Application/Documents/ReplaceRunbookDocument.cs`
- `src/DevOpsAI.Rag.Application/Documents/RunbookDocumentChunker.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/Documents/RunbookDocumentChunkerTests.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/Documents/RunbookDocumentLifecycleTests.cs`
- `tests/DevOpsAI.Rag.IntegrationTests/Persistence/SqliteRunbookDocumentStoreTests.cs`
- `_bmad-output/planning-artifacts/project-2/P2-05-chunking-indexing-pipeline.md`

### Change Log

- 2026-11-19: Implemented deterministic document chunking and atomic SQLite indexing integration; added offline unit and integration coverage.
- 2026-11-19: Fixed chunking review findings and added regression tests; post-fix build/test verification awaits a .NET 10 SDK.

## Status
review
