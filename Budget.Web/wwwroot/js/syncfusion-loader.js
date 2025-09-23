// Edge CSS Rule Limit Workaround
// This script conditionally loads Syncfusion themes based on browser capabilities

(function() {
    // Check if we're running in Edge and have CSS rule limit issues
    const isEdge = navigator.userAgent.indexOf('Edg') !== -1;
    const isDevMode = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
    
    // Function to load CSS dynamically
    function loadCSS(href, id) {
        if (document.getElementById(id)) return; // Already loaded
        
        const link = document.createElement('link');
        link.id = id;
        link.rel = 'stylesheet';
        link.href = href;
        link.onerror = function() {
            console.warn('Failed to load CSS:', href);
            // Fallback to minimal theme
            loadCSS('/css/syncfusion-minimal.css', 'syncfusion-fallback');
        };
        document.head.appendChild(link);
    }
    
    // Load appropriate theme based on browser and environment
    if (isEdge && isDevMode) {
        // Use minimal theme for Edge in development
        loadCSS('/css/syncfusion-minimal.css', 'syncfusion-minimal');
    } else {
        // Try to load full theme for other browsers/environments
        loadCSS('/_content/Syncfusion.Blazor.Themes/bootstrap5.css', 'syncfusion-bootstrap5');
    }
})();