# DevOps AI Assistant — General Architecture

**Document type:** Architecture Spine  
**Scope:** Projects 1–4  
**Primary stack:** .NET 10 / C#  
**Architecture style:** Clean Architecture + modular monolith foundations  
**Method:** BMAD  
**Status:** Initial baseline

---

## 1. Purpose

This document is the shared architectural contract for the four projects that make up the **DevOps AI Assistant**:

1. Incident Update Polisher — LLM
2. Runbook Chatbot — RAG
3. Deployment Assistant — Tool Calling
4. Claude Integration — MCP

The projects live in one repository and evolve progressively. Each project can be developed and executed independently, while shared domain/application/infrastructure components may be reused when the behavior and ownership are truly common.

### Architectural goals

- Keep the solution easy to start and easy to extend.
- Preserve clean dependency boundaries.
- Apply SOLID consistently.
- Keep AI-provider details outside business logic.
- Make prompts, AI policies, tools, and integrations replaceable.
- Prefer deterministic application code over implicit model behavior.
- Make dangerous operations explicit, validated, authorized, confirmed, and auditable.
- Allow Projects 2–4 to build on earlier capabilities without coupling the codebase to one UI or one AI vendor.

---

## 2. Product Evolution

```text
                    DEVOPS AI ASSISTANT
                           |
          +----------------+----------------+
          |                                 |
      PROJECT 1                         PROJECT 2
         LLM                                RAG
          |                                 |
          +----------------+----------------+
                           |
                       PROJECT 3
                    TOOL CALLING
                           |
                    Deployment API
                           ^
                           |
                       PROJECT 4
                          MCP
                           ^
                           |
                        Claude
```

The progression is intentional:

```text
LLM -> RAG -> Tool Calling -> MCP
```

Project 1 proves reliable AI generation.

Project 2 adds grounding in operational knowledge.

Project 3 adds controlled actions.

Project 4 exposes those actions through MCP so an external AI client can use them.

---

## 3. Repository Structure

```text
devops-ai-assistant/
|
+-- src/
|   +-- DevOpsAI.Domain/
|   +-- DevOpsAI.Application/
|   +-- DevOpsAI.Infrastructure/
|   |
|   +-- DevOpsAI.Api/                 # Project 1 HTTP API
|   +-- DevOpsAI.Rag.Api/             # Project 2 HTTP API
|   +-- DevOpsAI.Deployment.Api/      # Project 3 system-of-record API
|   +-- DevOpsAI.Mcp/                 # Project 4 MCP server
|   |
|   +-- DevOpsAI.Contracts/            # Cross-process DTOs only when required
|   |
|   +-- DevOpsAI.Web/                  # Optional shared UI, added when needed
|
+-- tests/
|   +-- DevOpsAI.UnitTests/
|   +-- DevOpsAI.ApplicationTests/
|   +-- DevOpsAI.IntegrationTests/
|   +-- DevOpsAI.ContractTests/
|
+-- docs/
|   +-- adr/
|   +-- prompts/
|   +-- runbooks/
|   +-- api/
|
+-- infra/
|   +-- docker/
|   +-- azure/
|
+-- .github/
|   +-- workflows/
|
+-- AGENTS.md
+-- ARCHITECTURE.md
+-- PROJECT-1-ARCHITECTURE.md
+-- README.md
+-- Directory.Build.props
+-- Directory.Packages.props
+-- .editorconfig
+-- .gitignore
```

### Important rule

Do not create shared code "just in case".

A component belongs in `Domain`, `Application`, or `Infrastructure` only when at least two projects genuinely share the same concept or behavior. Prefer local ownership first.

---

## 4. Architectural Style

Use **Clean Architecture** with explicit dependency direction.

```text
                 +----------------------+
                 |      Presentation    |
                 | API / MCP / Web      |
                 +----------+-----------+
                            |
                            v
                 +----------------------+
                 |     Application      |
                 | Use cases / ports     |
                 +----------+-----------+
                            |
                            v
                 +----------------------+
                 |        Domain        |
                 | Business rules       |
                 +----------------------+

Infrastructure implements Application ports:

Infrastructure ---> Application
Infrastructure ---> Domain

Domain ---> nothing
Application ---> Domain
Presentation ---> Application
```

### Dependency rule

Allowed:

```text
Presentation -> Application -> Domain
Infrastructure -> Application
Infrastructure -> Domain
```

Forbidden:

```text
Domain -> Infrastructure
Domain -> ASP.NET Core
Domain -> EF Core
Domain -> Azure SDK
Domain -> OpenAI/Claude SDK
Application -> HTTP client implementation
Application -> concrete database implementation
```

---

## 5. SOLID Rules

### S — Single Responsibility

One class should have one reason to change.

Prefer:

```text
IncidentPolisher
AudiencePolicy
IncidentPromptBuilder
IncidentResponseParser
```

over:

```text
IncidentServiceThatDoesEverything
```

### O — Open/Closed

Integrations must be replaceable.

Examples:

```csharp
public interface IChatClient
{
    Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken);
}
```

Do not spread Azure OpenAI/Claude-specific types throughout the application.

