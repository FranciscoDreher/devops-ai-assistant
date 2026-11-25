# P2-07 - Grounded answer generation and citation validation

- ID: P2-07
- Title: Grounded answer generation and citation validation
- Type: User Story

## Description
As a chatbot user, I need every answer to rely only on text retrieved from the corpus and to include verifiable references so that I can trust the answer without assuming the model invents facts.

## Objective
Define grounded answer generation with citation validation, abstention when evidence is insufficient, and structured API output.

## Technical Context
The project does not allow answers based on knowledge outside the retrieved context. Answers must be generated from relevant chunks, with citations pointing to specific documents and chunks, never to invented or partially supported sources.

## Scope
- Define `IRunbookAnswerGenerator` and `IRunbookPromptProvider`.
- Define prompt and output formats with grounding constraints.
- Establish the insufficient-answer rule for cases where the corpus does not provide enough evidence.
- Define deterministic citation validation: accept only references associated with retrieved chunks.
- Prepare the layer for structured answers and metadata useful to the API.

## Verifiable Acceptance Criteria
1. The final answer is constructed from retrieved chunks, not from the model's unrestricted knowledge.
2. Every successful answer includes references to valid, retrieved documents/chunks.
3. When evidence is insufficient, the answer clearly states that the available information is not enough.
4. Citation validation is deterministic and fails if the answer references sources that were not retrieved.
5. Generation providers and prompts are hidden behind Application ports.

## Affected Components
- `src/DevOpsAI.Rag.Application/Chat/`
- `src/DevOpsAI.Rag.Application/Ports/`
- `src/DevOpsAI.Rag.Infrastructure/Generation/`
- `src/DevOpsAI.Rag.Infrastructure/Prompts/`
- `docs/prompts/runbook-chatbot/`

## Dependencies
- Depends on `P2-06`.
- Requires retrieval candidates, prompts, and a generation format compatible with citations.

## Expected Tests
- Unit tests for grounding and citation validation.
- Insufficient-evidence cases.
- Cases with valid references and references to unretrieved sources.

## Definition of Done
- Answer generation is separated from retrieval, and the prompt is managed as a versioned resource.
- Answers have an explicit minimum-evidence policy.
- The API can consume structured answers with verifiable references.

## Tasks / Subtasks

- [x] Define Application-owned answer-generation and prompt contracts, structured outcomes, deterministic abstention, and retrieved-citation validation.
- [x] Implement the versioned prompt assets, file prompt provider, Ollama generation adapter, configuration, and Project 2 composition-root registrations.
- [x] Add offline tests for sufficient and insufficient evidence, trusted citations, unretrieved citations, malformed model output, prompt loading, and cancellation; run solution validation and verify architecture boundaries.

## Dev Agent Record

### Implementation Plan

Keep orchestration and contracts in Project 2 Application. Skip generation when retrieval is below the existing sufficiency threshold; otherwise load versioned prompt instructions, send only the question and retrieved chunk IDs/text to the Infrastructure generator, then build references from retrieved chunk metadata after exact ID validation.

### Decisions

- Reused the configured local Ollama endpoint and timeout from the existing `Ollama` settings and introduced a separate required chat model setting (`Ollama:Chat:ModelId`). This avoids duplicate endpoint/timeout settings while allowing distinct embedding and chat models.
- Kept provider output as strict JSON (`status`, `answer`, `citedChunkIds`). Application rejects invalid statuses, empty or duplicate citations, and any citation ID not present in the retrieved candidates. References are created from retrieved domain chunks, never model-provided metadata.
- A citation confirms retrieved-source membership, not semantic entailment; the prompt restricts generation to retrieved passages and requires abstention, consistent with P2-ADR-004.
- No package or Project 1 changes were required. The model/provider remains uncalled in automated tests.

### Completion Notes

Implemented Application ports, prompt/output models, generation orchestration and explicit insufficient-evidence results. Added versioned system/format prompt assets, a file provider, an Ollama `IChatClient` adapter with strict response validation, DI/configuration, and local API prompt asset copying. Added application, infrastructure and API composition tests for grounded answers, trusted and invented citations, abstention, malformed output, cancellation, and prompt deployment.

Validation:
- `dotnet build`: passed, 0 warnings and 0 errors.
- `dotnet test devops-ai-assistant.sln`: passed, 174 tests.
- `dotnet format devops-ai-assistant.sln --verify-no-changes --include ...`: passed for all changed C# files.
- Full-solution `dotnet format devops-ai-assistant.sln --verify-no-changes --no-restore` still reports existing end-of-line diagnostics in unrelated files (for example `src/DevOpsAI.Domain/Incidents/IncidentAudience.cs`); no diagnostics were reported for changed C# files.
- `git diff --check`: passed. Project 2 boundary tests passed as part of the full suite.

### File List

- `README.md`
- `_bmad-output/planning-artifacts/project-2/P2-07-grounded-answer-generation.md`
- `docs/prompts/runbook-chatbot/system.md`
- `docs/prompts/runbook-chatbot/answer-format.md`
- `src/DevOpsAI.Rag.Api/DevOpsAI.Rag.Api.csproj`
- `src/DevOpsAI.Rag.Api/Program.cs`
- `src/DevOpsAI.Rag.Api/appsettings.json`
- `src/DevOpsAI.Rag.Application/Chat/GenerateRunbookAnswer.cs`
- `src/DevOpsAI.Rag.Application/Chat/GeneratedRunbookAnswer.cs`
- `src/DevOpsAI.Rag.Application/Chat/RunbookAnswerPrompt.cs`
- `src/DevOpsAI.Rag.Application/Chat/RunbookAnswerResult.cs`
- `src/DevOpsAI.Rag.Application/Chat/RunbookAnswerStatus.cs`
- `src/DevOpsAI.Rag.Application/Chat/RunbookPromptEvidence.cs`
- `src/DevOpsAI.Rag.Application/Chat/RunbookPromptInstructions.cs`
- `src/DevOpsAI.Rag.Application/Ports/IRunbookAnswerGenerator.cs`
- `src/DevOpsAI.Rag.Application/Ports/IRunbookPromptProvider.cs`
- `src/DevOpsAI.Rag.Infrastructure/DependencyInjection.cs`
- `src/DevOpsAI.Rag.Infrastructure/Generation/OllamaChatOptions.cs`
- `src/DevOpsAI.Rag.Infrastructure/Generation/OllamaRunbookAnswerGenerator.cs`
- `src/DevOpsAI.Rag.Infrastructure/Prompts/FileRunbookPromptProvider.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/Chat/GenerateRunbookAnswerTests.cs`
- `tests/DevOpsAI.Rag.IntegrationTests/RagApiSmokeTests.cs`
- `tests/DevOpsAI.Rag.UnitTests/Generation/OllamaRunbookAnswerGeneratorTests.cs`
- `tests/DevOpsAI.Rag.UnitTests/Prompts/FileRunbookPromptProviderTests.cs`

### Change Log

- 2026-11-25: Implemented P2-07 grounded answer generation, deterministic citation validation, abstention, and offline verification.

## Status
review
