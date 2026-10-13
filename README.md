# DevOps AI Assistant — Project 1

Incident Update Polisher is a .NET 10 Minimal API that turns a rough incident note into a structured status update for a leadership or customer audience.

## Project structure

```text
src/
  DevOpsAI.Api/             HTTP endpoints, composition root, and API error handling
  DevOpsAI.Application/     Incident-polishing use case, validation, and application ports
  DevOpsAI.Domain/          Incident audience and polished incident model
  DevOpsAI.Infrastructure/  AI adapter, options, and infrastructure implementations
tests/
  DevOpsAI.UnitTests/
  DevOpsAI.ApplicationTests/
  DevOpsAI.IntegrationTests/
docs/prompts/incident-polisher/
  system.md
  leadership.md
  customer.md
```

The dependency direction follows Clean Architecture: API → Application → Domain. Infrastructure implements application-owned ports and may depend on Application and Domain; Domain does not depend on the other layers or provider SDKs.

## Prerequisites

- .NET 10 SDK
- No external service is needed to build or run the automated tests.

## Run locally

From the repository root:

```bash
dotnet run --project src/DevOpsAI.Api/DevOpsAI.Api.csproj --launch-profile http
```

The `http` launch profile starts the API at `http://localhost:5198` in Development. Check `GET /health` for the health endpoint. The OpenAPI document is available at `GET /openapi/v1.json` in Development.

To stop the API, press `Ctrl+C`.

## AI configuration

The API reads the `Ai` configuration section. Environment variables use the standard `Ai__...` names:

| Environment variable | Purpose | Default |
| --- | --- | --- |
| `Ai__Endpoint` | Provider endpoint | Empty |
| `Ai__ApiKey` | Provider credential | Empty |
| `Ai__Model` | Model identifier | `gpt-4o-mini` |
| `Ai__TimeoutSeconds` | Generation timeout in seconds (1–86400) | `30` |

For example, set values in your local shell or an approved secret store; replace every placeholder with values from your provider:

```bash
export Ai__Endpoint="<provider-endpoint>"
export Ai__ApiKey="<local-secret>"
export Ai__Model="<provider-model>"
export Ai__TimeoutSeconds="30"
```

Do not put real keys in source control, committed settings files, or documentation. The committed `appsettings.json` contains empty endpoint and key values.

**Current integration limitation:** the API currently registers a placeholder `IChatClient`, not a provider-specific implementation. Setting these options alone does not enable AI generation; `POST /api/incidents/polish` returns `500` with `AI_GENERATION_FAILED` until an `IChatClient` provider is registered. The API, health endpoint, OpenAPI document, and tests can still be run locally.

## API

`POST /api/incidents/polish` accepts `leadership` or `customer` as the audience. `incidentNote` is required and limited to 4,000 characters. Invalid notes or audiences return `400`; AI generation or output failures return `500`.

Request:

```http
POST /api/incidents/polish
Content-Type: application/json

{
  "incidentNote": "Database latency increased after deployment and users experienced degraded performance. Restarted the affected pod and traffic recovered.",
  "audience": "leadership"
}
```

Expected success response shape (illustrative; a provider implementation must be registered for a successful generation):

```json
{
  "summary": "Database latency increased after deployment and has since improved.",
  "impact": "Users experienced degraded performance.",
  "actionsTaken": [
    "Restarted the affected pod"
  ],
  "currentStatus": "Traffic has recovered.",
  "nextSteps": [
    "Continue monitoring database performance."
  ]
}
```

The generated wording depends on the input and model. The application validates the request and the generated JSON structure; model output is not an authorization or safety boundary.

You can also use [DevOpsAI.Api.http](./src/DevOpsAI.Api/DevOpsAI.Api.http) with the API running locally.

## Prompt and use-case flow

The versioned prompt files are under [`docs/prompts/incident-polisher/`](./docs/prompts/incident-polisher/):

- `system.md` defines the assistant role, factuality constraints, tone, and JSON expectation.
- `leadership.md` provides concise, business-focused guidance.
- `customer.md` provides plain-language, customer-facing guidance.

For each request, the application validates the note and audience, loads the system and selected audience prompts, combines them with the incident note and expected JSON fields, calls the AI text-generation port, and validates/deserializes the returned structure. The API maps the result or a safe error response to HTTP.

## Build and test

From the repository root:

```bash
dotnet build devops-ai-assistant.sln
dotnet test devops-ai-assistant.sln
```

The integration tests use a fake AI generator; they do not call an LLM or require external network access.
