---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
  - 7
  - 8
inputDocuments:
  - _bmad-output/planning-artifacts/project-2/prd.md
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
  - src/DevOpsAI.Application/Incidents/IncidentPolisher.cs
  - src/DevOpsAI.Application/Prompts/IPromptProvider.cs
  - src/DevOpsAI.Infrastructure/AI/MicrosoftExtensionsAiTextGenerator.cs
  - src/DevOpsAI.Infrastructure/AI/AiOptions.cs
  - src/DevOpsAI.Infrastructure/AI/AiServiceCollectionExtensions.cs
  - src/DevOpsAI.Infrastructure/Prompts/FilePromptProvider.cs
  - src/DevOpsAI.Api/ApiPromptProvider.cs
  - src/DevOpsAI.Api/DependencyInjection.cs
  - src/DevOpsAI.Api/Program.cs
  - docs/prompts/incident-polisher/system.md
  - docs/prompts/incident-polisher/leadership.md
  - docs/prompts/incident-polisher/customer.md
workflowType: architecture
project_name: DevOps AI Assistant - Project 2: Runbook Chatbot (RAG)
user_name: Francisco Dreher
date: 2026-11-11
lastStep: 8
status: complete
approval_status: approved
approvedAt: 2026-11-11
completedAt: 2026-11-11
---

# Architecture Decision Document

## Project Context Analysis

### Requirements Overview

**Functional requirements:** The PRD defines 29 FRs across document lifecycle (FR1–FR8), query and retrieval (FR9–FR14), grounded responses and citations (FR15–FR19), configuration/operation (FR20–FR25), and quality verification (FR26–FR29). Architecturally, the core flow is document ingestion and versioning, text extraction, chunking, embedding, persistent indexing, query embedding, relevance-filtered retrieval, context assembly, generation, and deterministic citation validation. Administrative document operations and chat are separate use cases over one local collection.

**Non-functional requirements:** 19 NFRs address retrieval performance (10,000 chunks under 500 ms in a documented reference environment, excluding query embedding/generation), input and context limits, safe local binding, secret/content redaction, atomic updates, persistence across restart, adapter portability, deterministic offline tests, and per-stage observability.

**Project scale:** Medium learning/demo MVP: one .NET 10 modular monolith, one local collection, Markdown/TXT ingestion, synchronous HTTP requests, no authentication, UI, multi-tenancy, distributed jobs, or cloud dependency.

- Primary domain: DevOps knowledge retrieval API.
- Complexity: Medium due to end-to-end RAG quality, source consistency and grounding safety; no regulated domain identified.
- Architectural components: one API host; project-local Domain, Application, Infrastructure and test assemblies; separate adapters for chat generation, embeddings, vector/document persistence and prompt loading only where each has a concrete responsibility.

### Technical Constraints & Dependencies

- Preserve the existing solution conventions, centralized package management, .NET 10, Clean Architecture and current test framework.
- Domain stays technology-free. Application owns use cases, request/result contracts, policy and ports; Infrastructure implements persistence, embedding, generation and filesystem/provider adapters; API handles HTTP and composition.
- Keep Project 1 API, projects, contracts and behavior unchanged. Do not reference the Project 1 API from Project 2.
- Project 1's `IAiTextGenerator` is a narrow text-generation port, but its API is currently exposed through the existing `DevOpsAI.Application` assembly alongside incident-specific logic and its DI extension is not a suitable direct composition boundary. Do not force RAG query/context contracts into that interface. Reuse shared project components only if implementation inspection proves clean ownership and no incident coupling; default to Project 2-local ports/adapters.
- The runtime must support local development without mandatory Azure services. Any external model provider remains configurable and optional to test execution. Data persistence must be local and survive process restart.
- Provider-specific model SDKs must remain in Infrastructure. Prompt assets remain versioned under `docs/prompts/`.
- No tool calling, MCP, action execution, microservices, or premature shared framework.

### Cross-Cutting Concerns

- **Grounding and safe output:** retrieved text and generated response are untrusted; only retrieved chunk IDs may be cited; low-relevance evidence must trigger deterministic abstention.
- **Document consistency:** updates and deletes must keep source metadata, chunks, and vectors aligned; active readers must not see a partially indexed replacement.
- **Privacy/security:** bind loopback by default; bound uploads and context; redact document/question/prompt/answer payloads; do not treat prompt instructions as authorization.
- **Portability:** local persistence and embedding/generation providers must sit behind Application-owned ports so an Azure adapter can be added without changing use-case/domain contracts.
- **Evaluation:** retrieval quality and answer/citation behavior need distinct, repeatable offline checks, separate from integration tests against external providers.
- **Operations:** trace each request and record stage duration/outcome without logging sensitive content; clearly distinguish insufficient evidence from provider/storage failures.

## Architecture Workflow Analysis

The PRD and the existing architecture spine agree on Clean Architecture, .NET 10, Minimal APIs, `Microsoft.Extensions.AI`, local-first learning and Azure migration as a later adapter/deployment choice. The primary design trade-off is minimizing local setup friction while keeping vector operations durable and replaceable. The implementation should begin with a single service and explicit ports, not a network of services or a generic RAG framework.

## Starter Template Evaluation

### Primary Technology Domain

ASP.NET Core API backend, matching the repository's existing .NET 10 solution.

### Options Considered

| Option | Benefits | Trade-offs | Decision |
|---|---|---|---|
| Generate a new standalone .NET Web API solution | Fast scaffold; built-in Minimal API and OpenAPI templates | Duplicates solution, package, analyzer and test setup; risks drifting from the Project 1 baseline | Rejected |
| Add a project-specific set of .NET projects to the existing solution | Reuses the established repository build, test and formatting policy; keeps the RAG API independently runnable | Requires deliberate project boundaries and references rather than template defaults | Selected |
| Introduce a community RAG starter/framework | May provide connectors and orchestration quickly | Adds abstractions and provider choices beyond current MVP needs; obscures learning and introduces package coupling | Rejected |

