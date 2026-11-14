# Way of working and GitHub

- Repo `Doxoh/StandIn`, default branch `main`, changes only through PRs. `CODEOWNERS`: `* @Doxoh`.
- CI (`.github/workflows/ci.yml`): build and tests on Ubuntu and Windows, .NET 8 and 10 SDK, pack. A PR is ready only when CI is green.
- Release (`release.yml`): on tag `v*`, version from MinVer (git tag, prefix `v`), published through NuGet Trusted Publishing (OIDC, `NuGet/login@v1`, short-lived key), no API key. Needs the secret `NUGET_USER` (nuget.org profile name) and the environment `nuget`.
- Dependabot (`dependabot.yml`): major updates for `Microsoft.AspNetCore.*` and `Microsoft.Extensions.*` are excluded because they are tied to the .NET version.
- Issues #1 to #16 with labels `phase-1` to `phase-6`, `security`, `compat` and milestones 1 to 6.
- Through the proxy of the agent environment, repo settings (description, topics), tag pushes and GraphQL are blocked; REST is enough for issues and PRs.

## Manual steps of the owner

Repo description and topics, branch protection for `main`, environment `nuget` (GitHub), secret `NUGET_USER` (profile name), Trusted Publishing policy on nuget.org (repo `Doxoh/StandIn`, workflow `release.yml`, environment `nuget`), reserve the NuGet prefix `StandIn`, push release tags.

## Rules for agents

- Do not merge, close, publish or change settings without being asked.
- Before phase work, read the matching issues and implement only their scope.
- Commits and PRs carry the given attribution.
