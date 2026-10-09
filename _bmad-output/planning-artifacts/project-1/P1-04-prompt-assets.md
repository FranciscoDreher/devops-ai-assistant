# P1-04 — Prompt assets

## Summary

Externalize the system and audience prompts so the use-case logic is independent from prompt text embedded in code and so audience-specific behavior can change without modifying the orchestration logic.

## Type

Story / prompt ticket

## Status

Completed

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

- Added the prompt asset files under [docs/prompts/incident-polisher](../../../docs/prompts/incident-polisher): system, leadership, and customer prompts.
- Added the application port `IPromptProvider` and wired the prompt orchestration to use it when available.
- Implemented `FilePromptProvider` in the infrastructure layer to read prompt files deterministically by audience and fail with controlled exceptions for missing or unsupported prompts.
- Added unit coverage for leadership/customer selection and invalid audience handling.

## Code review

- Dependency direction preserved: Application owns the prompt contract; Infrastructure implements it.
- Prompt content is externalized from code and can be changed without modifying the orchestration logic.

## Implementation notes

- Keep prompts clear, brief, and focused on factuality rules.
- Clearly document the difference between `system` and `audience` prompts.
- Ensure the model always receives explicit, coherent context.

## Final status after code review fixes

The review findings were addressed as follows:

- Removed the embedded system prompt from the application layer and kept prompt content as file assets only.
- Made the prompt provider the required dependency for incident polishing so the runtime path always uses file-based prompts instead of a hardcoded fallback.
- Hardened `FilePromptProvider` to resolve the prompt root from `Environment.CurrentDirectory` and `AppContext.BaseDirectory`, including parent directories, so it works in both local repo execution and built output scenarios.
- Added the prompt files to the API output so the app can read them from runtime artifacts.
- Registered the prompt provider in the API startup pipeline so the runtime wiring matches the architecture contract.
- Added a regression test covering default resolution from the current project directory.

## Verification evidence

Executed successfully:

- `dotnet test tests/DevOpsAI.ApplicationTests/DevOpsAI.ApplicationTests.csproj --nologo`
  - Result: 13 tests passed, 0 failed.
- `dotnet build devops-ai-assistant.sln --nologo`
  - Result: full solution build succeeded.

This task is complete and the prompt asset implementation is now aligned with the project architecture and the acceptance criteria.
