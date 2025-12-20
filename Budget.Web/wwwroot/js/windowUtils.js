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
  console.log('Initializing draft field navigation...');
  
  // Prevent duplicate event listeners
  if (window._draftNavigationInitialized) {
    console.log('Already initialized, skipping...');
    return;
  }
  window._draftNavigationInitialized = true;

  // Use event delegation on the document to handle keydown on draft fields
  document.addEventListener('keydown', function (event) {
    console.log('Keydown event:', event.key, 'on', event.target);
    
    // Check if the event target is within a draft input field
    const target = event.target;
    if (!target || target.tagName !== 'INPUT') return;

    // Check if this is a draft field by looking at the parent element's id
    const parentField = target.closest('[id^="draft-"]');
    console.log('Parent field found:', parentField);
    if (!parentField) return;

    // Only handle Tab and Enter keys
    if (event.key !== 'Tab' && event.key !== 'Enter') return;

    console.log('Tab or Enter detected in draft field');
    
    // Prevent default behavior
    event.preventDefault();

    // Get the envelope ID and month index from data attributes
    const envelopeId = parentField.getAttribute('data-envelope-id');
    const monthIndex = parentField.getAttribute('data-month-index');

    console.log('Envelope ID:', envelopeId, 'Month Index:', monthIndex);
    
    // getAttribute returns null if attribute doesn't exist, so we check against null
    if (!envelopeId || monthIndex === null) return;

    // Find all draft fields with the same month index (same column)
    const allDraftsInColumn = Array.from(document.querySelectorAll(`[data-month-index="${monthIndex}"]`));
    console.log('All drafts in column:', allDraftsInColumn.length);
    
    // Find the current field's index
    const currentIndex = allDraftsInColumn.findIndex(el => el === parentField);
    console.log('Current index:', currentIndex);
    
    if (currentIndex >= 0 && currentIndex < allDraftsInColumn.length - 1) {
      // Move to the next field
      const nextField = allDraftsInColumn[currentIndex + 1];
      const nextInput = nextField.querySelector('input');
      if (nextInput) {
        console.log('Moving focus to next field');
        nextInput.focus();
        nextInput.select();
      }
    }
  });
  
  console.log('Draft field navigation initialized');
};
