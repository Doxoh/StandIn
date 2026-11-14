# .ai – Wissensbasis für KI-Agenten

Kurzer, gepflegter Projektkontext für Claude und andere Agenten. Zuerst diese Datei lesen, dann nur die Datei, die zur Aufgabe passt.

| Datei | Inhalt |
| --- | --- |
| [project.md](project.md) | Ziel, Umfang, Stand, Repo-Aufbau |
| [architecture.md](architecture.md) | Klassen, Ablauf, Endpunkte, JSON-Format |
| [decisions.md](decisions.md) | Entscheidungen des Eigentümers (verbindlich) |
| [security.md](security.md) | Sicherheitsregeln, die nie aufgeweicht werden |
| [parity.md](parity.md) | Parität zu RimDev.Stuntman und bewusste Abweichungen |
| [conventions.md](conventions.md) | Code-, Test- und Dokumentationsregeln |
| [workflow.md](workflow.md) | GitHub, CI, Release, Arbeitsweise |
| [roadmap.md](roadmap.md) | Phasen und offene Issues |

## Regeln für diese Wissensbasis

- Nur Fakten, die sich aus Code, Issues oder Entscheidungen des Eigentümers ergeben. Keine Vermutungen.
- Ändert ein PR Verhalten, Architektur oder Entscheidungen, wird die passende Datei im selben PR angepasst.
- Kurz halten. Details gehören in den Code, die Issues oder `docs/`.
- Der ausführliche Konzepttext liegt als Claude-Doc beim Eigentümer; `docs/concept.md` ist die Kurzfassung.
