# Implementation concept StandIn

This file summarizes the binding decisions and the original design. For the current state of the implementation see the [roadmap](../.ai/roadmap.md).

## Decisions

| Question | Decision |
| --- | --- |
| Name | StandIn, packages `StandIn.*` (not `RimDev.*`) |
| Frameworks | `net8.0;net10.0` |
| Frontends | All .NET frontends and REST APIs |
| Path | `/stuntman` stays the default; redirects must work; `FrontendBaseUrl` for separate frontends |
| Publishing | Public on NuGet, reserve the prefix `StandIn` |

## Feature parity with RimDev.Stuntman (excerpt)

- Users: `AddUser` (duplicate id → exception); `StandInUser(id, name)`, `(name)` with an auto GUID,
  `(id, name, nameClaimType, roleClaimType)`; `AddClaim`, `AddName`, `AddRole`, `SetAccessToken`,
  `SetDescription` (StandIn sets the source itself, there is no public `SetSource`)
- Sources: `AddUsersFromJson(pathOrUrl, configureUsers?)`, JSON format `{ "Users": [ { "Id", "Name", "AccessToken", "Description", "NameClaimType", "RoleClaimType", "Claims": [ { "Type", "Value" } ] } ] }`
- Endpoints under `/stuntman/`: `sign-in?OverrideUserId=&ReturnUrl=` (unknown id → 404; without an id the login page),
  `sign-out?ReturnUrl=`, `server` (only with `EnableServer()`)
- Bearer: no header → continue; wrong format → 400; unknown token → 403 (unless `AllowBearerTokenPassthrough`);
  a match → claim `access_token`
- Switches: `AllowCookieAuthentication`, `AllowBearerTokenAuthentication` (both on by default), `AllowBearerTokenPassthrough`
- Picker: `UserPicker(principal[, returnUrl])`, alignment Left/Center/Right, logout entry
- Configuration from another server: `AddConfigurationFromServer`, `TryAddConfigurationFromServer`
- Hook `AfterBearerValidateIdentity`

**New:** `POST /stuntman/token` (`EnableTokenEndpoint()`), `FrontendBaseUrl`, `AllowedReturnOrigins`, `PathBase`,
Minimal APIs, Blazor, `StandIn.Client`, `StandIn.Testing`, test header `X-Stuntman-User` (off by default).

## Security rules (binding)

1. Activates itself only in `Development` (further environments only if explicitly allowed).
2. Start guard: registered in `Production` → exception at startup.
3. `ReturnUrl` only local or from `AllowedReturnOrigins` (the original has an open redirect).
4. The original's server endpoint hands out all tokens publicly: Development only, warning in the log.
5. Cookie `HttpOnly`, `SameSite=Lax`, contains only the user id.
6. Test header off by default.

## Phases

| Phase | Content | Result |
| --- | --- | --- |
| 0 | Scaffold, CI, repo setup | repo ready |
| 1 | Handler, store, code/JSON sources, cookie, endpoints, guard | runs locally |
| 2 | Bearer including passthrough, picker, login page | preview 0.1 |
| 3 | Server/client configuration, `IConfiguration`, inject middleware, test header, `StandIn.Testing` | parity 0.5 |
| 4 | `FrontendBaseUrl`, `PathBase`, origins, 308, CORS, token endpoint, OpenAPI | 0.8 |
| 5 | `StandIn.Blazor`, `StandIn.Client`, sample apps | 0.9 |
| 6 | Tests, docs, migration guide, optional analyzer | 1.0 |
