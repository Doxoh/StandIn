# StandIn

Simulate users during development: a maintained successor of
[RimDev.Stuntman](https://github.com/ritterim/stuntman) for ASP.NET Core on .NET 8 and later.

[![CI](https://github.com/Doxoh/StandIn/actions/workflows/ci.yml/badge.svg)](https://github.com/Doxoh/StandIn/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/StandIn.AspNetCore.svg)](https://www.nuget.org/packages/StandIn.AspNetCore)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

With StandIn you sign in to your development environment as any test user with one click, without an identity provider.
It offers cookie sign-in with a user picker for MVC, Razor Pages and Blazor Server, bearer tokens for REST APIs, a token endpoint for clients
(Blazor WebAssembly, MAUI, WPF, console) and helpers for integration tests.

> **For development only.** StandIn switches itself on only in `Development` and cannot be activated in `Production`.
> It does not replace real authentication. Details: [SECURITY.md](SECURITY.md).

> Status: preview (0.x). The feature set matches RimDev.Stuntman plus extensions. The API may still change before 1.0.

## Installation

```
dotnet add package StandIn.AspNetCore --prerelease
```

| Package | Purpose |
| --- | --- |
| `StandIn.AspNetCore` | Handler, user sources, endpoints, user picker for MVC/Razor Pages/Blazor Server, token endpoint |
| `StandIn.Blazor` | Picker component and `AuthenticationStateProvider` for WebAssembly, Hybrid and Auto-WASM |
| `StandIn.Client` | `HttpClient` handler for MAUI, WPF, WinForms, console, worker |
| `StandIn.Testing` | Helpers for `WebApplicationFactory` integration tests |

Target frameworks: `net8.0` and `net10.0`. `net8.0` stays in the packages for existing projects, even though Microsoft has ended its support.

## Quick start

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

You get the user picker like this:

- MVC and Razor Pages: `@Html.StandInUserPicker()` in the view.
- Blazor Server and any HTML page: `app.UseStandInPicker()` inserts it into every page.
- Blazor WebAssembly, Hybrid, Auto-WASM: `<StandInPicker />` from `StandIn.Blazor`.

Set position and alignment with `SetUserPickerAlignment` and `SetUserPickerVerticalPosition`.

`AddStandIn` is active only in `Development`. In all other environments it does nothing, and it cannot be switched on in `Production`. The endpoints live under `/stuntman`.

## Where to go next

| I want to ... | Read |
| --- | --- |
| switch from RimDev.Stuntman | [docs/migration.md](docs/migration.md) |
| test a REST API with a bearer token | [samples/MinimalApi](samples/MinimalApi) |
| MVC with cookie and picker | [samples/Mvc](samples/Mvc) |
| sign a client (MAUI, WPF, Blazor WASM) in to the API | `StandIn.Client`, `AddStandInClient`; Blazor: `AddStandInBlazor` |
| run integration tests as a specific user | `factory.CreateStandInClient("admin")` from `StandIn.Testing` |
| security rules and design | [docs/concept.md](docs/concept.md) |

## Contributing

Please report bugs and ideas as an [issue](https://github.com/Doxoh/StandIn/issues) and send changes as a pull request (see [CONTRIBUTING.md](CONTRIBUTING.md)).
Please do not report security vulnerabilities in public, but as described in [SECURITY.md](SECURITY.md).

## License

MIT. Parts are based on RimDev.Stuntman (MIT, © 2018 Ritter Insurance Marketing),
see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
