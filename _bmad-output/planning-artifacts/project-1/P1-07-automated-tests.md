# P1-07 — Automated tests

## Summary

Cover use-case and endpoint behavior with deterministic tests that do not depend on a real LLM or external services.

## Type

Story / test ticket

## Status

Not started

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

- Implement a fake `IAiTextGenerator` for tests.
- Add tests for request and AI output validation.
- Add tests for valid and invalid use-case responses.
- Add tests for endpoints returning `200` and `400` status codes.
- Ensure the suite runs without any secrets in the environment.

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

## Implementation notes

- Prioritize contract and validation tests.
- Keep fakes simple, deterministic, and representative of system behavior.
- Tests should be runnable by any developer without access to secrets.
