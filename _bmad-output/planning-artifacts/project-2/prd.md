---
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02b-vision
  - step-02c-executive-summary
  - step-03-success
  - step-04-journeys
  - step-05-domain
  - step-06-innovation-skipped
  - step-07-project-type
  - step-08-scoping
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-12-complete
inputDocuments:
  - AGENTS.md
  - ARCHITECTURE.md
  - PROJECT-1-ARCHITECTURE.md
  - _bmad-output/planning-artifacts/project-1/README.md
  - _bmad-output/planning-artifacts/project-1/P1-01-solution-skeleton.md
  - _bmad-output/planning-artifacts/project-1/P1-02-domain-contracts.md
  - _bmad-output/planning-artifacts/project-1/P1-03-application-use-case.md
  - _bmad-output/planning-artifacts/project-1/P1-04-prompt-assets.md
  - _bmad-output/planning-artifacts/project-1/P1-05-ai-adapter.md
  - _bmad-output/planning-artifacts/project-1/P1-06-http-api.md
  - _bmad-output/planning-artifacts/project-1/P1-07-automated-tests.md
  - _bmad-output/planning-artifacts/project-1/P1-08-observability-hardening.md
  - _bmad-output/planning-artifacts/project-1/P1-09-documentation.md
  - src/DevOpsAI.Application/AI/IAiTextGenerator.cs
  - src/DevOpsAI.Application/AI/AiGenerationException.cs
  - src/DevOpsAI.Application/AI/AiServiceCollectionExtensions.cs
  - src/DevOpsAI.Application/Incidents/IncidentPolisher.cs
  - src/DevOpsAI.Application/Prompts/IPromptProvider.cs
  - src/DevOpsAI.Infrastructure/AI/AiOptions.cs
  - src/DevOpsAI.Infrastructure/AI/AiServiceCollectionExtensions.cs
  - src/DevOpsAI.Infrastructure/AI/MicrosoftExtensionsAiTextGenerator.cs
  - src/DevOpsAI.Infrastructure/Prompts/FilePromptProvider.cs
  - src/DevOpsAI.Api/ApiPromptProvider.cs
  - src/DevOpsAI.Api/DependencyInjection.cs
  - src/DevOpsAI.Api/Program.cs
  - docs/prompts/incident-polisher/system.md
  - docs/prompts/incident-polisher/leadership.md
  - docs/prompts/incident-polisher/customer.md
documentCounts:
  productBriefs: 0
  research: 0
  projectDocs: 3
  project1PlanningArtifacts: 10
  implementationFiles: 12
workflowType: prd
status: complete
approval_status: approved
approvedAt: 2026-11-11
project_name: DevOps AI Assistant - Project 2: Runbook Chatbot (RAG)
user_name: Francisco Dreher
date: 2026-11-11
classification:
  projectType: api_backend
  domain: developer_tool
  complexity: medium
  projectContext: brownfield
---

# Product Requirements Document — Runbook Chatbot (RAG)

**Author:** Francisco Dreher

**Date:** 2026-11-11

## Executive Summary

Project 2 extends the DevOps AI Assistant with an API for querying technical documentation and operational runbooks using Retrieval-Augmented Generation (RAG). Its primary user is a developer or operator who needs to find procedures and operational knowledge in Markdown and TXT files and receive concise, traceable answers that support prudent decision-making.

The MVP prioritizes hands-on RAG learning and reproducible local execution. It manages documents in a local collection through explicit ingestion, update and deletion operations; processes and indexes them; retrieves relevant passages for a question; and generates a grounded answer that cites retrieved sources. When evidence is insufficient, the system must say so clearly and avoid filling gaps with unsupported knowledge. This is a query service; it does not perform operational actions.

### Differentiator

Fluent wording alone is not a successful answer: the response must be traceable to identifiable document passages. Explicit separation of retrieval and generation makes each stage independently learnable, testable and improvable. The architecture must allow provider and storage adapters to be replaced, while the MVP keeps the local path simple and avoids distributed infrastructure.

## Project Classification