### Selected Starter: Existing Solution + .NET SDK project templates

No separate starter repository or full-solution generator is needed. The first implementation stories should add only the Project 2-owned projects to `devops-ai-assistant.sln`, using `dotnet new classlib` and `dotnet new webapi` (Minimal API) as scaffolding where useful, then align generated files with repository conventions.

**Rationale:** Existing solution already provides .NET 10, nullable reference types, analyzer and warning policy, central package management, OpenAPI conventions and xUnit/NSubstitute test projects. Reuse reduces setup and prevents Project 2 from becoming a duplicate repository or altering Project 1.

**Initialization command:** No single scaffold command. Use narrowly scoped .NET SDK templates and `dotnet sln add` during the skeleton task. Verify installed SDK with `dotnet --list-sdks`; do not install or retarget as part of this architecture task.

**Architectural decisions provided by the existing starter:**

- **Language/runtime:** C# / .NET 10, nullable enabled, existing repository target.
- **Build tooling:** SDK-style projects, central package versions, analyzers, warnings as errors, `dotnet format`.
- **Testing:** xUnit, NSubstitute and ASP.NET Core integration testing already present.
- **Code organization:** Clean Architecture project boundaries already established for Project 1.
- **Development experience:** Existing solution/build commands, Minimal API/OpenAPI patterns and test host.

**Version policy:** Keep target framework and common package versions aligned with the repository's `Directory.Build.props` and `Directory.Packages.props`; do not upgrade unrelated packages. Public NuGet metadata checked during architecture planning reports latest stable majors `Microsoft.Extensions.AI 11.0.0`, `Microsoft.EntityFrameworkCore.Sqlite 11.0.0`, `OllamaSharp 5.5.2`, and `Qdrant.Client 1.20.0`. The latest compatible .NET 10 lines observed were Microsoft.Extensions.AI 10.8.0 and EF Core SQLite 10.0.15. OllamaSharp 5.5.2 targets net10.0 and depends on Microsoft.Extensions.AI 10.8.0. Therefore, align the shared Microsoft.Extensions.AI central version to 10.8.0 only in the implementation task and run all Project 1 tests/build before accepting that dependency update; if it causes regressions, use an Infrastructure-local Ollama HTTP adapter rather than changing Project 1 behavior. Do not copy latest major versions blindly.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical decisions made for the MVP**

1. Project 2 is a separate API and set of project-local layers; it does not depend on Project 1 application or API assemblies.
2. Keep the system as one modular monolith, hosted by ASP.NET Core Minimal API, with synchronous API-driven ingestion and query.
3. Use a single local SQLite database for document metadata, raw Markdown/TXT, chunks, and embedding vectors; perform a bounded cosine-similarity scan in the local Infrastructure adapter for the MVP corpus (10,000 chunk acceptance target).
4. Use Ollama as the local chat and embedding runtime, reached through an Infrastructure adapter; keep Application contracts provider-neutral. Azure OpenAI/AI Foundry and Azure AI Search are future adapters, not MVP requirements.
5. Grounded generation uses structured output with retrieved chunk IDs. Application code validates JSON shape, required answer, non-empty citations, citation membership in retrieved IDs, and retrieval sufficiency. It returns `insufficient_evidence` before generation when retrieval scores do not meet a calibrated threshold.
6. Document replacement is staged: parse and embed the new version before an atomic SQLite transaction replaces the active document, chunks and vectors. A failed operation leaves the previous active version intact. Delete removes source and derived records transactionally.

**Important decisions**

- Text extraction supports UTF-8 Markdown/TXT, preserves line ranges and Markdown heading metadata, and does not execute or render embedded content.
- Chunking is deterministic and structure-aware: split on headings/paragraph boundaries first; pack into a target of approximately 1,000 characters with approximately 150 characters of overlap; preserve document version, chunk ordinal, heading and original line range. These initial values are configurable and must be tuned against the evaluation corpus.
- Embedding model identity and dimensions are stored with every index generation. Changing model or dimensions requires a full re-embedding/reindex; mixed embedding spaces are never compared.
- Query retrieval defaults to top 5 chunks, deduplicates overlaps and caps total context size. Relevance threshold and top-k are options calibrated with the gold dataset rather than assumed to be universal constants.
- Chat model receives only the question, retrieved evidence and versioned system instructions. Evidence is delimited as untrusted data; system instructions explicitly prohibit following instructions embedded in runbooks.
- `PUT /api/documents/{documentId}` is an idempotent upsert. `GET /api/documents`, `DELETE /api/documents/{documentId}`, `POST /api/chat` and `GET /health` are the API surface.
- No auth in local MVP; bind to loopback by default and do not deploy publicly.

**Deferred decisions**

- Cloud deployment topology, Azure AI Search schema/SKU, Blob Storage use, identity and access policy are deferred to a production/Azure story.
- Exact Ollama model names, dimensions, quantization and supported hardware depend on the target machine and must be selected by benchmark during implementation; configuration is explicit and must not silently switch model.
- Numeric relevance threshold is selected from evaluation data, not a model-generated confidence value.
- SQLite migration/versioning tool and backup/restore UX can be selected in the persistence story; database schema versioning is required before persistent data is released.
- Authentication, multi-tenant data isolation, network exposure and production service-level objectives are outside MVP.

### Data Architecture

#### Alternatives