### L — Liskov Substitution

Implementations of an application port must honor the same contract and failure semantics.

### I — Interface Segregation

Prefer small interfaces:

```text
IIncidentPolisher
IAudiencePolicy
IPromptTemplateProvider
IAiClient
```

Avoid large generic "god interfaces".

### D — Dependency Inversion

Application code depends on interfaces/ports. Infrastructure supplies adapters.

---

## 6. Recommended Technology Stack

### Core

- .NET 10
- C# with nullable reference types enabled
- ASP.NET Core
- Minimal APIs for small HTTP surfaces
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Options
- `CancellationToken` throughout async I/O

### AI

- `Microsoft.Extensions.AI`
- Provider adapter for Azure OpenAI / Azure AI Foundry
- Provider-specific SDKs only inside Infrastructure
- Structured output where supported
- Streaming only where it improves UX

### Data

- SQLite for the learning/demo environment
- EF Core
- Azure AI Search for Project 2 vector search
- Azure Blob Storage only when document storage becomes necessary

### Testing

- xUnit
- NSubstitute
- ASP.NET Core integration testing
- Testcontainers when a real service dependency is required locally

### Quality

- `dotnet format`
- analyzers enabled
- nullable enabled
- warnings treated as errors where practical
- static analysis in CI
- OpenTelemetry for logs/metrics/traces when the project needs distributed observability

### DevOps

- GitHub Actions initially
- Docker for reproducible local execution
- Azure deployment definitions under `infra/azure/`

---

## 7. AI Integration Boundary

AI is an infrastructure concern, not a domain concern.

### Preferred flow

```text
Use Case
   |
   v
Application AI Port
   |
   v
Infrastructure Adapter
   |
   v
Microsoft.Extensions.AI
   |
   v
Provider
```

The application should know:

```text
"I need a structured incident summary."
```

It should not know:

```text
"I need AzureOpenAIChatClient with deployment XYZ."
```

### Model configuration

Keep these outside code:

```text
AI_PROVIDER
AI_MODEL
AI_ENDPOINT
AI_API_KEY
AI_TEMPERATURE
AI_MAX_OUTPUT_TOKENS
```

Use strongly typed options.

Never commit secrets.

---

## 8. Prompt Architecture

Prompts are application assets, not random strings embedded across services.

Recommended:

```text
docs/
  prompts/
    incident-polisher/
      system.md
      leadership.md
      customer.md
```

Version prompt changes like code.

Each prompt should define:

- Purpose
- Expected input
- Hard constraints
- Output contract
- Non-goals
- Example inputs/outputs only when useful

### AI safety convention

The model must not invent facts.

When information is missing, the application should prefer:

```text
unknown
not provided
insufficient information
```

over fabricated details.

---

## 9. API Conventions

Use resource-oriented endpoints and explicit request/response contracts.

Example:

```http
POST /api/incidents/polish
```

Request:

```json
{
  "incidentNote": "db slow, restarted pod, looks ok now",
  "audience": "leadership"
}
```

Response:

```json
{
  "summary": "...",
  "impact": "...",
  "actionsTaken": ["..."],
  "currentStatus": "...",
  "nextSteps": ["..."]
}
```

### HTTP rules

- `400` for invalid client input
- `401/403` for authentication/authorization when introduced
- `404` when a requested resource does not exist
- `409` for state conflicts
- `422` only when semantic validation justifies it
- `500` for unexpected server errors

Do not expose provider exceptions, stack traces, secrets, or internal infrastructure details.

---

## 10. Error Handling

Use a consistent error contract.

Example:

```json
{
  "code": "AI_PROVIDER_UNAVAILABLE",
  "message": "The AI provider is temporarily unavailable.",
  "traceId": "..."
}
```

Do not return raw exception messages to clients.

Expected failures should be represented explicitly.

Unexpected failures should be handled centrally.

---

## 11. Validation

Validate at the application boundary.

Examples:

```text
incidentNote:
- required
- not empty
- maximum length defined
```

```text
audience:
- enum-like value
- leadership
- customer
```

Do not rely on the LLM to validate business input.

Business-critical validation must be deterministic.

---

## 12. Observability

Every request should have a trace/correlation identifier.

Log:

- request received
- use case started/completed
- external dependency call started/completed
- elapsed time
- failure category
- safe operational metadata

Never log:

- API keys
- authorization tokens
- sensitive credentials
- full prompts/responses when they may contain sensitive data

For AI calls, capture safe operational metrics such as:

```text
provider
model
latency
success/failure
input/output token usage when available
```

---

## 13. Testing Strategy

### Domain

Pure unit tests.

### Application

Unit tests with mocked ports.

### Infrastructure

Integration tests against real adapters where useful.

### API

Integration tests covering HTTP behavior, validation, status codes, and serialization.

### AI output

Do not make core automated tests depend on exact LLM wording.

Instead test:

- request construction
- structured response parsing
- validation
- fallback/error behavior
- provider adapter behavior
- prompt/template selection

For deterministic automated tests, use a fake AI adapter.

---

## 14. Naming Conventions

### Projects

