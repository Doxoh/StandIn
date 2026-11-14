# Architektur (`StandIn.AspNetCore`)

Alle Typen im Namespace `StandIn.AspNetCore`.

| Typ | Aufgabe |
| --- | --- |
| `StandInUser` | Benutzer: Id, Name, Claims, AccessToken, Description, Source; fluent API; `ToPrincipal` (intern) |
| `StandInOptions` | `AuthenticationSchemeOptions`; Benutzerliste, Pfade, Schalter, Hooks, erlaubte Rücksprung-Origins |
| `StandInAuthenticationHandler` | Bearer und Cookie authentifizieren; 400/403 beim Challenge schreiben |
| `StandInServiceCollectionExtensions` | `AuthenticationBuilder.AddStandIn(env, configure, additionalEnvironments)` |
| `StandInEndpointRouteBuilderExtensions` | `MapStandIn()` mit `sign-in`, `sign-out`, `server`, `users` |
| `StandInUserLoader` | JSON lesen und schreiben (Datei oder http(s)-URL) |
| `StandInPickerRenderer` | HTML für Picker und Login-Seite, ohne JavaScript (`details/summary`) |
| `StandInViewExtensions` | `Html.StandInUserPicker(returnUrl?)` |
| `StandInApplicationBuilderExtensions` | `UseStandInPicker()`: Picker automatisch vor `</body>` in HTML-Seiten (nur GET mit `Accept: text/html`, nicht unter dem Root-Pfad, nicht bei komprimierten oder Nicht-UTF-8-Antworten) |
| `StandInDefaults` | Schemaname `StandIn`, Root `/stuntman`; Rest intern |

## Ablauf

1. `AddStandIn` prüft die Umgebung. Nur Development oder ausdrücklich freigegebene Umgebungen; sonst passiert nichts. Aktiv wird über den internen `StandInMarker` im Container erkannt.
2. Aktiv: StandIn wird Standard-Schema.
3. `MapStandIn` liest die Optionen einmal (über `GetStandInOptions()`, `null` = StandIn nicht aktiv), warnt im Log, wenn der Server-Endpunkt an ist, und mappt bedingt: `sign-in` und `sign-out` nur mit `AllowCookieAuthentication`, `server` nur mit `EnableServer()`, `users` immer.
4. Handler, in dieser Reihenfolge: `Authorization: Bearer <token>`, dann (nur mit `AllowTestHeader`) `X-Stuntman-User: <Id>`, dann Cookie `.StandIn.User` (enthält nur die Benutzer-Id). Unbekannte Id im Test-Header ergibt 403.
5. Fehler aus dem Bearer-Pfad (400/403) landen in `HttpContext.Items["StandIn.Failure"]` und werden in `HandleChallengeAsync` geschrieben. Browser-GET mit `Accept: text/html` wird zum Sign-in umgeleitet, alles andere bekommt 401.

## Endpunkte (unter `RootPath`, Standard `/stuntman`)

| Pfad | Verhalten |
| --- | --- |
| `GET sign-in?OverrideUserId=&ReturnUrl=` | Cookie setzen und weiterleiten; unbekannte Id 404; ohne Id Login-Seite |
| `GET sign-out?ReturnUrl=` | Cookie löschen und weiterleiten |
| `GET server` | alle Benutzer inkl. Tokens (nur mit `EnableServer()`) |
| `GET users` | Benutzer ohne Tokens; CORS-Header für freigegebene Origins |
| `POST token` | nur mit `EnableTokenEndpoint()`; Body `{ "userId": "..." }` (nur JSON), Antwort `access_token`, `token_type`, `user_id`, `name`, `claims`; 404 unbekannter Benutzer, 400 Body fehlt/kaputt, 415 kein JSON. Benutzer ohne Token bekommen beim Start ein zufälliges (nur im Speicher). Preflight (`OPTIONS`) für freigegebene Origins |

## Getrennte Frontends, PathBase, Root-Pfad

