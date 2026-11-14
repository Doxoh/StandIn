# Sample apps

Both run with `dotnet run` in the `Development` environment (that is the default when started from the project folder; otherwise set `ASPNETCORE_ENVIRONMENT=Development`). In CI they serve as smoke tests (`tests/StandIn.Samples.Tests`).

| App | Shows |
| --- | --- |
| `MinimalApi` | REST API: bearer tokens (`dev-admin-token`, `dev-viewer-token`), `POST /stuntman/token` |
| `Mvc` | Cookie sign-in, user picker with `@Html.StandInUserPicker()`, protected page |

```
# MinimalApi
curl -H "Authorization: Bearer dev-admin-token" http://localhost:5000/me
curl -X POST -H "Content-Type: application/json" -d '{"userId":"viewer"}' http://localhost:5000/stuntman/token

# Mvc: open http://localhost:5000/ in the browser and pick a user in the picker
```