```text
DevOpsAI.<Area>
DevOpsAI.<Area>.Api
DevOpsAI.<Area>.Tests
```

### Classes

PascalCase.

### Methods

PascalCase.

### Variables

camelCase.

### Interfaces

Prefix with `I`.

### Async

Suffix async methods with `Async`.

### DTOs

Use explicit suffixes:

```text
PolishIncidentRequest
PolishIncidentResponse
```

Avoid ambiguous names like:

```text
Data
Result
Info
ResponseModel
```

### Files

One main public type per file.

---

## 15. Repository Conventions

Use:

- one solution file
- central package management
- `.editorconfig`
- nullable reference types
- analyzers
- XML documentation only where it adds value
- small focused commits
- Conventional Commits

Examples:

```text
feat(project-1): add incident polishing use case
test(project-1): add polishing application tests
refactor(shared): extract ai client port
docs(architecture): clarify ai boundary
```

---

## 16. BMAD Development Workflow

Use BMAD to preserve decisions between agent sessions.

Recommended lifecycle:

```text
Idea / requirement
        |
        v
Planning / specification
        |
        v
Architecture spine
        |
        v
Epic / tickets
        |
        v
Implementation
        |
        v
Review
        |
        v
Tests
        |
        v
Refinement
```

For this repository:

1. Keep `ARCHITECTURE.md` as the shared technical source of truth.
2. Create project-specific architecture/specification documents for each project.
3. Break implementation into small stories/tickets.
4. Build one story at a time.
5. Start each major implementation session with the relevant architecture/specification context.
6. After implementation, run tests and review for architectural drift.
7. Update architecture/ADR only when a durable decision changes.

### BMAD sizing rule

Prefer small implementation slices that can be understood, implemented, tested, and reviewed in one focused session.

Do not ask an agent to "build the entire project" in one run.

---

## 17. BMAD Agent Responsibilities

Recommended roles:

### Analyst / PM

Own:

- problem definition
- scope
- acceptance criteria
- non-functional requirements

### Architect

Own:

- boundaries
- dependencies
- technology decisions
- cross-project consistency
- ADRs

### Dev

Own:

- implementation
- tests
- refactoring
- keeping code aligned with architecture

### QA / Test Architect

Own:

- risk analysis
- test strategy
- acceptance verification

### Reviewer

Check:

- SOLID
- dependency direction
- security
- test coverage
- accidental coupling
- unnecessary abstraction

---

## 18. Architecture Decision Records

Store durable decisions under:

```text
docs/adr/
```

Example:

```text
ADR-001-use-clean-architecture.md
ADR-002-ai-provider-abstraction.md
ADR-003-shared-deployment-api.md
```

Create an ADR when a decision would materially affect future projects.

Do not create an ADR for trivial implementation details.

---

## 19. Security Baseline

Even in a demo repository:

- never commit credentials
- use environment variables or local secret storage
- validate all tool parameters
- authorize dangerous actions
- require human confirmation for destructive operations
- maintain an audit record for destructive operations
- enforce environment boundaries
- never allow an LLM to bypass application authorization

For production-like behavior:

```text
LLM intent
   |
   v
Application validation
   |
   v
Authorization
   |
   v
Human confirmation if required
   |
   v
Tool execution
```

---

## 20. Scalability Rules

Do not prematurely introduce microservices.

Start as a modular monolith with clean boundaries.

Extract a service only when there is a concrete reason:

- independent deployment
- independent scaling
- distinct security boundary
- operational isolation
- external integration requirement

Project 3's Deployment API is the first natural candidate for an independently deployable service because Project 4 consumes it.

---

## 21. Definition of Done

A story is done when:

- implementation follows dependency boundaries
- validation is deterministic
- tests cover important behavior
- no secrets are committed
- errors are handled consistently
- logs are safe
- public contracts are documented
- the solution builds cleanly
- relevant tests pass
- architecture documentation is updated when a durable decision changed

---

## 22. Non-Negotiable Rules

1. Domain never depends on Infrastructure.
2. Application never depends on concrete AI providers.
3. AI never owns business-critical validation.
4. LLM output is treated as untrusted input.
5. Dangerous operations require deterministic validation and authorization.
6. Do not duplicate a concept across projects without an explicit reason.
7. Do not introduce abstractions with no current use.
8. Prefer composition over inheritance.
9. Keep methods small and cohesive.
10. Every new project must preserve the shared architecture.

---

## 23. Project Roadmap

```text
Project 1
LLM
|
+-- AI abstraction
+-- prompts
+-- structured output
+-- validation
+-- tests
|
v
Project 2
RAG
|
+-- document ingestion
+-- chunking
+-- embeddings
+-- vector search
+-- citations
|
v
Project 3
Tool Calling
|
+-- Deployment API
+-- service health
+-- deployment history
+-- action tools
+-- authorization
+-- confirmation
|
v
Project 4
MCP
|
+-- C# MCP server
+-- expose health tool
+-- expose restart tool
+-- expose deployment history
+-- Claude client integration
```

This architecture is intentionally stable enough to support the four projects while allowing implementation details to evolve.
