# Konventionen

## Einfachheit zuerst

Der Eigentümer verlangt ausdrücklich Einfachheit (Review mit dem Plugin `keep-it-simple`):

- Keine Interfaces mit einer Implementierung, keine Factories, keine Basisklassen auf Vorrat.
- Eine Datei nur, wenn sie etwas trägt. Eine Methode statt einer Klasse, wenn es reicht.
- Öffentliche API ist teuer, weil sie nach der Veröffentlichung bleibt. Alles, was nicht von außen gebraucht wird, ist `internal`.
- Keine Parameter, die niemand übergibt. Keine Konfiguration ohne zweiten Wert.

## Code

- `Nullable` an, `TreatWarningsAsErrors` an (nur `CS1591` für fehlende XML-Docs ist stumm geschaltet).
- Pakete zentral in `Directory.Packages.props`, TFM-abhängige Floating-Versionen (`8.0.*`, `10.0.*`).
- Code-Kommentare knapp, ASCII (`ae`, `oe`, `ue`); Markdown mit Umlauten.
- API-Namen folgen Stuntman (`AddUser`, `EnableServer`, ...), außer eine Abweichung steht in [parity.md](parity.md).

## Tests

- xUnit, TestServer-basiert (`tests/StandIn.AspNetCore.Tests/TestApp.cs`).
- Jedes Verhalten aus [security.md](security.md) hat einen Test.
- Lokal gibt es oft kein .NET SDK; die GitHub-CI ist dann der Compiler. Vor dem Push Code sorgfältig lesen (fehlende `using`, Header-Validierung).