- **Type:** API backend.
- **Domain:** DevOps developer/operations tool.
- **Complexity:** Medium; ingestion and retrieval require validation and evaluation, although the domain is not regulated.
- **Context:** Brownfield; a new capability in the existing .NET repository, without modifying Project 1.

## Success Criteria

### User Success

- A developer can add Markdown/TXT documents to a local collection and query them over HTTP.
- For answerable questions in the test corpus, the response is useful, based on retrieved passages, and includes references that locate the documents.
- When evidence is insufficient, the system states that it cannot answer from the available sources instead of generating unsupported factual claims.
- After a user replaces or deletes a document, subsequent queries reflect the current version and do not return stale passages.

### Project Success

- The MVP demonstrates the complete RAG lifecycle locally and reproducibly, without mandatory cloud services.
- Decisions and components support migration of embedding and vector-storage adapters to Azure without moving provider SDKs into Domain or Application.
- Project 1 behavior is neither expanded nor broken; reuse is limited to components whose responsibilities and contracts genuinely fit.

### Technical Success

- Ingestion, processing, retrieval and generation are distinguishable and verifiable stages.
- Every successful grounded answer contains references to one or more retrieved sources; citations are never fabricated.
- Deleting a source also deletes or invalidates its passages and vector representations.
- Automated tests run without real keys, a real LLM or external network access.
- Offline retrieval metrics are computed against a question set with known relevance; numeric quality thresholds are set when that dataset is created and before MVP versions are evaluated.

### Verifiable MVP Outcomes

1. An end-to-end test with Markdown and TXT files confirms ingestion, cited querying, updating and deletion.
2. Grounding tests confirm that uncovered questions return an insufficient-evidence result and no fabricated citations.
3. Local integration tests confirm that restarting the process preserves the index and document references, consistent with the persistence design.
4. A repeatable evaluation reports Recall@k/MRR (or agreed equivalent metrics) and the rate of answers with valid citations; production-quality claims are not based on arbitrary thresholds before a baseline is measured.

## Product Scope

### MVP

- Chat API for querying a local document collection.
- Markdown and TXT as the initial document formats.
- Explicit document ingestion, replacement/update and deletion.
- Text extraction, deterministic chunking, embeddings, indexing and local persistence.
- Semantic top-k retrieval with a configurable relevance threshold and bounded context construction.
- Responses based only on retrieved context, with citations identifying the document and passage.
- Explicit insufficient-information response when relevant evidence is unavailable.
- Documented REST API, error handling, safe logging, and offline retrieval/generation tests.
- Replaceable chat and embedding providers through adapters; the MVP implementation is configurable and documented.

### Post-MVP Growth

- Automated ingestion from repositories, incremental synchronization or watched folders.
- PDF, HTML, Word, wiki connectors and crawling.
- Advanced hybrid/semantic retrieval, reranking and multi-query retrieval.
- Authentication, authorization, multitenancy, permission filters and access auditing.
- Continuous evaluation, user feedback, distributed observability and cost optimization.
- Production deployment in Azure using managed services and horizontal scaling.

### Future Vision

A reliable, governed operational-knowledge capability that can serve later assistant projects while keeping retrieval and answers explainable and avoiding operational actions. Tool calling and MCP are outside Project 2.

## User Journeys

### Query with Sufficient Evidence

An on-call operator has several Markdown runbooks and TXT notes in a local collection but cannot recall which procedure applies to a symptom. They submit a natural-language question through the API. The system retrieves relevant passages, builds bounded context and generates a concise answer with references containing identifiers and locations. The operator opens the sources to verify the procedure before acting. The system helps find knowledge; it does not run commands or replace operational judgment.

**Capabilities revealed:** HTTP query, semantic retrieval, bounded context, navigable citations, no action execution.

### Query without Sufficient Evidence

A developer asks about a procedure that is absent from the indexed documents. The search results do not meet the relevance criterion. Rather than improvising from general knowledge, the system reports that it cannot answer from the available sources, includes sources only if they are genuinely relevant, and lets the user refine the question or add documentation.

**Capabilities revealed:** relevance threshold/abstention, strict grounding, no fabricated citations, structured response distinguishing insufficient evidence from errors.

### Collection Maintenance

