# Sicherheitsregeln

Diese Regeln nie aufweichen. Jede Änderung braucht Tests.

1. **Nur Development.** Aktiv nur in `Development` oder in ausdrücklich freigegebenen Zusatzumgebungen. In allen anderen Umgebungen sind `AddStandIn` und `MapStandIn` stille No-ops.
2. **Production nie.** `additionalEnvironments` mit `Production` wirft `ArgumentException`.
3. **ReturnUrl nur lokal.** Erlaubt sind lokale Pfade; `//host` und `/\host` sind abgelehnt. Fremde Ziele nur über `AddAllowedReturnOrigin`. Referer-Fallback wird bei gleichem Host zu einem lokalen Pfad.
4. **Cookie:** HttpOnly, SameSite=Lax, enthält nur die Benutzer-Id.
5. **Tokens:** `/server` liefert Tokens aus (wie das Original), deshalb nur mit `EnableServer()` und nur in Development. `/users` gibt nie Tokens aus.
6. Neue Endpunkte, Parameter und Rückleitungen werden wie Eingaben nicht vertrauenswürdiger Clients behandelt.
