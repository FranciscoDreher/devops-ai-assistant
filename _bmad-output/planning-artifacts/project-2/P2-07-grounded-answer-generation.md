# P2-07 - Grounded answer generation and citation validation

- ID: P2-07
- Título: Grounded answer generation and citation validation
- Tipo: User Story

## Descripción
Como usuario del chatbot, necesito que cada respuesta esté basada únicamente en texto recuperado del corpus y que las referencias sean verificables para poder confiar en la respuesta sin asumir que el modelo inventa hechos.

## Objetivo
Definir el flujo de generación de respuestas grounded con validación de citas, abstención cuando no hay evidencia suficiente y salida estructurada para la API.

## Contexto técnico
El proyecto no permite responder con conocimiento externo al contexto recuperado. La respuesta debe ser generada a partir de fragmentos relevantes, con citas que apunten al documento y fragmento específicos, nunca a una fuente inventada o parcialmente soportada.

## Alcance específico
- Definir `IRunbookAnswerGenerator` y `IRunbookPromptProvider`.
- Definir el formato de prompt y salida con restricciones de groundedness.
- Establecer la regla de “respuesta insuficiente” cuando el corpus no aporta evidencia suficiente.
- Definir validación determinista de citas: solo se aceptan referencias asociadas a chunks recuperados.
- Preparar la capa para respuestas con estructura y metadatos útiles para la API.

## Criterios de aceptación verificables
1. La respuesta final se construye en base a chunks recuperados y no a conocimiento libre del modelo.
2. Cada respuesta exitosa incluye referencias a documentos/chunks válidos y recuperados.
3. Cuando la evidencia es insuficiente, la respuesta indica claramente que no puede responder con la información disponible.
4. La validación de citas es determinista y falla si la respuesta menciona fuentes que no se recuperaron.
5. Los proveedores de generación y prompts están ocultos detrás de puertos de Application.

## Componentes afectados
- `src/DevOpsAI.Rag.Application/Chat/`
- `src/DevOpsAI.Rag.Application/Ports/`
- `src/DevOpsAI.Rag.Infrastructure/Generation/`
- `src/DevOpsAI.Rag.Infrastructure/Prompts/`
- `docs/prompts/runbook-chatbot/`

## Dependencias
- Depende de `P2-06`.
- Requiere la existencia de candidatos recuperados, prompts y formato de generación compatible con citas.

## Tests esperados
- Pruebas unitarias de groundedness y validación de citas.
- Casos de respuesta insuficiente.
- Casos de respuesta con referencias válidas y con fuentes no recuperadas.

## Definition of Done
- La generación de respuestas queda separada de la recuperación y el prompt es gestionado como recurso versionado.
- Las respuestas cuentan con una política explícita de evidencia mínima.
- La API puede consumir respuestas estructuradas con referencias verificables.