The service operator adds a file, re-ingests the same document ID after editing its content, or requests its deletion. The API validates the format and limits, reports processing results, and keeps the source aligned with its chunks and embeddings. If processing fails, the user receives an explicit error and the collection is not left partially updated or silently inconsistent.

**Capabilities revealed:** explicit ingestion/update/deletion, stable document identity, defined idempotency, processing status and index consistency.

### API Consumer and Troubleshooting

A developer integrates a client with the query endpoint and encounters a provider failure or a citation they cannot locate. They use the stable REST contract, error code and trace ID to distinguish invalid input, insufficient evidence, incomplete configuration or a transient failure. Logs contain metrics and minimal technical references, not document bodies, prompts or secrets.

**Capabilities revealed:** OpenAPI, stable error contract, health checks, safe structured logs, correlation and deterministic integration tests.

### Journey-Derived Requirements Summary

- Distinguish successful answers, insufficient evidence and technical errors.
- Identify each citation's source, version/hash and reproducible location.
- Prevent document updates from mixing results across versions.
- Ensure deletion invalidates derived source data.
- Verify citations against the retrieved set without trusting free-form model text.
- Provide documented contracts and observability without logging sensitive content.

## Domain-Specific Requirements

### Operational Integrity

- Runbooks are untrusted reference material: they may be outdated, contradictory or contain dangerous instructions. The system retrieves and summarizes only; it does not execute instructions or elevate privileges.
- Responses identify their sources and make clear that operators must verify a procedure's applicability and currency.
- The model cannot determine authorization or turn retrieved text into an automatic action.

### Security and Privacy

- The MVP operates on a local collection supplied by the operator. File-size and file-count limits prevent accidental resource exhaustion.
- Documents, queries, retrieved content, provider responses and citations are treated as untrusted input.
- By default, logs do not include document bodies, full questions, prompts, answers or secrets.
- The MVP has no multi-user isolation and must not be exposed to untrusted networks without additional controls.

### Domain Boundaries

- No industry-specific regulatory compliance is assumed; the product is not represented as a certified decision system.
- Updates and deletion must keep source documents and derived data (chunks/embeddings) consistent; failures must be visible and recoverable.
- Documents may contain malicious instructions or prompt injection; source content is cited data, not system instructions.

## API Backend-Specific Requirements

### MVP HTTP Interfaces

- `PUT /api/documents/{documentId}` accepts a Markdown/TXT file and minimal metadata, validates it, and ingests or atomically replaces the document identified by the route.
- `DELETE /api/documents/{documentId}` deletes the document and its derived data.
- `GET /api/documents` lists identifiers and metadata without exposing full text.
- `POST /api/chat` accepts a question and returns an answer, grounding status and citations.
- `GET /health` reports basic process health and required operational dependencies.
- OpenAPI documents schemas, limits, errors and response statuses.

### Contracts and Operations

- JSON is used for chat and administrative interactions; document upload uses multipart/form-data or an equivalent documented mechanism.
- Document identity is explicit, stable and safe to use as a route identifier. Re-ingesting the same ID replaces its version and prevents duplicates.
- Authentication is not included in the local MVP; document that the service is not suitable for public exposure.
- Errors have a stable code, safe message and trace ID. “Insufficient evidence” is a valid use-case response, not an HTTP failure.
- Enforce and document configurable limits for file size, question length, retrieved tokens/chunks and provider timeouts.
- Streaming, client SDKs and distributed versioning are excluded at this stage; `/api` leaves room for future API versioning.

## Scope and Phased Development

### MVP Strategy

**Approach:** A learning MVP that can be validated against a real task: answering questions about a controlled corpus with inspectable evidence. Focus on a simple end-to-end path rather than maximizing features or preparing for premature scale.

**MVP users:** one local operator/developer who ingests documents and queries the collection, plus a technical API consumer. No accounts, roles or user separation.

### Phase 1 — MVP

