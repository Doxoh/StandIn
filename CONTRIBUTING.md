# Contributing

Thanks for your interest. In short:

1. For larger changes, open an issue first and agree on the plan.
2. Fork, create a branch, keep the change small and focused.
3. Build and test: `dotnet test` (target frameworks `net8.0` and `net10.0`, warnings are errors).
4. Open a pull request against `main`. It is ready when CI is green.

Notes on structure, decisions and conventions are in [.ai/README.md](.ai/README.md).
The basic rule: as simple as possible. No abstraction without a benefit today.

Security-relevant changes (environment check, ReturnUrl, tokens) need a test that proves the behavior.
