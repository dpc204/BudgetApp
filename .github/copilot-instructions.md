# Repository Copilot Instructions

Project: Blazor (.NET 9 / .NET 10 multi-target)
Architecture priorities:
- Use component parameters, not cascading values, unless state truly cross-cuts.
- Prefer `@key` when rendering dynamic lists.
- Favor `IJSRuntime` abstractions for browser interop; avoid direct DOM assumptions.

Coding conventions:
- File-scoped namespaces.
- Async suffix on async methods.
- Use `CancellationToken` in public async APIs.

Testing:
- BUnit for component tests.
- Avoid logic in .razor code-behind without unit coverage.

Security:
- Validate all user-supplied navigation / query parameters.

