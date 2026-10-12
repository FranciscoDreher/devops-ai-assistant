# P1-07 — Automated tests

## Summary

Cover use-case and endpoint behavior with deterministic tests that do not depend on a real LLM or external services.

## Type

Story / test ticket

## Status

done

## Dependencies

- P1-01
- P1-03
- P1-05
- P1-06

## Objective

Ensure the reliability of the MVP with repeatable automated tests that run without secrets or real AI provider connectivity.

## Scope

### Included

- application unit tests
- validation tests
- HTTP integration tests
- fake AI provider
- tests for provider errors and malformed input/output

### Excluded

- complex model sequence tests
- secret-redaction validation at the deployed system level

## Acceptance criteria

1. The tests are deterministic and do not require a real API key.
2. The use case is tested without a real LLM.
3. Audience behavior for leadership vs customer is verified.
4. Malformed output is explicitly rejected.
5. Integration tests verify the HTTP endpoint and correct status codes.
6. Validation errors are checked without depending on an external provider.

## Work to do

- [x] Implement a fake `IAiTextGenerator` for tests.
- [x] Add tests for request and AI output validation.
- [x] Add tests for valid and invalid use-case responses.
- [x] Add tests for endpoints returning `200` and `400` status codes.
- [x] Ensure the suite runs without any secrets in the environment.

## Technical constraints

- The exact wording of the LLM should not be tested.
- Tests should focus on the system logic and public contract.

## Verification

- Run `dotnet test` for the repository.
- Verify all tests pass in an environment without an API key.
- Confirm the suite does not depend on real network access.

## Risks / notes

- If tests start depending on exact LLM wording, they become brittle.
- Integration tests should not assume hidden model capabilities.

## Definition of done

The task is complete when the test suite covers the main flow, validation, and expected errors without depending on the real provider.

## Changes made

## Code review

- **[HIGH] Reject missing required AI output fields:** the application previously treated missing `actionsTaken` or `nextSteps` as empty arrays. It now rejects any missing required field; tests cover all five required fields.
- **[MEDIUM] Enforce strict JSON syntax:** removed permissive handling for comments and trailing commas, with regression tests for both.
- **[MEDIUM] Reject numeric audience aliases:** the API now accepts only the declared audience names, case-insensitively; integration tests confirm `"0"` and `"1"` return `400`.
- Review findings corrected and verified below.

## Implementation notes

- Prioritize contract and validation tests.
- Keep fakes simple, deterministic, and representative of system behavior.
- Tests should be runnable by any developer without access to secrets.

## Dev Agent Record

### Implementation Plan

- Add deterministic application and endpoint coverage for audience selection, request validation, response mapping, malformed generated output, and provider failures.
- Verify the public HTTP status contract and correct the classification of invalid AI output if tests expose a mismatch.
- Run the complete solution tests without AI credentials, then build and formatting checks.

### Completion Notes

- Added a deterministic fake `IAiTextGenerator` for API integration tests; it returns a configured response or provider exception and never accesses an external service.
- Expanded application tests for unsupported audiences, leadership/customer prompt selection through the use case, valid output mapping, malformed JSON, and provider failure mapping.
- Expanded endpoint tests for the response contract, unsupported audience, provider failure (`500`), and malformed generated output.
- Corrected endpoint error mapping so invalid client input returns `400`, while malformed AI-generated output returns `500`.
- Addressed review findings by requiring all five AI output fields, rejecting non-standard JSON comments/trailing commas, and rejecting numeric audience aliases.
- Verification: `env -u AI_API_KEY -u AI__API_KEY dotnet test devops-ai-assistant.sln --no-restore` passed (44 tests); `dotnet build devops-ai-assistant.sln --no-restore` passed with 0 warnings/errors; `dotnet format devops-ai-assistant.sln --verify-no-changes --no-restore` passed.
- No real AI provider, external network, or API key was used.

## File List

- `src/DevOpsAI.Api/Endpoints/IncidentEndpoints.cs`
- `src/DevOpsAI.Application/Incidents/IncidentPolisher.cs`
- `tests/DevOpsAI.ApplicationTests/Incidents/IncidentPolisherTests.cs`
- `tests/DevOpsAI.IntegrationTests/Fakes/FakeAiTextGenerator.cs`
- `tests/DevOpsAI.IntegrationTests/IncidentEndpointTests.cs`
- `_bmad-output/planning-artifacts/project-1/P1-07-automated-tests.md`

## Change Log

- 2026-10-12: Added deterministic automated test coverage, corrected malformed AI output HTTP status handling, and marked P1-07 ready for review.
- 2026-10-12: Resolved all P1-07 code review findings; enforced required AI fields and strict JSON, rejected numeric audience aliases, and added regression tests.
