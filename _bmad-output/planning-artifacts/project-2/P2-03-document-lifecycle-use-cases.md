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

## Tasks / Subtasks

- [x] Define Application validation configuration and deterministic policies for document IDs, Markdown/TXT extensions, byte size, strict UTF-8, and non-empty content.
- [x] Implement explicit request/response contracts and normalized lifecycle results for replace, delete, and list use cases.
- [x] Extend the Application-owned document-store contract for listing and consistent replacement/deletion semantics without implementing persistence.
- [x] Add offline Application tests for validation, errors, lifecycle outcomes, port contracts, and validation-before-store ordering.
- [x] Run build, tests, formatter/analyzers, verify dependency direction, and update the story record.

## Dev Agent Record

### Implementation Plan

Implement P2-03 entirely through Application contracts/use cases and offline tests, reusing the P2-02 `KnowledgeDocument` model and `IRunbookDocumentStore` port. Do not implement SQLite, parsing/chunking, embeddings, retrieval adapters, or API endpoints.

### Decisions

- The PRD requires a configurable maximum file size but does not state a value. Alternatives considered: require explicit configuration with no default, use a configurable 1 MiB default, or use a larger configurable 10 MiB default. Use a configurable 1 MiB default because the corpus is Markdown/TXT runbooks and the lower default limits accidental resource consumption while allowing API configuration to override it.
- Validate the byte count before decoding; decode with strict UTF-8, allow and strip one leading UTF-8 BOM, reject malformed sequences, and treat whitespace-only decoded content as empty.
- Allow `.md` and `.txt` extensions case-insensitively. Reuse the Domain document ID invariant instead of defining a separate identifier format.
- Store operations remain Application ports. Replacement/deletion consistency is specified as a port contract; concrete atomic persistence and derived-index changes remain outside P2-03.

### Completion Notes

Implemented the replace/create, delete, and list Application use cases with explicit request/response types and normalized operation statuses. Validation runs before any store access: IDs reuse the Domain invariant, extensions allow `.md`/`.txt` case-insensitively, raw byte limits are enforced, malformed UTF-8 is rejected, one leading UTF-8 BOM is stripped, and empty/whitespace-only content is rejected. The configurable maximum defaults to 1 MiB.

Extended the Application document-store contract to list metadata without loading document bodies and to require atomic invalidation of prior searchable data during replacement/deletion. The use cases do not depend on SQLite, Ollama, ASP.NET Core, or concrete Infrastructure types; persistence and indexing implementations remain deferred to later stories. Store failures are propagated rather than converted to success-shaped results.

Offline Application tests cover validation boundaries, replacement/deletion states, tombstones, not-found behavior, store failures, metadata-only listing, and port signatures. `dotnet build --no-restore` passed with 0 warnings/errors; `dotnet test --no-build --no-restore` passed all 110 solution tests; changed C# files passed scoped `dotnet format --verify-no-changes --no-restore`; Clean Architecture boundary tests passed in the full suite. Repository-wide `dotnet format --verify-no-changes --no-restore` was also run but reports pre-existing line-ending violations in untouched files; changed C# files were formatted and the scoped verification passes.

## File List

- `src/DevOpsAI.Rag.Application/Configuration/RunbookDocumentOptions.cs`
- `src/DevOpsAI.Rag.Application/Documents/DeleteRunbookDocument.cs`
- `src/DevOpsAI.Rag.Application/Documents/DeleteRunbookDocumentRequest.cs`
- `src/DevOpsAI.Rag.Application/Documents/ListRunbookDocuments.cs`
- `src/DevOpsAI.Rag.Application/Documents/ListRunbookDocumentsResponse.cs`
- `src/DevOpsAI.Rag.Application/Documents/ReplaceRunbookDocument.cs`
- `src/DevOpsAI.Rag.Application/Documents/ReplaceRunbookDocumentRequest.cs`
- `src/DevOpsAI.Rag.Application/Documents/RunbookDocumentOperationStatus.cs`
- `src/DevOpsAI.Rag.Application/Documents/RunbookDocumentResult.cs`
- `src/DevOpsAI.Rag.Application/Documents/RunbookDocumentSummary.cs`
- `src/DevOpsAI.Rag.Application/Documents/RunbookDocumentValidationErrorCode.cs`
- `src/DevOpsAI.Rag.Application/Documents/RunbookDocumentValidationException.cs`
- `src/DevOpsAI.Rag.Application/Documents/RunbookDocumentValidator.cs`
- `src/DevOpsAI.Rag.Application/Documents/ValidatedRunbookDocument.cs`
- `src/DevOpsAI.Rag.Application/Ports/IRunbookDocumentStore.cs`
- `src/DevOpsAI.Rag.Domain/Documents/KnowledgeDocument.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/Documents/RunbookDocumentLifecycleTests.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/Documents/RunbookDocumentValidatorTests.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/RunbookDocumentStoreContractTests.cs`
- `_bmad-output/planning-artifacts/project-2/P2-03-document-lifecycle-use-cases.md`

## Change Log

- 2026-11-19: Reconciled the P2-03 implementation scope and recorded validation-policy decisions before implementation.
- 2026-11-19: Implemented deterministic document validation and lifecycle use cases with offline tests; full build/test and scoped formatting passed.

## Status

review
