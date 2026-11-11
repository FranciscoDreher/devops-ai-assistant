# P2-ADR-005: Keep Ingestion Synchronous and Replacement Atomic

- **Status:** Accepted for the Project 2 MVP
- **Date:** 2026-11-11

## Context

The MVP ingests individual Markdown/TXT files through a local API and must not expose partially updated or stale chunks. There are no requirements for background jobs or batch ingestion.

## Decision

Parse and chunk the replacement, and generate all embeddings before changing the active version. Save metadata, content, chunks and vectors in a SQLite transaction. If preparation or commit fails, retain the previous active version. Delete the document and derived records in a transaction. Keep ingestion synchronous until measured payload or latency limits justify a queue.

## Consequences

- The simple local flow needs no queue, worker or additional infrastructure.
- Request duration depends on upload/model limits; explicit limits are required.
- Large or batch ingestion may require asynchronous job state and staging storage; that would be a deliberate architectural change.
- Rollback, repeated upserts, failed embeddings and deletion consistency require integration tests.
