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
  console.log('[DEBUG] Initializing draft field navigation...');
  
  // Prevent duplicate event listeners
  if (window._draftNavigationInitialized) {
    console.log('[DEBUG] Already initialized, skipping...');
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

    console.log('[DEBUG] ====== Tab/Enter pressed in draft field ======');
    console.log('[DEBUG] Input value:', target.value);
    console.log('[DEBUG] Input classList:', target.classList.toString());
    
    // Prevent default behavior - we'll handle navigation manually
    event.preventDefault();

    // Find the table cell (td) containing this input
    const currentCell = target.closest('td');
    if (!currentCell) {
      console.log('[DEBUG] Could not find table cell');
      return;
    }

    // Find the table row containing this cell
    const currentRow = currentCell.closest('tr');
    if (!currentRow) {
      console.log('[DEBUG] Could not find table row');
      return;
    }

    // Get the cell index within the row
    const cellIndex = Array.from(currentRow.cells).indexOf(currentCell);
    console.log('[DEBUG] Current cell index:', cellIndex);

    // Find all rows in the table
    const table = currentRow.closest('table');
    const allRows = Array.from(table.querySelectorAll('tbody tr'));
    const currentRowIndex = allRows.indexOf(currentRow);
    console.log('[DEBUG] Current row:', currentRowIndex, 'of', allRows.length);

    console.log('[DEBUG] Blurring current input to trigger validation...');
    
    // Blur the current input to trigger validation
    target.blur();
    
    // Wait for validation to complete, then check if there was an error
    // Increased delay to 200ms to give MudBlazor time to re-validate and clear old errors
    setTimeout(function() {
      console.log('[DEBUG] ====== After 200ms delay ======');
      
      // Check again for validation errors after blur (in case they appear after blur)
      const tableCellAfterBlur = target.closest('td');
      if (tableCellAfterBlur) {
        console.log('[DEBUG] Checking for errors after blur...');
        console.log('[DEBUG] Table cell classes after blur:', tableCellAfterBlur.classList.toString());
        
        // Check aria-invalid again
        const ariaInvalidAfterBlur = target.getAttribute('aria-invalid');
        console.log('[DEBUG] aria-invalid after blur:', ariaInvalidAfterBlur);
        
        // Check for error elements in cell after blur
        const errorElementsAfterBlur = tableCellAfterBlur.querySelectorAll('.mud-error, .mud-input-error, .validation-message, .mud-error-text');
        console.log('[DEBUG] Error elements after blur:', errorElementsAfterBlur.length);
        
        if (errorElementsAfterBlur.length > 0) {
          errorElementsAfterBlur.forEach((el, idx) => {
            console.log(`[DEBUG] Error element ${idx} after blur:`, el.tagName, el.textContent.trim());
          });
        }
        
        // Check for error text again
        const hasErrorTextAfterBlur = Array.from(tableCellAfterBlur.querySelectorAll('*')).some(el => {
          const text = el.textContent.trim().toLowerCase();
          return text.includes('not a valid') || text.includes('invalid') || text.includes('error');
        });
        console.log('[DEBUG] Has error text after blur:', hasErrorTextAfterBlur);
        
        if (ariaInvalidAfterBlur === 'true' || errorElementsAfterBlur.length > 0 || hasErrorTextAfterBlur) {
          console.log('[DEBUG] *** Validation error APPEARED after blur, REFOCUSING field ***');
          target.focus();
          target.select();
          return;
        } else {
          console.log('[DEBUG] No error found after blur');
        }
      }
      
      console.log('[DEBUG] Checking server validation error flag:', window._validationError);
      if (window._validationError) {
        console.log('[DEBUG] *** Server validation error detected, STAYING in current field ***');
        // Focus back to the field with error
        target.focus();
        target.select();
        window._validationError = false; // Reset the flag
        return;
      }
      
      console.log('[DEBUG] No validation errors detected, proceeding with navigation');
      
      // No validation error, move to the next row
      if (currentRowIndex >= 0 && currentRowIndex < allRows.length - 1) {
        const nextRow = allRows[currentRowIndex + 1];
        
        // Get the same cell index in the next row
        const nextCell = nextRow.cells[cellIndex];
        if (nextCell) {
          // Find the input in that cell
          const nextInput = nextCell.querySelector('.draft-input-right input') || 
                           nextCell.querySelector('input');
          
          if (nextInput) {
            console.log('[DEBUG] *** MOVING to next row input ***');
            console.log('[DEBUG] Next input value:', nextInput.value);
            nextInput.focus();
            nextInput.select();
          } else {
            console.log('[DEBUG] ERROR: No input found in next row cell');
          }
        } else {
          console.log('[DEBUG] ERROR: No cell found at index', cellIndex, 'in next row');
        }
      } else {
        console.log('[DEBUG] Already at last row (', currentRowIndex, 'of', allRows.length, ')');
      }
    }, 200); // Wait 200ms for validation to complete and error state to update
  });
  
  console.log('[DEBUG] Event listener added');
};
