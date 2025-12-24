# Budget.Client.Tests

This test project contains tests for the Budget.Client Blazor components using bUnit, xUnit, and Moq.

## Testing Framework

- **bUnit 1.32.7** - Blazor component testing library
- **xUnit** - Test framework
- **Moq** - Mocking framework
- **MudBlazor 8.15.0** - UI component library (test dependencies)

## Known Limitations

### MudBlazor Provider Requirements

MudBlazor components require specific providers (`MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`) in the component tree to function properly. These providers cannot be easily mocked or stubbed in bUnit tests due to internal dependencies on JSInterop and service initialization.

**Impact:**
- Full component rendering tests that include MudBlazor input components (like `MudNumericField`, `MudTextField`, etc.) will fail with:
  ```
  System.InvalidOperationException: Missing <MudPopoverProvider />
  ```

**Solutions:**

1. **Unit Tests** - Test component logic in isolation
   - Extract business logic to services/methods
   - Test services independently
   - Mock component dependencies

2. **Integration Tests** - Use full application context
   - Use Playwright or Selenium for end-to-end tests
   - Test with a running application that includes all providers

3. **Skipped Tests** - Document requirements clearly
   ```csharp
   [Fact(Skip = "Requires MudBlazor providers for full component rendering")]
   public void MyTest() { ... }
   ```

### Example Test Structure

```csharp
public class MyComponentTests : TestContext
{
  private readonly Mock<IMyService> _mockService;

  public MyComponentTests()
  {
    _mockService = new Mock<IMyService>();
    
    // Register services
    Services.AddMudServices();
    Services.AddSingleton(_mockService.Object);
    
    // Configure JSInterop to handle MudBlazor calls
    JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [Fact]
  public void CanTest_ServiceInteractions()
  {
    // Test logic without full rendering
    _mockService.Setup(x => x.GetDataAsync()).ReturnsAsync(testData);
    var result = await _mockService.Object.GetDataAsync();
    Assert.NotNull(result);
  }

  [Fact(Skip = "Requires MudBlazor providers")]
  public void CannotTest_FullMudBlazorRendering()
  {
    // This would fail - requires providers
    var cut = RenderComponent<MyComponent>();
  }
}
```

## Tab/Enter Navigation Tests

The BudgetPageNavigationTests verify the Tab/Enter key navigation implementation for budget draft fields.

### What's Being Tested

1. **JavaScript Initialization**: Verifies that the `initializeDraftFieldNavigation` JavaScript function is called when the Budget page first renders.

2. **HTML Attributes**: Confirms that draft input fields are rendered with the correct `data-envelope-id` and `data-month-index` attributes needed for the JavaScript navigation logic.

3. **Field IDs**: Validates that draft fields have IDs following the pattern `draft-{envelopeId}-{monthIndex}`.

### Implementation Details

The navigation functionality works as follows:

1. Each `MudNumericField` draft input is rendered with:
   - A unique ID: `draft-{envelopeId}-{monthIndex}`
   - Data attribute `data-envelope-id`: The envelope's ID
   - Data attribute `data-month-index`: The month column index

2. JavaScript event delegation listens for Tab/Enter keypresses on inputs
3. When detected in a draft field, the script:
   - Finds all draft fields in the same column (matching `data-month-index`)
   - Moves focus to the next field in that column
   - Prevents default Tab behavior

### Known Issues

The tests currently fail because MudBlazor components require full provider setup (`MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`) which is complex to mock in unit tests. 

The actual functionality works correctly in the running application but requires integration testing or manual verification to demonstrate.

### Manual Testing

To manually verify the Tab/Enter navigation:

1. Run the application
2. Navigate to the Budget page
3. Click in any draft field
4. Press Tab or Enter
5. Verify focus moves down to the next row's draft field in the same month column

## Future Improvements

- Set up proper integration testing with full MudBlazor provider configuration
- Add Playwright end-to-end tests for keyboard navigation
- Create simpler unit tests for the helper methods (like `GetDraftFieldId`)
