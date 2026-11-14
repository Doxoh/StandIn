# Decisions by the owner

Binding. Changes only after asking.

1. Name **StandIn**, license MIT, API modeled on Stuntman, full feature parity.
2. Supports **.NET 8 and newer**, multi-target `net8.0;net10.0`.
3. All .NET frontends and REST APIs: MVC/Razor, Blazor (Server, WASM, Auto, Hybrid), MAUI/WPF/WinForms.
4. Public NuGet publishing; the prefix `StandIn` is to be reserved.
5. The path `/stuntman` stays the default, with redirect and the option `FrontendBaseUrl`.
6. Development started only after explicit approval by the owner. Do not merge, close or publish without being asked.
7. Code, documentation and the repository are in English (changed from German on 2026-11-14, when the repo went public). Agents still talk to the owner in the language the owner uses.
8. No Roslyn analyzer for 1.0 (keep-it-simple): the migration guide (`docs/migration.md`) is enough, an analyzer only when there is a concrete need.
9. NuGet publishing through Trusted Publishing (OIDC) instead of a long-lived `NUGET_API_KEY`: nothing to rotate or leak.