| Option | Local simplicity/cost | Azure path | Trade-off |
|---|---|---|---|
| SQLite + EF Core + vector BLOBs and in-process cosine scan | Simplest: one file, no Docker/service, no infrastructure cost; predictable for a capped 10k-chunk learning corpus | Replace the persistence/search adapter with Azure AI Search; add Blob Storage only when document scale/lifecycle needs it | Linear scan and local single-writer semantics; must benchmark the explicit 500 ms target |
| Qdrant local container | Mature vector indexing and metadata filters; local requires Docker and a separate service | Azure AI Search is a different service/API; migration requires adapter and reindex | Better for larger vectors/corpora but increases local setup and operating components |
| PostgreSQL + pgvector | Familiar relational/vector stack and good SQL filtering; local container/service required | Azure Database for PostgreSQL with pgvector is a closer data migration path than Azure AI Search | More operations than MVP needs and a less direct fit to the repository's Azure AI Search direction |
| Azure AI Search from day one | No local-only runtime; cloud cost and credentials required | Already the target service | Conflicts with local-first, offline tests and learning cost goals |

#### Decision

Use SQLite through EF Core for durable MVP data and a project-owned local vector search implementation that computes cosine similarity against stored normalized embedding vectors. EF Core is already part of the repository's recommended local data stack; SQLite is compact and durable. At 10,000 chunks a full scan is simple enough to teach the retrieval mechanics and should meet the measurable target, but the benchmark is a release gate. If it does not, first profile and optimize the local adapter; do not silently change the product scope.

Store original text, filename, content hash/version, timestamps, chunks, line ranges, headings, model ID/dimensions and vector bytes in one SQLite database. Do not rely on arbitrary filesystem paths after upload. The future Azure implementation can replace the index adapter with Azure AI Search and optionally move raw content to Blob Storage; document and chunk IDs remain stable across reindexing.

### Authentication & Security

- Local-only deployment: no authentication or authorization in MVP; listen on loopback by default and explicitly document the limitation.
- Enforce upload size/type/encoding, question length, retrieval count and context/token bounds at deterministic boundaries.
- Treat runbook contents, user questions, retrieved passages and model output as untrusted. Do not permit document content to alter system policy or trigger tools/actions.
- Validate generated response structure and citations against the retrieval set. Unknown citation IDs are invalid output; never convert arbitrary model text into a source URL or path.
- Do not log request/document/prompt/response bodies, secrets or provider exceptions. Log safe identifiers, stage outcomes, duration, count and trace ID.
- Keep provider credentials in environment/user-secrets or secret store. Azure identity, network controls, auth, encryption-at-rest policy and audit design belong to the production deployment phase.

### API & Communication Patterns

Use one REST API with stable JSON contracts, OpenAPI in Development, centralized safe errors and cancellation propagation.

#### `PUT /api/documents/{documentId}`

Accept `multipart/form-data` field `file` and optional metadata. Allowed extensions: `.md`, `.txt`; content is stored as UTF-8. Repeating the same ID replaces its version atomically.

```http
PUT /api/documents/runbook-cache
Content-Type: multipart/form-data
```

Created response (`201 Created`):

```json
{
  "documentId": "runbook-cache",
  "fileName": "cache-recovery.md",
  "contentHash": "sha256:...",
  "status": "indexed",
  "chunkCount": 8,
  "createdAtUtc": "2026-11-11T12:00:00Z"
}
```

Updated response: `200 OK` with the same shape and updated version/hash. Invalid file returns `400`/`413`/`415`; provider/index failure returns safe `5xx` error and leaves the previous version active.

#### `GET /api/documents`

Returns an array of metadata only: document ID, filename, content hash/version, ingestion status, chunk count and timestamps. Does not expose raw body content.

#### `DELETE /api/documents/{documentId}`

Returns `204 No Content` when deleted; `404` when absent. Removal includes all derived chunks and vectors.

#### `POST /api/chat`

Request:

```json
{
  "question": "How do I recover the cache service after a failed deployment?"
}
```

Grounded response (`200 OK`):

```json
{
  "status": "answered",
  "answer": "Use the documented recovery procedure and verify the service health before restoring traffic.",
  "citations": [
    {
      "documentId": "runbook-cache",
      "fileName": "cache-recovery.md",
      "chunkId": "runbook-cache:v3:chunk-04",
      "heading": "Recovery",
      "startLine": 24,
      "endLine": 38
    }
  ],
  "traceId": "..."
}
```

Insufficient evidence (`200 OK`, not an HTTP error):

```json
{
  "status": "insufficient_evidence",
  "answer": "I couldn't find enough information in the indexed runbooks to answer that.",
  "citations": [],
  "traceId": "..."
}
```

Use stable error payloads with `code`, safe `message` and `traceId`. Invalid request is `400`, absent document is `404`, request conflict is `409`, payload too large is `413`, unsupported media is `415`, provider/storage failure is `503` where known, and unexpected failure is `500`. Do not expose implementation/provider details.

### Infrastructure & Deployment

- One ASP.NET Core API process, one persistent SQLite file and a local Ollama process/model are sufficient for local MVP execution. Model download is an explicit setup prerequisite; no secret is required for local Ollama.
- The service fails clearly if required Ollama model/configuration is unavailable; tests and retrieval evaluation use deterministic fakes and do not require Ollama.
- Configuration uses strongly typed `IOptions<T>` for paths, upload/query limits, chunking/retrieval parameters, timeouts, chat/embedding provider endpoints and model identifiers.
- Local startup instructions include supported .NET SDK, Ollama installation/model setup, database location, and how to reset local development data safely.
- Azure is a future deployment target: add Azure AI Search adapter, model adapter (Azure OpenAI/AI Foundry), managed identity/secrets, storage and deployment definitions only in a separately approved story. Keep the MVP free of Azure SDK references outside Infrastructure.
- No background worker or microservice for synchronous file ingestion. If later payload/embedding latency exceeds API limits, move to queued ingestion as a measured architectural change.

