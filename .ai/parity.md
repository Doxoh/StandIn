# Parität zu RimDev.Stuntman

## Enthalten (Auszug)

- Bearer: kein Header weiter; falsches Format 400; unbekanntes Token 403, außer `AllowBearerTokenPassthrough`; Treffer liefert Claim `access_token`.
- Cookie-Anmeldung über `sign-in` und `sign-out`, User Picker mit Ausrichtung, Source und Description als Tooltip.
- `server`-Endpunkt, `AddConfigurationFromServer`, `TryAddConfigurationFromServer`, `AddUsersFromJson`.
- `AfterBearerValidateIdentity`, `SetUserPickerAlignment`, drei Konstruktoren von `StandInUser`.

## Bewusste Abweichungen

| Original | StandIn | Grund |
| --- | --- | --- |
| Production wirft | No-op; nur explizites Freigeben von Production wirft | derselbe Code in allen Umgebungen |
| beliebige ReturnUrl | nur lokal oder freigegebene Origins | Open-Redirect |
| jeder Authorization-Header | nur Schema `Bearer` | andere Schemas nicht stören |
| 400/403 in Authenticate | beim Challenge geschrieben | saubere Handler-Semantik |
| Claims minimal | zusätzlich `ClaimTypes.Name`, `NameIdentifier`, `standin_user_id` | Kompatibilität mit Identity-Code und Picker |

## Bewusst nicht umgesetzt

- Asynchrone Varianten von `AddUsersFromJson` und `AddConfigurationFromServer` (die Optionen-API bleibt fluent und synchron, Timeout 10 s). Nachrüsten, wenn jemand es braucht.
- Tag Helper `<standin-picker />` (siehe [conventions.md](conventions.md)).
- `StandIn.Testing` ohne `WebApplicationFactory`-Hilfsmethode und ohne Abhängigkeit zu `Microsoft.AspNetCore.Mvc.Testing`: Die Methode wäre ohne eigenen Einstiegspunkt nicht testbar. Das Muster steht in der Doku (`WithWebHostBuilder` + `AllowStandInTestHeader()` + `AsStandInUser`).

## Noch offen (Phase 4)

Token-Endpunkt, `FrontendBaseUrl`, PathBase und `X-Forwarded-*`, 308-Weiterleitung, CORS.
