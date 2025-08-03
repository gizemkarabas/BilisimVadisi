// FOUC Prevention Script
// This script must be loaded as early as possible to prevent flash of unstyled content

(function () {
    'use strict';

    // Hide content immediately when script loads
    function hideContent() {
        document.documentElement.style.visibility = 'hidden';
        document.documentElement.style.opacity = '0';
    }

    // Show content when ready
    function showContent() {
        document.documentElement.classList.add('blazor-loaded');
        document.body.classList.add('blazor-loaded');

        var loadingElement = document.getElementById('blazor-loading');
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
    }

    // Initialize FOUC prevention
    function initFoucPrevention() {
        // Hide content immediately
        hideContent();

        // Show loading and content when DOM is ready
        document.addEventListener('DOMContentLoaded', function () {
            var loadingElement = document.getElementById('blazor-loading');
            if (loadingElement) {
                loadingElement.style.display = 'flex';
            }

            // Simple timeout approach - more reliable than complex detection
            setTimeout(showContent, 300);
        });
    }

    // Start immediately
    initFoucPrevention();

})();
