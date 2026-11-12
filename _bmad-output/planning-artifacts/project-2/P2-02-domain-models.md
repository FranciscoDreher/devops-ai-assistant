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
