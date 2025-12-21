// Window utility functions for responsive design
window.windowUtils = {
  getInnerWidth: function () {
    return window.innerWidth;
  },
  getInnerHeight: function () {
    return window.innerHeight;
  }
};

// Focus an element by ID
window.focusElementById = function (elementId) {
  const element = document.getElementById(elementId);
  if (element) {
    // For MudBlazor numeric fields, we need to focus the actual input inside
    const input = element.querySelector('input');
    if (input) {
      input.focus();
      input.select();
    } else {
      element.focus();
    }
  }
};

// Validation error flag - set by C# when validation fails
window._validationError = false;
window.setValidationError = function(hasError) {
  window._validationError = hasError;
};

// Initialize draft field navigation for Tab and Enter keys
window.initializeDraftFieldNavigation = function () {
  // Prevent duplicate event listeners
  if (window._draftNavigationInitialized) {
    return;
  }
  window._draftNavigationInitialized = true;

  // Use event delegation on the document to handle keydown on draft fields
  document.addEventListener('keydown', function (event) {
    // Check if the event target is an input field
    const target = event.target;
    if (!target || target.tagName !== 'INPUT') return;

    // Check if this input has the draft-input-right class (our draft fields)
    const draftField = target.closest('.draft-input-right');
    if (!draftField) return;

    // Only handle Tab and Enter keys
    if (event.key !== 'Tab' && event.key !== 'Enter') return;

    // Prevent default behavior - we'll handle navigation manually
    event.preventDefault();

    // Find the table cell (td) containing this input
    const currentCell = target.closest('td');
    if (!currentCell) return;

    // Find the table row containing this cell
    const currentRow = currentCell.closest('tr');
    if (!currentRow) return;

    // Get the cell index within the row
    const cellIndex = Array.from(currentRow.cells).indexOf(currentCell);

    // Find all rows in the table
    const table = currentRow.closest('table');
    const allRows = Array.from(table.querySelectorAll('tbody tr'));
    const currentRowIndex = allRows.indexOf(currentRow);

    // Store reference to the current input before blurring
    const currentInput = target;
    
    // Blur the current input to trigger validation
    target.blur();
    
    // Wait for validation to complete, then check if there was an error
    // 200ms delay gives MudBlazor time to re-validate and clear old errors
    setTimeout(function() {
      // Check for validation errors after blur
      const tableCellAfterBlur = currentInput.closest('td');
      if (tableCellAfterBlur) {
        // Check aria-invalid attribute
        const ariaInvalidAfterBlur = currentInput.getAttribute('aria-invalid');
        
        // Check for error elements in cell after blur
        const errorElementsAfterBlur = tableCellAfterBlur.querySelectorAll('.mud-error, .mud-input-error, .validation-message, .mud-error-text');
        
        // Check for error text
        const hasErrorTextAfterBlur = Array.from(tableCellAfterBlur.querySelectorAll('*')).some(el => {
          const text = el.textContent.trim().toLowerCase();
          return text.includes('not a valid') || text.includes('invalid') || text.includes('error');
        });
        
        if (ariaInvalidAfterBlur === 'true' || errorElementsAfterBlur.length > 0 || hasErrorTextAfterBlur) {
          // Validation error found - refocus the field
          currentInput.focus();
          currentInput.select();
          return;
        }
      }
      
      // Check server validation error flag
      if (window._validationError) {
        // Server validation error - stay in current field
        currentInput.focus();
        currentInput.select();
        window._validationError = false; // Reset the flag
        return;
      }
      
      // No validation error, proceed with navigation
      if (currentRowIndex === allRows.length - 1) {
        // We're at the last row - keep focus in current field
        currentInput.focus();
        currentInput.select();
      } else if (currentRowIndex >= 0 && currentRowIndex < allRows.length - 1) {
        // Move to the next row
        const nextRow = allRows[currentRowIndex + 1];
        
        // Get the same cell index in the next row
        const nextCell = nextRow.cells[cellIndex];
        if (nextCell) {
          // Find the input in that cell
          const nextInput = nextCell.querySelector('.draft-input-right input') || 
                           nextCell.querySelector('input');
          
          if (nextInput) {
            nextInput.focus();
            nextInput.select();
          }
        }
      }
    }, 200); // Wait 200ms for validation to complete and error state to update
  });
};
