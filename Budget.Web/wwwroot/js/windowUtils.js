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
  // Prevent duplicate event listeners
  if (window._draftNavigationInitialized) {
    return;
  }
  window._draftNavigationInitialized = true;

  // Use event delegation on the document to handle keydown on draft fields
  document.addEventListener('keydown', function (event) {
    // Check if the event target is within a draft input field
    const target = event.target;
    if (!target || target.tagName !== 'INPUT') return;

    // Check if this is a draft field by looking at the parent element's id
    const parentField = target.closest('[id^="draft-"]');
    if (!parentField) return;

    // Only handle Tab and Enter keys
    if (event.key !== 'Tab' && event.key !== 'Enter') return;

    // Prevent default behavior
    event.preventDefault();

    // Get the envelope ID and month index from data attributes
    const envelopeId = parentField.getAttribute('data-envelope-id');
    const monthIndex = parentField.getAttribute('data-month-index');

    if (!envelopeId || monthIndex == null) return;

    // Find all draft fields with the same month index (same column)
    const allDraftsInColumn = Array.from(document.querySelectorAll(`[data-month-index="${monthIndex}"]`));
    
    // Find the current field's index
    const currentIndex = allDraftsInColumn.findIndex(el => el === parentField);
    
    if (currentIndex >= 0 && currentIndex < allDraftsInColumn.length - 1) {
      // Move to the next field
      const nextField = allDraftsInColumn[currentIndex + 1];
      const nextInput = nextField.querySelector('input');
      if (nextInput) {
        nextInput.focus();
        nextInput.select();
      }
    }
  });
};
