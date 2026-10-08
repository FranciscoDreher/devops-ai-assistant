---
applyTo: "**/*Tests/**/*.cs,**/*.Tests/**/*.cs,**/tests/**/*.cs"
---

# Testing Instructions

- Add or update tests for behavior changed by the task.
- Use xUnit and NSubstitute, following the versions and patterns already in the repository.
- Unit tests must not call real LLM providers, external networks, or production services.
- Substitute external boundaries such as AI generation, persistence, and HTTP clients.
- Prefer assertions about behavior, output structure, and invariants over exact LLM wording.
- Include tests for invalid input, provider failures, malformed AI output, and cancellation where relevant.
- Use integration tests for API routing, dependency registration, serialization, and error handling.
- Keep tests deterministic and independent of execution order.
- Use names that describe scenario and expected outcome, for example `PolishAsync_WhenInputIsEmpty_ReturnsValidationError`.
