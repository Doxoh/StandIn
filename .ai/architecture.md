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
| `GET users` | Benutzer ohne Tokens |

## JSON-Format (kompatibel zu Stuntman)

```json
{ "Users": [ { "Id": "", "Name": "", "AccessToken": "", "Description": "",
               "NameClaimType": "", "RoleClaimType": "",
               "Claims": [ { "Type": "", "Value": "" } ] } ] }
```

Schreiben in PascalCase, `null` weggelassen. Lesen ohne Beachtung der Groß-/Kleinschreibung, Kommentare und Komma am Ende erlaubt.

## Benutzerquellen

`AddUser`, `AddUsersFromJson(pfadOderUrl)`, `AddUsersFromConfiguration(IConfiguration)` (gleiche Struktur wie das JSON, z. B. `GetSection("StandIn")` mit `Users:0:Id`), `AddConfigurationFromServer` und `TryAddConfigurationFromServer` (nur https, http nur für localhost). Jeder Benutzer merkt sich seine Quelle (`Local`, Pfad/URL, `Configuration`, Server-URL).

## Pakete

`StandIn.Testing`: `services.AllowStandInTestHeader()` schaltet den Test-Header ein, `httpClient.AsStandInUser("id")` setzt ihn für alle Anfragen. Abhängig nur von `StandIn.AspNetCore`.

## Claims im Principal

Eigene Claims plus `standin_user_id`, Name unter dem Namens-Claim-Typ des Benutzers und unter `ClaimTypes.Name`, `ClaimTypes.NameIdentifier` (falls nicht vorhanden), bei Bearer `access_token`. Keine doppelten Claims.
