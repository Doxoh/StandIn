# Architecture (`StandIn.AspNetCore`)

All types are in the namespace `StandIn.AspNetCore`.

| Type | Purpose |
| --- | --- |
| `StandInUser` | User: id, name, claims, access token, description, source; fluent API; `ToPrincipal` (internal) |
| `StandInOptions` | `AuthenticationSchemeOptions`; user list, paths, switches, hooks, allowed return origins |
| `StandInAuthenticationHandler` | Authenticates bearer and cookie; writes 400/403 at challenge |
| `StandInServiceCollectionExtensions` | `AuthenticationBuilder.AddStandIn(env, configure, additionalEnvironments)` |
| `StandInEndpointRouteBuilderExtensions` | `MapStandIn()` with `sign-in`, `sign-out`, `server`, `users` |
| `StandInUserLoader` | Reads and writes JSON (file or http(s) URL) |
| `StandInPickerRenderer` | HTML for the picker and the login page, without JavaScript (`details/summary`) |
| `StandInViewExtensions` | `Html.StandInUserPicker(returnUrl?)` |
| `StandInApplicationBuilderExtensions` | `UseStandInPicker()`: inserts the picker before `</body>` in HTML pages (only GET with `Accept: text/html`, not under the root path, not for compressed or non-UTF-8 responses) |
| `StandInDefaults` | Scheme name `StandIn`, root `/stuntman`; the rest is internal |

## Flow

1. `AddStandIn` checks the environment. Only Development or explicitly allowed environments; otherwise nothing happens. An active registration is detected through the internal `StandInMarker` in the container.
2. If active: StandIn becomes the default scheme.
3. `MapStandIn` reads the options once (via `GetStandInOptions()`, `null` = StandIn not active), logs a warning if the server endpoint is on, and maps conditionally: `sign-in` and `sign-out` only with `AllowCookieAuthentication`, `server` only with `EnableServer()`, `users` always.
4. The handler, in this order: `Authorization: Bearer <token>`, then (only with `AllowTestHeader`) `X-Stuntman-User: <id>`, then the cookie `.StandIn.User` (contains only the user id). An unknown id in the test header gives 403.
5. Failures from the bearer path (400/403) end up in `HttpContext.Items["StandIn.Failure"]` and are written in `HandleChallengeAsync`. A browser GET with `Accept: text/html` is redirected to sign-in, everything else gets 401.

## Endpoints (under `RootPath`, default `/stuntman`)

| Path | Behavior |
| --- | --- |
| `GET sign-in?OverrideUserId=&ReturnUrl=` | Set the cookie and redirect; unknown id 404; without an id the login page |
| `GET sign-out?ReturnUrl=` | Delete the cookie and redirect |
| `GET server` | All users including tokens (only with `EnableServer()`) |
| `GET users` | Users without tokens; CORS headers for allowed origins |
| `POST token` | Only with `EnableTokenEndpoint()`; body `{ "userId": "..." }` (JSON only), response `access_token`, `token_type`, `user_id`, `name`, `claims`; 404 unknown user, 400 body missing/broken, 415 not JSON. Users without a token get a random one at startup (in memory only). Preflight (`OPTIONS`) for allowed origins |

## Separate frontends, PathBase, root path

