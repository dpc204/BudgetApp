# Playwright Testing Setup

This directory contains Playwright end-to-end tests for the Budget Blazor application.

## Prerequisites

Before running Playwright tests, you need to install the Playwright browsers:

```powershell
# Navigate to the test project directory
cd Budget.Client.Tests

# Restore packages
dotnet restore

# Install Playwright browsers
pwsh bin/Debug/net10.0/playwright.ps1 install

# Or install specific browser
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

## Project Structure

```
Budget.Client.Tests/Playwright/
??? MockAuthenticationHandler.cs      # Mock authentication to bypass Entra ID
??? TestWebApplicationFactory.cs      # Custom factory for hosting the app in tests
??? PlaywrightTestBase.cs            # Base class with setup/teardown logic
??? SamplePlaywrightTests.cs         # Example tests demonstrating various scenarios
```

## Running Tests

### Run all Playwright tests
```bash
dotnet test --filter "FullyQualifiedName~Playwright"
```

### Run specific test
```bash
dotnet test --filter "FullyQualifiedName~SamplePlaywrightTests.HomePage_Should_Load_Successfully"
```

### Run tests with verbose output
```bash
dotnet test --filter "FullyQualifiedName~Playwright" --logger "console;verbosity=detailed"
```

## Authentication

The tests use **MockAuthenticationHandler** to bypass Entra ID authentication. The mock handler provides:

- **Name**: Test User
- **Email**: testuser@example.com
- **User ID**: test-user-id-12345

This allows tests to run without requiring actual Entra ID credentials or network access.

## Writing Tests

### Basic Test Structure

```csharp
public class MyPlaywrightTests : PlaywrightTestBase
{
  [Fact]
  public async Task MyTest_Should_DoSomething()
  {
    // Arrange
    await NavigateToAsync("/my-page");

    // Act
    await Page.ClickAsync("button:has-text('Submit')");

    // Assert
    var result = await Page.TextContentAsync(".result");
    result.Should().Be("Success");
  }
}
```

### Common Playwright Operations

```csharp
// Navigation
await NavigateToAsync("/fund");

// Click elements
await Page.ClickAsync("button");
await Page.ClickAsync("button:has-text('Fund Envelopes')");

// Fill inputs
await Page.FillAsync("input[name='amount']", "100");

// Wait for elements
await Page.WaitForSelectorAsync(".mud-table");

// Get text content
var text = await Page.TextContentAsync("h1");

// Take screenshot (for debugging)
await TakeScreenshotAsync("test-screenshot");

// Check element existence
var element = await Page.QuerySelectorAsync(".my-element");
element.Should().NotBeNull();
```

## Debugging Tests

### Run tests in headed mode (visible browser)
Modify `PlaywrightTestBase.cs`:
```csharp
Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
  Headless = false,  // Set to false
  SlowMo = 100       // Slow down actions (milliseconds)
});
```

### Take screenshots during test execution
```csharp
await TakeScreenshotAsync("before-action");
await Page.ClickAsync("button");
await TakeScreenshotAsync("after-action");
```

Screenshots are saved to `Budget.Client.Tests/screenshots/`

### Use browser developer tools
Set `Devtools = true` in launch options:
```csharp
Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
  Headless = false,
  Devtools = true
});
```

## MudBlazor Components

When testing MudBlazor components, use appropriate selectors:

```csharp
// MudButton
await Page.ClickAsync(".mud-button");

// MudTextField
await Page.FillAsync(".mud-input-slot input", "value");

// MudTable
await Page.WaitForSelectorAsync(".mud-table");

// MudDialog
await Page.WaitForSelectorAsync(".mud-dialog");

// MudSnackbar
var snackbar = await Page.QuerySelectorAsync(".mud-snackbar");
```

## CI/CD Integration

For CI/CD pipelines, ensure Playwright browsers are installed:

```yaml
# GitHub Actions example
- name: Install Playwright
  run: pwsh Budget.Client.Tests/bin/Debug/net10.0/playwright.ps1 install --with-deps

# Azure DevOps example
- script: pwsh Budget.Client.Tests/bin/Debug/net10.0/playwright.ps1 install --with-deps
  displayName: 'Install Playwright'
```

## Troubleshooting

### Issue: Browsers not installed
**Error**: `Executable doesn't exist at ...`
**Solution**: Run `pwsh bin/Debug/net10.0/playwright.ps1 install`

### Issue: Test times out
**Solution**: Increase timeout or wait for specific selectors
```csharp
await Page.WaitForSelectorAsync(".my-element", new PageWaitForSelectorOptions
{
  Timeout = 10000 // 10 seconds
});
```

### Issue: Element not found
**Solution**: Ensure the page is fully loaded
```csharp
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

### Issue: Authentication redirects
**Solution**: Verify `TestWebApplicationFactory` is properly configured with mock authentication

## Best Practices

1. **Use semantic selectors**: Prefer text-based or role-based selectors over CSS classes
2. **Wait for elements**: Always wait for elements before interacting
3. **Isolate tests**: Each test should be independent
4. **Clean up**: The base class handles cleanup automatically
5. **Use FluentAssertions**: Make assertions readable and clear
6. **Mock external dependencies**: Use mock authentication to avoid external service dependencies

## Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [Playwright Selectors](https://playwright.dev/dotnet/docs/selectors)
- [MudBlazor Components](https://mudblazor.com/components/appbar)
