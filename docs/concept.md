# Umsetzungskonzept StandIn

Diese Datei fasst die verbindlichen Entscheidungen und den ursprünglichen Entwurf zusammen. Den aktuellen Stand der Umsetzung zeigt die [Roadmap](../.ai/roadmap.md).

## Entscheidungen

| Frage | Entscheidung |
| --- | --- |
| Name | StandIn, Pakete `StandIn.*` (nicht `RimDev.*`) |
| Frameworks | `net8.0;net10.0` |
| Frontends | Alle .NET-Frontends und REST-APIs |
| Pfad | `/stuntman` bleibt Standard; Weiterleitungen müssen funktionieren; `FrontendBaseUrl` für getrennte Frontends |
| Veröffentlichung | Öffentlich auf NuGet, Präfix `StandIn` reservieren |

## Feature-Parität zu RimDev.Stuntman (Auszug)

- Benutzer: `AddUser` (doppelte Id → Exception); `StandInUser(id, name)`, `(name)` mit Auto-GUID,
  `(id, name, nameClaimType, roleClaimType)`; `AddClaim`, `AddName`, `AddRole`, `SetAccessToken`,
  `SetDescription` (die Quelle setzt StandIn selbst, kein öffentliches `SetSource`)
- Quellen: `AddUsersFromJson(pfadOderUrl, configureUsers?)`, JSON-Format `{ "Users": [ { "Id", "Name", "AccessToken", "Description", "NameClaimType", "RoleClaimType", "Claims": [ { "Type", "Value" } ] } ] }`
- Endpunkte unter `/stuntman/`: `sign-in?OverrideUserId=&ReturnUrl=` (unbekannte Id → 404; ohne Id Login-Seite),
  `sign-out?ReturnUrl=`, `server` (nur mit `EnableServer()`)
- Bearer: kein Header → weiter; falsches Format → 400; unbekanntes Token → 403 (außer `AllowBearerTokenPassthrough`);
  Treffer → Claim `access_token`
- Schalter: `AllowCookieAuthentication`, `AllowBearerTokenAuthentication` (beide Standard an), `AllowBearerTokenPassthrough`
- Picker: `UserPicker(principal[, returnUrl])`, Ausrichtung Left/Center/Right, Logout-Eintrag
- Konfiguration von anderem Server: `AddConfigurationFromServer`, `TryAddConfigurationFromServer`
- Hook `AfterBearerValidateIdentity`

**Neu:** `POST /stuntman/token` (`EnableTokenEndpoint()`), `FrontendBaseUrl`, `AllowedReturnOrigins`, `PathBase`,
Minimal APIs, Blazor, `StandIn.Client`, `StandIn.Testing`, Test-Header `X-Stuntman-User` (Standard aus).

## Sicherheitsregeln (verbindlich)

1. Aktiviert sich nur in `Development` (weitere Umgebungen nur ausdrücklich freigegeben).
2. Start-Guard: in `Production` registriert → Exception beim Start.
3. `ReturnUrl` nur lokal oder aus `AllowedReturnOrigins` (Original hat ein Open Redirect).
4. Server-Endpunkt liefert im Original alle Tokens öffentlich: nur Development, Warnung im Log.
5. Cookie `HttpOnly`, `SameSite=Lax`, enthält nur die Benutzer-Id.
6. Test-Header standardmäßig aus.

## Phasen

| Phase | Inhalt | Ergebnis |
| --- | --- | --- |
| 0 | Gerüst, CI, Repo-Einrichtung | Repo bereit |
| 1 | Handler, Store, Code-/JSON-Quellen, Cookie, Endpunkte, Guard | läuft lokal |
| 2 | Bearer inkl. Passthrough, Picker, Login-Seite | Vorschau 0.1 |
| 3 | Server/Client-Konfiguration, `IConfiguration`, Inject-Middleware, Test-Header, `StandIn.Testing` | Parität 0.5 |
| 4 | `FrontendBaseUrl`, `PathBase`, Origins, 308, CORS, Token-Endpunkt, OpenAPI | 0.8 |
| 5 | `StandIn.Blazor`, `StandIn.Client`, Beispiel-Apps | 0.9 |
| 6 | Tests, Doku, Migrationsleitfaden, optional Analyzer | 1.0 |
