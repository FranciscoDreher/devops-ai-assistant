# Project 2 Architecture — Runbook Chatbot (RAG)

**Status:** Approved; implementation has not started

**Date:** 2026-11-11

**PRD:** [_bmad-output/planning-artifacts/project-2/prd.md](./_bmad-output/planning-artifacts/project-2/prd.md)

## 1. Objective and Scope

Project 2 adds an API that answers questions about Markdown/TXT runbooks and technical documentation. Each answer must be associated with retrieved passages and include verifiable references. When the collection does not provide sufficient evidence, the system abstains rather than filling gaps with unsupported knowledge.

The MVP is a single-process local service for learning and validating RAG against a controlled corpus. It supports ingesting, replacing, listing and deleting documents; preserves a local index across restarts; and exposes a REST API for queries. It does not execute commands or operational actions.

**Out of scope:** frontend, authentication/authorization, multitenancy, batch ingestion or filesystem watchers, PDF/HTML/Word, crawling and external connectors, streaming, conversation persistence, production deployment, microservices, tool calling and MCP. The MVP must not be exposed to untrusted networks.

## 2. Technology Stack

| Technology | Use | Rationale |
|---|---|---|
| .NET 10 / C# | Runtime and language | Matches the existing solution and conventions. |
| ASP.NET Core Minimal API | REST host | Reuses the Project 1 pattern and is sufficient for MVP endpoints without adding a UI or unnecessary web layers. |
| EF Core + SQLite 10.x | Local persistence and migrations | Durable single-file database without a container or separate server; the local option recommended by `ARCHITECTURE.md`. |
| SQLite BLOB vectors and in-process cosine search | Local vector index | Demonstrates fundamentals and minimizes requirements for a corpus capped at 10,000 chunks. Performance is an acceptance condition, not an assumption. |
| `Microsoft.Extensions.AI` 10.x | AI contracts in Infrastructure | Follows the repository's AI boundary and allows provider replacement. It is not referenced from Domain or Application. |
| Ollama + OllamaSharp 5.5.2 | Local embeddings and generation | Runs models locally without cloud credentials. Model/hardware selection is validated by benchmark. |
| Azure AI Search (future) | Managed vector index | Azure target to replace the local adapter if cloud deployment is needed. Not an MVP requirement. |
| Azure OpenAI / AI Foundry (future) | Managed chat and embeddings | Replaceable providers for Azure evolution; not required for local execution. |
| xUnit, NSubstitute and ASP.NET Core Test Host | Testing | Reuses the existing stack and enables deterministic offline tests. |
| `dotnet format` and repository analyzers | Quality | Maintains the same build and formatting controls as Project 1. |

### Versions and Compatibility

The repository targets .NET 10 and centrally manages package versions in `Directory.Packages.props`. Public NuGet metadata checked on 2026-11-11 showed stable latest versions of `Microsoft.Extensions.AI 11.0.0`, `Microsoft.EntityFrameworkCore.Sqlite 11.0.0`, `OllamaSharp 5.5.2` and `Qdrant.Client 1.20.0`. The observed .NET 10 lines are `Microsoft.Extensions.AI 10.8.0` and EF Core SQLite `10.0.15`.

OllamaSharp 5.5.2 targets net10.0 and requires `Microsoft.Extensions.AI 10.8.0`; the repository currently pins 10.0.0. If OllamaSharp is selected, the dependency task must update the central pin to 10.8.0 and validate the entire solution and Project 1. If this introduces regressions, implement a small Ollama HTTP adapter in Infrastructure. Do not update to major version 11 or modify unrelated packages without a reason and tests.

## 3. Clean Architecture and Dependencies

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

The API references Infrastructure only to register adapters in the composition root; endpoints and HTTP contracts do not use Infrastructure types. Domain does not depend on ASP.NET Core, EF Core, SQLite, Azure or AI SDKs. Application defines use cases, policies and its own ports. Infrastructure implements those ports and contains all provider and persistence details.

Project 2 is an independent module in the solution, not a separate microservice or an extension of incident code. It must not reference Project 1 assemblies.

### Responsibilities

