# Arbeitsweise und GitHub

- Repo `Doxoh/StandIn`, Standardbranch `main`, Änderungen nur über PRs. `CODEOWNERS`: `* @Doxoh`.
- CI (`.github/workflows/ci.yml`): Build und Tests auf Ubuntu und Windows, .NET 8 und 10 SDK, Pack. PR gilt erst bei grüner CI als bereit.
- Release (`release.yml`): bei Tag `v*`, Version aus MinVer (Git-Tag, Präfix `v`), Veröffentlichung per NuGet Trusted Publishing (OIDC, `NuGet/login@v1`, kurzlebiger Key), kein API-Key. Nötig: Secret `NUGET_USER` (nuget.org-Profilname) und Umgebung `nuget`.
- Dependabot (`dependabot.yml`): Major-Updates für `Microsoft.AspNetCore.*` und `Microsoft.Extensions.*` sind ausgeschlossen, weil sie an die .NET-Version gebunden sind.
- Issues #1 bis #16 mit Labels `phase-1` bis `phase-6`, `security`, `compat` und Meilensteinen 1 bis 6.
- Über den Proxy der Agentenumgebung sind Repo-Einstellungen (Beschreibung, Topics) und GraphQL gesperrt; REST genügt für Issues und PRs.

## Manuelle Schritte des Eigentümers

Repo-Beschreibung und Topics, Branch-Schutz für `main`, Umgebung `nuget` (GitHub), Secret `NUGET_USER` (Profilname), Trusted-Publishing-Richtlinie auf nuget.org (Repo `Doxoh/StandIn`, Workflow `release.yml`, Umgebung `nuget`), NuGet-Präfix `StandIn` reservieren.

## Regeln für Agenten

- Nichts mergen, schließen, veröffentlichen oder Einstellungen ändern ohne Auftrag.
- Vor Phase-Arbeit die zugehörigen Issues lesen und nur deren Umfang umsetzen.
- Commits und PRs tragen die vorgegebene Attribution.
