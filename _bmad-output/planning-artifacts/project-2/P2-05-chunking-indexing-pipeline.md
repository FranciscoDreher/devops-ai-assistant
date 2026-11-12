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