- **Domain:** stable document, version, chunk, retrieval candidate and citation concepts; invariants without serialization or storage details.
- **Application:** ingestion/replacement, listing, deletion and query use cases; deterministic validation; chunking/context and citation verification; port contracts.
- **Infrastructure:** parser, EF Core/SQLite, cosine search, Ollama embedding and chat adapters, prompt loading, options, timeouts and external failure translation.
- **API:** transport boundaries, Minimal API, DTO mapping, HTTP status codes, OpenAPI, health, correlation and global error handling.

## 4. Project Structure

Add the following projects to `devops-ai-assistant.sln`; do not move or rename Project 1 projects.

```text
src/
+-- DevOpsAI.Rag.Domain/
|   +-- Documents/KnowledgeDocument.cs
|   +-- Documents/DocumentChunk.cs
|   +-- Chat/GroundedAnswer.cs
|   +-- Chat/CitationReference.cs
|   +-- Chat/RetrievalCandidate.cs
+-- DevOpsAI.Rag.Application/
|   +-- Documents/ReplaceRunbookDocument.cs
|   +-- Documents/DeleteRunbookDocument.cs
|   +-- Documents/ListRunbookDocuments.cs
|   +-- Documents/RunbookDocumentResult.cs
|   +-- Chat/AskRunbookQuestion.cs
|   +-- Chat/AskRunbookQuestionResult.cs
|   +-- Chat/RunbookAnswerPrompt.cs
|   +-- Ports/IRunbookKnowledgeIndex.cs
|   +-- Ports/ITextEmbeddingGenerator.cs
|   +-- Ports/IRunbookAnswerGenerator.cs
|   +-- Ports/IRunbookPromptProvider.cs
|   +-- Configuration/RagOptions.cs
+-- DevOpsAI.Rag.Infrastructure/
|   +-- Persistence/RagDbContext.cs
|   +-- Persistence/Entities/
|   +-- Persistence/Migrations/
|   +-- Persistence/SqliteRunbookKnowledgeIndex.cs
|   +-- Embeddings/OllamaTextEmbeddingGenerator.cs
|   +-- Generation/OllamaRunbookAnswerGenerator.cs
|   +-- Prompts/FileRunbookPromptProvider.cs
|   +-- DependencyInjection.cs
+-- DevOpsAI.Rag.Api/
|   +-- Endpoints/DocumentEndpoints.cs
|   +-- Endpoints/ChatEndpoints.cs
|   +-- Contracts/
|   +-- ErrorHandling/
|   +-- Program.cs
|   +-- DependencyInjection.cs
|   +-- appsettings.json
|   +-- appsettings.Development.json
tests/
+-- DevOpsAI.Rag.UnitTests/ (Domain, Infrastructure)
+-- DevOpsAI.Rag.ApplicationTests/ (Documents, Chat, Evaluation)
+-- DevOpsAI.Rag.IntegrationTests/ (Api, Persistence, Fakes, Fixtures)
|   +-- Fixtures/corpus/
|   +-- Fixtures/rag-evaluation.json
docs/
+-- prompts/runbook-chatbot/system.md
+-- prompts/runbook-chatbot/answer-format.md
+-- api/project-2-openapi.md
+-- adr/P2-ADR-001...P2-ADR-005
PROJECT-2-ARCHITECTURE.md
README.md (add Project 2 instructions without replacing Project 1 instructions)
```

These names are concrete organizational guidance, not a requirement to create a class for every file before it is needed. Keep one responsibility per type and do not add generic layers.

## 5. End-to-End RAG Flow

### Ingestion, Update and Deletion

1. `PUT /api/documents/{documentId}` validates the ID, allowed extension, size, UTF-8 encoding and non-empty content.
2. Application extracts Markdown/TXT text without rendering HTML or executing content. It records a SHA-256 hash, filename, date and version.
3. The parser preserves Markdown headings and source line numbers.
4. A deterministic chunker splits first by headings/paragraphs and groups up to approximately 1,000 characters, with approximately 150 characters of overlap. Values are configurable and calibrated against the corpus; each chunk retains its ordinal, heading and line range.
5. The embedding adapter generates a vector for each chunk. Model identity/dimensions are stored; incompatible vector spaces are never mixed.
6. Only after processing succeeds does SQLite replace the document, chunks and vectors in one transaction. If parsing, embedding or commit fails, the previous version remains active.
7. Deleting a document transactionally deletes the source data and all its chunks/vectors. Queries exclude deleted or inactive versions.

