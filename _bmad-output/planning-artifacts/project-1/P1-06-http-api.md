# P1-06 — HTTP API

## Summary

Expose the use case through an HTTP endpoint with JSON serialization, input validation, error handling, and basic operational documentation for development.

## Type

Story / API ticket

## Status

Not started

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

## Code review

## Implementation notes

- Keep serialization simple and consistent.
- Use `ProblemDetails` or equivalent for structured errors when applicable.
- Prepare the endpoint to evolve with projects 2 and 3 without coupling it to provider details.
