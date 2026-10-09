# P1-04 — Prompt assets

## Summary

Externalize the system and audience prompts so the use-case logic is independent from prompt text embedded in code and so audience-specific behavior can change without modifying the orchestration logic.

## Type

Story / prompt ticket

## Status

Not started

## Dependencies

- P1-01
- P1-03

## Objective

Create a reusable mechanism to load prompts from files and select the correct one based on the incident audience.

## Scope

### Included

- `system.md`
- `leadership.md`
- `customer.md`
- `IPromptProvider`
- `FilePromptProvider`
- prompt selection and assembly by audience

### Excluded

- work with concrete AI providers
- domain model validation logic
- dynamic or highly complex response styling

## Acceptance criteria

1. Prompts are stored in files rather than as large literals in C#.
2. The application can load the system prompt and the audience prompt based on input values.
3. Changing the content of `leadership.md` or `customer.md` does not require modifying the use-case flow.
4. The prompt provider behaves deterministically.
5. If an audience does not exist, the request fails in a controlled way.

## Work to do

- Create the prompt folder structure under `docs/prompts/incident-polisher/`.
- Add the base system and audience instructions.
- Implement the abstraction for reading prompts.
- Test prompt selection by audience and expected failures.

## Technical constraints

- Prompt content must be separated from application code and not mixed with serialization or web concerns.
- Prompts must be treated as project-managed assets rather than hidden strings inside classes.

## Verification

- Run tests related to prompt selection.
- Validate that changing prompt files does not require code changes for basic behavior.

## Risks / notes

- If the prompt layer depends on the application instead of being a port, unnecessary coupling is introduced.
- Prompt quality and flow determinism must be validated through testing, not by model intuition.

## Definition of done

The ticket is complete when the prompt system provides a configurable, maintainable, and testable layer for each audience.

## Changes made

## Code review

## Implementation notes

- Keep prompts clear, brief, and focused on factuality rules.
- Clearly document the difference between `system` and `audience` prompts.
- Ensure the model always receives explicit, coherent context.
