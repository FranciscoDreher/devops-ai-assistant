# P2-ADR-002: Use SQLite as the MVP Local Vector Index

- **Status:** Accepted for the Project 2 MVP
- **Date:** 2026-11-11

## Context

The MVP must run locally with minimal infrastructure, preserve its index across restarts, support source lifecycle operations and demonstrate vector search. The PRD limits the acceptance corpus to 10,000 chunks and requires retrieval under 500 ms, excluding embeddings and generation.

## Decision

Persist source metadata, text, chunks and embedding vectors in an EF Core SQLite database. Store normalized vectors as binary data and implement bounded cosine-similarity ranking in Project 2's Infrastructure adapter. Keep Application's dependency behind `IRunbookKnowledgeIndex`. Azure AI Search is a future adapter; Qdrant and PostgreSQL/pgvector remain alternatives only if measurements rule out SQLite.

## Consequences

- Local execution requires neither Docker nor a separate vector database; persistence is a file.
- Similarity scanning is linear in the number of chunks and dimensions; the 10k/500 ms benchmark is an acceptance condition.
- SQLite is not distributed or multi-writer production storage. Azure migration requires reindexing and evaluation of score/threshold differences.
- Schema changes require versioned migrations; document the database path and backup procedure.
