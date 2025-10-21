# Repository Copilot Instructions

Project: Blazor (.NET 9 / .NET 10 multi-target)
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