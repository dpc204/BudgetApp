# Repository Copilot Instructions

Project: Blazor (.NET 9 / .NET 10 multi-target)
Architecture priorities:
- Use component parameters, not cascading values, unless state truly cross-cuts.
- Prefer `@key` when rendering dynamic lists.
- Favor `IJSRuntime` abstractions for browser interop; avoid direct DOM assumptions.
- Your task is not complete until the solution builds cleanly

Coding conventions:
- File-scoped namespaces.
- Async suffix on async methods.
- Use `CancellationToken` in public async APIs.
- Always use code-behind for .razor pages
- All using directives should be in the _imports.razor file.

Testing:
- BUnit for component tests.
- Avoid logic in .razor code-behind without unit coverage.

Security:
- Validate all user-supplied navigation / query parameters.

## Intervention Policy
Goal: Apply smallest possible code change to fix a stated symptom. 
If a single helper/service change resolves an issue, stop there.

Priorities (in order): 
1. Correctness (fix the bug)
2. Minimal diff (surgical change)
3. Preserve existing public APIs
4. Maintain current UX

Constraints:
- Do not introduce new libraries without explicit request.
- Do not refactor unrelated code blocks.
- Avoid speculative optimizations.

Blazor Auth:
- Treat auth pages as interactive (not static-only).
- Redirects should not rely on exceptions; use NavigateTo and return.
- Avoid attributes that force mono-directional flow (`[DoesNotReturn]`) unless strictly accurate.

