---
applyTo: "**/*.cs"
---

# Architecture Instructions

- Follow the dependency direction documented in `ARCHITECTURE.md`.
- Keep Domain independent of ASP.NET Core, EF Core, AI SDKs, and Infrastructure.
- Keep use-case orchestration in Application.
- Keep HTTP concerns in API/Presentation projects.
- Keep database, filesystem, provider SDK, and external-service implementations in Infrastructure.
- Depend on interfaces/ports at application boundaries; implement adapters in Infrastructure.
- Apply SOLID pragmatically. Prefer small cohesive types and composition over inheritance.
- Avoid generic `Helper`, `Manager`, or `Service` classes without a clear responsibility.
- Do not add a shared abstraction solely for hypothetical future reuse.
- If a change materially alters architecture or dependency direction, update the architecture documentation and propose an ADR.