- `FrontendBaseUrl` (z. B. `https://localhost:5173/`): Rücksprung-Ziel, wenn weder `ReturnUrl` noch `Referer` vorliegen; die Origin gilt automatisch als freigegeben. Weitere Origins über `AddAllowedReturnOrigin`.
- Freigegebene Origins gelten für Rücksprung **und** CORS (`users`, `token`). `server` bekommt nie CORS-Header. Fremde Origins bekommen keine CORS-Header.
- Alle Links und Weiterleitungen nutzen `Request.PathBase`. Hinter einem Reverse Proxy muss die Anwendung `UseForwardedHeaders` (Host, Proto) selbst einsetzen; StandIn liest nur `Request.Host`, `Scheme` und `PathBase` und hat keine eigene Auswertung von `X-Forwarded-*`.
- Ändert sich `RootPath`, leitet `{/stuntman}/{**rest}` per 308 (Methode bleibt) auf den neuen Pfad. Kein Redirect, wenn der neue Pfad unter `/stuntman` liegt (Schleife). Das Ziel ist immer lokal (führende `/` und `\` im Rest werden entfernt).
- OpenAPI/Swagger: kein Code im Paket (keine zusätzliche Abhängigkeit). Das Bearer-Schema trägt die Anwendung selbst ein (`AddSecurityDefinition("Bearer", ...)`).

## JSON-Format (kompatibel zu Stuntman)

```json
{ "Users": [ { "Id": "", "Name": "", "AccessToken": "", "Description": "",
               "NameClaimType": "", "RoleClaimType": "",
               "Claims": [ { "Type": "", "Value": "" } ] } ] }
```

Schreiben in PascalCase, `null` weggelassen. Lesen ohne Beachtung der Groß-/Kleinschreibung, Kommentare und Komma am Ende erlaubt.

## Client (`StandIn.Client`)

Für alles, was keine Cookies hat (MAUI, WPF, WinForms, Konsole, Worker, Blazor WASM/Hybrid). Hängt nur an `Microsoft.Extensions.Http`, nicht an `StandIn.AspNetCore`; läuft damit auch im Browser.

| Typ | Aufgabe |
| --- | --- |
| `StandInSession` | Singleton: `GetUsersAsync()` (`GET users`), `SignInAsync(id)` (`POST token`), `SignOut()`, `Current`, `Changed`, `GetAccessTokenAsync()` (meldet beim ersten Aufruf den Start-Benutzer an) |
| `StandInTokenHandler` | `DelegatingHandler`: setzt `Authorization: Bearer` des angemeldeten Benutzers; ein vorhandener Header bleibt |
| `AddStandInClient(o => o.ServerAddress = ...)` | registriert die Session; danach `services.AddHttpClient("api").AddStandInBearerToken()` |

Die Token-Antwort enthält `name_claim_type`, `role_claim_type` und die vollständige Claim-Liste, die der Server selbst bilden würde (`ToPrincipal`), damit Clients denselben Principal bauen. Der Zustand liegt nur im Speicher: ein Neuladen (Blazor WASM) meldet wieder den Start-Benutzer an.

## Blazor (`StandIn.Blazor`)

| Render-Art | Verfahren |
| --- | --- |
| Blazor Server / Server-Teil von Auto | Cookie wie MVC. Keine eigene Komponente: `UseStandInPicker()` blendet den Picker in die ausgelieferte HTML-Seite ein |
| WebAssembly, WASM-Teil von Auto, Hybrid | Token: `services.AddStandInBlazor(o => o.ServerAddress = ...)` (nur in Development aufrufen), `<StandInPicker />` (Auswahlfeld mit den Benutzern des Servers), `StandInAuthenticationStateProvider` (Singleton, Principal aus der Token-Antwort), API-Clients mit `.AddStandInBearerToken()` |

`StandIn.Blazor` hängt nur an `StandIn.Client` und `Microsoft.AspNetCore.Components.Authorization`/`.Web` (läuft im Browser). Der Zustand liegt nur im Speicher; ein Neuladen meldet wieder den Start-Benutzer (`StandInClientOptions.UserId`) an. Auch in Release ist der Client harmlos: ohne Token-Endpunkt auf dem Server schlägt die Anmeldung mit 404 fehl.

## Benutzerquellen

`AddUser`, `AddUsersFromJson(pfadOderUrl)`, `AddUsersFromConfiguration(IConfiguration)` (gleiche Struktur wie das JSON, z. B. `GetSection("StandIn")` mit `Users:0:Id`), `AddConfigurationFromServer` und `TryAddConfigurationFromServer` (nur https, http nur für localhost). Jeder Benutzer merkt sich seine Quelle (`Local`, Pfad/URL, `Configuration`, Server-URL).

## Pakete

`StandIn.Testing`: `services.AllowStandInTestHeader()` schaltet den Test-Header ein, `httpClient.AsStandInUser("id")` setzt ihn für alle Anfragen. Abhängig nur von `StandIn.AspNetCore`.

## Claims im Principal

Eigene Claims plus `standin_user_id`, Name unter dem Namens-Claim-Typ des Benutzers und unter `ClaimTypes.Name`, `ClaimTypes.NameIdentifier` (falls nicht vorhanden), bei Bearer `access_token`. Keine doppelten Claims.