### Decision Impact Analysis

**Implementation sequence:** project skeleton and dependency boundary tests → domain/application contracts → deterministic parser/chunker and use cases → local SQLite schema/transactional index adapter → Ollama chat and embedding adapters → API → retrieval/grounding evaluation → observability/hardening → documentation.

**Cross-component dependencies:** embedding model and vector dimensions determine index compatibility; chunk strategy affects both retrieval metrics and citation locations; transactional replacement requires embeddings to complete before switching the active version; retrieval thresholds determine when generation is allowed; generated citation IDs must be validated against retrieval output; local Ollama setup and SQLite persistence define the local run experience.

## Implementation Patterns & Consistency Rules

### Critical Conflict Points

The main consistency risks are split ownership of RAG orchestration, inconsistent source/chunk IDs, different failure semantics across providers, citation fabrication, non-atomic document replacement, and logging sensitive prompt or document content.

### Naming Patterns

- C# namespaces and types use `DevOpsAI.Rag.*` and PascalCase. Interfaces use `I`; async I/O methods end in `Async`; use `CancellationToken` throughout.
- Use explicit names such as `AskRunbookQuestionRequest`, `AskRunbookQuestionResponse`, `ReplaceRunbookDocumentRequest`, `DocumentCitation`, and `RetrievedChunk`; avoid generic `Data`, `Manager`, `Helper`, or `Info`.
- API routes are plural resource nouns for documents and a clear use-case route for chat: `/api/documents`, `/api/documents/{documentId}`, `/api/chat`.
- JSON uses camelCase; date-times are UTC ISO-8601 strings; identifiers are opaque strings/UUIDs, never physical paths.
- EF entity/property naming follows C# PascalCase with explicitly configured stable table/column mapping in migrations. Schema changes are versioned; do not rely on implicit naming drift.

### Structure Patterns

- Organize application types by feature/use case: `Documents/`, `Chat/`, `Retrieval/`, and `Evaluation/` only where needed; do not create a generic shared `Common` layer.
- Keep HTTP binding and transport DTOs in `DevOpsAI.Rag.Api`; application commands/results and ports in `DevOpsAI.Rag.Application`; provider and SQLite implementations in `DevOpsAI.Rag.Infrastructure`; pure concepts/policies in `DevOpsAI.Rag.Domain`.
- Unit tests live in existing style-specific test projects; Project 2 test classes use `Runbook`/`Rag` feature folders. No tests are added to or modify Project 1-specific behavior.
- Prompts live in `docs/prompts/runbook-chatbot/`; representative Markdown/TXT corpus and evaluation dataset live in `tests`/fixture assets and contain synthetic, non-sensitive data.
- Composition root is the Project 2 API. Application has no dependency on Infrastructure or ASP.NET Core.

### Format Patterns

- Success responses use explicit response contracts, not an arbitrary `{ data: ... }` wrapper. Error responses match repository convention: `{ "code": "...", "message": "...", "traceId": "..." }`.
- Chat response has `status` (`answered` or `insufficient_evidence`), `answer`, `citations[]`, and `traceId`. An insufficient-evidence response is HTTP 200; malformed request is 400; absent document 404; size/type 413/415; known dependency outage 503; unexpected failure 500.
- Citation response is built from trusted retrieved metadata, not copied from model-provided filenames/URLs. The model may return only chunk IDs; Application validates all IDs against the current query's retrieved set and maps IDs to persisted metadata/line ranges.
- Upload upsert distinguishes new vs existing: 201 for new document and 200 for replacement. Delete returns 204 or 404.
- All API request and response types are explicit. Do not serialize EF entities, vector arrays, filesystem paths, or provider-specific objects.

### Retrieval and Generation Patterns

- Text extraction preserves source line ranges, headings and UTF-8 content hash. Chunk IDs are opaque and unique per document version; chunk order and metadata are deterministic.
- Embeddings are generated in batches where supported and stored with model ID and dimensions. Never compare vectors from different embedding models/dimensions.
- Similarity score definition and ordering are consistent (cosine similarity, descending); exclude deleted/non-active versions before ranking. Thresholds are options calibrated by evaluation, not AI output.
- Retrieve top-k, deduplicate overlapping chunks and cap total context. No retrieval evidence above threshold means abstain without calling chat generation.
- Prompt requires JSON with answer and cited chunk IDs, prohibits unsupported facts, and frames retrieved text as untrusted evidence. Validate strict JSON, required fields and citation membership; invalid model output is an explicit generation failure, not a success-shaped fallback.
- Every answer citation must resolve to a retrieved chunk with source ID, filename, version, heading and original line range. Never create citations from model-generated paths or URLs.

### Error Handling and Observability Patterns

- Expected application outcomes use typed result/error codes; provider/storage adapters translate provider exceptions to safe application errors without exposing raw exception details.
- Propagate cancellation. Configure external call timeout. Do not broadly catch or silently fall back to empty results, random embeddings or a different model.
- API owns central exception-to-HTTP mapping. The API does not expose stack traces, model responses, SQL or local file paths.
- Logs include trace ID, stage, outcome, duration, safe model/provider identifier and counts only. No raw file contents, query, context, prompt or generated answer.
- Metrics separately measure ingestion parsing/chunking, embedding, SQLite commit/search and generation.

### Test and Evaluation Patterns

