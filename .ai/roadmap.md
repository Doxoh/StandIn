# Roadmap

Quelle der Wahrheit sind die GitHub-Issues und Meilensteine. Hier nur der Überblick.

| Phase | Inhalt | Issues | Stand |
| --- | --- | --- | --- |
| 1 Kern | `StandInUser`, Optionen, JSON, Handler, Endpunkte, Production-Schutz | #1–#5 | umgesetzt (PR #22) |
| 2 Bearer und Picker | Bearer-Pfad, Picker | #6, #7 | umgesetzt (PR #22) |
| 3 Server und Test | Server/Konfiguration, `IConfiguration`, Picker-Injektion, Test-Header, `StandIn.Testing` | #8–#10 | umgesetzt (PR #26) |
| 4 Frontend und Token | Token-Endpunkt, `FrontendBaseUrl`, PathBase, 308, CORS | #11, #12 | umgesetzt (PR #27) |
| 5 Frontends | Client (5a, #14), Blazor (5b, #13), Beispiel-Apps (5c, #15) | #13–#15 | umgesetzt (PR #29, #30, #31) |
| 6 Release | README, Migrationsleitfaden, NuGet 1.0 | #16 | Doku in PR; Analyzer bewusst nicht (YAGNI); NuGet-Schritte siehe [workflow.md](workflow.md) |

Geschätzt 10 bis 14 Personentage insgesamt.
