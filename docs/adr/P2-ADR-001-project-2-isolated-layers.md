# P2-ADR-001: Isolate Project 2 Layers

- **Status:** Accepted for the Project 2 MVP
- **Date:** 2026-11-11

## Context

The repository implements a progressive DevOps assistant and already contains Project 1. Project 2 adds document processing, retrieval and citations. Project 1's `IAiTextGenerator` is a generic text port, but it resides in Project 1's Application assembly alongside incident-specific use cases and composition. Making RAG depend on that assembly would couple the projects and hinder their independent evolution.

## Decision

Create `DevOpsAI.Rag.Domain`, `DevOpsAI.Rag.Application`, `DevOpsAI.Rag.Infrastructure` and `DevOpsAI.Rag.Api` with their own ports and use cases. Project 2 will have no project references to Project 1. Reuse repository conventions and framework/package decisions, not incident-specific classes. Consider extracting a shared AI contract only when multiple implemented use cases demonstrate a stable common responsibility.

## Consequences

- Project 2 can evolve independently without risking changes to Project 1 behavior.
- Similar ports may initially have local contracts; this avoids dependencies between projects and layers.
- Any solution-level dependency change requires Project 1 regression checks.
