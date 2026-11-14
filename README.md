# StandIn

Benutzer während der Entwicklung simulieren: ein gepflegter Nachfolger von
[RimDev.Stuntman](https://github.com/ritterim/stuntman) für ASP.NET Core ab .NET 8.

> Status: Funktionsumfang komplett (Phase 1 bis 5), Release-Vorbereitung läuft. Noch nicht auf NuGet veröffentlicht.

## Pakete

| Paket | Zweck |
| --- | --- |
| `StandIn.AspNetCore` | Handler, Benutzerquellen, Endpunkte, User Picker für MVC/Razor Pages, Token-Endpunkt |
| `StandIn.Blazor` | Picker-Komponente, `AuthenticationStateProvider`, Token-Handling (Server, WASM, Auto, Hybrid) |
| `StandIn.Client` | `HttpClient`-Handler für MAUI, WPF, WinForms, Konsole, Worker |
| `StandIn.Testing` | Hilfen für `WebApplicationFactory`-Integrationstests |

Ziel-Frameworks: `net8.0` und `net10.0`. .NET 8 und 9 sind seit dem 10.11.2026 nicht mehr
von Microsoft unterstützt, `net8.0` bleibt für Bestandsprojekte im Paket.

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

In Views (MVC, Razor Pages) steht der User Picker mit `@Html.StandInUserPicker()` bereit; `app.UseStandInPicker()` blendet ihn zusätzlich in jede HTML-Seite ein.

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

## Lizenz

MIT. Teile basieren auf RimDev.Stuntman (MIT, © 2018 Ritter Insurance Marketing),
siehe [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
