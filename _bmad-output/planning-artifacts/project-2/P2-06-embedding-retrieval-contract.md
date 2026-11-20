# P2-06 - Embedding generation and retrieval contract

- ID: P2-06
- Title: Embedding generation and retrieval contract
- Type: User Story

## Description
As a RAG module, we need an embedding layer and a retrieval contract that can compare queries with chunks and return relevance-ranked candidates so that generation uses only available evidence.

## Objective
Define the ports and minimum implementation for generating embeddings, querying the local vector index, and returning retrieval candidates with scores and source metadata.

## Technical Context
The architecture requires generation and retrieval to remain separate. The local system must support integration with `Microsoft.Extensions.AI` and a local embedding adapter so the MVP can be validated end to end without Azure or external providers.

## Scope
- Define `ITextEmbeddingGenerator` and its contract for generating embeddings from text.
- Define `IRunbookKnowledgeIndex` for indexing and querying relevant candidates.
- Add local similarity search with a configured threshold and controlled top-k.
- Ensure retrieval results include chunk IDs and system-generated scores for the generation stage.
- Prepare offline evaluation using a fixed set of questions with known relevance.

## Verifiable Acceptance Criteria
1. Application defines embedding and search ports without depending on concrete implementations.
2. Retrieval returns relevance-ranked results with an explicit sufficiency level for answering.
3. Results can be mapped to exact chunks and their source documents.
4. Insufficient content can be distinguished from provider or storage failures.

## Affected Components
- `src/DevOpsAI.Rag.Application/Ports/`
- `src/DevOpsAI.Rag.Infrastructure/Embeddings/`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`
- `tests/DevOpsAI.Rag.ApplicationTests/`

## Dependencies
- Depends on `P2-04` and `P2-05`.
- Requires indexed chunks and entity models for relevance search.

## Expected Tests
- Similarity and ranking tests with a controlled corpus.
- Threshold and top-k selection tests.
- Tests distinguishing insufficient evidence from infrastructure errors.

## Definition of Done
- A clear contract exists for local embeddings and retrieval.
- The search layer can return useful results for a question about the real corpus.
- Relevance and threshold decisions are deterministic and verifiable.

## Tasks / Subtasks

- [x] Define Application-owned embedding and retrieval contracts, including explicit evidence sufficiency and source-ranked candidates.
- [x] Generate chunk embeddings and persist them atomically with document snapshots; implement configured SQLite cosine retrieval and the local Ollama adapter.
- [x] Add deterministic tests for embedding generation, similarity ranking, threshold/top-k behavior, source mapping, insufficient evidence, and provider/storage failures.
- [x] Add a fixed offline retrieval evaluation corpus with known relevant chunks; verify the Project 2 dependency boundary and update local configuration guidance.
- [x] Run solution build, tests, analyzers, formatter, and Clean Architecture dependency verification.

## Dev Agent Record

### Implementation Plan

Define Application-owned embedding and retrieval ports, generate and persist chunk vectors as part of the existing document snapshot replacement, and implement local cosine retrieval in Infrastructure. Keep provider-specific Ollama code outside Application and verify ranking, sufficiency, failure behavior, and document/chunk mapping with offline tests.

### Decisions

- Used OllamaSharp through `Microsoft.Extensions.AI` as the local embedding adapter, consistent with ADR-003. The model ID is required configuration; no model or credentials are hardcoded.
- Extended `IRunbookKnowledgeIndex` with atomic snapshot indexing so document content, chunks, and embeddings are saved through the existing SQLite transaction. Existing document-store use cases remain separate from retrieval.
- Stored vectors as little-endian SQLite BLOBs with their model ID and dimensions. Reindexing is required for older chunks without embeddings; incompatible embedding spaces fail explicitly.
- Kept top-k constrained to 1–20 and the initial similarity threshold configurable (0.7). The default is a starting point, not a threshold calibrated against a production corpus.
- Used a fixed synthetic evaluation fixture for reproducible offline ranking checks. No real runbook corpus or local Ollama service was available for quality calibration.

### Completion Notes

Implemented Application ports and retrieval result/sufficiency contracts; an Ollama embedding adapter; SQLite embedding persistence and cosine ranking with configured threshold/top-k; and atomic embedding generation/indexing during document replacement. Added deterministic tests for provider failures and cancellation, vector validation, ranking, threshold/top-k, source mapping, insufficient evidence, and persistence behavior. Updated local configuration documentation.

Validation with .NET SDK 10.0.404: solution build succeeded with 0 warnings/errors; all 151 solution tests passed, including Project 2 dependency-boundary tests. `dotnet format` verification passed for all changed C# files. Full-solution formatter verification still reports line-ending diagnostics in pre-existing files outside this change; no diagnostics remain in changed C# files. `git diff --check` passes.

The Definition of Done requiring retrieval to be demonstrated against the real corpus remains unverified: the available evaluation fixture is synthetic and no local Ollama service/corpus was used. The story is therefore left in `review`, not marked complete.

## File List

- `Directory.Packages.props`
- `README.md`
- `src/DevOpsAI.Rag.Api/Program.cs`
- `src/DevOpsAI.Rag.Api/appsettings.json`
- `src/DevOpsAI.Rag.Application/Chat/RunbookEvidenceSufficiency.cs`
- `src/DevOpsAI.Rag.Application/Chat/RunbookRetrievalResult.cs`
- `src/DevOpsAI.Rag.Application/Configuration/RunbookRetrievalOptions.cs`
- `src/DevOpsAI.Rag.Application/Documents/ReplaceRunbookDocument.cs`
- `src/DevOpsAI.Rag.Application/Ports/IRunbookDocumentStore.cs`
- `src/DevOpsAI.Rag.Application/Ports/IRunbookKnowledgeIndex.cs`
- `src/DevOpsAI.Rag.Application/Ports/ITextEmbeddingGenerator.cs`
- `src/DevOpsAI.Rag.Application/Ports/RunbookChunkEmbedding.cs`
- `src/DevOpsAI.Rag.Infrastructure/DependencyInjection.cs`
- `src/DevOpsAI.Rag.Infrastructure/DevOpsAI.Rag.Infrastructure.csproj`
- `src/DevOpsAI.Rag.Infrastructure/Embeddings/OllamaEmbeddingOptions.cs`
- `src/DevOpsAI.Rag.Infrastructure/Embeddings/OllamaTextEmbeddingGenerator.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/Entities/RunbookChunkEmbeddingEntity.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/Entities/RunbookChunkEntity.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/RagDbContext.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/SqliteEmbeddingVectorConverter.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/SqliteRunbookDocumentStore.cs`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/SqliteRunbookKnowledgeIndex.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/Chat/RunbookRetrievalContractTests.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/Documents/RunbookDocumentLifecycleTests.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/RunbookDocumentStoreContractTests.cs`
- `tests/DevOpsAI.Rag.IntegrationTests/DevOpsAI.Rag.IntegrationTests.csproj`
- `tests/DevOpsAI.Rag.IntegrationTests/Fixtures/rag-evaluation.json`
- `tests/DevOpsAI.Rag.IntegrationTests/Persistence/SqliteRunbookDocumentStoreTests.cs`
- `tests/DevOpsAI.Rag.IntegrationTests/Persistence/SqliteRunbookKnowledgeIndexTests.cs`
- `tests/DevOpsAI.Rag.UnitTests/Embeddings/OllamaTextEmbeddingGeneratorTests.cs`

### Change Log

- 2026-11-20: Implemented embedding generation and local retrieval contracts, SQLite vector indexing, Ollama adapter, and offline verification.

## Status
review
