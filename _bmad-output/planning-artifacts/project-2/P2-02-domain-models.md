# P2-02 - Domain models and core invariants

- ID: P2-02
- Título: Domain models and core invariants
- Tipo: User Story

## Descripción
Como desarrollador del módulo RAG, necesitamos modelar los conceptos del dominio de documentos, chunks y referencias para poder definir reglas de negocio centrales antes de implementar persistencia y casos de uso.

## Objetivo
Definir los tipos del dominio que representan documentos, fragmentos recuperables, citas y resultados de consulta, manteniendo el dominio independiente de SQLite, ASP.NET Core y proveedores de IA.

## Contexto técnico
El proyecto debe apoyar documentos Markdown/TXT, fragmentación determinista, trazabilidad a fuentes y respuestas con citas verificables. Los modelos del dominio deben encapsular invariantes como identificador, hash, versión y contenido válido, sin depender de detalles de infraestructura o serialización.

## Alcance específico
- Definir el modelo para `KnowledgeDocument` con ID, nombre, contenido, hash, versión y metadatos mínimos.
- Definir `DocumentChunk` con relación a documento, ubicación, texto y metadatos de origen.
- Definir `RetrievalCandidate` y `CitationReference` con identificadores y confianza/soporte.
- Definir `GroundedAnswer` para resultados que solo aceptan fuentes verificables.
- Asegurar que el dominio no contenga acceso a APIs HTTP, bases de datos, SQLite, EF Core ni SDKs de modelos.

## Criterios de aceptación verificables
1. El Domain contiene únicamente entidades y contratos del negocio, sin referencias a infraestructura ni transporte.
2. Cada entidad define al menos una regla de invariancia clara: documento no nulo, chunk con contenido válido, respuesta con citas verificables.
3. Los modelos permiten representar un documento reemplazado o eliminado sin perder trazabilidad del contenido previo.
4. Los tipos del dominio son consumidos por Application y mapeados desde infraestructura, no al revés.

## Componentes afectados
- `src/DevOpsAI.Rag.Domain/Documents/`
- `src/DevOpsAI.Rag.Domain/Chat/`
- `src/DevOpsAI.Rag.Application/Ports/`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`

## Dependencias
- Depende de `P2-01`.
- Requiere la decisión de qué metadatos de documentación deben persistirse y cómo se representan las citas.

## Tests esperados
- Pruebas unitarias de invariantes del dominio.
- Pruebas de validación de entidad: documento vacío, chunk inválido, respuesta sin referencias y respuesta con referencia inválida.
- Verificación de que no se introducen dependencias de infraestructura.

## Definition of Done
- El modelo de dominio está definido y documentado.
- Las reglas críticas están representadas como invariantes o validaciones deterministas.
- El resto del proyecto puede consumir estos tipos sin acceso a detalles de infraestructura.
