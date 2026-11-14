# Migrating from RimDev.Stuntman

StandIn is a successor with the same concept, the same paths under `/stuntman` and the same JSON file. Most projects only need renamed types.

## Steps

1. Swap the package: remove `RimDev.Stuntman.*`, add `StandIn.AspNetCore`.
2. Rename types: `StuntmanUser` → `StandInUser`, `StuntmanOptions` → `StandInOptions`.
3. Switch the registration to `AddAuthentication().AddStandIn(environment, options => ...)` and `app.MapStandIn()` (see the README). The call belongs between `UseAuthentication` and the endpoints.
4. Picker in views: `@Html.StandInUserPicker()`. You can optionally pass a `returnUrl`.
5. Your own paths, cookies and headers stay as they were: `/stuntman/sign-in`, `sign-out`, `server`, the JSON format and the bearer behavior are the same.

The options keep their names: `AddUser`, `AddUsersFromJson`, `AddConfigurationFromServer`, `TryAddConfigurationFromServer`, `EnableServer`, `SetUserPickerAlignment`, `AllowCookieAuthentication`, `AllowBearerTokenAuthentication`, `AllowBearerTokenPassthrough` and `AfterBearerValidateIdentity`. `StandInUser` has `AddClaim`, `AddName`, `AddRole`, `SetAccessToken`, `SetDescription` and the three constructors.

## What changes

| Topic | RimDev.Stuntman | StandIn |
| --- | --- | --- |
| Production | throws at startup | does nothing; only an explicitly allowed `Production` throws |
| `ReturnUrl` | any | local only, or allowed through `AddAllowedReturnOrigin` |
| Authorization header | any scheme | `Bearer` only |
| Claims | as configured | additionally `ClaimTypes.Name`, `NameIdentifier`, `standin_user_id` |
| 400/403 for bearer | while authenticating | written at challenge |
| Server endpoint | always reachable | only with `EnableServer()` and only in Development |

The reasons are in [.ai/parity.md](../.ai/parity.md) and [.ai/security.md](../.ai/security.md).

## New in StandIn

- `POST /stuntman/token` for clients without cookies (`EnableTokenEndpoint()`), plus `StandIn.Client` and `StandIn.Blazor`.
- `FrontendBaseUrl` for a frontend on another address, with CORS only for allowed origins.
- `SetUserPickerVerticalPosition` (picker at the bottom instead of the top).
- `AddUsersFromConfiguration` (users from `appsettings`).
- Test header `X-Stuntman-User` (`AllowTestHeader`, off by default) and the `StandIn.Testing` package.

## Checking

I compared the original API from memory, not against every version of the original. If the compiler still cannot find something after a rename, the name is listed in [.ai/parity.md](../.ai/parity.md) or was left out on purpose.
