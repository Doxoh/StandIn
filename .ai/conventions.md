# Conventions

## Simplicity first

The owner explicitly asks for simplicity (review with the `keep-it-simple` plugin):

- No interfaces with a single implementation, no factories, no base classes "just in case".
- A file only if it carries something. A method instead of a class if that is enough.
- Public API is expensive because it stays after publishing. Everything not needed from outside is `internal`.
- No parameters nobody passes. No configuration without a second value.

## Code

- `Nullable` on, `TreatWarningsAsErrors` on (only `CS1591` for missing XML docs is silenced).
- Packages are managed centrally in `Directory.Packages.props`, with TFM-dependent floating versions (`8.0.*`, `10.0.*`).
- Code, comments, XML docs, tests and documentation are written in English. Code comments are short.
- API names follow Stuntman (`AddUser`, `EnableServer`, ...), unless a deviation is listed in [parity.md](parity.md).

## Tests

- xUnit, TestServer based (`tests/StandIn.AspNetCore.Tests/TestApp.cs`).
- Every behavior from [security.md](security.md) has a test.
- There is often no local .NET SDK; GitHub CI is then the compiler. Read the code carefully before pushing (missing `using`, header validation).
