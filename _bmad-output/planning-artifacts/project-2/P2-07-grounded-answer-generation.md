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
