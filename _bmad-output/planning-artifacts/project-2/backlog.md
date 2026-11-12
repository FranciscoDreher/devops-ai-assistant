# Project 2 Backlog — Runbook Chatbot (RAG)

## Objetivo

Este backlog define un conjunto de historias de usuario independientes y ordenadas por dependencia técnica para construir la MVP de Project 2. La prioridad refleja la secuencia mínima necesaria para soportar validación, persistencia, recuperación y generación con referencias verificables, sin introducir decisiones arquitectónicas fuera del alcance del proyecto.

## Orden recomendado de implementación

1. [P2-01 - Solution skeleton and project boundaries](./P2-01-solution-skeleton.md)
2. [P2-02 - Domain models and core invariants](./P2-02-domain-models.md)
3. [P2-03 - Application document lifecycle use cases](./P2-03-document-lifecycle-use-cases.md)
4. [P2-04 - SQLite persistence and document metadata storage](./P2-04-sqlite-persistence.md)
5. [P2-05 - Deterministic chunking and indexing pipeline](./P2-05-chunking-indexing-pipeline.md)
6. [P2-06 - Embedding generation and retrieval contract](./P2-06-embedding-retrieval-contract.md)
7. [P2-07 - Grounded answer generation and citation validation](./P2-07-grounded-answer-generation.md)
8. [P2-08 - Document management API endpoints](./P2-08-document-management-api.md)
9. [P2-09 - Chat query API and insufficient-evidence handling](./P2-09-chat-query-api.md)
10. [P2-10 - Automated validation, evaluation and observability](./P2-10-validation-observability.md)

## Primera historia lista para implementar

La primera historia lista para implementar es [P2-01 - Solution skeleton and project boundaries](./P2-01-solution-skeleton.md). Proporciona la base mínima de estructura, referencias y configuración sin introducir lógica de negocio ni decisiones de flujo RAG.