- Unit tests use fakes/substitutes for embedding, chat and index ports; assertions cover validation, interactions, consistency, structured output and cancellation.
- Integration tests use temporary SQLite files and in-memory host; no external network, real provider credentials, Ollama requirement or cloud service.
- Retrieval evaluation uses a versioned synthetic gold dataset: question, relevant document/chunk IDs, expected abstention and citation evidence. Report Recall@k and MRR; define baseline and acceptance threshold only after measuring the initial dataset.
- Grounding tests verify returned citations are retrieved IDs, citations resolve to source metadata, irrelevant queries abstain, and prompt-injection content does not override system constraints. Do not assert exact generated prose.
- Include failure tests for corrupted document, unsupported encoding, duplicate upsert, failed embedding, transaction rollback, provider timeout, malformed JSON, unknown citation ID, delete and restart persistence.

### Enforcement Guidelines

All implementation agents must preserve the project-reference graph, use `CancellationToken`, keep external I/O asynchronous, validate inputs/AI output deterministically, and follow the conventions above. Verify with architecture reference tests, targeted tests, full solution build/test/format, and review `git diff` to confirm Project 1 is unchanged. Any deviation that changes a durable decision must update the Project 2 architecture and an ADR before merging.

## Project Structure & Boundaries

### Complete Project Directory Structure

Add these projects to the existing `devops-ai-assistant.sln`; do not move or rename Project 1 projects.

```text
src/
+-- DevOpsAI.Rag.Domain/
|   +-- Documents/
|   |   +-- KnowledgeDocument.cs
|   |   +-- DocumentChunk.cs
|   +-- Chat/
|       +-- GroundedAnswer.cs
|       +-- CitationReference.cs
|       +-- RetrievalCandidate.cs
|
+-- DevOpsAI.Rag.Application/
|   +-- Documents/
|   |   +-- ReplaceRunbookDocument.cs
|   |   +-- DeleteRunbookDocument.cs
|   |   +-- ListRunbookDocuments.cs
|   |   +-- RunbookDocumentResult.cs
|   +-- Chat/
|   |   +-- AskRunbookQuestion.cs
|   |   +-- AskRunbookQuestionResult.cs
|   |   +-- RunbookAnswerPrompt.cs
|   +-- Ports/
|   |   +-- IRunbookKnowledgeIndex.cs
|   |   +-- ITextEmbeddingGenerator.cs
|   |   +-- IRunbookAnswerGenerator.cs
|   |   +-- IRunbookPromptProvider.cs
|   +-- Configuration/
|       +-- RagOptions.cs
|
+-- DevOpsAI.Rag.Infrastructure/
|   +-- Persistence/
|   |   +-- RagDbContext.cs
|   |   +-- Entities/
|   |   +-- Migrations/
|   |   +-- SqliteRunbookKnowledgeIndex.cs
|   +-- Embeddings/
|   |   +-- OllamaTextEmbeddingGenerator.cs
|   +-- Generation/
|   |   +-- OllamaRunbookAnswerGenerator.cs
|   +-- Prompts/
|   |   +-- FileRunbookPromptProvider.cs
|   +-- DependencyInjection.cs
|
+-- DevOpsAI.Rag.Api/
|   +-- Endpoints/
|   |   +-- DocumentEndpoints.cs
|   |   +-- ChatEndpoints.cs
|   +-- Contracts/
|   +-- ErrorHandling/
|   +-- Program.cs
|   +-- DependencyInjection.cs
|   +-- appsettings.json
|   +-- appsettings.Development.json
|
tests/
+-- DevOpsAI.Rag.UnitTests/
|   +-- Domain/
|   +-- Infrastructure/
+-- DevOpsAI.Rag.ApplicationTests/
|   +-- Documents/
|   +-- Chat/
|   +-- Evaluation/
+-- DevOpsAI.Rag.IntegrationTests/
    +-- Api/
    +-- Persistence/
    +-- Fakes/
    +-- Fixtures/
        +-- corpus/
        +-- rag-evaluation.json

docs/
+-- prompts/
|   +-- runbook-chatbot/
|       +-- system.md
|       +-- answer-format.md
+-- api/
|   +-- project-2-openapi.md

PROJECT-2-ARCHITECTURE.md
README.md                       # Add Project 2 local run section, preserve Project 1 docs
```

### Dependency Boundaries

```text
DevOpsAI.Rag.Api ----------------------> DevOpsAI.Rag.Application
        |                                       ^
        | composition root only                |
        +--> DevOpsAI.Rag.Infrastructure -------+
                   |                            |
                   +--> DevOpsAI.Rag.Domain -----+

DevOpsAI.Rag.Application ----------------------> DevOpsAI.Rag.Domain
DevOpsAI.Rag.Domain ---------------------------> no project references
```

The API references Infrastructure only at its composition root to register adapters; endpoint and contract code must not use Infrastructure types. Infrastructure references Application/Domain to implement ports. There are no Project 2 -> Project 1 assembly references. Test projects reference only the layer/API under test.

### Architectural Boundaries and Port Contracts

- **Domain:** stable document/version/chunk/citation concepts and invariants only. No HTTP, EF Core, SQLite, AI packages, file IO, ASP.NET Core or provider SDKs.
- **Application:** use-case orchestration and deterministic rules. Owns ports:
  - `IRunbookKnowledgeIndex.ReplaceDocumentAsync(document, chunksWithEmbeddings, cancellationToken)`, `DeleteDocumentAsync`, `ListDocumentsAsync`, `SearchAsync(queryEmbedding, topK, cancellationToken)`. Replace is atomic from the use-case perspective; SQLite implementation commits source/chunks/vectors as one transaction.
  - `ITextEmbeddingGenerator.EmbedAsync(texts, cancellationToken)` returns vectors and configured model identity/dimensions, without provider SDK types.
  - `IRunbookAnswerGenerator.GenerateAsync(prompt, cancellationToken)` returns structured generated answer data (answer text and cited chunk IDs), not an HTTP response or vendor object.
  - `IRunbookPromptProvider.GetSystemPromptAsync(cancellationToken)` supplies versioned instructions to the query use case.
