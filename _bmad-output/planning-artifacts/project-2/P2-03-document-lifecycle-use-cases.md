# P2-03 - Application document lifecycle use cases

- ID: P2-03
- Título: Application document lifecycle use cases
- Tipo: User Story

## Descripción
Como usuario del sistema, necesito poder registrar, reemplazar, listar y borrar documentos del índice local para mantener el corpus de conocimiento sincronizado con los runbooks que están realmente disponibles.

## Objetivo
Definir los casos de uso de aplicación para el ciclo de vida de documentos, con políticas de validación deterministas y contratos explícitos de entrada/salida antes de implementar cualquier almacenamiento real.

## Contexto técnico
Los documentos serán Markdown/TXT, requieren validación inicial (ID, extensión, tamaño, UTF-8, contenido no vacío) y se gestionarán con acciones explícitas de reemplazo y eliminación. El flujo debe ser determinista, sin depender del modelo para decidir si el documento es válido o suficiente para responder.

## Alcance específico
- Definir casos de uso para `ReplaceRunbookDocument`, `DeleteRunbookDocument` y `ListRunbookDocuments`.
- Definir un resultado normalizado para documentos y una clasificación de estado: creado, reemplazado, eliminado, error de validación.
- Definir las políticas de validación para extensión, tamaño y contenido no vacío.
- Preparar la capa Application para consumir puertos de persistencia y recuperación sin conocer la implementación.

## Criterios de aceptación verificables
1. Cada caso de uso tiene una entrada y salida explícita con nombres de request/response claros.
2. La validación de contenido se produce en Application antes de persistir o realizar indexación.
3. El reemplazo de un documento produce un único estado consistente para nuevas consultas y elimina referencias previas a versiones antiguas.
4. La eliminación de un documento no deja chunks o vectores activos para consultas posteriores.
5. Los casos de uso no conocen SQLite, Ollama ni ASP.NET Core.

## Componentes afectados
- `src/DevOpsAI.Rag.Application/Documents/`
- `src/DevOpsAI.Rag.Application/Configuration/`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`
- `src/DevOpsAI.Rag.Api/Contracts/`

## Dependencias
- Depende de `P2-02`.
- Requiere la estructura del dominio para documentar metadatos y expectativas de reemplazo.

## Tests esperados
- Pruebas unitarias de validación de ID, extensión, tamaño y contenido.
- Pruebas de manejo de errores y resultados de reemplazo y eliminación.
- Verificación del contrato entre Application e Infrastructure.

## Definition of Done
- Los casos de uso de documento están definidos y se alinean con la política de validación establecida.
- El flujo de reemplazo y eliminación está especificado con estados deterministas.
- La API puede implementar estos casos de uso sin decisiones de almacenamiento ocultas.
