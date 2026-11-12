# P2-08 - Document management API endpoints

- ID: P2-08
- Título: Document management API endpoints
- Tipo: User Story

## Descripción
Como operador de la base de conocimiento, necesito una API HTTP para añadir, reemplazar, listar y eliminar documentos para mantener el corpus disponible para consultas sin intervención manual sobre archivos locales.

## Objetivo
Exponer los casos de uso de documentos a través de endpoints HTTP con contratos de DTO, códigos de estado y manejo de errores consistentes.

## Contexto técnico
La API de Project 2 debe seguir el patrón Minimal API y separar transporte, composición de dependencias y contratos de entrada/salida. Los endpoints deben mapear solicitudes HTTP sobre los casos de uso de Application sin filtrar infraestructura o detalles de SQLite.

## Alcance específico
- Definir endpoints para `PUT /api/documents/{documentId}` y `DELETE /api/documents/{documentId}`.
- Definir consultas de listado y recuperación de estado de documentos.
- Validar la entrada HTTP en el transporte y mantener la lógica de negocio en Application.
- Definir códigos de error para validación, no encontrado, contenido inválido y falla técnica.
- Garantizar que notas, errores y logs no revelen contenido sensitivo.

## Criterios de aceptación verificables
1. La API expone solo contratos HTTP y no tipos de infraestructura directa.
2. Cada operación devuelve un resultado claro: éxito, validación fallida, recurso inexistente o error inesperado.
3. Un documento con contenido inválido no se indexa ni reemplaza estado previo.
4. Las operaciones de listado y borrado reflejan el estado real del corpus.
5. El transporte usa la capa de aplicación como interfaz lógica y no lógica de persistencia.

## Componentes afectados
- `src/DevOpsAI.Rag.Api/Endpoints/`
- `src/DevOpsAI.Rag.Api/Contracts/`
- `src/DevOpsAI.Rag.Api/ErrorHandling/`
- `src/DevOpsAI.Rag.Api/Program.cs`
- `src/DevOpsAI.Rag.Api/DependencyInjection.cs`

## Dependencias
- Depende de `P2-03`, `P2-04` y `P2-05`.
- Requiere las decisiones de estado de documento y de validación para que la API sea consistente.

## Tests esperados
- Pruebas de integración de endpoints con TestHost.
- Casos de error HTTP para validación, recurso inexistente y errores de infraestructura.
- Verificación de respuestas del documento antes y después del reemplazo.

## Definition of Done
- Los endpoints de documentos están implementados y documentados por contrato.
- La API refleja el estado real del corpus local sin exponer detalles de almacenamiento.
- Las respuestas de error y estado están claramente definidas para operaciones críticas.
