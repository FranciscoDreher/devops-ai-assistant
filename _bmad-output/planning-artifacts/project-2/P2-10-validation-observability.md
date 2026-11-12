# P2-10 - Automated validation, evaluation and observability

- ID: P2-10
- Título: Automated validation, evaluation and observability
- Tipo: User Story

## Descripción
Como equipo que valida el MVP, necesitamos pruebas reproducibles y observabilidad suficiente para confirmar que el sistema recupera información relevante, genera respuestas con citas válidas y no se rompe en contextos de error o reinicio del proceso.

## Objetivo
Establecer la capa de validación automatizada, métricas de evaluación y trazabilidad operacional para la MVP del RAG local.

## Contexto técnico
El proyecto exige mediciones offline sobre corpus controlado y validación contra respuestas esperadas. Además, la solución no debe depender de claves reales ni de LLMs externos en pruebas normales; la observabilidad debe registrar duraciones y estados sin exponer contenido sensible.

## Alcance específico
- Crear conjunto básico de pruebas de aplicación e integración para generación, citas y recuperación.
- Definir una estrategia de evaluación local con preguntas tipo y nivel de relevancia conocido.
- Registrar métricas básicas de latencia, resultados correctos, respuestas con citas y casos de insuficiente evidencia.
- Verificar la coherencia de resultados tras reinicio del proceso y cambios de documento.
- Preparar el estado para futuras mejoras post-MVP sin introducir complejidad innecesaria.

## Criterios de aceptación verificables
1. Las pruebas automatizadas pueden ejecutarse sin claves reales ni acceso a redes externas.
2. La evaluación mide al menos recuperación relevante y validez de citas.
3. Las respuestas con contenido insuficiente se distinguen claramente de fallas técnicas.
4. Los logs y métricas no contienen contenido sensible de usuario o documentos.
5. La colección local se valida al menos en una secuencia de ingestión, consulta, reemplazo y eliminación.

## Componentes afectados
- `tests/DevOpsAI.Rag.UnitTests/`
- `tests/DevOpsAI.Rag.ApplicationTests/`
- `tests/DevOpsAI.Rag.IntegrationTests/`
- `docs/prompts/runbook-chatbot/`
- `docs/api/`
- `src/DevOpsAI.Rag.Api/ErrorHandling/`

## Dependencias
- Depende de todas las historias anteriores.
- Se ejecuta cuando el MVP ya puede soportar flujo real de consultas y persistencia.

## Tests esperados
- Suite completa de pruebas de integración de flujo end-to-end.
- Evaluación con corpus controlado y comprobación de tasa de citas válidas.
- Validación de observabilidad y logging seguro.

## Definition of Done
- El proyecto cuenta con un conjunto de pruebas automatizadas que validan la funcionalidad central del MVP.
- Las respuestas, citas y recuperación se pueden evaluar sin depender de un entorno de producción.
- La solución entrega una base sólida de verificación y observabilidad para futuras evoluciones de Project 2.
