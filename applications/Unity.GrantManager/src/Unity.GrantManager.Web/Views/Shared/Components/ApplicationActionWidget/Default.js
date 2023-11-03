(function () {
    abp.widgets.ApplicationActionWidget = function ($wrapper) {
        let widgetManager = $wrapper.data('abp-widget-manager');
        let $actionButtons = $wrapper.find('.details-dropdown-action');
        let widgetAppId = decodeURIComponent(document.querySelector("#DetailsViewApplicationId").value);
       
        function init (filters) {
            $actionButtons.each(function () {
                let $this = $(this);
                $this.on("click", function () {
                    $(this).buttonBusy();
                    $('#ApplicationActionDropdown .dropdown-toggle').buttonBusy();
                    let triggerAction = $(this).data('appAction');
                    customConfirmation(triggerAction);

                });
            });
        };

        function getFilters() {
            return {
                applicationId: widgetAppId
            };
        }

        function customConfirmation(triggerAction) {
            let confirmationDetails = getConfirmationText(triggerAction);
            if (confirmationDetails.isConfirmationRequired) {
                Swal.fire({
                    title: confirmationDetails.title,
                    text: confirmationDetails.text,
                    showCancelButton: true,
                    confirmButtonText: confirmationDetails.confirmButtonText,
                    customClass: {
                        confirmButton: 'btn btn-primary',
                        cancelButton: 'btn btn-secondary'
                    }
                }).then((result) => {
                    if (result.isConfirmed) {
                        triggerStatusAction(triggerAction);
                    }
                    else {
                        widgetManager.refresh();
                    }
                });

            }
            else {
                triggerStatusAction(triggerAction);
            }

        }
        function getConfirmationText(triggerAction) {
            switch (triggerAction) {
                case 'Approve':
                    return { isConfirmationRequired: true, title: 'Confirm Action', text: 'Are you sure you want to approve the application?', confirmButtonText: 'Confirm', };

                case 'Deny':

                    return { isConfirmationRequired: true, title: 'Confirm Action', text: 'Are you sure you want to decline the application?', confirmButtonText: 'Confirm' };

                default:
                    return { isConfirmationRequired: false };
            }
        }



        function triggerStatusAction(triggerAction) {
            unity.grantManager.grantApplications.grantApplication
                .triggerAction(widgetAppId, triggerAction, {})
                .then(function (result) {
                    // TODO: PUBSUB & REFRESH WIDGET
                    widgetManager.refresh();
                    abp.notify.success(
                        l(`Enum:GrantApplicationAction.${triggerAction}`),
                        "Application Status Changed"
                    );
                });
        }
        return {
            init: init,
            getFilters: getFilters
        };
    };
})();