# P2-ADR-003: Use Local Ollama Behind Replaceable Ports

- **Status:** Accepted for the Project 2 MVP
- **Date:** 2026-11-11

## Context

The MVP must demonstrate embeddings and answer generation locally without requiring Azure credentials. Provider details must remain outside Application and Domain, with support for a future Azure migration.

## Decision

Use Ollama for local chat inference and embeddings behind the Application-owned `IRunbookAnswerGenerator` and `ITextEmbeddingGenerator` ports. Keep model identifiers, endpoint, timeout and vector dimensions explicit in configuration and index metadata. Azure OpenAI/AI Foundry adapters are future Infrastructure work.

Use OllamaSharp only if its dependency graph can be aligned safely. Metadata checked on 2026-11-11 indicates that OllamaSharp 5.5.2 targets net10.0 and requires Microsoft.Extensions.AI 10.8.0, while the repository centrally pins 10.0.0. The implementation task must evaluate a central update to 10.8.0 and run all Project 1 regression tests. If that is not acceptable, implement a small Ollama HTTP adapter in Infrastructure using standard .NET APIs rather than adding an incompatible package.

## Consequences

- Developers must install Ollama and configure local models; tests do not require it.
- Model quality, hardware requirements and licenses must be verified and documented before defaults are selected.
- Changing the embedding model or dimensions requires recalculating all vectors; vectors from different embedding spaces must never be compared.
- No local runtime or provider type leaks into Application/Domain.
