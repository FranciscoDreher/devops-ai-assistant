# P2-09 - Chat query API and insufficient-evidence handling

- ID: P2-09
- Título: Chat query API and insufficient-evidence handling
- Tipo: User Story

## Descripción
Como usuario de soporte técnico, necesito consultar el corpus usando lenguaje natural y recibir una respuesta basada en evidencia recuperada con referencias verificables, o una abstención clara cuando la información no está disponible.

## Objetivo
Exponer la capa de consulta y respuesta sobre HTTP, incorporando recuperación, generación grounded y manejo de evidencia insuficiente.

## Contexto técnico
La API de chat es la pieza de valor del MVP: debe recuperar chunks relevantes y generar una respuesta útil y segura, sin inventar hechos. La política debe establecer claramente qué ocurre cuando no hay suficientes pasajes para responder una pregunta.

## Alcance específico
- Definir endpoint para `POST /api/chat/query` (o equivalente) con pregunta y contexto del usuario.
- Integrar casos de uso de `AskRunbookQuestion` con recuperación y generación grounded.
- Definir estructura de respuesta con respuesta final, citas, comprobación de evidencia y estado de resultado.
- Establecer el patrón de abstención cuando no se recupera evidencia suficiente o cuando los chunks no dan cobertura.
- Asegurar que el transporte no revela secretos ni llama a proveedores externos durante pruebas sin configuración.

## Criterios de aceptación verificables
1. Una consulta con evidencia suficiente devuelve una respuesta acompañada por referencias a documentos/chunks recuperados.
2. Una consulta sin evidencia suficiente devuelve un estado explícito de insuficiente información y no genera hechos no soportados.
3. La API devuelve resultados estructurados con estado del sistema y referencias verificables.
4. Las respuestas contienen solo información referenciada por el corpus cargado.
5. El transporte y los DTOs no incluyen detalles de infraestructura ni implementación del proveedor.

## Componentes afectados
- `src/DevOpsAI.Rag.Api/Endpoints/`
- `src/DevOpsAI.Rag.Api/Contracts/`
- `src/DevOpsAI.Rag.Application/Chat/`
- `src/DevOpsAI.Rag.Infrastructure/Generation/`

## Dependencias
- Depende de `P2-06`, `P2-07` y `P2-08`.
- Requiere la existencia del pipeline de recuperación, generación grounded y API de documentos.

## Tests esperados
- Pruebas de integración del endpoint con corpus controlado.
- Casos de respuesta correcta y de insuficiente evidencia.
- Verificación de la presencia y validez de citas referencias.

## Definition of Done
- La consulta de chat responde con evidencia verificable y decisiones claras sobre insuficiencia.
- La API presenta un contrato consistente para clientes HTTP.
- Se confirma que el resultado final no depende de conocimiento externo no recuperado.