- `FrontendBaseUrl` (e.g. `https://localhost:5173/`): return target when neither `ReturnUrl` nor `Referer` is present; its origin counts as allowed automatically. More origins via `AddAllowedReturnOrigin`.
- Allowed origins apply to the return redirect **and** to CORS (`users`, `token`). `server` never gets CORS headers. Foreign origins get no CORS headers.
- All links and redirects use `Request.PathBase`. Behind a reverse proxy the application has to use `UseForwardedHeaders` (host, proto) itself; StandIn only reads `Request.Host`, `Scheme` and `PathBase` and does not evaluate `X-Forwarded-*` on its own.
- If `RootPath` changes, `{/stuntman}/{**rest}` redirects with 308 (method is kept) to the new path. No redirect if the new path is below `/stuntman` (loop). The target is always local (leading `/` and `\` in the rest are removed).
- OpenAPI/Swagger: no code in the package (no extra dependency). The application registers the bearer scheme itself (`AddSecurityDefinition("Bearer", ...)`).

## JSON format (compatible with Stuntman)

```json
{ "Users": [ { "Id": "", "Name": "", "AccessToken": "", "Description": "",
               "NameClaimType": "", "RoleClaimType": "",
               "Claims": [ { "Type": "", "Value": "" } ] } ] }
```

Written in PascalCase, `null` omitted. Read case-insensitively, comments and trailing commas allowed.

## Client (`StandIn.Client`)

For everything without cookies (MAUI, WPF, WinForms, console, worker, Blazor WASM/Hybrid). Depends only on `Microsoft.Extensions.Http`, not on `StandIn.AspNetCore`, so it also runs in the browser.

| Type | Purpose |
| --- | --- |
| `StandInSession` | Singleton: `GetUsersAsync()` (`GET users`), `SignInAsync(id)` (`POST token`), `SignOut()`, `Current`, `Changed`, `GetAccessTokenAsync()` (signs in the start user on first call) |
| `StandInTokenHandler` | `DelegatingHandler`: sets `Authorization: Bearer` of the signed-in user; an existing header is kept |
| `AddStandInClient(o => o.ServerAddress = ...)` | Registers the session; then `services.AddHttpClient("api").AddStandInBearerToken()` |

The token response contains `name_claim_type`, `role_claim_type` and the full claim list the server itself would build (`ToPrincipal`), so clients build the same principal. State lives in memory only: a reload (Blazor WASM) signs the start user in again.

## Blazor (`StandIn.Blazor`)

| Render mode | Approach |
| --- | --- |
| Blazor Server / server part of Auto | Cookie like MVC. No component of its own: `UseStandInPicker()` inserts the picker into the delivered HTML page |
| WebAssembly, WASM part of Auto, Hybrid | Token: `services.AddStandInBlazor(o => o.ServerAddress = ...)` (call only in Development), `<StandInPicker />` (a select with the server's users), `StandInAuthenticationStateProvider` (singleton, principal from the token response), API clients with `.AddStandInBearerToken()` |

`StandIn.Blazor` depends only on `StandIn.Client` and `Microsoft.AspNetCore.Components.Authorization`/`.Web` (runs in the browser). State lives in memory only; a reload signs the start user (`StandInClientOptions.UserId`) in again. The client is harmless in Release too: without a token endpoint on the server, sign-in fails with 404.

## Sample apps and smoke tests

`samples/MinimalApi` (bearer, token endpoint) and `samples/Mvc` (cookie, `@Html.StandInUserPicker()`), each with its own `Program` class in its own namespace (no top-level program, so two samples can be referenced from one test project without a name clash). `tests/StandIn.Samples.Tests` starts them with `WebApplicationFactory` and checks sign-in, roles, token endpoint, `StandIn.Client`, `StandIn.Testing` and the behavior outside Development.

## User sources

`AddUser`, `AddUsersFromJson(pathOrUrl)`, `AddUsersFromConfiguration(IConfiguration)` (same structure as the JSON, e.g. `GetSection("StandIn")` with `Users:0:Id`), `AddConfigurationFromServer` and `TryAddConfigurationFromServer` (https only, http only for localhost). Each user remembers its source (`Local`, path/URL, `Configuration`, server URL).

## Testing

`StandIn.Testing` (depends on `StandIn.AspNetCore` and `Microsoft.AspNetCore.Mvc.Testing`): `services.AllowStandInTestHeader()` turns the test header on, `httpClient.AsStandInUser("id")` sets it for all requests, `factory.CreateStandInClient("id")` does both for a `WebApplicationFactory`.

## Claims in the principal

Own claims plus `standin_user_id`, the name under the user's name claim type and under `ClaimTypes.Name`, `ClaimTypes.NameIdentifier` (if missing), and `access_token` for bearer. No duplicate claims.
