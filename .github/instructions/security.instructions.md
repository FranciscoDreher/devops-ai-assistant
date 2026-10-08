---
applyTo: "**/*.cs,**/*.json,**/*.yml,**/*.yaml,**/*.md"
---

# Security Instructions

- Never commit API keys, passwords, access tokens, private keys, or real customer/incident secrets.
- Read secrets from environment variables or an approved secret store; use safe example values in documentation.
- Treat user input, external API responses, retrieved documents, and LLM output as untrusted.
- Validate input at system boundaries and validate generated structured output before use.
- Do not let model output decide authentication, authorization, or access-control policy.
- Do not log credentials, tokens, or full sensitive payloads.
- For any future privileged or destructive tool action, enforce this sequence: validate request -> authenticate/authorize -> require confirmation where appropriate -> execute with least privilege -> audit outcome.
- Fail safely when provider configuration is missing or an external service is unavailable.