Ingestion is synchronous and handles one file per operation. Jobs/queues are deferred until actual limits are measured.

### Query, Retrieval and Generation

1. `POST /api/chat` validates the question and requests its embedding.
2. The local index searches by descending cosine similarity and retrieves up to top-k (initially 5), filtering inactive versions.
3. Overlapping duplicates are removed and total context is bounded. Scores and thresholds are deterministic and calibrated by evaluation; model-generated confidence is not used.
4. If no result exceeds the threshold, the response is `insufficient_evidence` and the generation model is not called.
5. If evidence is sufficient, the prompt contains the question and only retrieved chunks, delimited as untrusted data.
6. The model returns strict JSON containing `answer` and `citedChunkIds`.
7. Application validates the schema, non-empty answer/citations, and membership of every ID in the results retrieved for that query. Unknown IDs or invalid JSON are errors, never empty successful responses.
8. The response is assembled from trusted persisted metadata (document, version, chunk, heading and lines); model-generated names, URLs or paths are not accepted.

### Vector Storage and Migration

The MVP uses EF Core SQLite, with source content, metadata, chunks and embedding vectors stored as BLOBs in a local database. The adapter compares normalized vectors using in-process cosine similarity. Search is linear in chunk count and dimensions; it must be measured against 10,000 chunks with a target under 500 ms in the reference environment, excluding embeddings and generation.

| Option | Local simplicity/cost | Azure migration | Decision |
|---|---|---|---|
| SQLite + local cosine search | One file, no Docker or service cost; easy to run | Replace the adapter with Azure AI Search; optionally use Blob Storage if source storage grows | **Selected for MVP**; limited by benchmark and single-process usage |
| Local Qdrant | Good vector engine, but requires Docker and another process | Azure AI Search has a different API; requires an adapter and reindexing | Possible evolution if the local benchmark fails |
| PostgreSQL + pgvector | Familiar technology and SQL filters; requires a server/container | Azure Database for PostgreSQL offers a compatible path but differs from the AI Search target | More operations than the MVP needs |
| Azure AI Search from the start | Requires cloud, credentials and cost | Direct target | Rejected for the local-first MVP |

Azure migration requires a full reindex when the model/dimensions change and a new evaluation of scores and thresholds; semantic ranking differs between engines.

## 6. Main Contracts and Types

Suggested contracts owned by Application:

- `ReplaceRunbookDocumentCommand`: stable ID, filename, normalized content and permitted metadata.
- `RunbookDocumentResult`: ID, filename, content hash, status, version, chunk count and dates.
- `AskRunbookQuestionRequest`: question and, if added later, an explicit collection (not required for the single-collection MVP).
- `AskRunbookQuestionResult`: `answered`/`insufficient_evidence` status, text and trusted citation list.
- `RetrievedChunk`: ID, content, score, document ID/version, heading and line range.
- `GeneratedRunbookAnswer`: generator response and cited chunk IDs only; no HTTP DTOs or provider types.

Application ports:

- `IRunbookKnowledgeIndex`: transactional document/chunk/vector replacement, deletion, listing and vector search.
- `ITextEmbeddingGenerator`: generates vectors for text and exposes model identity/dimensions without Ollama/Azure types.
- `IRunbookAnswerGenerator`: generates a structured answer from a fully constructed prompt.
- `IRunbookPromptProvider`: loads the versioned system prompt.

The index interface groups operations that form one indexed knowledge unit and allows the local adapter to implement atomicity. `DbContext`, entities, provider vectors, file paths and Azure types are not exposed outside Infrastructure.

## 7. Proposed REST API

### `PUT /api/documents/{documentId}`

Accepts `multipart/form-data` with a `file` field; supports `.md` and `.txt`. The same ID replaces its version idempotently.

