# P2-06 - Embedding generation and retrieval contract

- ID: P2-06
- Título: Embedding generation and retrieval contract
- Tipo: User Story

## Descripción
Como módulo RAG, necesitamos una capa de embeddings y un contrato de recuperación que permita comparar consultas con chunks y devolver candidatos con relevancia ordenada para que la generación responda solo con evidencia disponible.

## Objetivo
Definir los puertos y la implementación mínima para generar embeddings, consultar el índice vectorial local y devolver candidatos de recuperación con scores y metadatos de fuente.

## Contexto técnico
La arquitectura exige que la generación y la recuperación estén separadas. El sistema local debe soportar integración con `Microsoft.Extensions.AI` y un adaptador local con embeddings para validar end-to-end sin depender de Azure ni de proveedores externos para la MVP.

## Alcance específico
- Definir `ITextEmbeddingGenerator` y su contrato para generar embeddings a partir de texto.
- Definir `IRunbookKnowledgeIndex` para indexar y consultar candidatos relevantes.
- Incorporar la capa de búsqueda por similitud local con threshold configurado y top-k controlado.
- Asegurar que la respuesta de recuperación incluye ids de chunk y puntuación del sistema para pasar a la etapa de generación.
- Preparar la evaluación offline con un conjunto fijo de preguntas y relevancia conocida.

## Criterios de aceptación verificables
1. La capa Application define los puertos de embeddings y búsqueda, sin depender de implementaciones concretas.
2. La receta de recuperación retorna resultados ordenados por relevancia y con un nivel explícito de suficiencia para la respuesta.
3. Los resultados pueden mapearse a chunks exactos y al documento de origen.
4. La búsqueda de contenido insuficiente se puede distinguir de una falla técnica de proveedor o almacenamiento.

## Componentes afectados
- `src/DevOpsAI.Rag.Application/Ports/`
- `src/DevOpsAI.Rag.Infrastructure/Embeddings/`
- `src/DevOpsAI.Rag.Infrastructure/Persistence/`
- `tests/DevOpsAI.Rag.ApplicationTests/`

## Dependencias
- Depende de `P2-04` y `P2-05`.
- Requiere los chunks indexados y los modelos de entidad para la búsqueda relevante.

## Tests esperados
- Pruebas de similitud y ranking con corpus controlado.
- Pruebas de threshold y selección de top-k.
- Pruebas de insuficiente evidencia frente a errores de infraestructura.

## Definition of Done
- Existe un contrato claro para embeddings y recuperación local.
- La capa de búsqueda puede devolver resultados útiles para una pregunta del corpus real.
- Las decisiones de relevancia y threshold quedan deterministas y comprobables.
