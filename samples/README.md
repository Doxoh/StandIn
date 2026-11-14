# Beispiel-Apps

Beide laufen mit `dotnet run` in der Umgebung `Development` (ohne Angabe ist das beim Start aus dem Projektordner der Fall, sonst `ASPNETCORE_ENVIRONMENT=Development`). In der CI dienen sie als Smoke-Tests (`tests/StandIn.Samples.Tests`).

| App | Zeigt |
| --- | --- |
| `MinimalApi` | REST-API: Bearer-Token (`dev-admin-token`, `dev-viewer-token`), `POST /stuntman/token` |
| `Mvc` | Cookie-Anmeldung, User Picker mit `@Html.StandInUserPicker()`, geschützte Seite |

```
# MinimalApi
curl -H "Authorization: Bearer dev-admin-token" http://localhost:5000/me
curl -X POST -H "Content-Type: application/json" -d '{"userId":"viewer"}' http://localhost:5000/stuntman/token

# Mvc: im Browser http://localhost:5000/ öffnen und im Picker einen Benutzer wählen
```
