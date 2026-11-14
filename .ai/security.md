# Security rules

Never relax these rules. Every change needs tests.

1. **Development only.** Active only in `Development` or in explicitly allowed additional environments. In all other environments `AddStandIn` and `MapStandIn` are silent no-ops.
2. **Never Production.** `additionalEnvironments` containing `Production` throws `ArgumentException`.
3. **ReturnUrl local only.** Local paths are allowed; `//host` and `/\host` are rejected. Foreign targets only through `AddAllowedReturnOrigin`. The referer fallback becomes a local path when the host is the same.
4. **Cookie:** HttpOnly, SameSite=Lax, contains only the user id.
5. **Tokens:** `/server` hands out tokens (like the original), so only with `EnableServer()` and only in Development. `/users` never hands out tokens.
6. New endpoints, parameters and redirects are treated like input from untrusted clients.
7. **Token endpoint:** only with `EnableTokenEndpoint()` and only in Development; JSON only (a foreign form cannot trigger it, CORS requires a preflight); log warning.
8. **CORS** only for explicitly allowed origins and only for `users` and `token`, never for `server`.
9. The **308 redirect** of the old root path never leaves the application.
10. `AddConfigurationFromServer`: https only, http only for localhost (the response contains tokens).
