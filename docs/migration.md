# Migration von RimDev.Stuntman

StandIn ist ein Nachfolger mit demselben Konzept, den Pfaden unter `/stuntman` und derselben JSON-Datei. Die meisten Projekte brauchen nur umbenannte Typen.

## Schritte

1. Paket tauschen: `RimDev.Stuntman.*` entfernen, `StandIn.AspNetCore` hinzufügen.
2. Typen umbenennen: `StuntmanUser` → `StandInUser`, `StuntmanOptions` → `StandInOptions`.
3. Registrierung auf `AddAuthentication().AddStandIn(environment, options => ...)` und `app.MapStandIn()` umstellen (siehe README). Der Aufruf gehört zwischen `UseAuthentication` und die Endpunkte.
4. Picker in Views: `@Html.StandInUserPicker()`. Optional lässt sich eine `returnUrl` übergeben.
5. Eigene Pfade, Cookies oder Header bleiben, wie sie waren: `/stuntman/sign-in`, `sign-out`, `server`, JSON-Format und Bearer-Verhalten sind gleich.

Die Optionen heißen weiter `AddUser`, `AddUsersFromJson`, `AddConfigurationFromServer`, `TryAddConfigurationFromServer`, `EnableServer`, `SetUserPickerAlignment`, `AllowCookieAuthentication`, `AllowBearerTokenAuthentication`, `AllowBearerTokenPassthrough` und `AfterBearerValidateIdentity`. An `StandInUser` gibt es `AddClaim`, `AddName`, `AddRole`, `SetAccessToken`, `SetDescription` und die drei Konstruktoren.

## Was sich ändert

| Thema | RimDev.Stuntman | StandIn |
| --- | --- | --- |
| Production | wirft beim Start | tut nichts; nur ein ausdrücklich freigegebenes `Production` wirft |
| `ReturnUrl` | beliebig | nur lokal oder über `AddAllowedReturnOrigin` freigegeben |
| Authorization-Header | jedes Schema | nur `Bearer` |
| Claims | wie konfiguriert | zusätzlich `ClaimTypes.Name`, `NameIdentifier`, `standin_user_id` |
| 400/403 bei Bearer | beim Authentifizieren | beim Challenge geschrieben |
| Server-Endpunkt | immer erreichbar | nur mit `EnableServer()` und nur in Development |

Die Gründe stehen in [.ai/parity.md](../.ai/parity.md) und [.ai/security.md](../.ai/security.md).

## Neu in StandIn

- `POST /stuntman/token` für Clients ohne Cookie (`EnableTokenEndpoint()`), dazu `StandIn.Client` und `StandIn.Blazor`.
- `FrontendBaseUrl` für ein Frontend auf anderer Adresse, mit CORS nur für freigegebene Origins.
- `SetUserPickerVerticalPosition` (Picker unten statt oben).
- `AddUsersFromConfiguration` (Benutzer aus `appsettings`).
- Test-Header `X-Stuntman-User` (`AllowTestHeader`, standardmäßig aus) und das Paket `StandIn.Testing`.

## Prüfen

Die Original-API habe ich aus der Erinnerung gegenübergestellt, nicht gegen jede Version des Originals geprüft. Wenn der Compiler nach einer Umbenennung noch etwas nicht findet, steht der Name in [.ai/parity.md](../.ai/parity.md) oder fehlt bewusst.
