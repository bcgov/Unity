// Move variables to global scope
let summaryWidgetApplicationId;
let summaryWidgetIsReadOnly;
let summaryWidgetContactModal;
let summaryWidgetContactsWidgetManager;
let summaryWidgetInitialized = false;

// Global initialization function for lazy loading
window.initializeSummaryWidget = function (containerSelector = 'body') {
    console.log('Initializing SummaryWidget component');

    // Use container selector to scope the search
    const $container = $(containerSelector);

    // Get application ID and readonly status
    summaryWidgetApplicationId =
        $container.find('#SummaryWidgetApplicationId').val() ||
        document.getElementById('SummaryWidgetApplicationId')?.value ||
        $('#DetailsViewApplicationId').val(); // Fallback to main app ID

    summaryWidgetIsReadOnly =
        $container.find('#SummaryWidgetIsReadOnly').val() ||
        document.getElementById('SummaryWidgetIsReadOnly')?.value ||
        'false';

    if (!summaryWidgetApplicationId) {
        console.error('SummaryWidget: Application ID not found');
        return;
    }

    console.log('SummaryWidget initializing with:', {
        applicationId: summaryWidgetApplicationId,
        isReadOnly: summaryWidgetIsReadOnly,
        container: containerSelector,
    });

    // Initialize contact modal
    summaryWidgetContactModal = new abp.ModalManager(
        abp.appPath + 'ApplicationContact/CreateContactModal'
    );

    // Initialize widget manager
    summaryWidgetContactsWidgetManager = new abp.WidgetManager({
        wrapper:
            $container.find('#applicationContactsWidget').length > 0
                ? $container.find('#applicationContactsWidget')
                : '#applicationContactsWidget',
        filterCallback: function () {
            return {
                applicationId: summaryWidgetApplicationId,
                isReadOnly: summaryWidgetIsReadOnly,
            };
        },
    });

    // Setup event handlers with container scoping
    const $addContactButton =
        $container.find('#AddContactButton').length > 0
            ? $container.find('#AddContactButton')
            : $('#AddContactButton');

    // Remove existing handlers to prevent duplicates
    $addContactButton.off('click.summaryWidget');

    // Add new handler with namespace
    $addContactButton.on('click.summaryWidget', function (e) {
        e.preventDefault();
        summaryWidgetContactModal.open({
            applicationId: summaryWidgetApplicationId,
        });
    });

    // Setup modal result handler
    summaryWidgetContactModal.onResult(function () {
        abp.notify.success(
            'The application contact have been successfully added.',
            'Application Contacts'
        );
        summaryWidgetContactsWidgetManager.refresh();
    });

    // Setup PubSub subscription (only once)
    if (!summaryWidgetInitialized) {
        PubSub.subscribe('refresh_application_contacts', (msg, data) => {
            if (summaryWidgetContactsWidgetManager) {
                summaryWidgetContactsWidgetManager.refresh();
            }
        });
        summaryWidgetInitialized = true;
    }

    console.log('SummaryWidget initialized successfully');
};

// Global refresh function
window.refreshSummaryWidget = function () {
    if (summaryWidgetContactsWidgetManager) {
        summaryWidgetContactsWidgetManager.refresh();
    }
};

// Global cleanup function
window.cleanupSummaryWidget = function () {
    if (summaryWidgetContactModal) {
        summaryWidgetContactModal.close();
    }
    $('#AddContactButton').off('click.summaryWidget');
    console.log('SummaryWidget cleaned up');
};

// ADD DEBUG FUNCTIONS HERE (at the end of the file)
window.SummaryWidgetDebug = {
    isInitialized: () => summaryWidgetInitialized,
    getApplicationId: () => summaryWidgetApplicationId,
    forceRefresh: () => window.refreshSummaryWidget(),
    forceReinitialize: () => {
        window.cleanupSummaryWidget();
        window.initializeSummaryWidget('body');
    },
    getContactModal: () => summaryWidgetContactModal,
    getWidgetManager: () => summaryWidgetContactsWidgetManager,
};

// Original initialization for backward compatibility (Details.js)
$(function () {
    // Check if we're in a lazy loading context by looking for specific elements
    const hasLazyContainer = $('.lazy-component-container').length > 0;
    const isDetailsV2 = window.location.pathname.includes('DetailsV2');

    // Only auto-initialize if NOT in lazy loading context
    if (!hasLazyContainer && !isDetailsV2) {
        console.log('Auto-initializing SummaryWidget for non-lazy context');
        window.initializeSummaryWidget('body');
    } else {
        console.log(
            'Skipping auto-initialization - lazy loading context detected'
        );
    }
});
