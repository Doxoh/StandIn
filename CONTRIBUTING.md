# Mitmachen

Danke für dein Interesse. Kurz und knapp:

1. Für größere Änderungen zuerst ein Issue anlegen und das Vorhaben abstimmen.
2. Fork und Branch anlegen, Änderung klein und fokussiert halten.
3. Bauen und testen: `dotnet test` (Ziel-Frameworks `net8.0` und `net10.0`, Warnungen sind Fehler).
4. Pull Request gegen `main` öffnen. Er ist bereit, wenn die CI grün ist.

Hinweise zum Aufbau, zu Entscheidungen und Konventionen stehen in [.ai/README.md](.ai/README.md).
Die Grundregel: so einfach wie möglich. Keine Abstraktion ohne heutigen Nutzen.

Sicherheitsrelevante Änderungen (Umgebungsprüfung, ReturnUrl, Token) brauchen einen Test, der das Verhalten belegt.
