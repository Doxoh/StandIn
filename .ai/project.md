# Projekt

**StandIn** ist der gepflegte Nachfolger von [RimDev.Stuntman](https://github.com/ritterim/stuntman) (archiviert, MIT). Es simuliert Benutzer während der Entwicklung: Anmeldung per Cookie mit User Picker und per Bearer-Token, für ASP.NET Core Anwendungen und REST-APIs.

- Lizenz MIT (Till Schreiber); Stuntman-Lizenztext in `THIRD-PARTY-NOTICES.md`.
- Ziel-Frameworks: `net8.0;net10.0`. .NET 8 und 9 sind seit 10. November 2026 ohne Support, .NET 10 LTS bis November 2028.
- Eigentümer: Till (GitHub `Doxoh`), Repo `Doxoh/StandIn`, vorerst privat, später öffentlich mit NuGet-Veröffentlichung.
- Standardpfad bleibt `/stuntman` (Migration ohne Umbau), Option `FrontendBaseUrl` ist geplant.

## Pakete

| Projekt | Stand |
| --- | --- |
| `src/StandIn.AspNetCore` | Kern, umgesetzt (Phase 1 und 2) |
| `src/StandIn.Blazor` | leer, Phase 5 |
| `src/StandIn.Client` | leer, Phase 5 (MAUI/WPF/WinForms/Konsole) |
| `src/StandIn.Testing` | leer, Phase 3 |
| `tests/StandIn.AspNetCore.Tests` | xUnit, 60 Testfälle |

Stand der Phasen siehe [roadmap.md](roadmap.md).
