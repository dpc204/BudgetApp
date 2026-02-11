# ?? PLAYWRIGHT TESTING - READY TO USE!

## ? Everything Is Fixed and Working

All issues have been resolved:
- ? Playwright installed and configured
- ? Mock authentication implemented
- ? Dependency injection issues fixed
- ? Sample tests ready
- ? Documentation complete

## Quick Start (2 Steps)

### 1. Start Budget.Web in Test Mode

**Recommended (stops, rebuilds, starts):**
```powershell
.\Restart-TestMode.ps1
```

**Quick start (if already built):**
```powershell
.\Start-TestMode.ps1
```

**You should see:**
```
?? TEST MODE: Using mock authentication instead of Entra ID
? Test mode authentication configured with controllers
Now listening on: http://localhost:5146
Application started
```

### 2. Run Playwright Tests (New Terminal)

```powershell
dotnet test Budget.Client.Tests --filter "FullyQualifiedName~Playwright"
```

**That's it!** Tests will run without authentication prompts. ?

## What's Working

? **Authentication Mock** - No "Sign in with Microsoft" page  
? **Browser Automation** - Chromium launches and navigates  
? **6 Sample Tests** - Demonstrating various scenarios  
? **Zero Dependencies** - No Entra ID, no Azure, just works  
? **Easy Debugging** - Set `Headless = false` to watch tests  

## Test Examples

### Current Tests (4 active, 2 examples)

| Test | Status | Description |
|------|--------|-------------|
| HomePage_Should_Load_Successfully | ? Active | Verifies home page loads |
| Navigation_Should_Work_With_Authentication | ? Active | Tests navigation |
| MudBlazor_Components_Should_Render | ? Active | Checks MudBlazor |
| Authenticated_User_Information_Should_Be_Available | ? Active | Validates no auth prompt |
| Sample_Button_Click_Interaction | ?? Skipped | Example for buttons |
| Sample_Form_Input_Interaction | ?? Skipped | Example for forms |

## Writing Your Own Tests

### Basic Test Structure

```csharp
[Fact]
public async Task Fund_Page_Should_Display_Envelopes()
{
  // Arrange
  await NavigateToAsync("/fund");
  
  // Act
  await Page.WaitForSelectorAsync(".mud-table");
  
  // Assert
  var table = await Page.QuerySelectorAsync(".mud-table");
  table.Should().NotBeNull();
}
```

### Common Patterns

```csharp
// Click a button
await Page.ClickAsync("button:has-text('Fund Envelopes')");

// Fill input
await Page.FillAsync("input[name='amount']", "100");

// Wait for element
await Page.WaitForSelectorAsync(".success-message");

// Get text
var text = await Page.TextContentAsync("h1");

// Take screenshot
await TakeScreenshotAsync("test-result");

// Check URL
Page.Url.Should().Contain("/fund");
```

## Configuration

### Adjust Browser Behavior

Edit `PlaywrightTestBase.cs` line 33:

```csharp
Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
  Headless = false,  // See the browser
  SlowMo = 100      // Slow down for visibility
});
```

### Change Test URL

Edit `PlaywrightTestBase.cs` line 23:

```csharp
BaseUrl = "https://localhost:7141"; // Your app's URL
```

## Debugging

### See What's Happening

```csharp
// PlaywrightTestBase.cs
Headless = false,  // Watch the browser
SlowMo = 500,      // Slow down significantly
```

### Take Screenshots

```csharp
await NavigateToAsync("/fund");
await TakeScreenshotAsync("before-action");
await Page.ClickAsync("button");
await TakeScreenshotAsync("after-action");
```

Screenshots saved to: `Budget.Client.Tests/screenshots/`

### Run Single Test

```powershell
dotnet test --filter "HomePage_Should_Load_Successfully"
```

## Troubleshooting

### Issue: "Sign in with Microsoft" appears

**Solution:** Make sure you started Budget.Web with `USE_TEST_AUTH=true`

```powershell
Stop-Process -Name "Budget.Web" -Force -ErrorAction SilentlyContinue
.\Start-TestMode.ps1
```

### Issue: "Executable doesn't exist"

**Solution:** Install Playwright browsers

```powershell
cd Budget.Client.Tests\bin\Debug\net10.0
pwsh playwright.ps1 install chromium
```

### Issue: Tests timeout

**Solution:** Increase timeouts in tests

```csharp
await Page.WaitForSelectorAsync(".element", new PageWaitForSelectorOptions 
{ 
  Timeout = 30000 // 30 seconds
});
```

### Issue: Connection refused

**Solution:** Verify Budget.Web is running

```powershell
# Check if running
Get-Process | Where-Object {$_.ProcessName -like "*Budget.Web*"}

# If not, start it:
.\Start-TestMode.ps1
```

## Test User

When in test mode, you're automatically logged in as:

- **Name:** Test User
- **Email:** testuser@example.com  
- **User ID:** test-user-id-12345

No login required, no authentication prompts!

## Normal Operation

To run the app normally (with real Entra ID):

```powershell
# Don't set USE_TEST_AUTH
cd Budget.Web
dotnet run

# Or use AppHost:
cd Budget.AppHost
dotnet run
```

## Documentation

- **AUTHENTICATION_FIXED.md** - Technical details of the fix
- **README.md** - Comprehensive Playwright guide
- **SETUP_INSTRUCTIONS.md** - Installation instructions
- **QUICK_START.md** - Fast start guide

## CI/CD Integration

For automated builds:

```yaml
# GitHub Actions
- name: Start app in test mode
  run: |
    $env:USE_TEST_AUTH = "true"
    Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run --project Budget.Web"
    Start-Sleep -Seconds 10

- name: Run Playwright tests
  run: dotnet test Budget.Client.Tests --filter "FullyQualifiedName~Playwright"
```

## Next Steps

1. ? Run `.\Start-TestMode.ps1` to start the app
2. ? Run `dotnet test --filter Playwright` to run tests
3. ? Watch them pass!
4. ? Write your own tests for Fund, Budget, and Envelope pages
5. ? Add to CI/CD pipeline

## Summary

| Component | Status |
|-----------|--------|
| Playwright Installed | ? |
| Browsers Downloaded | ? |
| Mock Authentication | ? |
| DI Issues Fixed | ? |
| Sample Tests | ? |
| Documentation | ? |
| Build Passing | ? |
| Tests Executable | ? |

## Status: ?? COMPLETE AND READY!

Everything is set up and working. Start writing tests for your app! 

**No more issues, no more blockers - just test!** ?
