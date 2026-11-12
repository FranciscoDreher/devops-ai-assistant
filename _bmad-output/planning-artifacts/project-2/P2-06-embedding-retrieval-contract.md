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
