<!-- BMAD:START -->
# BMAD Method — Project Instructions

## Project Configuration

- **Project**: devops-ai-assistant
- **User**: Francisco Dreher
- **Communication Language**: English
- **Document Output Language**: English
- **User Skill Level**: intermediate
- **Output Folder**: {project-root}/_bmad-output
- **Planning Artifacts**: {project-root}/_bmad-output/planning-artifacts
- **Implementation Artifacts**: {project-root}/_bmad-output/implementation-artifacts
- **Project Knowledge**: {project-root}/docs

## BMAD Runtime Structure

- **Agent definitions**: `_bmad/bmm/agents/` (BMM module) and `_bmad/core/agents/` (core)
- **Workflow definitions**: `_bmad/bmm/workflows/` (organized by phase)
- **Core tasks**: `_bmad/core/tasks/` (help, editorial review, indexing, sharding, adversarial review)
- **Core workflows**: `_bmad/core/workflows/` (brainstorming, party-mode, advanced-elicitation)
- **Workflow engine**: `_bmad/core/tasks/workflow.xml` (executes YAML-based workflows)
- **Module configuration**: `_bmad/bmm/config.yaml`
- **Core configuration**: `_bmad/core/config.yaml`
- **Agent manifest**: `_bmad/_config/agent-manifest.csv`
- **Workflow manifest**: `_bmad/_config/workflow-manifest.csv`
- **Help manifest**: `_bmad/_config/bmad-help.csv`
- **Agent memory**: `_bmad/_memory/`

## Key Conventions

- Always load `_bmad/bmm/config.yaml` before any agent activation or workflow execution
- Store all config fields as session variables: `{user_name}`, `{communication_language}`, `{output_folder}`, `{planning_artifacts}`, `{implementation_artifacts}`, `{project_knowledge}`
- MD-based workflows execute directly — load and follow the `.md` file
- YAML-based workflows require the workflow engine — load `workflow.xml` first, then pass the `.yaml` config
- Follow step-based workflow execution: load steps JIT, never multiple at once
- Save outputs after EACH step when using the workflow engine
- The `{project-root}` variable resolves to the workspace root at runtime

## Available Agents

| Agent | Persona | Title | Capabilities |
|---|---|---|---|
| bmad-master | BMad Master | BMad Master Executor, Knowledge Custodian, and Workflow Orchestrator | runtime resource management, workflow orchestration, task execution, knowledge custodian |
| analyst | Mary | Business Analyst | market research, competitive analysis, requirements elicitation, domain expertise |
| architect | Winston | Architect | distributed systems, cloud infrastructure, API design, scalable patterns |
| dev | Amelia | Developer Agent | story execution, test-driven development, code implementation |
| pm | John | Product Manager | PRD creation, requirements discovery, stakeholder alignment, user interviews |
| qa | Quinn | QA Engineer | test automation, API testing, E2E testing, coverage analysis |
| quick-flow-solo-dev | Barry | Quick Flow Solo Dev | rapid spec creation, lean implementation, minimum ceremony |
| sm | Bob | Scrum Master | sprint planning, story preparation, agile ceremonies, backlog management |
| tech-writer | Paige | Technical Writer | documentation, Mermaid diagrams, standards compliance, concept explanation |
| ux-designer | Sally | UX Designer | user research, interaction design, UI patterns, experience strategy |

## Slash Commands

Type `/bmad-` in Copilot Chat to see all available BMAD workflows and agent activators. Agents are also available in the agents dropdown.
<!-- BMAD:END -->

<!-- REPOSITORY INSTRUCTIONS:START -->
# Repository-wide Copilot Instructions

These instructions apply to all work in this repository. Keep the BMAD-managed section above intact unless updating it through the appropriate BMAD configuration/workflow.

## Before making changes
- Read `AGENTS.md` and `ARCHITECTURE.md` before changing architecture or shared code.
- For Project 1, also read `PROJECT-1-ARCHITECTURE.md`.
- Follow the current task scope. Do not implement future projects (RAG, tool calling, MCP) unless explicitly requested.
- Prefer small, reviewable changes over broad rewrites.

## Architecture
- Preserve the dependency direction: Presentation/API -> Application -> Domain; Infrastructure may depend on Application and Domain.
- Domain must not depend on APIs, persistence frameworks, AI SDKs, or Infrastructure.
- Keep provider-specific AI SDK code inside Infrastructure.
- Do not introduce abstractions without a current use case.
- Reuse code across projects only when the shared responsibility is genuinely common.

## Engineering workflow
1. Inspect the existing code and relevant documentation.
2. State a concise implementation plan for non-trivial tasks.
3. Implement only the requested scope.
4. Add or update tests.
5. Run relevant tests, build, and formatting checks.
6. Summarize changes, checks run, and any remaining risks.

## AI safety and reliability
- Treat model output as untrusted input.
- Validate generated output deterministically before returning or using it.
- Never use an LLM as an authorization or security boundary.
- Never log secrets, credentials, tokens, or unnecessary sensitive incident content.
- Do not execute external actions based only on model-generated text.

## Documentation
- Keep prompts versioned under `docs/prompts/`.
- Record significant architecture decisions in `docs/adr/`.
- Update relevant README or API documentation when behavior or setup changes.
<!-- REPOSITORY INSTRUCTIONS:END -->

