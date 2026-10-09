# DevOps AI Assistant — Agent Instructions

## Mission

This repository implements one progressive DevOps AI Assistant:

1. Incident Update Polisher — LLM
2. Runbook Chatbot — RAG
3. Deployment Assistant — Tool Calling
4. Claude Integration — MCP

Before changing code, read `ARCHITECTURE.md` and the relevant project-specific architecture document.

## Non-negotiable architecture

- Preserve Clean Architecture dependency direction.
- Domain must not depend on Infrastructure, ASP.NET Core, EF Core, Azure SDKs, or AI provider SDKs.
- Application depends on Domain and application-owned abstractions.
- Infrastructure implements Application ports.
- API/presentation depends on Application.
- AI provider details belong in Infrastructure.
- LLM output is untrusted input.
- Business-critical validation must be deterministic.
- The LLM must never bypass authorization or safety rules.
- Prefer composition over inheritance.
- Do not introduce abstractions without a current use.
- Keep classes and methods focused.

## BMAD workflow

Architecture → Story/Task → Implementation → Tests → Review → Documentation/ADR

For every story:

1. Read the relevant architecture.
2. Identify acceptance criteria.
3. Implement the smallest coherent change.
4. Add/update tests.
5. Run build, tests, and formatting.
6. Review for SOLID, security, and dependency violations.
7. Update architecture/ADR only when a durable decision changes.
8. Update the task file under planning-artifacts directory after finishing the implementation.

Do not build an entire project in one agent run unless explicitly requested.

## .NET conventions

- Nullable reference types enabled.
- Async I/O with `CancellationToken`.
- No `.Result` or `.Wait()` for async code.
- Dependency injection for external dependencies.
- `IOptions<T>` for grouped configuration.
- Interfaces use `I` prefix.
- Async methods use `Async` suffix.
- Explicit request/response names.
- One primary public type per file.
- Avoid static mutable state.
- Avoid generic `Helper`, `Manager`, `Data`, or `Info` classes when a precise name exists.

## AI boundary

Preferred flow:

Application → Application Port → Infrastructure Adapter → Microsoft.Extensions.AI → Provider

Application code must not reference provider-specific SDK types.
Treat generated content as untrusted. Never allow the model to invent facts or directly enforce authorization.
Use structured output whenever model-generated data is consumed programmatically.

## Prompt rules

Prompts are versioned assets under `docs/prompts/`.
They should define role, goal, hard constraints, output expectations, factuality rules, and audience/context rules.
Avoid large prompt literals in C#.

## Testing

Use xUnit, NSubstitute, and integration tests at HTTP/infrastructure boundaries.
Normal unit tests must not call real LLMs or external networks.
Test structure, validation, mappings, errors, interactions, and invariants rather than exact LLM wording.

Run when applicable:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Security

Never commit secrets.
Never log API keys, tokens, passwords, or sensitive customer information.
Future destructive tools must follow: User intent → validation → authorization → confirmation → execution → audit.
The application is the security boundary, not the prompt.

## Shared code

Start local. Promote code to shared projects only when at least two projects genuinely need the same concept/behavior and sharing does not create coupling.

## Architecture changes

Never silently violate architecture. Prefer the smallest change and document durable decisions in `docs/adr/`.

## Current focus

Project 1 — Incident Update Polisher.
Read `ARCHITECTURE.md` and `PROJECT-1-ARCHITECTURE.md`.
First implementation target: `P1-01 — Solution skeleton`.
Do not implement business logic in P1-01.

## First BMAD implementation instruction

Implement P1-01 — Solution skeleton.

Read ARCHITECTURE.md and PROJECT-1-ARCHITECTURE.md first.

Create only the solution/project structure, project references, baseline configuration, analyzers, formatting rules, and test projects described by P1-01.

Do not implement business logic yet.

Run build and tests before finishing.
Do not introduce technologies not defined by the architecture.
