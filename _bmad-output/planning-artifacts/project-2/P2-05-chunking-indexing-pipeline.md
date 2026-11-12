# P2-05 - Deterministic chunking and indexing pipeline

- ID: P2-05
- Título: Deterministic chunking and indexing pipeline
- Tipo: User Story

## Descripción
Como sistema de recuperación documental, necesitamos dividir los documentos en fragments reutilizables y indexarlos con metadatos que permitan búsqueda contextual, recuperación y referencias exactas.

## Objetivo
Crear el pipeline de ingestión y indexación que convierta documentos validados en chunks deterministas, con referencias de origen y capacidad de actualización limpia.

## Contexto técnico
El proyecto exige un chunker determinista para Markdown/TXT capaz de dividir por headings, párrafos y tamaño máximo sugerido; el objetivo es mantener los fragmentos útiles para recuperación semántica y las citas trazables a documentos y líneas o secciones relevantes.

## Alcance específico
- Definir el proceso de extracción de texto básico para Markdown/TXT sin ejecutar contenido.
- Establecer la estrategia de chunking y superposición por tamaño máximo y estructura del documento.
- Crear el flujo de indexación que asocie cada chunk con un documento, su ubicación y un hash de contenido.
- Integrar el pipeline con los puertos de persistencia e indexación.
- Preparar la lógica para reemplazo limpio y eliminación de chunks antiguos.

## Criterios de aceptación verificables
1. El pipeline procesa documentos válidos en chunks deterministas sin depender del contenido del modelo o de proveedores externos.
2. Cada chunk conserva metadata de origen suficiente para construir citas verificables.
3. Un documento reemplazado destruye o invalida los chunks previos antes de aceptar nuevas consultas.
4. El proceso no interpreta ni ejecuta el contenido del archivo; solo extrae texto plano y lo fragmenta.

## Componentes afectados
- `src/DevOpsAI.Rag.Application/Documents/`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`
- `src/DevOpsAI.Rag.Infrastructure/DependencyInjection.cs`
- `tests/DevOpsAI.Rag.ApplicationTests/`

## Dependencias
- Depende de `P2-03` y `P2-04`.
- Requiere que el modelo de documento y persistencia estén estabilizados.

## Tests esperados
- Pruebas unitarias de chunking determinista con textos de diferentes estructuras.
- Verificación de que los chunks conservan metadatos de origen.
- Pruebas de reemplazo para asegurar limpieza de chunks antiguos.

## Definition of Done
- Los documentos se convierten en chunks y se almacenan de forma consistente.
- El índice local permite recuperación posterior sin depender de un modelo de generación.
- Queda claro cómo se traducen las citas del resultado a ubicaciones en documentos fuente.
