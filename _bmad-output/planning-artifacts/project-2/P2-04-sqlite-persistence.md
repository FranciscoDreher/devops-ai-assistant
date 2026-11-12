# P2-04 - SQLite persistence and document metadata storage

- ID: P2-04
- Título: SQLite persistence and document metadata storage
- Tipo: User Story

## Descripción
Como sistema local de RAG, necesitamos una capa de persistencia fiable que conserve documentos, chunks, metadatos y referencias para que el estado del corpus sobreviva reinicios del proceso y no dependa de memoria temporal.

## Objetivo
Implementar la capa de persistencia Infrastructure para almacenar metadatos del documento, relaciones de chunk y estado de índice, preparando el camino para actualización, consulta y borrado consistentes.

## Contexto técnico
El proyecto adoptó SQLite como almacenamiento local para un MVP sencillo y duradero. La capa de infraestructura debe manejar entidades persistentes, migraciones y accesos de lectura/escritura claros, evitando mezclar detalles de EF Core con la lógica de aplicación.

## Alcance específico
- Definir `RagDbContext` y entidades persistentes para documentos, chunks y referencias.
- Configurar SQLite con migraciones o esquema inicial compatible con la MVP.
- Exponer una implementación concreta del puerto de almacenamiento para documentos y chunks.
- Garantizar la coherencia de actualizaciones y eliminación de encabezados, chunks y metadatos.
- Preparar el soporte para reconstrucción de índice tras reinicio del proceso.

## Criterios de aceptación verificables
1. El almacenamiento persistente conserva el estado del corpus entre reinicios.
2. Las entidades de infraestructura mantienen una correspondencia clara con los modelos de dominio.
3. La eliminación de un documento invalida o borra los chunks asociados y sus referencias.
4. El contexto de persistencia no contiene reglas de negocio ni lógica de respuesta del usuario.
5. Las operaciones de escritura cumplen una semántica explicitamente consistente y atómica para reemplazos.

## Componentes afectados
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`
- `src/DevOpsAI.Rag.Infrastructure/DependencyInjection.cs`
- `src/DevOpsAI.Rag.Application/Ports/`
- `tests/DevOpsAI.Rag.IntegrationTests/`

## Dependencias
- Depende de `P2-02` y `P2-03`.
- Requiere la definición de puertos y estados del documento para mapear a SQLite.

## Tests esperados
- Pruebas de persistencia para crear, actualizar y borrar documentos.
- Pruebas de integridad entre documentos y chunks.
- Pruebas de “reapertura de proceso” para confirmar que los datos persisten correctamente.

## Definition of Done
- La persistencia SQLite está implementada con una estructura de datos estable.
- El estado del corpus es durable y consistente tras reinicio.
- La capa de aplicación puede consumir la persistencia sin conocer la base de datos real.
