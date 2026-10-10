# P1-06 — HTTP API

## Summary

Expose the use case through an HTTP endpoint with JSON serialization, input validation, error handling, and basic operational documentation for development.

## Type

Story / API ticket

## Status

Completed

## Dependencies

- P1-01
- P1-03
- P1-05

## Objective

Provide a stable HTTP interface so clients can send an incident note and receive a polished update based on the selected audience.

## Scope

### Included

- `POST /api/incidents/polish`
- request/response mapping
- centralized exception handling
- OpenAPI/Swagger in development
- health endpoint
- stable JSON contract

### Excluded

- authentication or authorization
- persistence
- front-end
- risky operations outside this use case

## Acceptance criteria

1. The endpoint responds with `200` for a valid request.
2. The API returns `400` for invalid input.
3. HTTP codes are consistent for validation and application errors.
4. The JSON response follows the public contract defined by the architecture.
5. Swagger/OpenAPI is enabled in development.
6. Errors do not expose secrets or internal traces.

## Work to do

- Configure Minimal API.
- Create the incident endpoint.
- Map the HTTP request to the use-case request.
- Invoke `IIncidentPolisher`.
- Centralize exception handling.
- Add developer-friendly documentation.

## Technical constraints

- The API must depend on Application and not on a concrete AI provider.
- Error handling must be deterministic and safe.

## Verification

- Test the valid endpoint.
- Test the invalid endpoint.
- Verify the response type and JSON schema.
- Check the Swagger/OpenAPI documentation when enabled.

## Risks / notes

- Inconsistent error handling can break clients and complicate diagnosis.
- Technical provider details should not be returned to the client.

## Definition of done

The story ends when the endpoint is accessible, documented, and follows the use-case output contract consistently.

## Changes made

- Added the `POST /api/incidents/polish` endpoint using a minimal API route in the API layer.
- Mapped HTTP request payloads to `PolishIncidentRequest` and mapped domain results back to a stable JSON response contract.
- Added deterministic validation for missing incident notes and unsupported audiences.
- Centralized a safe exception handler returning `ProblemDetails`-style payloads without exposing internal details.
- Added a health endpoint at `/health` and enabled OpenAPI in development.
- Registered API services through a composition root that keeps the API dependent on the application contract rather than a concrete provider.
- Added focused integration tests covering valid and invalid requests.

## Code review

- The API uses the `IIncidentPolisher` application contract instead of reaching into infrastructure-specific providers.
- Validation and provider failures are normalized to a deterministic HTTP contract and safe problem payloads.
- Response serialization remains lightweight and consistent with the domain model.
- The implementation keeps prompt sourcing isolated to the API composition layer while preserving the application dependency direction.

## Implementation notes

- Keep serialization simple and consistent.
- Use `ProblemDetails` or equivalent for structured errors when applicable.
- Prepare the endpoint to evolve with projects 2 and 3 without coupling it to provider details.

### Verification performed

- `dotnet test tests/DevOpsAI.IntegrationTests/DevOpsAI.IntegrationTests.csproj --filter IncidentEndpoint --nologo`

The endpoint-focused verification passed successfully after the follow-up fix, with zero failed tests.

### Code review follow-up

- Fixed the root cause where provider-level `InvalidOperationException` instances were being misclassified as validation failures and returned as `400 Bad Request` instead of `500 Internal Server Error`.
- Standardized the global error payload so the API always emits a structured `ProblemDetails` response with a stable `code` extension for unexpected processing failures.
- Added a regression test covering the unhandled exception path to keep the HTTP contract consistent.
