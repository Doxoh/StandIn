# Parity with RimDev.Stuntman

## Included (excerpt)

- Bearer: no header continues; wrong format 400; unknown token 403 unless `AllowBearerTokenPassthrough`; a match yields the claim `access_token`.
- Cookie sign-in through `sign-in` and `sign-out`, user picker with alignment, source and description as tooltip.
- `server` endpoint, `AddConfigurationFromServer`, `TryAddConfigurationFromServer`, `AddUsersFromJson`.
- `AfterBearerValidateIdentity`, `SetUserPickerAlignment`, three constructors of `StandInUser`.

## Additions

- `SetUserPickerVerticalPosition(Top | Bottom)`: picker at the top (default) or the bottom; the original only knows the horizontal alignment.

## Deliberate deviations

| Original | StandIn | Reason |
| --- | --- | --- |
| Production throws | No-op; only explicitly allowing Production throws | the same code in all environments |
| any ReturnUrl | local only or allowed origins | open redirect |
| any Authorization header | only the `Bearer` scheme | do not disturb other schemes |
| 400/403 in Authenticate | written at challenge | clean handler semantics |
| minimal claims | additionally `ClaimTypes.Name`, `NameIdentifier`, `standin_user_id` | compatibility with identity code and the picker |

## Deliberately not implemented

- Async variants of `AddUsersFromJson` and `AddConfigurationFromServer` (the options API stays fluent and synchronous, timeout 10 s). Add them if someone needs them.
- Tag helper `<standin-picker />` (see [conventions.md](conventions.md)).
- Sample apps only for Minimal API (bearer, token endpoint, client) and MVC (cookie, picker). Razor Pages, Blazor Server and Blazor WebAssembly samples are missing: same mechanics, but extra maintenance; the approach is described in [architecture.md](architecture.md).
- OpenAPI security scheme as package code (see [architecture.md](architecture.md)).
- Own evaluation of `X-Forwarded-*`: that is the job of the application's `UseForwardedHeaders`.
