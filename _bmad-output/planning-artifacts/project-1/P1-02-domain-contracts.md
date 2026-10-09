# P1-02 — Domain contracts

## Summary

Define the domain types that represent the incident audience and the final formatted incident result without depending on ASP.NET, EF Core, AI SDKs, or external infrastructure.

## Type

Story / domain ticket

## Status

Not started

## Dependencies

- P1-01
- ARCHITECTURE.md
- PROJECT-1-ARCHITECTURE.md

## Objective

Create the minimum domain concepts so the application can model the business scenarios without coupling to infrastructure technologies.

## Scope

### Included

- `IncidentAudience`
- `PolishedIncident`
- domain invariants if relevant
- explicit models for incident results

### Excluded

- HTTP endpoints
- AI providers
- reading prompts from files
- HTTP request validation logic
- auditing or infrastructure logging concerns

## Acceptance criteria

1. The Domain project does not reference ASP.NET or Azure/OpenAI/Claude packages.
2. The audience model supports at least `Leadership` and `Customer`.
3. The incident output type includes the expected fields: summary, impact, actionsTaken, currentStatus, nextSteps.
4. Entities are immutable or designed with a clear, stable structure.
5. The domain does not know infrastructure details such as IaC, HTTP, or provider SDKs.

## Work to do

- Define the audience enum.
- Define the domain output record/DTO.
- Ensure the representation stays minimal and clear.
- Add simple unit tests for invariants or type consistency if valuable.

## Technical constraints

- Avoid introducing infrastructure logic into the domain.
- Respect the principles of Clean Architecture from this point onward.

## Verification

- Review the dependency graph of the project.
- Run tests for the Domain project once created.
- Confirm there is no infrastructure code inside the project.

## Risks / notes

- The domain must remain unaware of how the content is generated.
- Any formatting rule should be explicit and not depend on the AI provider.

## Definition of done

The ticket is complete when the domain types are well-defined and reusable by the application layer without introducing forbidden coupling.

## Changes made

## Code review

## Implementation notes

- Use explicit, small types.
- Keep business semantics ahead of serialization details.
- Do not add unnecessary abstractions.