- One local ASP.NET Core service in the repository.
- Individual Markdown/TXT ingestion with validation, metadata, hash/versioning and consistent replacement.
- Plain-text/Markdown parsing without executing HTML or commands.
- Deterministic chunking, embeddings, persistent local metadata/chunk/vector storage and top-k search.
- Chat endpoint with bounded context, versioned prompt, grounding, insufficient-evidence threshold and verifiable citations.
- Document listing and deletion; explicit errors and recoverable update behavior.
- Options/environment configuration, input limits, redacted logs, health and OpenAPI.
- Unit/integration tests without network, evaluation corpus and local setup guide.

### Phase 2 — Growth

- Batch ingestion and synchronization from a directory or repository.
- Additional formats, metadata filters, hybrid search and reranking.
- Retrieval improvements driven by evaluation results and feedback.
- Identity and authorization options for shared environments, auditing and retention policy.
- Azure adapters for embeddings and vector search/storage; deployment and managed secrets.

### Phase 3 — Expansion

- Multitenant collections, large-scale incremental indexing, high availability and independent scaling when justified by evidence.
- Additional knowledge integrations and source-level access controls.
- Integration with later roadmap capabilities only under approved scope; tool calling and MCP are not part of this project.

### Scope Risks and Mitigations

- **Variable RAG quality:** create a small gold corpus before tuning parameters; evaluate retrieval and grounding separately.
- **Provider cost/latency:** bound tokens/chunks, configure timeouts and record durations without content; reuse embeddings per document version.
- **Update desynchronization:** use atomic operations or explicit ingestion state; ensure derived data is invalidated/deleted and covered by regression tests.
- **Malicious document instructions:** treat them as data, bound context and instruct the generator not to obey corpus content.
- **Unauthenticated local service exposure:** bind locally by default and show a warning; do not consider the service Internet-ready until access controls are added.

## Functional Requirements

### Document Management

- FR1: Operators can ingest Markdown and TXT documents into a collection.
- FR2: The system validates document type, size, identifier and content before accepting a document.
- FR3: Operators can identify a document by a stable identifier and retrieve its ingestion metadata.
- FR4: The system can replace an existing document version without leaving passages from the previous version active.
- FR5: The system can reject unsupported or invalid files with an explicit error without corrupting the collection.
- FR6: Operators can list collection documents and their metadata/status without exposing full text.
- FR7: Operators can delete a document and all derived data that makes it searchable.
- FR8: The system reports processing failures and allows operators to retry or replace a failed ingestion.

### Query and Retrieval

- FR9: Consumers can submit a text question to query a selected collection.
- FR10: The system can retrieve passages relevant to a question and limit how many passages contribute to an answer.
- FR11: The system can filter results by relevance and declare evidence insufficient when results fail the configured criterion.
- FR12: The system can build answer context only from content retrieved from collection documents.
- FR13: The system can return a response distinguishing a grounded answer, insufficient evidence and a processing error.
- FR14: The system can abstain from a factual answer when documents do not contain sufficient information.

### Grounded Answers and Sources

- FR15: Consumers can receive references for every source used in an answer.
- FR16: Each reference identifies its source document and a reproducible location within it.
- FR17: The system can emit citations only for passages actually retrieved for that query.
- FR18: Consumers can distinguish sources used as evidence from documents that do not support the answer.
- FR19: The system treats retrieved text as untrusted content and does not execute instructions found in documents.

### Configuration and Operations

- FR20: Operators can configure answer-generation and embedding providers without changing use cases.
- FR21: Operators can start and run the local service by following versioned documentation.
- FR22: Consumers can check service health.
- FR23: Consumers can access documented API contracts for requests, responses and errors.
- FR24: The system returns stable, safe errors that distinguish invalid input, provider configuration/failure and internal failures.
- FR25: The system propagates a correlation identifier in responses and observability events.

### Quality Verification

- FR26: The team can run tests without real credentials, external network access or a real LLM provider.
- FR27: The team can run a reproducible retrieval evaluation using questions and documents with known relevance.
- FR28: The team can automatically verify citation validity and abstention behavior against evaluation cases.
- FR29: The team can replace external providers with deterministic doubles in application and API tests.

## Non-Functional Requirements

### Performance and Limits

