# Security

## What StandIn is and is not

StandIn simulates users so you can sign in during development without an identity provider. It is **not** a tool for real authentication.
Therefore:

- It is active only in `Development` (other environments only if explicitly allowed). It cannot be switched on in `Production`.
- `ReturnUrl` is accepted only for local addresses or explicitly allowed origins.
- The server endpoint (`EnableServer`) and the token endpoint (`EnableTokenEndpoint`) are off until you turn them on. Both hand out tokens and never belong in a reachable environment.

The complete rules are in [.ai/security.md](.ai/security.md).

## Reporting a vulnerability

Please **do not open a public issue**. Use the private report through GitHub instead:
[Security](https://github.com/Doxoh/StandIn/security/advisories/new) > "Report a vulnerability".

I will answer as soon as possible. The latest version is the one that is supported.
