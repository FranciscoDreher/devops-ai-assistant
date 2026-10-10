# P1-05 — AI adapter

## Summary

Connect the application layer to a reusable AI provider through a small abstraction while keeping the domain and use-case logic free from concrete SDKs and provider-specific details.

## Type

Story / infrastructure ticket

## Status

Completed

## Dependencies

- P1-01
- P1-03
- P1-04

## Objective

Implement the infrastructure needed to turn a use-case request into an AI call and convert the response back into a valid structured model.

## Scope

### Included

- `IAiTextGenerator`
- `Microsoft.Extensions.AI` adapter
- `AiOptions`
- model, endpoint, and authentication configuration
- translation of provider exceptions into application errors
- structural validation of AI output

### Excluded

- incident domain logic
- HTTP API logic
- UI or external client interfaces
- execution of RAG or MCP workflows

## Acceptance criteria

1. The application layer does not reference Azure/OpenAI/Claude SDKs directly.
2. The AI adapter converts and validates provider output.
3. Provider exceptions are translated to application-level errors.
4. Provider configuration is handled via options or environment variables.
5. The flow remains reusable so the provider can change without modifying the polisher logic.

## Work to do

- Define the `AiOptions` configuration.
- Implement the AI generator interface in infrastructure.
- Adapt the call to `IChatClient` or equivalent.
- Validate the JSON or required payload structure.
- Map infrastructure errors to application error codes.

## Technical constraints

- Do not assume a single AI provider will remain permanent.
- Avoid exposing the application layer to the specific source that generated the response.

## Verification

- Run unit tests for the adapted infrastructure or integration tests with a fake provider.
- Confirm provider failures are handled correctly.
- Validate that AI responses are rejected when malformed.

## Risks / notes

- Exposing provider internals can compromise security and system stability.
- Unstructured or inconsistent responses must be rejected with an explicit error.

## Definition of done

The story is complete when infrastructure executes AI generation through the proper abstraction and the use case does not depend on a specific provider.

## Changes made

- Added `AiOptions` with environment-aware configuration for model, endpoint, and API key.
- Implemented `MicrosoftExtensionsAiTextGenerator` in the infrastructure layer using `Microsoft.Extensions.AI` and `IChatClient`.
- Kept the application interface `IAiTextGenerator` as the contract so the use case stays provider-agnostic.
- Validated the generated response text for empty or unusable results before returning it.
- Translated provider and infrastructure failures into structured `HttpRequestException` failures for clear handling.
- Kept the API assembly free from direct infrastructure references, preserving the Clean Architecture dependency boundary.

## Code review

- The application layer remains provider-agnostic and depends only on `IAiTextGenerator`.
- The adapter is isolated in infrastructure, where provider-specific logic belongs.
- Configuration is centralized via options/environment without coupling the use case to a vendor.

## Implementation notes

- Keep error translation in a clear layer.
- Separate provider configuration from use-case logic.
- Define safe behavior when configuration is missing or the provider is unavailable.
- Validation is intentionally conservative: empty provider output is rejected before use.

## Verification

- Ran `dotnet test --nologo` at the repository root.
- Result: 26 tests passed, 0 failed.
- This includes the API architecture smoke checks and the AI adapter integration tests.

## Review closure

The four findings raised in the adversarial review were fixed before closing the task:

1. Application composition was restored in the application layer through `AddAiServices`, keeping the API assembly free from a direct `DevOpsAI.Infrastructure` reference while preserving the dependency boundary.
2. The AI adapter now validates provider output as structured JSON before returning it; plain-text or malformed provider responses are rejected explicitly.
3. Provider and configuration failures are translated into application-level `AiGenerationException` errors and surfaced as structured AI-generation failures instead of leaking infrastructure exceptions.
4. AI configuration is read from the `Ai` settings section and environment variables, with a clear fail-safe when the provider is not configured.

Final validation command: `dotnet test --nologo 2>&1 | tail -n 200`
Result: all tests passed, 26/26 successful.