- NFR1: Local retrieval over a 10,000-chunk evaluation collection must complete in under 500 ms in the documented reference environment, excluding LLM calls.
- NFR2: Generation must use configurable timeouts and cancellation; requests that exceed the configured limit must terminate with a safe, observable error.
- NFR3: Maximum file size, question length, chunks per query and context tokens must be configurable and validated before external resources are consumed.
- NFR4: Latency evaluations must report parsing/ingestion, embeddings, retrieval and generation separately so external latency is not attributed to local components.

### Security and Privacy

- NFR5: In local configuration, the API binds to loopback by default; documentation warns against exposure without authentication and access controls.
- NFR6: Logs and errors exclude API keys, tokens, document bodies, prompts, full questions and generated answers.
- NFR7: The system validates and limits file and request inputs to reduce resource abuse and path traversal.
- NFR8: Sources, retrieved results and model output are untrusted data; document instructions cannot alter policy, configuration or authorization.
- NFR9: Providers and local storage are configured through options/safe environment variables; secrets are not stored in versioned files.

### Reliability and Integrity

- NFR10: Failed ingestion or replacement must be explicit and must not leave a partially visible active version.
- NFR11: After document deletion, no subsequent query can retrieve its active passages.
- NFR12: Local persistence survives normal service restarts; storage errors do not become empty collections that appear successful.
- NFR13: Missing configuration or unavailable providers result in a controlled failure without fabricated factual answers.

### Maintainability and Portability

- NFR14: Domain references no presentation, persistence, Azure or AI SDK frameworks; Application depends only on Domain and its own ports.
- NFR15: The MVP builds and runs locally on .NET 10 using documented reproducible steps.
- NFR16: Replacing providers or persistence does not require changing use-case contracts or Domain types.
- NFR17: Local unit and integration tests do not depend on the Internet, secrets or real cloud services.

### Observability

- NFR18: Every HTTP request has a trace/correlation ID; duration, outcome and stage failures are logged in structured form without sensitive payloads.
- NFR19: Health checks distinguish API availability from optional or unconfigured external dependencies.

## MVP Acceptance Criteria

1. **Local execution:** A developer can follow the documentation from a clean checkout to build, run the API and execute all tests without mandatory cloud credentials.
2. **Ingestion and persistence:** An integration test ingests Markdown and TXT fixtures, verifies metadata/status, restarts the process and confirms the collection remains available.
3. **Validation:** Unsupported types, empty files, exceeded limits and unreadable content return explicit errors; the previous document state remains consistent.
4. **Update:** Re-ingesting the same identifier changes its version, and retrieval no longer includes chunks from the previous version.
5. **Deletion:** After deleting a document, no subsequent query retrieves its passages.
6. **Grounded answer:** With a deterministic test provider and known corpus, returned citation IDs belong to the retrieved document/chunk set; invented citations are rejected or omitted.
7. **Abstention:** For questions absent from the corpus or results below threshold, the response has status `insufficient_evidence`, makes no claims as grounded facts and invents no citations.
8. **Provider failure:** Timeout, cancellation, invalid output or missing provider configuration produce a safe, observable error rather than an empty success.
9. **Evaluation:** A documented command/test runs the evaluation dataset and reports retrieval Recall@k and MRR, citation coverage/validity and abstentions without external services.
10. **Performance limits:** A controlled test measures retrieval below 500 ms for 10,000 chunks on the documented reference hardware, excluding query embeddings and generation.
11. **Security:** Logging/error tests confirm the absence of secrets and document content; local configuration binds to loopback by default.
12. **Regression:** `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` pass for the solution without altering Project 1 contracts or behavior.

## Explicit Constraints and Exclusions

- Markdown and TXT only; ingestion is explicitly initiated, with no filesystem watcher.
- One local collection per installation; no tenant, authentication, document permissions or guaranteed concurrent writers.
- No frontend, persisted multi-turn conversations, streaming, UI feedback, batch ingestion, OCR or external connectors.
- No DevOps actions, script execution, tool calling or MCP.
- Azure deployment is not required for MVP acceptance; migration is a design goal, not a cloud deliverable for this phase.
- No promise of absolute factual accuracy: answers are limited to retrieved evidence and must be able to abstain.
