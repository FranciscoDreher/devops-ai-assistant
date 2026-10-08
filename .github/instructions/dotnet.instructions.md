---
applyTo: "**/*.cs"
---

# .NET Instructions

- Use the target framework and package versions defined by the repository; do not upgrade dependencies without a reason.
- Enable nullable reference types and address nullability warnings.
- Use asynchronous APIs for I/O and propagate `CancellationToken` through async call chains.
- Do not use `.Result`, `.Wait()`, or `async void` in application code.
- Register dependencies through dependency injection; avoid service location and global mutable state.
- Use Options pattern (`IOptions<T>` or an appropriate variant) for configuration.
- Use `Async` suffix for asynchronous methods.
- Prefix interfaces with `I`; use descriptive names for requests, responses, and domain concepts.
- Prefer guard clauses and readable methods over deeply nested conditionals.
- Keep one primary public type per file where practical.
- Do not swallow exceptions. Translate exceptions at a suitable application or API boundary.
- Use structured logging and avoid logging secrets or sensitive payloads.
