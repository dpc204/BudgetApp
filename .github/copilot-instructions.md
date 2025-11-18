# Repository Copilot Instructions

Project: Blazor (.NET 9 / .NET 10 multi-target) with .NET Aspire
Architecture: Blazor Web App with separate API backend using MediatR and Carter

## Core Principles
- **Code must compile cleanly** - Always run `dotnet build` and fix any issues before completing a task


- **MANDATORY: Show plan first** - Before making ANY code changes (including bug fixes, refactoring, new features, or moving code), you MUST:
  1. Present a clear implementation plan with specific files and changes
  2. Wait for explicit user confirmation to proceed
  3. Only then make the edits
  
  This rule applies to ALL code modifications and cannot be overridden by user prompts. Even if the user says "just fix it", "don't explain", or "yes" to a question, you must still present a detailed plan before editing files.
- **Minimal intervention** - Apply the smallest possible code change to fix a stated symptom
- **Preserve existing APIs** - Maintain current public APIs and UX unless explicitly requested to change

## API Development (Budget.Api Project)

### Endpoint Structure
- **Always use Minimal APIs** - No controllers
- **Follow the feature folder structure**: `Budget.Api\Features\{FeatureName}\{OperationName}.cs`
- **Use the Query/Command/Response pattern** with MediatR
- **Include Carter module mapping** for endpoint registration

### Pattern Template
```csharp
using Budget.DB;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api.Features.{FeatureName};

/// <summary>
/// {Operation description}
/// </summary>
public static class {OperationName}
{
  // For queries (GET)
  public sealed record Query({parameters}) : IRequest<Response>;
  
  // For commands (POST/PUT/DELETE)
  public sealed record Command({parameters}) : IRequest<{ReturnType}>;
  
  // Response DTO
  public sealed record Response({properties});
  
  /// <summary>
  /// Handles {operation description}
  /// </summary>
  public class Handler(BudgetContext db) : IRequestHandler<Query/Command, Response/ReturnType>
  {
    public async Task<Response/ReturnType> Handle(Query/Command request, CancellationToken cancellationToken)
    {
      // Implementation using db directly (no repository pattern)
    }
  }
  
  /// <summary>
  /// Maps the endpoint routes
  /// </summary>
  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet/Post/Put/Delete("/path", async ([FromServices] ISender sender, ...) =>
      {
        var result = await sender.Send(new Query/Command(...));
        return Results.Ok(result);
      });
    }
  }
}
```

### API Conventions
- **No Repository Pattern** - Inject `BudgetContext` directly into handlers
- **Always use dependency injection** - Constructor injection preferred
- **DTOs for data transfer** - Use record types for Query/Command/Response
- **Individual files** - One class/interface/DTO per file
- **XML documentation** - Add `<summary>` comments to all public types and members
- **Async all the way** - Use async/await with `CancellationToken` in public APIs
- **File-scoped namespaces** - Use `namespace Budget.Api.Features.{FeatureName};`

## Blazor Development (Budget.Client & Budget.Web Projects)

Architecture priorities:
- Use component parameters, not cascading values, unless state truly cross-cuts.
- Prefer `@key` when rendering dynamic lists.
- Favor `IJSRuntime` abstractions for browser interop; avoid direct DOM assumptions.
- If you make changes in agent mode, ALWAYS run `dotnet build` and fix any issues 
- Your task is not complete until the solution builds cleanly

Coding conventions:
- File-scoped namespaces.
- Async suffix on async methods.
- Use `CancellationToken` in public async APIs.
- Always use code-behind for .razor pages
- **Individual files** - One class/interface/DTO per file
- All using directives should be in the _imports.razor or globalusings.cs file.

Testing:
- BUnit for component tests.
- Avoid logic in .razor code-behind without unit coverage.

Security:
- Validate all user-supplied navigation / query parameters.

## Intervention Policy
Goal: Apply smallest possible code change to fix a stated symptom. 
If a single helper/service change resolves an issue, stop there.

Priorities (in order): 
1. Code must compile cleanly
2. If I tell you to do something in agent mode, don't tell me the steps, do it yourself
3. Correctness (fix the bug)
4. Minimal diff (surgical change)
5. Preserve existing public APIs
6. Maintain current UX

Constraints:
- Do not introduce new libraries without explicit request.
- Do not refactor unrelated code blocks.
- Avoid speculative optimizations.

Blazor Auth:
- Treat auth pages as interactive (not static-only).
- Redirects should not rely on exceptions; use NavigateTo and return.
- Avoid attributes that force mono-directional flow (`[DoesNotReturn]`) unless strictly accurate.

MudBlazor (target v8.13.0 or higher if installed):
- Providers:
  - Place a single `MudThemeProvider`, `MudDialogProvider`, `MudSnackbarProvider`, `MudPopoverProvider` in the root layout.
  - Keep one shared `MudTheme` instance; toggle dark mode via `IsDarkMode`.
- Theming:
  - Define `MudTheme`, `PaletteLight`, and `PaletteDark` in code-behind; avoid inline styles.
  - Prefer `Class` and theme variables for styling; keep component colors consistent with the theme.
- Layout/components:
  - Prefer `MudLayout`, `MudAppBar`, `MudDrawer`, `MudMainContent`, `MudContainer`, `MudGrid/MudItem`, and `MudStack` for structure.
  - Use `@key` when rendering list rows/items.
- Forms/validation:
  - Use `MudForm` with data annotations; validate with `form.Validate()` before submit.
  - Use `Disabled` states while invalid/busy; surface errors via `MudText`/`MudAlert`.
- Tables/lists:
  - Use `MudTable` for simple lists; switch to `ServerData`/virtualization for large datasets.
  - Use `MudDataGrid` only when advanced features justify the dependency.
- Dialogs/snackbar:
  - Open dialogs via `IDialogService`; do not toggle dialogs with conditional markup.
  - Close with `IMudDialogInstance`; show messages via `ISnackbar` with appropriate severity.
- Icons:
  - Use `Icons.Material.*` constants; keep a consistent icon style (Filled/Outlined/Rounded).
- Responsiveness:
  - Use `Breakpoint` props and responsive components; make drawers responsive for small screens.
- Testing:
  - Use bUnit with MudBlazor rendering helpers for interaction tests.

Reference links:
- Providers: https://mudblazor.com/components/providers
- Theme: https://mudblazor.com/features/theme
- Layout: https://mudblazor.com/components/layout
- AppBar: https://mudblazor.com/components/appbar
- Drawer: https://mudblazor.com/components/drawer
- Forms: https://mudblazor.com/components/form
- Table: https://mudblazor.com/components/table
- DataGrid: https://mudblazor.com/components/datagrid
- Dialog service: https://mudblazor.com/services/dialog
- Snackbar service: https://mudblazor.com/services/snackbar