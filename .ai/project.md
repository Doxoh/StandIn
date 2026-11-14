# Project

**StandIn** is the maintained successor of [RimDev.Stuntman](https://github.com/ritterim/stuntman) (archived, MIT). It simulates users during development: sign-in by cookie with a user picker and by bearer token, for ASP.NET Core applications and REST APIs.

- License MIT (Till Schreiber); the Stuntman license text is in `THIRD-PARTY-NOTICES.md`.
- Target frameworks: `net8.0;net10.0`. .NET 8 and 9 have been out of support since 10 November 2026, .NET 10 LTS runs until November 2028.
- Owner: Till (GitHub `Doxoh`), repo `Doxoh/StandIn`, public with NuGet publishing.
- The default path stays `/stuntman` (migration without changes); `FrontendBaseUrl` covers separate frontends.

## Packages

| Project | Status |
| --- | --- |
| `src/StandIn.AspNetCore` | Core, done (phases 1 and 2) |
| `src/StandIn.Blazor` | Picker component, AuthenticationStateProvider (phase 5b) |
| `src/StandIn.Client` | Session and token handler (phase 5a) |
| `src/StandIn.Testing` | Test header helpers (phase 3) |
| `samples/MinimalApi`, `samples/Mvc` | Sample apps (phase 5c) |
| `tests/StandIn.AspNetCore.Tests` | xUnit |
| `tests/StandIn.Samples.Tests` | Smoke tests of the sample apps |

For the status of the phases see [roadmap.md](roadmap.md).
