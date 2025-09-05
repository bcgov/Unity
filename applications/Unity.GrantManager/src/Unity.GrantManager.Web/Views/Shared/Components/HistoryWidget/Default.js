// Move variables to global scope
let historyWidgetApplicationId;
let historyWidgetInitialized = false;

// Global initialization function for lazy loading
window.initializeHistoryWidget = function (containerSelector = 'body') {
    console.log('Initializing HistoryWidget component');

    // Use container selector to scope the search
    const $container = $(containerSelector);

    // Get application ID
    historyWidgetApplicationId =
        $container.find('#HistoryWidgetApplicationId').val() ||
        document.getElementById('HistoryWidgetApplicationId')?.value ||
        $('#DetailsViewApplicationId').val(); // Fallback to main app ID

    if (!historyWidgetApplicationId) {
        console.error('HistoryWidget: Application ID not found');
        return;
    }

    console.log('HistoryWidget initializing with:', {
        applicationId: historyWidgetApplicationId,
        container: containerSelector,
    });

    // UI Elements with container scoping
    const UIElements = {
        historyLength:
            $container.find('#historyLength').length > 0
                ? $container.find('#historyLength')[0]
                : $('#historyLength')[0],
        expandedDiv:
            $container.find('#expanded-div').length > 0
                ? $container.find('#expanded-div')
                : $('#expanded-div'),
        cardHeaderDiv:
            $container.find('#card-header-div').length > 0
                ? $container.find('#card-header-div')
                : $('#card-header-div'),
        historyTab:
            $container.find('#history-tab').length > 0
                ? $container.find('#history-tab')
                : $('#history-tab'),
    };

    // Remove existing handlers to prevent duplicates
    UIElements.historyTab.off('click.historyWidget');

    // Add new handler with namespace
    UIElements.historyTab.on('click.historyWidget', function (e) {
        if (UIElements.historyLength && UIElements.historyLength.value > 0) {
            UIElements.expandedDiv.removeClass('hidden');
            UIElements.cardHeaderDiv.addClass('custom-active');
        }
    });

    historyWidgetInitialized = true;
    console.log('HistoryWidget initialized successfully');
};

// Global refresh function
window.refreshHistoryWidget = function () {
    // Add any refresh logic here if needed
    console.log('HistoryWidget refreshed');
};

// Global cleanup function
window.cleanupHistoryWidget = function () {
    $('#history-tab').off('click.historyWidget');
    console.log('HistoryWidget cleaned up');
};

// ADD DEBUG FUNCTIONS HERE (at the end of the file)
window.HistoryWidgetDebug = {
    isInitialized: () => historyWidgetInitialized,
    getApplicationId: () => historyWidgetApplicationId,
    forceRefresh: () => window.refreshHistoryWidget(),
    forceReinitialize: () => {
        window.cleanupHistoryWidget();
        window.initializeHistoryWidget('body');
    },
};

// Original initialization for backward compatibility
$(function () {
    // Check if we're in a lazy loading context by looking for specific elements
    const hasLazyContainer = $('.lazy-component-container').length > 0;
    const isDetailsV2 = window.location.pathname.includes('DetailsV2');

    // Only auto-initialize if NOT in lazy loading context
    if (!hasLazyContainer && !isDetailsV2) {
        console.log('Auto-initializing HistoryWidget for non-lazy context');
        window.initializeHistoryWidget('body');
    } else {
        console.log(
            'Skipping auto-initialization - lazy loading context detected'
        );
    }
});