- **Infrastructure:** EF Core SQLite persistence, normalized-vector cosine scan, Ollama chat/embedding adapters, file prompt loading, configuration and translation of provider failures.
- **API:** validates transport shape/limits, maps contracts to Application use cases, maps results/errors to HTTP; it does not embed, retrieve, prompt, or call providers itself.

### Requirements to Structure Mapping

- FR1–FR8: `Application/Documents`, Domain `Documents`, `IRunbookKnowledgeIndex`, Infrastructure `Persistence`, API `DocumentEndpoints`.
- FR9–FR14: `Application/Chat/AskRunbookQuestion`, embedding and index ports, local index/search adapter, chat generator adapter.
- FR15–FR19: Domain citation/retrieval concepts, `AskRunbookQuestion` validation/context assembly, prompt assets, API chat response mapping.
- FR20–FR25: options and Infrastructure DI; API OpenAPI/error handling/health; safe observability middleware/filters.
- FR26–FR29 and NFR1/NFR17: test doubles, temporary SQLite integration tests and evaluation fixtures under `tests/DevOpsAI.Rag.*`.

### Data Boundaries and Persistence

SQLite is one local database file. Tables conceptually include `Documents`, `Chunks` and `EmbeddingVectors` (embedding may be stored as binary on the chunk row to avoid a needless join). Foreign keys cascade or are explicitly removed in one transaction. Each active document version has a content hash and embedding model ID; chunks store chunk ID, ordinal, heading, source line range, text and vector dimension. Queries exclude inactive/deleted documents.

All persistence/search access flows through `IRunbookKnowledgeIndex`. The application never receives a `DbContext`, entity, SQL connection, file path or vendor search response. The Infrastructure implementation controls migrations, transactions, SQLite vector decoding and score calculation.

### Integration Points and Data Flow

**Ingestion/upsert:** HTTP upload → boundary validation → Application reads normalized document content → markdown/TXT parser retains line/heading metadata → deterministic chunker → embedding port → stage complete version → knowledge-index atomic replace → API returns document metadata/status.

**Query:** HTTP question → Application validation → embedding port → knowledge-index top-k cosine search → active-version filtering and threshold → no evidence: return `insufficient_evidence`; otherwise deduplicate and cap context → prompt provider + user question + retrieved passages → answer generator → strict structured-output validation and citation-ID allowlist → map trusted chunk metadata into response → API serializes result.

**Delete:** HTTP resource ID → use case validates existence → index transaction removes document and all chunks/vectors → `204` or `404`.

**Azure evolution:** retain Application contracts and IDs; add Infrastructure adapters for Azure AI Search and Azure OpenAI/AI Foundry. Azure document payloads may move to Blob Storage. Reindex all documents when changing embedding model. No local SQLite or Ollama references escape Infrastructure.

### File, Configuration, Test and Development Organization

- All settings bind from `Rag` options and environment variables; safe defaults are committed only in appsettings. Development database path is ignored by git. No secrets in appsettings.
- Prompts are read as content assets from `docs/prompts/runbook-chatbot/` and copied to API output, following the repository's prompt-asset convention.
- Test names describe scenario and expected behavior. Tests use in-memory fake embedding/generation, temporary database files and synthetic fixtures.
- Local development starts API and Ollama separately; both commands and model requirements are documented in the root README and Project 2 architecture. Build/test/format use existing solution-wide commands.
- Database and model downloads are external runtime assets; never commit SQLite databases, downloaded models, API credentials or operational runbooks.

## Technology Stack

| Technology | MVP role | Rationale |
|---|---|---|
| .NET 10 / C# | Runtime and language | Matches repository target and established conventions. |
| ASP.NET Core Minimal API | REST host | Matches Project 1 and is sufficient for the small API surface without MVC/UI overhead. |
| EF Core SQLite 10.x | Local persistence and migrations | Durable single-file DB, no separate server/container, already recommended by the repository for learning/demo use. |
| SQLite BLOB vectors + cosine scan | Local vector index | Keeps local setup simple and allows hands-on retrieval learning for a capped 10k chunk corpus; acceptance benchmark determines whether this remains viable. |
| Microsoft.Extensions.AI 10.x | Provider-neutral chat/embedding boundary in Infrastructure | Aligns with existing repository AI boundary and allows provider adapters without SDK types in Application. |
| Ollama + OllamaSharp 5.5.2 | Local model runtime and adapter | Enables local embeddings and answer generation without cloud credentials. OllamaSharp targets net10 and requires Microsoft.Extensions.AI 10.8.0; central package update is gated by Project 1 regression checks. |
| Azure AI Search (future) | Managed vector search | Matches the repository's Project 2 Azure direction; add only after local MVP and cost/security review. |
| Azure OpenAI / AI Foundry (future) | Managed chat/embedding models | Azure-compatible provider replacement; not required for local MVP. |
| xUnit, NSubstitute, ASP.NET Core test host | Automated tests | Reuses existing test stack, supports deterministic offline tests and real HTTP-boundary checks. |
| `dotnet format`, existing analyzers | Quality checks | Reuses repository-wide build and formatting gates. |

No separate vector database is selected for MVP. Qdrant and PostgreSQL/pgvector remain alternatives only if measured local performance or scale invalidates SQLite; Azure AI Search remains the planned cloud adapter.

## Architecture Decision Records

These are durable decisions; their concise records are also maintained under `docs/adr/`:

