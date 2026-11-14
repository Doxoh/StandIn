# StandIn

Benutzer während der Entwicklung simulieren: ein gepflegter Nachfolger von
[RimDev.Stuntman](https://github.com/ritterim/stuntman) für ASP.NET Core ab .NET 8.

> Status: in Entwicklung (Phase 1). Noch nicht auf NuGet veröffentlicht.

## Pakete

| Paket | Zweck |
| --- | --- |
| `StandIn.AspNetCore` | Handler, Benutzerquellen, Endpunkte, User Picker für MVC/Razor Pages, Token-Endpunkt |
| `StandIn.Blazor` | Picker-Komponente, `AuthenticationStateProvider`, Token-Handling (Server, WASM, Auto, Hybrid) |
| `StandIn.Client` | `HttpClient`-Handler für MAUI, WPF, WinForms, Konsole, Worker |
| `StandIn.Testing` | Hilfen für `WebApplicationFactory`-Integrationstests |

Ziel-Frameworks: `net8.0` und `net10.0`. .NET 8 und 9 sind seit dem 10.11.2026 nicht mehr
von Microsoft unterstützt, `net8.0` bleibt für Bestandsprojekte im Paket.

## Schnellstart (Zielbild)

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

`AddStandIn` registriert nur in der Umgebung `Development`. Details: [docs/concept.md](docs/concept.md).

## Lizenz

MIT. Teile basieren auf RimDev.Stuntman (MIT, © 2018 Ritter Insurance Marketing),
siehe [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
