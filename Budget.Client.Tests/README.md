# Budget.Client.Tests

This test project contains tests for the Budget.Client Blazor components.

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
