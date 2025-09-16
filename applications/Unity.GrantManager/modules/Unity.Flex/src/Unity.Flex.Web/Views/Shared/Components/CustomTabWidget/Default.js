// Move variables to global scope
let customTabWidgetInitialized = false;
let customTabWidgets = {}; // Track multiple instances by tab name

// Global initialization function for lazy loading
window.initializeCustomTabWidget = function (
    containerSelector = 'body',
    tabName = null
) {
    // Use container selector to scope the search
    const $container = $(containerSelector);

    // Initialize Flex components or any custom tab functionality
    const $forms = $container.find('form[id*="customForm"]');

    $forms.each(function () {
        const $form = $(this);
        const formId = $form.attr('id');

        // Initialize form-specific functionality
        console.log(`Initializing custom form: ${formId}`);

        // Add any custom tab specific initialization here
        // For example, if it has worksheets or form interactions
    });

    // Track initialization
    if (tabName) {
        customTabWidgets[tabName] = true;
    }

    customTabWidgetInitialized = true;
    console.log('CustomTabWidget initialized successfully');
};

// Original initialization for backward compatibility
$(function () {
    // Check if we're in a lazy loading context
    const hasLazyContainer = $('.lazy-component-container').length > 0;
    const isDetailsV2 = window.location.pathname.includes('DetailsV2');

    // Only auto-initialize if NOT in lazy loading context
    if (!hasLazyContainer && !isDetailsV2) {
        console.log('Auto-initializing CustomTabWidget for non-lazy context');
        window.initializeCustomTabWidget('body');
    } else {
        console.log(
            'Skipping auto-initialization - lazy loading context detected custom tab'
        );
    }
});