Create response (`201 Created`):

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

Replacement returns `200 OK` with the new version/hash. Rejections: `400` invalid input, `413` size exceeded, `415` unsupported extension/format. A provider or storage failure returns a safe error and preserves the previous active version.

### `GET /api/documents`

Returns metadata (ID, filename, hash/version, status, chunks and timestamps); never the full body.

### `DELETE /api/documents/{documentId}`

Returns `204 No Content` if the document existed and was deleted, or `404 Not Found` if it does not exist.

### `POST /api/chat`

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
  "answer": "Follow the documented recovery procedure and verify service health before restoring traffic.",
  "citations": [
    {
      "documentId": "runbook-cache",
      "fileName": "cache-recovery.md",
      "chunkId": "opaque-chunk-id",
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

Illustrative response strings are in English to demonstrate the JSON contract; MVP answers can be configured or tested with Spanish content. Contract fields and status values are stable and documented in OpenAPI.

### Health and Errors

`GET /health` reports host health and identifies required dependencies that are not configured. The common JSON error is `{ "code": "...", "message": "...", "traceId": "..." }`. Use `400`, `404`, `409`, `413`, `415`, `503` (known unavailable dependency) and `500` according to cause. `insufficient_evidence` is a normal chat result, not an infrastructure error.

## 8. Prompts, Grounding and Response Safety

Version prompts in `docs/prompts/runbook-chatbot/`:

- `system.md`: role, goal, constraints, no invented information, abstention, DevOps audience, instructions to treat sources as untrusted evidence and not to follow instructions in the corpus.
- `answer-format.md`: expected JSON schema and citation ID rules.

The prompt is not a security boundary. Application controls whether generation occurs, what context is provided, which citations are accepted and which errors are returned. A citation proves that its source was retrieved, not perfect entailment for every claim; do not promise absolute factual guarantees.

## 9. Errors, Logging and Observability

- Validate file, encoding, query, retrieved chunk, context/token and external timeout limits before consuming resources.
- Propagate `CancellationToken`, use asynchronous I/O and configure provider timeouts. Do not use silent fallbacks, random vectors, success-shaped empty collections or implicit model changes.
- Translate adapter errors to safe Application/API codes. Do not return exceptions, SQL, stack traces, local paths or raw provider responses.
- Generate/propagate a trace ID. Log stage, outcome, duration, safely identified model/provider, counts and trace ID in structured form.
- Do not log documents, full questions, context, prompts, responses or secrets.
- Measure parsing/chunking, embeddings, commit, retrieval and generation separately.
- The unauthenticated MVP binds to loopback; document the restriction and do not expose it to public/untrusted networks. Shared deployment requires authentication, authorization, access limits and auditing.

## 10. RAG Testing and Evaluation

- **Domain:** pure tests for invariants and IDs.
- **Application:** tests with substitutes for embeddings, generation, prompts and index; validate ordering, cancellation, abstention and citations.
- **Infrastructure:** integration tests with temporary SQLite; transactions, rollback, deletion, restart and cosine scoring. Ollama is not required in routine tests.
- **API:** ASP.NET Core Test Host; OpenAPI, multipart binding, serialization, HTTP codes, error handling, health and correlation.
- **Offline evaluation:** versioned synthetic corpus with questions, relevant chunks and expected abstention. Report Recall@k and MRR for retrieval; citation validity/precision and abstention outcomes for answers. Record a baseline and set thresholds after measurement, not before.
- **Grounding/adversarial tests:** absent question, contradictory or irrelevant evidence, fabricated citation ID, invalid JSON, prompt injection in a runbook and malicious document. Do not compare exact prose.
- **No external dependencies:** unit/integration tests do not call real APIs or require secrets. Optional Ollama tests are explicitly separated.
- **Performance:** measure 10,000 chunks on documented reference hardware; retrieval under 500 ms, excluding query embedding and LLM. Record dimensions and embedding model for reproducibility.

## 11. Conventions and SOLID

- Follow repository nullable settings, analyzers, central package management, xUnit, NSubstitute, `CancellationToken`, DI and `IOptions<T>`.
- Keep one clear responsibility per class and one primary public type per file.
- Use explicit names such as `AskRunbookQuestion`, `ReplaceRunbookDocument` and `CitationReference`; avoid `Helper`, `Manager`, `Data` or `Info`.
- Use camelCase JSON, UTC ISO-8601 dates, REST resource names and explicit request/response types.
- Keep tests in `tests/DevOpsAI.Rag.*`; prompts in `docs/prompts/runbook-chatbot/`; evaluation data in synthetic fixtures.
- Interfaces must address a real need. Do not create a RAG framework, universal provider abstraction or hypothetical `Common` layer.
- Domain contains no I/O. Application depends on Domain and its own ports. Infrastructure implements ports. API maps transport and composes DI.
- Verify boundaries with reference tests; confirm Project 1 remains unchanged.

## 12. Project 1 Reuse

**Reuse:** .NET 10 target, Minimal API style, test structure, OpenAPI, error/correlation handling as a reference, build rules, existing package versions when compatible, and the pattern of versioned prompt assets.

**Do not reference directly:** `DevOpsAI.Api`, `DevOpsAI.Application` or `DevOpsAI.Infrastructure` from new RAG projects. `IAiTextGenerator` is a small text port, but it resides in Project 1's Application assembly and does not cover embeddings or citation contracts; depending on that assembly would couple distinct capabilities. Define a local `IRunbookAnswerGenerator` and reuse Microsoft.Extensions.AI and its patterns inside Infrastructure. Extract contracts to a shared assembly only if both projects demonstrate a genuinely common, stable responsibility.

Do not change or move Project 1 code, contracts, endpoints or tests. Any central `Microsoft.Extensions.AI` update must pass the full build/tests and confirm Project 1 still works.

## 13. ADRs

- [P2-ADR-001 — Isolate Project 2 layers](./docs/adr/P2-ADR-001-project-2-isolated-layers.md)
- [P2-ADR-002 — SQLite local vector index](./docs/adr/P2-ADR-002-sqlite-local-vector-index.md)
- [P2-ADR-003 — Local Ollama and replaceable model adapters](./docs/adr/P2-ADR-003-local-ollama-provider-adapters.md)
- [P2-ADR-004 — Grounded answers and validated citations](./docs/adr/P2-ADR-004-grounded-answers-and-citations.md)
- [P2-ADR-005 — Synchronous and atomic document lifecycle](./docs/adr/P2-ADR-005-synchronous-atomic-document-lifecycle.md)

## 14. Risks and Open Decisions

| Risk/open decision | Mitigation or decision |
|---|---|
| Linear scan misses 500 ms at 10k chunks on modest hardware | Mandatory benchmark with documented dimensions and machine; optimize the adapter or evaluate Qdrant if it fails. |
| Ollama memory, latency and quality vary by model and machine | Select explicit IDs, measure candidate models and document requirements/licenses before setting defaults. |
| OllamaSharp requires Microsoft.Extensions.AI 10.8.0 while the repository uses 10.0.0 | Isolate the package change and test the solution/Project 1; use a custom HTTP adapter if the upgrade is unacceptable. |
| Cosine threshold depends on corpus and model | Create a gold dataset, establish a baseline and calibrate threshold/top-k; do not use LLM confidence. |
| A valid citation does not guarantee every claim is entailed by the source | Bound context, require citations, evaluate adversarial cases and do not claim perfect semantic grounding. |
| Documents may be outdated or contain prompt injection | Treat as untrusted data, cite version and never execute tools/actions. |
| SQLite has limited write concurrency | Keep the scope to a single local process; do not promise multi-user/production usage. |
| MVP has no authentication | Bind to loopback by default; no public/shared deployment without new controls. |
| Azure AI Search scores differ from local cosine search | Reindex and run the same evaluation dataset; recalibrate scores/threshold before migration. |

**Resolve during implementation:** reference hardware for performance, models and licenses, the specific `Microsoft.Extensions.AI` update, initial gold dataset/threshold, and SQLite path/migrations/backup.

## 15. Prioritized Backlog

Execute in order; each task is independently reviewable and verifiable.

### Epic 1 — Isolate the Project 2 Foundation

1. **P2-01 — Create the RAG solution skeleton.** Add four `DevOpsAI.Rag.*` projects and three test projects to the solution; configure references and analyzers. **Acceptance:** build succeeds; dependency graph tests prevent Domain→Infrastructure and Project 2→Project 1 references; existing Project 1 build/tests pass without code changes.
2. **P2-02 — Define document and chat contracts.** Add Domain concepts and Application contracts/ports. **Acceptance:** validation tests cover invalid values; Application does not reference ASP.NET Core, EF Core or provider SDKs.

### Epic 2 — Build the Local Index

3. **P2-03 — Implement the Markdown/TXT parser and chunker.** Preserve UTF-8, headings and lines. **Acceptance:** fixtures produce reproducible chunks and offsets; empty/malformed/unsupported files fail without side effects.
4. **P2-04 — Add local Ollama embeddings and model configuration.** Resolve `Microsoft.Extensions.AI` compatibility in an isolated task. **Acceptance:** timeout, cancellation and missing configuration produce explicit errors; routine tests do not require Ollama.
5. **P2-05 — Implement the SQLite schema and transactional index.** Persist documents, chunks, vectors and model identity. **Acceptance:** temporary database tests cover commit, rollback, deletion and persistence across restart.
6. **P2-06 — Implement ingestion, replacement, listing and deletion use cases.** Orchestrate parsing, chunking, embedding and indexing. **Acceptance:** if preparation fails, the previous version stays active; upsert changes the version; deletion removes all derived results.

### Epic 3 — Deliver Grounded Chat

7. **P2-07 — Implement local retrieval and relevance policy.** Rank by cosine, deduplicate and bound top-k/context; calibrate threshold with fixtures. **Acceptance:** expected rankings and abstention for irrelevant questions; retrieval over 10k chunks meets the NFR in the documented environment.
8. **P2-08 — Add prompts and local structured generation.** Implement the Ollama adapter and JSON with answer + IDs. **Acceptance:** tests cover invalid JSON, empty answer, unknown IDs, timeout and corpus injection without checking exact text.
9. **P2-09 — Implement question orchestration and citations.** Generate only when evidence is sufficient and map IDs to persisted metadata. **Acceptance:** generator is not called on abstention; all citations exist in retrieval and belong to the active version.

### Epic 4 — Expose and Verify the MVP

10. **P2-10 — Add document management endpoints.** Implement PUT, GET and DELETE with OpenAPI/errors. **Acceptance:** integration tests cover `201/200/204/404/400/413/415` and atomic replacement.
11. **P2-11 — Add chat, health and error handling.** **Acceptance:** integration tests cover answer, abstention, provider failure, serialization and trace ID.
12. **P2-12 — Create retrieval and grounding evaluation.** Add a synthetic dataset and reproducible report. **Acceptance:** reports Recall@k, MRR, citation validity and abstention without network access; record a baseline before setting thresholds.
13. **P2-13 — Harden, document and validate end to end.** Safe logs, per-stage metrics, loopback and local setup. **Acceptance:** `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` pass; local flow runs from a clean checkout; Project 1 remains unchanged and green.

## 16. Definition of Done

- Every PRD FR/NFR has associated implementation and verification, or an explicitly agreed exception.
- The service runs locally with SQLite and Ollama by following documentation; normal tests do not require Azure.
- Ingestion, replacement, listing, deletion and restart do not leave stale chunks.
- Answers use retrieved evidence; citations are validated against current IDs; insufficient evidence triggers deterministic abstention.
- Provider, persistence, parser and output errors are explicit, sanitized and include a trace ID.
- Clean Architecture, nullable/analyzers, async/cancellation, central package management and versioned prompts are respected.
- Unit/integration/evaluation tests are deterministic; the 10k-chunk target is measured on documented hardware.
- Project 1 code, contracts and behavior are not modified or broken; solution build and tests continue to pass.
- README, OpenAPI, architecture and ADRs reflect actual behavior. Secrets, SQLite databases, downloaded models and sensitive real runbooks are not committed.
