# P2-09 - Chat query API and insufficient-evidence handling

- ID: P2-09
- Title: Chat query API and insufficient-evidence handling
- Type: User Story

## Description
As a technical support user, I need to query the corpus in natural language and receive an answer based on retrieved evidence with verifiable references, or a clear abstention when the information is unavailable.

## Objective
Expose the query and answer layer over HTTP, incorporating retrieval, grounded generation, and insufficient-evidence handling.

## Technical Context
The chat API is the MVP's core value: it must retrieve relevant chunks and generate a useful, safe answer without inventing facts. The policy must clearly define what happens when there are not enough passages to answer a question.

## Scope
- Define an endpoint for `POST /api/chat/query` (or equivalent) with a question and user context.
- Integrate the `AskRunbookQuestion` use case with retrieval and grounded generation.
- Define a response structure containing the final answer, citations, evidence assessment, and result status.
- Establish an abstention pattern when evidence is insufficient or chunks do not cover the question.
- Ensure the transport does not reveal secrets or call external providers during tests without configuration.

## Verifiable Acceptance Criteria
1. A query with sufficient evidence returns an answer accompanied by references to retrieved documents/chunks.
2. A query without sufficient evidence returns an explicit insufficient-information status and generates no unsupported facts.
3. The API returns structured results with system status and verifiable references.
4. Answers contain only information supported by the loaded corpus.
5. Transport and DTOs contain no Infrastructure or provider implementation details.

## Affected Components
- `src/DevOpsAI.Rag.Api/Endpoints/`
- `src/DevOpsAI.Rag.Api/Contracts/`
- `src/DevOpsAI.Rag.Application/Chat/`
- `src/DevOpsAI.Rag.Infrastructure/Generation/`

## Dependencies
- Depends on `P2-06`, `P2-07`, and `P2-08`.
- Requires the retrieval pipeline, grounded generation, and document API.

## Expected Tests
- Endpoint integration tests with a controlled corpus.
- Successful-answer and insufficient-evidence cases.
- Verification that citations are present and valid.

## Definition of Done
- Chat queries return verifiable evidence and make clear decisions about insufficient evidence.
- The API presents a consistent contract to HTTP clients.
- The final result is confirmed not to depend on unretrieved external knowledge.
