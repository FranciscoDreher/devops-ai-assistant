# P1-08 — Observability and hardening

## Summary

Prepare the solution for a safer and more robust operational environment by reinforcing secure logging, traceability, diagnostics, and response-time control without exposing secrets or internal provider details.

## Type

Story / hardening ticket

## Status

done

## Dependencies

- P1-01
- P1-05
- P1-06
- P1-07

## Objective

Make the application diagnosable, secure, and resilient without leaking sensitive data or provider internals through errors.

## Scope

### Included

- secure structured logging
- request-level traceability and correlation
- AI latency measurement
- error sanitization
- timeout control and failure handling
- secret-free logging validation

### Excluded

- external monitoring infrastructure and complex dashboards
- cloud deployment
- incident management outside the system

## Acceptance criteria

1. Logs do not contain API keys, tokens, or secrets.
2. The system records correlation identifiers to trace calls.
3. AI generation latency is recorded safely.
4. Client errors do not leak internal provider details.
5. Timeout configuration is available through options or configuration.

## Work to do

- [x] Configure safe structured logging for the API and application/infrastructure layers.
- [x] Add trace IDs or correlation IDs to execution.
- [x] Measure AI timings and record only necessary metadata.
- [x] Add sanitization in the error-handling layer.
- [x] Review configuration to control timeouts.

## Technical constraints

- Secrets, keys, and full sensitive payloads must never be logged.
- Traces should help diagnose issues without exposing internal infrastructure.

## Verification

- Review logs from test runs and failure scenarios.
- Confirm the error output does not include sensitive details.
- Check that execution timing and traceability are recorded correctly.

## Risks / notes

- The main risk is accidentally logging secrets or verbose AI payloads.
- Maintaining observability without filtering can leak details that should remain internal.

## Definition of done

The task is considered complete when the system is observable, traceable, and secure, with sanitized errors and no leakage of sensitive information.

## Changes made

- Hardened AI timeout parsing so malformed `AI_TIMEOUT_SECONDS` or `AI__TIMEOUT_SECONDS` values fall back to the default 30-second timeout instead of crashing startup/runtime configuration processing.
- Reused a shared `RequestObservabilityMiddleware.CreateProblemDetails(...)` helper across endpoint validation and the global exception handler to keep sanitized error payloads consistent.
- Updated request trace handling to honor incoming W3C `traceparent` headers and `X-Trace-Id` values, ensuring request correlation survives across downstream execution and response headers.

## Code review

### Findings fixed

1. Timeout validation was fragile when config values were malformed. Resolved by centralizing timeout bounds in `AiOptions` and parsing with `int.TryParse`, falling back safely to the default timeout while preserving the 1–86400 validation contract.
2. Trace correlation did not honor incoming upstream trace headers. Resolved by extracting the trace ID from W3C `traceparent` and `X-Trace-Id` before creating or reusing the request activity.
3. Error payload construction was duplicated in the API and could drift over time. Resolved by centralizing the `ProblemDetails` factory used by both validation and global exception handling.

## Implementation notes

- Centralize secure logging logic in a dedicated layer.
- Keep events readable and consistent.
- Treat traceability as a product requirement, not an optional detail.

## Dev Agent Record

### Implementation Plan

- Add request-level structured logs and W3C trace correlation without logging request bodies or query strings.
- Record AI generation outcome, model, duration, and trace ID while sanitizing provider failures.
- Configure and validate AI request timeout through `AiOptions`; verify errors, logs, and response correlation with deterministic tests.

### Completion Notes

- Added API request logging with HTTP method, path, status, elapsed time, and a correlated `X-Trace-Id` response header. Problem responses include the same trace identifier.
- Added AI generation timing and outcome logs correlated through the current W3C activity. Logs exclude prompts, generated output, API keys, and provider exception details.
- Sanitized unexpected API and provider failures; provider exception text is not attached to the translated `AiGenerationException` or returned to clients.
- Added `Ai:TimeoutSeconds` configuration (default 30 seconds, valid range 1–86400) and cancellation of provider calls on timeout.
- Updated README with the timeout configuration. Added tests for AI log redaction and timing/correlation, provider-error sanitization, timeout behavior/configuration, and API error trace correlation.
- Verification passed: full solution tests (47 tests), full solution build (0 warnings/errors), and `dotnet format --verify-no-changes --no-restore`. Tests ran without `AI_API_KEY` or `AI__API_KEY`.

## File List

- `README.md`
- `src/DevOpsAI.Api/Endpoints/IncidentEndpoints.cs`
- `src/DevOpsAI.Api/Program.cs`
- `src/DevOpsAI.Api/RequestObservabilityMiddleware.cs`
- `src/DevOpsAI.Api/appsettings.json`
- `src/DevOpsAI.Infrastructure/AI/AiOptions.cs`
- `src/DevOpsAI.Infrastructure/AI/AiServiceCollectionExtensions.cs`
- `src/DevOpsAI.Infrastructure/AI/MicrosoftExtensionsAiTextGenerator.cs`
- `tests/DevOpsAI.IntegrationTests/AI/MicrosoftExtensionsAiTextGeneratorTests.cs`
- `tests/DevOpsAI.IntegrationTests/IncidentEndpointTests.cs`
- `_bmad-output/planning-artifacts/project-1/P1-08-observability-hardening.md`

## Change Log

- 2026-10-12: Added secure request/AI observability, trace correlation, sanitized errors, and configurable AI timeout; validated with automated tests.