- **P2-ADR-001 — Isolate Project 2 layers:** create `DevOpsAI.Rag.*` projects; no compile-time reference from Project 2 to Project 1. Reuse conventions and packages, not incident-specific classes. Revisit extraction only when two implemented use cases prove a shared contract.
- **P2-ADR-002 — Local-first SQLite vector index:** store document content, chunks and vectors in SQLite and scan cosine similarity for the bounded MVP corpus; use a provider-owned index port so Azure AI Search can replace it.
- **P2-ADR-003 — Local Ollama with replaceable model ports:** run chat and embeddings via Ollama locally behind Application-owned ports. Azure model services are later Infrastructure adapters. Keep embedding model and vector dimensions part of index compatibility.
- **P2-ADR-004 — Grounded structured answers and abstention:** generate structured answer plus retrieved chunk IDs, validate the response and citation allowlist deterministically, and do not call generation when evidence is below the calibrated threshold.
- **P2-ADR-005 — Atomic synchronous document lifecycle:** parse and embed before activating an upsert; commit source/chunks/vectors in one SQLite transaction. Delete source and all derived records together. Do not introduce queues before measured need.

## Risks, Pending Decisions and Mitigations

| Risk / pending decision | Impact | Mitigation or decision gate |
|---|---|---|
| Linear cosine scan may miss 500 ms at 10k chunks on weaker hardware | NFR1 not met | Benchmark with the selected vector dimension and documented reference machine; optimize the adapter or reconsider Qdrant before changing scope. |
| Ollama model memory/latency and embedding quality vary with hardware/model | Poor local experience or low retrieval quality | Keep model IDs explicit; benchmark two candidate local embedding/chat models against fixtures; document minimum local resources and license. |
| OllamaSharp 5.5.2 requires Microsoft.Extensions.AI 10.8.0 while repo pins 10.0.0 | Potential shared dependency update affects Project 1 | Update central version only in a dedicated P2 story; run full solution tests/build/format and inspect P1 behavior; fallback to a thin Infrastructure HTTP adapter. |
| Cosine threshold and top-k are corpus/model dependent | False answer or excessive abstention | Establish gold dataset and measure recall/abstention; select deterministic config after baseline. Do not use model confidence. |
| LLM may produce unsupported prose despite citation IDs | Citations alone do not prove every claim | Constrain prompt and context, require citations, test adversarial/unsupported cases; describe guarantees as evidence association, not perfect entailment. Keep no-action security boundary. |
| Prompt injection or harmful stale runbook text | Unsafe advice | Treat context as untrusted, cite provenance/version, warn operator to verify; never execute tools/actions. |
| SQLite single-writer / split-brain across processes | Concurrent ingestions may contend | MVP local single-process deployment; transactional writes and bounded timeout; no public/shared deployment claim. |
| No authentication in MVP | Anyone with network access may query/delete local corpus | Loopback default, no public exposure; add auth/access control before shared network deployment. |
| Encoding/large markdown edge cases | Incorrect offsets or resource usage | Define UTF-8-only MVP, input limits, preserve line ranges and test malformed files. |
| Azure AI Search migration changes scoring/filters and index semantics | Retrieval behavior differs after migration | Adapter contract and stable IDs, full reindex, shared evaluation suite, compare scores/thresholds before cutover. |

**Implementation gates to resolve:** hardware baseline for NFR1; model IDs and licensing; exact `Microsoft.Extensions.AI` dependency update; dataset baseline and relevance threshold; SQLite migration strategy and database file location.

## Architecture Validation Results

### Coherence Validation

- **Decision compatibility:** .NET 10, EF Core SQLite 10.x, Microsoft.Extensions.AI 10.x and an Ollama adapter targeting net10 are compatible at the target-framework level. The connector's 10.8 dependency is explicitly identified and gated; do not silently override it with the repository's current 10.0.0 pin.
- **Pattern consistency:** Application-owned ports, DTO validation, strict JSON parsing, deterministic citation mapping and sanitized errors align with the Clean Architecture and AI safety rules.
- **Structure alignment:** Separate `DevOpsAI.Rag.*` assemblies contain the RAG-specific use cases and adapters; API-to-Infrastructure dependency exists only in composition registration.

### Requirements Coverage Validation

- **FR1–FR8:** upload/upsert, metadata, list, invalid files, retry, atomic replace and delete are supported by document use cases and `IRunbookKnowledgeIndex`.
- **FR9–FR14:** question validation, embedding, retrieval, threshold, bounded context and abstention are supported by the chat use case and ports.
- **FR15–FR19:** model returns chunk IDs; Application checks the retrieval allowlist; API maps persisted citation metadata; prompts treat documents as untrusted.
- **FR20–FR25:** adapter config/options, local run, health, OpenAPI, safe error codes and trace IDs are in Infrastructure/API boundaries.
- **FR26–FR29:** fake providers, temporary SQLite, deterministic integration tests and gold-set evaluation are specified.
- **NFR1–NFR19:** performance gate and separate timings; timeouts/bounds; loopback/log redaction; validation/untrusted inputs; options; transactional lifecycle; persistence; safe provider failures; Clean Architecture; .NET 10/local execution; replaceable ports; offline tests; trace and health semantics are all assigned to patterns, ports or tests.

### Implementation Readiness

- **Decisions:** local index, model provider, response grounding, data lifecycle and dependency boundaries are decided. Model ID, performance baseline and thresholds are deliberately config/evaluation gates rather than guessed universal values.
- **Structure:** API, Domain, Application, Infrastructure and test assemblies plus prompts/evaluation fixtures are specified.
- **Patterns:** naming, DTO/error/date formats, citation validation, chunk/index lifecycle, cancellation, logging and evaluation are defined with enforceable tests.
- **Security:** no auth is explicitly bounded to local-only use; no tool calling/MCP or action execution is introduced.

### Gaps and Status

- **Blocking gaps:** none for starting the skeleton and deterministic core stories.
- **Important gates:** select hardware and model baseline before asserting local performance/model quality; resolve Microsoft.Extensions.AI 10.8.0 compatibility before adding OllamaSharp.
- **Deferred:** Azure deployment/SKU, auth, tenant model, production SLOs, large-corpus index and additional file formats.

