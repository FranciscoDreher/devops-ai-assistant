# Project 2 Backlog — Runbook Chatbot (RAG)

## Objective

This backlog defines a set of independent user stories ordered by technical dependencies to build the Project 2 MVP. The priority reflects the minimum sequence needed to support validation, persistence, retrieval, and generation with verifiable references, without introducing architectural decisions outside the project's scope.

## Recommended Implementation Order

1. [P2-01 - Solution skeleton and project boundaries](./P2-01-solution-skeleton.md)
2. [P2-02 - Domain models and core invariants](./P2-02-domain-models.md)
3. [P2-03 - Application document lifecycle use cases](./P2-03-document-lifecycle-use-cases.md)
4. [P2-04 - SQLite persistence and document metadata storage](./P2-04-sqlite-persistence.md)
5. [P2-05 - Deterministic chunking and indexing pipeline](./P2-05-chunking-indexing-pipeline.md)
6. [P2-06 - Embedding generation and retrieval contract](./P2-06-embedding-retrieval-contract.md)
7. [P2-07 - Grounded answer generation and citation validation](./P2-07-grounded-answer-generation.md)
8. [P2-08 - Document management API endpoints](./P2-08-document-management-api.md)
9. [P2-09 - Chat query API and insufficient-evidence handling](./P2-09-chat-query-api.md)
10. [P2-10 - Automated validation, evaluation, and observability](./P2-10-validation-observability.md)

## First Story Ready for Implementation

The first story ready for implementation is [P2-01 - Solution skeleton and project boundaries](./P2-01-solution-skeleton.md). It provides the minimum structure, references, and configuration without introducing business logic or RAG flow decisions.
