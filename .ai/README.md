# .ai – knowledge base for AI agents

Short, maintained project context for Claude and other agents. Read this file first, then only the file that fits the task.

| File | Content |
| --- | --- |
| [project.md](project.md) | Goal, scope, status, repo layout |
| [architecture.md](architecture.md) | Classes, flow, endpoints, JSON format |
| [decisions.md](decisions.md) | Decisions by the owner (binding) |
| [security.md](security.md) | Security rules that are never relaxed |
| [parity.md](parity.md) | Parity with RimDev.Stuntman and deliberate deviations |
| [conventions.md](conventions.md) | Code, test and documentation rules |
| [workflow.md](workflow.md) | GitHub, CI, release, way of working |
| [roadmap.md](roadmap.md) | Phases and open issues |

## Rules for this knowledge base

- Only facts that follow from the code, the issues or decisions by the owner. No guesses.
- If a PR changes behavior, architecture or decisions, the matching file is updated in the same PR.
- Keep it short. Details belong in the code, the issues or `docs/`.
- `docs/concept.md` is the short form of the original design.
