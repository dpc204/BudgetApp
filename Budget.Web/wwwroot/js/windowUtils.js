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

  // Use event delegation on the document to handle keydown on draft fields
  document.addEventListener('keydown', function (event) {
    // Check if the event target is an input field
    const target = event.target;
    if (!target || target.tagName !== 'INPUT') return;

    // Check if this input has the draft-input-right class (our draft fields)
    if (!target.closest('.draft-input-right')) return;

    // Only handle Tab and Enter keys
    if (event.key !== 'Tab' && event.key !== 'Enter') return;

    console.log('[DEBUG] Tab/Enter in draft field!');
    
    // Prevent default behavior
    event.preventDefault();

    // Find all draft input fields in the document
    const allDraftInputs = Array.from(document.querySelectorAll('.draft-input-right input'));
    console.log('[DEBUG] All draft inputs found:', allDraftInputs.length);
    
    // Find the current input's index
    const currentIndex = allDraftInputs.findIndex(input => input === target);
    console.log('[DEBUG] Current input index:', currentIndex);
    
    if (currentIndex >= 0 && currentIndex < allDraftInputs.length - 1) {
      // Move to the next input
      const nextInput = allDraftInputs[currentIndex + 1];
      console.log('[DEBUG] Moving to next input');
      nextInput.focus();
      nextInput.select();
      console.log('[DEBUG] Successfully focused next input');
    } else {
      console.log('[DEBUG] No next input available');
    }
  });
  
  console.log('[DEBUG] Event listener added');
};
