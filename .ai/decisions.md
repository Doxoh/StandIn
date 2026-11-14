# Entscheidungen des Eigentümers

Verbindlich. Änderungen nur nach Rückfrage.

1. Name **StandIn**, Lizenz MIT, API an Stuntman angelehnt, volle Feature-Parität.
2. Unterstützt **.NET 8 und neuer**, Multi-Target `net8.0;net10.0`.
3. Alle .NET-Frontends und REST-APIs: MVC/Razor, Blazor (Server, WASM, Auto, Hybrid), MAUI/WPF/WinForms.
4. Öffentliche NuGet-Veröffentlichung; Präfix `StandIn` soll reserviert werden.
5. Pfad `/stuntman` bleibt Standard, mit Weiterleitung und Option `FrontendBaseUrl`.
6. Entwicklung startet erst nach ausdrücklicher Freigabe des Eigentümers. Nichts mergen, schließen oder veröffentlichen ohne Auftrag.
7. Antworten und Dokumentation auf Deutsch.
8. Kein Roslyn-Analyzer für 1.0 (keep-it-simple): der Migrationsleitfaden (`docs/migration.md`) reicht, ein Analyzer erst bei konkretem Bedarf.
