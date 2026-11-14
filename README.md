# StandIn

Benutzer während der Entwicklung simulieren: ein gepflegter Nachfolger von
[RimDev.Stuntman](https://github.com/ritterim/stuntman) für ASP.NET Core ab .NET 8.

[![CI](https://github.com/Doxoh/StandIn/actions/workflows/ci.yml/badge.svg)](https://github.com/Doxoh/StandIn/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/StandIn.AspNetCore.svg)](https://www.nuget.org/packages/StandIn.AspNetCore)
[![Lizenz: MIT](https://img.shields.io/badge/Lizenz-MIT-blue.svg)](LICENSE)

Mit StandIn meldest du dich in deiner Entwicklungsumgebung per Klick als beliebiger Testbenutzer an, ohne Identity Provider.
Cookie-Anmeldung mit User Picker für MVC, Razor Pages und Blazor Server, Bearer-Token für REST-APIs, ein Token-Endpunkt für Clients
(Blazor WebAssembly, MAUI, WPF, Konsole) und Hilfen für Integrationstests.

> **Nur für die Entwicklung.** StandIn schaltet sich nur in `Development` ein und lässt sich in `Production` nicht aktivieren.
> Es ersetzt keine echte Authentifizierung. Details: [SECURITY.md](SECURITY.md).

> Status: Vorschau (0.x), der Funktionsumfang entspricht RimDev.Stuntman plus Erweiterungen. Die API kann sich bis 1.0 noch ändern.

## Installation

```
dotnet add package StandIn.AspNetCore --prerelease
```

| Paket | Zweck |
| --- | --- |
| `StandIn.AspNetCore` | Handler, Benutzerquellen, Endpunkte, User Picker für MVC/Razor Pages/Blazor Server, Token-Endpunkt |
| `StandIn.Blazor` | Picker-Komponente und `AuthenticationStateProvider` für WebAssembly, Hybrid und Auto-WASM |
| `StandIn.Client` | `HttpClient`-Handler für MAUI, WPF, WinForms, Konsole, Worker |
| `StandIn.Testing` | Hilfen für `WebApplicationFactory`-Integrationstests |

Ziel-Frameworks: `net8.0` und `net10.0`. `net8.0` bleibt für Bestandsprojekte im Paket, auch wenn Microsoft den Support beendet hat.

## Schnellstart

```csharp
builder.Services.AddAuthentication().AddStandIn(builder.Environment, options =>
{
    options.AddUser(new StandInUser("admin", "Local Admin").AddRole("Administrator"));
    options.AddUsersFromJson("stuntman-users.json");
});

app.UseAuthentication();
app.UseAuthorization();
app.MapStandIn();
```

Den User Picker bekommst du so:

- MVC und Razor Pages: `@Html.StandInUserPicker()` in der View.
- Blazor Server und beliebige HTML-Seiten: `app.UseStandInPicker()` blendet ihn in jede Seite ein.
- Blazor WebAssembly, Hybrid, Auto-WASM: `<StandInPicker />` aus `StandIn.Blazor`.

Position und Ausrichtung stellst du mit `SetUserPickerAlignment` und `SetUserPickerVerticalPosition` ein.

`AddStandIn` ist nur in `Development` aktiv. In allen anderen Umgebungen tut es nichts, in `Production` lässt es sich nicht einschalten. Die Endpunkte liegen unter `/stuntman`.

## Wohin als Nächstes

| Ich will ... | Lesen |
| --- | --- |
| von RimDev.Stuntman wechseln | [docs/migration.md](docs/migration.md) |
| eine REST-API per Bearer-Token testen | [samples/MinimalApi](samples/MinimalApi) |
| MVC mit Cookie und Picker | [samples/Mvc](samples/Mvc) |
| einen Client (MAUI, WPF, Blazor WASM) gegen die API anmelden | `StandIn.Client`, `AddStandInClient`; Blazor: `AddStandInBlazor` |
| Integrationstests als bestimmter Benutzer | `factory.CreateStandInClient("admin")` aus `StandIn.Testing` |
| Sicherheitsregeln und Entwurf | [docs/concept.md](docs/concept.md) |

## Mitmachen

Fehler und Wünsche bitte als [Issue](https://github.com/Doxoh/StandIn/issues) melden, Änderungen als Pull Request (siehe [CONTRIBUTING.md](CONTRIBUTING.md)).
Sicherheitslücken bitte nicht öffentlich melden, sondern wie in [SECURITY.md](SECURITY.md) beschrieben.

## Lizenz

MIT. Teile basieren auf RimDev.Stuntman (MIT, © 2018 Ritter Insurance Marketing),
siehe [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
