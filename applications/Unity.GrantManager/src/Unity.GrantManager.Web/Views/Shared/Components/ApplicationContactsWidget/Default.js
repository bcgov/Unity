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
            'The application contact have been successfully added.',
            'Application Contacts'
        );
    });

    _editContactModal.onResult(function () {
        PubSub.publish("refresh_application_contacts");
        abp.notify.success(
            'The application contact have been successfully updated.',
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

                // Subscribe to the applicant_info_merged event and store the token
                applicantContactsWidgetToken = PubSub.subscribe(
                    'refresh_application_contacts',
                    () => {
                        self.refresh();
                    }
                );

                // Handle Add Contact button click
                $wrapper.on('click', '#CreateContactButton', function (e) {
                    e.preventDefault();
                    _createContactModal.open({
                        applicationId: $('#ApplicationContactsWidget_ApplicationId').val()
                    });
                });

                $wrapper.on('click', '.contact-edit-btn', function (e) {
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
        wrapper: '.abp-widget-wrapper[data-widget-name="ApplicationContactsWidget"]',
        filterCallback: function () {
            return {
                'applicationId': $('#ApplicationContactsWidget_ApplicationId').val()
            };
        }
    });

    // Initialize the widget
    applicationContactsWidgetManager.init();
});
