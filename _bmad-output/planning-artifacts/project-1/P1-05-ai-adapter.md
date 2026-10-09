# P1-05 — AI adapter

## Summary

Connect the application layer to a reusable AI provider through a small abstraction while keeping the domain and use-case logic free from concrete SDKs and provider-specific details.

## Type

Story / infrastructure ticket

## Status

Not started

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

## Code review

## Implementation notes

- Keep error translation in a clear layer.
- Separate provider configuration from use-case logic.
- Define safe behavior when configuration is missing or the provider is unavailable.
