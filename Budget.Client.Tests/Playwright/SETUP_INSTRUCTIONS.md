# Playwright Setup Instructions

## Quick Start

### 1. Install Playwright Browsers

Before running Playwright tests for the first time, you need to install the browser binaries:

```powershell
# Navigate to the test project output directory
cd Budget.Client.Tests\bin\Debug\net10.0

# Install Playwright browsers (Chromium, Firefox, WebKit)
pwsh playwright.ps1 install

# Or install just Chromium (recommended for CI/CD)
pwsh playwright.ps1 install chromium
```

**Alternative method (if above doesn't work):**
```powershell
# Install globally using dotnet tool
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

### 2. Run the Tests

```powershell
# Run all Playwright tests
dotnet test Budget.Client.Tests --filter "FullyQualifiedName~Playwright"

# Run a specific test
dotnet test Budget.Client.Tests --filter "FullyQualifiedName~SamplePlaywrightTests.HomePage_Should_Load_Successfully"

# Run with verbose output
dotnet test Budget.Client.Tests --filter "FullyQualifiedName~Playwright" --logger "console;verbosity=detailed"
```

## What Was Set Up

### Files Created:
1. **MockAuthenticationHandler.cs** - Bypasses Entra ID authentication with test credentials
2. **TestWebApplicationFactory.cs** - Hosts the Blazor app in-memory for testing
3. **PlaywrightTestBase.cs** - Base class with browser setup/teardown
4. **SamplePlaywrightTests.cs** - Example tests showing common scenarios
5. **README.md** - Comprehensive documentation

### Changes Made:
1. **Budget.Client.Tests.csproj** - Added Playwright and AspNetCore.Mvc.Testing packages
2. **Budget.Web\Program.cs** - Added `ProgramMarker` class for test integration
3. **Budget.Web\Budget.Web.csproj** - Added InternalsVisibleTo for test project
4. **GlobalUsings.cs** - Added Playwright namespaces

## Mock Authentication

The tests use mock authentication with these default credentials:
- **Name**: Test User
- **Email**: testuser@example.com
- **User ID**: test-user-id-12345

This allows tests to run without Entra ID or any external authentication dependencies.

## Troubleshooting

### "Executable doesn't exist" Error
**Solution**: Run the Playwright installation command (step 1 above)

### Tests Timeout
**Solution**: Increase wait times or check if the app is starting correctly
```csharp
await Page.WaitForSelectorAsync(".my-element", new PageWaitForSelectorOptions 
{ 
    Timeout = 30000 // 30 seconds
});
```

### Browser Not Found
**Solution**: Ensure Playwright browsers are installed in the correct location
```powershell
# Verify installation
pwsh Budget.Client.Tests\bin\Debug\net10.0\playwright.ps1 install --help
```

## Next Steps

1. **Install browsers** (step 1 above)
2. **Run the sample tests** to verify everything works
3. **Write your own tests** using `SamplePlaywrightTests.cs` as a template
4. **Review README.md** in the Playwright folder for detailed guidance

## CI/CD Integration

For automated builds, add this step before running tests:

```yaml
# GitHub Actions
- name: Install Playwright
  run: pwsh Budget.Client.Tests/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps

# Azure DevOps
- script: pwsh Budget.Client.Tests/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps
  displayName: 'Install Playwright Browsers'
```

## Additional Resources

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [Playwright README](./README.md) - Detailed guide in this directory
- [MudBlazor Testing](https://mudblazor.com/) - Component-specific guidance
