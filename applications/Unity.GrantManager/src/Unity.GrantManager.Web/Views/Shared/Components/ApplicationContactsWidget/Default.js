$(function () {
    let applicantContactsWidgetToken = null;
    let _createContactModal = new abp.ModalManager(abp.appPath + 'ApplicationContact/CreateContactModal');
    let _editContactModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'ApplicationContact/EditContactModal',
        scriptUrl: '/Pages/ApplicationContact/EditContactModal.js',
        modalClass: "editOrDeleteContactModal"
    });

    // Handle modal result - refresh the widget after successful contact creation
    _createContactModal.onResult(function () {
        PubSub.publish("refresh_application_contacts");
        abp.notify.success(
            'The application contact has been successfully added.',
            'Application Contacts'
        );
    });

    _editContactModal.onResult(function () {
        PubSub.publish("refresh_application_contacts");
        abp.notify.success(
            'The application contact has been successfully updated.',
            'Application Contacts'
        );
    });

    abp.widgets.ApplicationContactsWidget = function ($wrapper) {

        let _widgetManager = $wrapper.data('abp-widget-manager');

        let widgetApi = {
            applicationId: null, // Cache the applicationId to prevent reading from stale DOM
            
            getFilters: function () {
                const appId = this.applicationId || $wrapper.find('#ApplicationContactsWidget_ApplicationId').val();
                
                return {
                    applicationId: appId
                };
            },

            init: function (filters) {
                this.applicationId =  $wrapper.find('#ApplicationContactsWidget_ApplicationId').val();
                this.setupEventHandlers();
            },

            refresh: function () {
                const currentFilters = this.getFilters();
                _widgetManager.refresh($wrapper, currentFilters);
            },

            setupEventHandlers: function() {
                const self = this;

                // Unsubscribe from previous subscription if it exists
                // This prevents duplicate event handlers after widget refresh
                if (applicantContactsWidgetToken) {
                    PubSub.unsubscribe(applicantContactsWidgetToken);
                    applicantContactsWidgetToken = null;
                }

                applicantContactsWidgetToken = PubSub.subscribe(
                    'refresh_application_contacts',
                    () => {
                        self.refresh();
                    }
                );

                // Prevent duplicate delegated click handlers on re-init by removing any
                // existing handlers in this widget's namespace before re-binding.
                $wrapper.off('click.ApplicationContactsWidget', '#CreateContactButton');
                $wrapper.off('click.ApplicationContactsWidget', '.contact-edit-btn');

                // Handle Add Contact button click
                $wrapper.on('click.ApplicationContactsWidget', '#CreateContactButton', function (e) {
                    e.preventDefault();
                    _createContactModal.open({
                        applicationId: self.applicationId || $wrapper.find('#ApplicationContactsWidget_ApplicationId').val()
                    });
                });

                $wrapper.on('click.ApplicationContactsWidget', '.contact-edit-btn', function (e) {
                    e.preventDefault();
                    let itemId = $(this).data('id');
                    _editContactModal.open({
                        id: itemId
                    });
                });
            }
        }

        return widgetApi;
    };

    // Initialize the ApplicationContactsWidget manager with filter callback
    let applicationContactsWidgetManager = new abp.WidgetManager({
        wrapper: '.abp-widget-wrapper[data-widget-name="ApplicationContactsWidget"]'
    });

    // Initialize the widget
    applicationContactsWidgetManager.init();
});
