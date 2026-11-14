# Sicherheit

## Was StandIn ist und nicht ist

StandIn simuliert Benutzer, damit man sich in der Entwicklung ohne Identity Provider anmelden kann. Es ist **kein** Werkzeug für echte Authentifizierung.
Deshalb gilt:

- Es ist nur in `Development` aktiv (weitere Umgebungen nur ausdrücklich freigegeben). In `Production` lässt es sich nicht einschalten.
- `ReturnUrl` wird nur für lokale Adressen oder ausdrücklich freigegebene Origins akzeptiert.
- Der Server-Endpunkt (`EnableServer`) und der Token-Endpunkt (`EnableTokenEndpoint`) sind aus, bis du sie einschaltest. Beide geben Tokens heraus und gehören nie in eine erreichbare Umgebung.

Die vollständigen Regeln stehen in [.ai/security.md](.ai/security.md).

## Sicherheitslücke melden

Bitte **kein öffentliches Issue** anlegen. Nutze stattdessen die private Meldung über GitHub:
[Security](https://github.com/Doxoh/StandIn/security/advisories/new) > „Report a vulnerability“.

Ich antworte so schnell wie möglich. Unterstützt wird jeweils die neueste Version.
