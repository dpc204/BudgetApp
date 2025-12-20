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

    console.log('[DEBUG] Tab/Enter in draft field!');
    
    // Check if there was a validation error - if so, don't navigate
    if (window._validationError) {
      console.log('[DEBUG] Validation error detected, staying in current field');
      event.preventDefault();
      window._validationError = false; // Reset the flag
      return;
    }
    
    // Prevent default behavior
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

    // Move to the next row
    if (currentRowIndex >= 0 && currentRowIndex < allRows.length - 1) {
      const nextRow = allRows[currentRowIndex + 1];
      
      // Get the same cell index in the next row
      const nextCell = nextRow.cells[cellIndex];
      if (nextCell) {
        // Find the input in that cell
        const nextInput = nextCell.querySelector('.draft-input-right input') || 
                         nextCell.querySelector('input');
        
        if (nextInput) {
          console.log('[DEBUG] Moving to next row input');
          nextInput.focus();
          nextInput.select();
        } else {
          console.log('[DEBUG] No input found in next row cell');
        }
      }
    } else {
      console.log('[DEBUG] Already at last row');
    }
  });
  
  console.log('[DEBUG] Event listener added');
};
          console.log('[DEBUG] Moving to next row, same column');
          nextInput.focus();
          nextInput.select();
          console.log('[DEBUG] Successfully focused next input');
        } else {
          console.log('[DEBUG] No input found in next cell');
        }
      } else {
        console.log('[DEBUG] Next row does not have cell at index', cellIndex);
      }
    } else {
      console.log('[DEBUG] Already at last row');
    }
  });
  
  console.log('[DEBUG] Event listener added');
};
