# P2-03 - Application document lifecycle use cases

- ID: P2-03
- Title: Application document lifecycle use cases
- Type: User Story

## Description
As a system user, I need to register, replace, list, and delete documents in the local index so that the knowledge corpus stays synchronized with the runbooks that are actually available.

## Objective
Define Application use cases for the document lifecycle, with deterministic validation policies and explicit input/output contracts, before implementing any real storage.

## Technical Context
Documents will be Markdown/TXT and require initial validation of ID, extension, size, UTF-8 encoding, and non-empty content. They will be managed through explicit replacement and deletion operations. The flow must be deterministic and must not rely on the model to decide whether a document is valid or sufficient to answer a question.

## Scope
- Define use cases for `ReplaceRunbookDocument`, `DeleteRunbookDocument`, and `ListRunbookDocuments`.
- Define a normalized document result and status classification: created, replaced, deleted, and validation error.
- Define validation policies for extension, size, and non-empty content.
- Prepare Application to consume persistence and retrieval ports without knowing their implementations.

## Verifiable Acceptance Criteria
1. Each use case has explicit inputs and outputs with clear request/response names.
2. Content validation occurs in Application before persistence or indexing.
3. Replacing a document produces one consistent state for new queries and removes references to previous versions.
4. Deleting a document leaves no active chunks or vectors available to subsequent queries.
5. Use cases do not know about SQLite, Ollama, or ASP.NET Core.

## Affected Components
- `src/DevOpsAI.Rag.Application/Documents/`
- `src/DevOpsAI.Rag.Application/Configuration/`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`
- `src/DevOpsAI.Rag.Api/Contracts/`

## Dependencies
- Depends on `P2-02`.
- Requires the domain structure to document metadata and replacement expectations.

## Expected Tests
- Unit tests for ID, extension, size, and content validation.
- Tests for error handling and replacement/deletion results.
- Verification of the contract between Application and Infrastructure.

## Definition of Done
- Document use cases are defined and aligned with the established validation policy.
- Replacement and deletion flows are specified with deterministic states.
- The API can implement these use cases without hidden storage decisions.