### Architecture Readiness Assessment

**Overall status:** READY FOR IMPLEMENTATION, with the implementation gates above tracked as explicit backlog items.

**Confidence:** Medium-high. Core flow, layer boundaries, API, local persistence, citation safety, testing and migration path are specified. The main uncertainties are model/hardware performance and package alignment, and neither blocks skeleton or deterministic application work.

**First implementation priority:** add the four Project 2 projects and three test projects to the existing solution, establish architecture-boundary tests and run the unchanged Project 1 test suite before adding provider packages or behavior.

## Backlog Prioritization

Execute in order; each item is independently reviewable and has a measurable completion check.

### Epic 1 — Establish the Project 2 boundary

1. **P2-01 — Add the RAG solution skeleton.** Add four `DevOpsAI.Rag.*` projects and three RAG-specific test projects; set references exactly as documented; add to solution without modifying Project 1 files. **Acceptance:** solution builds; reference-graph tests reject Domain → Infrastructure and verify no Project 2 → Project 1 reference; existing Project 1 tests/build pass.
2. **P2-02 — Define document and chat contracts.** Add domain concepts and Application request/results/ports for document lifecycle, retrieval, generation and prompts. **Acceptance:** contract validation tests cover required/invalid values; Application compiles without ASP.NET, EF Core or provider package references.

### Epic 2 — Build the local knowledge index

3. **P2-03 — Implement Markdown/TXT parsing and deterministic chunking.** Preserve UTF-8 content, headings, chunk ordinals and original line ranges; enforce configured limits. **Acceptance:** fixtures produce stable chunks and valid source offsets; malformed/unsupported files fail without side effects.
4. **P2-04 — Add local Ollama embeddings and model configuration.** Select and record explicit embedding model/dimension; resolve `Microsoft.Extensions.AI` central version compatibility in an isolated dependency change. **Acceptance:** Infrastructure adapter passes fake/provider-boundary tests; cancellation/timeout/configuration failures are explicit; no model/network is needed in ordinary tests.
5. **P2-05 — Implement SQLite schema and atomic index adapter.** Persist source metadata, text, chunks, vectors and model identity; add migrations and transactionally replace/delete. **Acceptance:** temporary-file integration tests prove commit, rollback, delete cascade and restart persistence.
6. **P2-06 — Implement document ingestion/update/delete/list use cases.** Orchestrate parse, chunk, embed, atomic replace and list/delete. **Acceptance:** failed parsing/embedding preserves previous active version; duplicate upsert changes version; delete removes all searchable chunks.

### Epic 3 — Deliver grounded chat

7. **P2-07 — Implement local vector retrieval and threshold policy.** Normalize vectors, rank by cosine descending, deduplicate/cap top-k and calibrate threshold from evaluation fixtures. **Acceptance:** known vectors rank expected chunks; irrelevant query returns insufficient evidence; 10k-chunk retrieval meets NFR1 on the documented reference environment.
8. **P2-08 — Add prompt assets and local answer generation.** Use Ollama chat adapter and structured answer/cited-chunk-ID output; treat sources as untrusted. **Acceptance:** fake generation tests cover valid/invalid JSON, empty answer, missing/unknown IDs, provider timeout and prompt-injection fixture; no exact wording assertions.
9. **P2-09 — Implement chat orchestration and deterministic citation mapping.** Compose bounded context only after retrieval sufficiency, map valid IDs to persisted source metadata/line ranges. **Acceptance:** generation is not called on insufficient evidence; all returned citations belong to retrieved chunks; citation metadata resolves to the current document version.

### Epic 4 — Expose and verify the MVP

10. **P2-10 — Add document management REST endpoints.** Implement PUT upsert, GET list, DELETE with OpenAPI and stable error mapping. **Acceptance:** integration tests assert `201/200/204/404/400/413/415` and atomic behavior.
11. **P2-11 — Add chat and health REST endpoints.** Implement POST chat, response schema, insufficient evidence, correlation and dependency status. **Acceptance:** integration tests cover answered/abstained/provider-failed cases and JSON contracts.
12. **P2-12 — Create retrieval and grounding evaluation suite.** Add synthetic gold questions/corpus and offline evaluation command or test reporting Recall@k, MRR, citation validity and abstention outcomes. **Acceptance:** repeatable without network; results are deterministic for fake embeddings/generator; initial baseline is recorded.
13. **P2-13 — Harden, document and verify end-to-end.** Add request/stage metrics, safe logging checks, loopback defaults, local Ollama/SQLite setup docs and sample requests. **Acceptance:** `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` pass; end-to-end local procedure is followed from a clean checkout; P1 remains unchanged and green.

## Definition of Done — Project 2 MVP

- Every MVP FR and NFR maps to an implementation and automated verification; deviations are documented.
- The service runs locally with SQLite and the documented Ollama model setup; no Azure credentials or services are required for normal tests.
- Document ingestion, replacement, listing, deletion and restart persistence work without stale chunks.
- Answers only use retrieved context, cite valid source/version/line references, and abstain deterministically below the calibrated evidence threshold.
- Provider, storage, parsing and output failures are explicit, sanitized, traced and do not appear as successful empty responses.
- Domain/Application dependency boundaries, nullable/analyzer policies, async cancellation, package centralization and prompt asset conventions are respected.
- Unit/integration/evaluation suites are deterministic; the 10k-chunk performance criterion is measured on the documented environment.
- Project 1 code/contracts/behavior are unchanged and its build/tests pass.
- README, OpenAPI and architecture/ADR docs reflect actual local setup and behavior; no secrets, real operational runbooks, model files or SQLite data are committed.
