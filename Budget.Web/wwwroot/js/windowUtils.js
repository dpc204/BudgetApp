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

// Initialize draft field navigation for Tab and Enter keys
window.initializeDraftFieldNavigation = function () {
  console.log('[DEBUG] Initializing draft field navigation...');
  
  // Prevent duplicate event listeners
  if (window._draftNavigationInitialized) {
    console.log('[DEBUG] Already initialized, skipping...');
    return;
  }
  window._draftNavigationInitialized = true;

  // Log all elements with draft- prefix in their id
  setTimeout(() => {
    const draftElements = document.querySelectorAll('[id^="draft-"]');
    console.log('[DEBUG] Found elements with draft- prefix:', draftElements.length);
    if (draftElements.length > 0) {
      const firstElement = draftElements[0];
      console.log('[DEBUG] Sample element structure:', firstElement.tagName, firstElement.outerHTML.substring(0, 300));
    }
  }, 2000);

  // Use event delegation on the document to handle keydown on draft fields
  document.addEventListener('keydown', function (event) {
    console.log('[DEBUG] Keydown:', event.key, 'Target:', event.target.tagName, event.target);
    
    // Check if the event target is within a draft input field
    const target = event.target;
    if (!target || target.tagName !== 'INPUT') return;

    // Check if this is a draft field by looking at the parent element's id
    const parentField = target.closest('[id^="draft-"]');
    console.log('[DEBUG] Parent field found:', parentField ? parentField.id : 'none');
    if (!parentField) return;

    // Only handle Tab and Enter keys
    if (event.key !== 'Tab' && event.key !== 'Enter') return;

    console.log('[DEBUG] Tab/Enter in draft field!');
    
    // Prevent default behavior
    event.preventDefault();

    // Get the envelope ID and month index from data attributes
    const envelopeId = parentField.getAttribute('data-envelope-id');
    const monthIndex = parentField.getAttribute('data-month-index');

    console.log('[DEBUG] EnvelopeId:', envelopeId, 'MonthIndex:', monthIndex);
    
    // getAttribute returns null if attribute doesn't exist, so we check against null
    if (!envelopeId || monthIndex === null) {
      console.log('[DEBUG] Missing attributes, returning');
      return;
    }

    // Find all draft fields with the same month index (same column)
    const allDraftsInColumn = Array.from(document.querySelectorAll(`[data-month-index="${monthIndex}"]`));
    console.log('[DEBUG] All drafts in column:', allDraftsInColumn.length);
    
    // Find the current field's index
    const currentIndex = allDraftsInColumn.findIndex(el => el === parentField);
    console.log('[DEBUG] Current index:', currentIndex);
    
    if (currentIndex >= 0 && currentIndex < allDraftsInColumn.length - 1) {
      // Move to the next field
      const nextField = allDraftsInColumn[currentIndex + 1];
      
      // MudBlazor nests the input deep inside, so we need to search more thoroughly
      // Try multiple selectors to find the input
      let nextInput = nextField.querySelector('input[type="text"]') || 
                      nextField.querySelector('input.mud-input-input-control') ||
                      nextField.querySelector('input');
      
      console.log('[DEBUG] Moving to next field:', nextField.id, 'Input found:', !!nextInput);
      
      if (nextInput) {
        nextInput.focus();
        nextInput.select();
        console.log('[DEBUG] Successfully focused next input');
      } else {
        console.log('[DEBUG] Could not find input in next field, HTML:', nextField.innerHTML.substring(0, 200));
      }
    } else {
      console.log('[DEBUG] No next field available');
    }
  });
  
  console.log('[DEBUG] Event listener added');
};
