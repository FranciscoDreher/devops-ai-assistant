# P2-01 - Solution skeleton and project boundaries

- ID: P2-01
- Título: Solution skeleton and project boundaries
- Tipo: User Story

## Descripción
Como equipo de ingeniería del proyecto, necesitamos una estructura de solución y límites de proyecto claros para Project 2 para poder desarrollar el módulo Runbook Chatbot sin acoplarlo al código de Project 1 ni introducir dependencias no previstas.

## Objetivo
Crear la base mínima del módulo RAG: proyectos, referencias, configuración compartida, convenciones y separación de responsabilidades entre Domain, Application, Infrastructure y API.

## Contexto técnico
Project 2 debe vivir dentro de la solución actual de .NET y seguir la arquitectura Clean Architecture definida en `ARCHITECTURE.md` y `PROJECT-2-ARCHITECTURE.md`. La base debe separar dominio, casos de uso, puertos, infraestructura y API sin tocar Project 1 ni crear un patrón de reutilización prematuro.

## Alcance específico
- Crear los proyectos de `DevOpsAI.Rag.Domain`, `DevOpsAI.Rag.Application`, `DevOpsAI.Rag.Infrastructure` y `DevOpsAI.Rag.Api` dentro de la solución.
- Configurar referencias correctas siguiendo la dirección `Api -> Application -> Domain` y `Infrastructure -> Application/Domain`.
- Mantener `Project 1` intacto y sin referencias cruzadas.
- Asegurar que el proyecto API sea un host mínimo vía ASP.NET Core con configuración básica.
- Preparar la base para xUnit/NSubstitute y pruebas de integración del módulo.
- Definir el conjunto de paquetes esenciales y configuraciones de formato/análisis del repositorio.

## Criterios de aceptación verificables
1. La solución incluye los cuatro proyectos de Project 2 y todos se agregan a `devops-ai-assistant.sln`.
2. No existen referencias de Project 2 hacia Project 1 ni a SDKs específicos de proveedor desde el Domain o Application.
3. El proyecto API compila con una configuración mínima sin lógica de negocio.
4. El proyecto Infrastructure queda preparado para implementar puertos de persistencia y LLM/RAG y no se mezclan dependencias de infraestructura con Domain.
5. Las pruebas unitarias/integración del módulo cuentan con estructura inicial y ejecutan compilación sin requerir proveedores externos.

## Componentes afectados
- `src/DevOpsAI.Rag.Domain/`
- `src/DevOpsAI.Rag.Application/`
- `src/DevOpsAI.Rag.Infrastructure/`
- `src/DevOpsAI.Rag.Api/`
- `tests/DevOpsAI.Rag.UnitTests/`
- `tests/DevOpsAI.Rag.ApplicationTests/`
- `tests/DevOpsAI.Rag.IntegrationTests/`
- `devops-ai-assistant.sln`
- `Directory.Build.props` / `Directory.Packages.props` (solo si la estructura de paquetes exige ajuste)

## Dependencias
- Depende de la aprobación del alcance del proyecto definido en `ARCHITECTURE.md` y `PROJECT-2-ARCHITECTURE.md`.
- Requiere la existencia de la base del repositorio con .NET 10 y convenios de estilo.

## Tests esperados
- `dotnet build` sobre la solución.
- `dotnet test` para proyectos vacíos o con pruebas de bootstrapping.
- Verificación de que no haya referencias cruzadas no permitidas.

## Definition of Done
- La estructura del módulo existe y se compila.
- Las capas quedan separadas según la arquitectura acordada.
- La API y la infraestructura están listas para recibir casos de uso reales sin introducir lógica de negocio.
- Los archivos de configuración y referencias están documentados y consistentes con el repositorio.
