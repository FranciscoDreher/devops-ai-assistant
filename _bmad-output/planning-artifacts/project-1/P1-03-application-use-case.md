# P1-03 — Application use case

## Summary

Implement the deterministic orchestration for the "Polish Incident" use case, including validation, audience selection, prompt assembly, AI generation invocation, and structured result return.

## Type

Story / application ticket

## Status

Completed

## Dependencies

- P1-01
- P1-02

## Objective

Centralize the use-case logic in Application so validation and flow composition can be tested without depending on ASP.NET or a real AI provider.

## Scope

### Included

- `PolishIncidentRequest`
- `PolishIncidentResult`
- `IIncidentPolisher`
- `IncidentPolisher`
- `IncidentPrompt`
- `IncidentPolishingRules`
- `IAiTextGenerator`
- deterministic validation of input and output
- prompt selection by audience
- application error handling

### Excluded

- reading prompts from files
- Azure/OpenAI/Claude SDK usage
- HTTP serialization logic
- ASP.NET Core dependency injection concerns

## Acceptance criteria

1. Application can be tested without ASP.NET.
2. Application can be tested without a real AI.
3. Validation rejects an empty or overlong `IncidentNote`.
4. Validation rejects unsupported audiences.
5. If the input is invalid, the AI is not invoked.
6. If the AI output is invalid, the application returns a structured error.
7. Prompt construction flow is deterministic.
8. The use case supports a fake implementation in tests.

## Work to do

- Define the request and result contracts.
- Create the polisher interface.
- Implement the orchestration class that validates input and calls the text generator.
- Create validation rules and error translation logic.
- Ensure `CancellationToken` is used on all async calls.

## Technical constraints

- Provider SDK types must not be used in Application.
- The code must be testable with deterministic fakes.
- Prefer composition over inheritance.

## Verification

- Run application unit tests.
- Check validation and use-case behavior with valid and invalid data.
- Confirm the AI is only invoked when the input is valid.

## Risks / notes

- If validation happens too late, there is a risk of invoking the AI unnecessarily.
- AI output must be validated before it is returned to the system.

## Definition of done

The task is complete when the use-case flow is implemented, validated, and reusable by the API layer without coupling to infrastructure details.

## Changes made

- Added the Application use-case contracts for the incident polisher flow:
  - `PolishIncidentRequest`
  - `PolishIncidentResult`
  - `IncidentPolishingError`
  - `IncidentPrompt`
  - `IIncidentPolisher`
  - `IAiTextGenerator`
- Implemented `IncidentPolisher` in Application with deterministic validation, prompt assembly, AI invocation, and structured error conversion.
- Centralized the policy logic in `IncidentPolishingRules` to enforce request validation and prompt creation rules.
- Kept provider-specific dependencies out of Application by using a small AI abstraction interface.
- Updated domain validation so empty `actionsTaken` and `nextSteps` arrays are valid while blank entries remain rejected.
- Added application-level tests covering invalid input, valid AI output, malformed AI output, and empty array output handling.

## Code review

- Dependency direction is preserved: Application depends on Domain and owns orchestration logic; no provider SDK types are used in Application.
- The use case is testable without ASP.NET or a real AI client.
- Validation happens before invoking the AI to avoid unnecessary calls.
- Failures are represented as explicit application errors instead of provider-specific exceptions.

## Implementation notes

- Business errors are explicit and independent from JSON or concrete providers.
- Validation rules are centralized to improve maintainability.
- Prompt construction is deterministic and audience-aware.
- The AI call is only executed when the request passes validation.
- `CancellationToken` is propagated through the async generation call.

## Verification

Executed:

`cd /home/fdreher/Documentos/projects/devops-ai-assistant && dotnet test devops-ai-assistant.sln --nologo`

Result:

- 15 tests passed
- 0 failed
- Build succeeded
