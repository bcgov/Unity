(function () {
    abp.widgets.ApplicationActionWidget = function ($wrapper) {

        let widgetManager = $wrapper.data('abp-widget-manager');
        let $actionButtons = $wrapper.find('.details-dropdown-action');
        let widgetAppId = decodeURIComponent(document.querySelector("#DetailsViewApplicationId").value);

        function init() {
            $actionButtons.each(function () {
                let $button = $(this);
                attachClickEvent($button);
            });

            let isRedStop = document.getElementById('actionWidget_ApplicantIsRedStop')?.value === 'true';
            if (isRedStop) {
                let $dropdownToggle = $wrapper.find('#ApplicationActionDropdown .dropdown-toggle');
                $dropdownToggle.attr({
                    'data-bs-toggle': 'popover',
                    'data-bs-trigger': 'hover focus',
                    'data-bs-placement': 'top'
                });
                let redstopPopover = new bootstrap.Popover($dropdownToggle[0]);
                redstopPopover.setContent({
                    '.popover-title': 'Red-Stop Restriction',
                    '.popover-body': l("GrantApplication:ActionButton.RedStopWarning")
                });
            }
        }

        function attachClickEvent($button) {
            $button.on("click", function () {
                handleButtonClick($button);
            });
        }

        function handleButtonClick($button) {
            setButtonBusy($button);
            triggerAction($button);
        }

        function setButtonBusy($button) {
            $button.buttonBusy();
            $('#ApplicationActionDropdown .dropdown-toggle').buttonBusy();
        }

        function triggerAction($button) {
            let action = getActionData($button);
            customConfirmation(action);
        }

        function getActionData($button) {
            return $button.data('appAction');
        }

        function getFilters() {
            return {
                applicationId: widgetAppId
            };
        }

        function customConfirmation(triggerActionEnum) {
            let confirmationDetails = getConfirmationText(triggerActionEnum);
            let isRedStop = document.getElementById('actionWidget_ApplicantIsRedStop')?.value === 'true';
        
            if (isRedStop) {
                return handleRedStopAction();
            }
        
            if (triggerActionEnum === 'CompleteAssessment') {
                handleCompleteAssessment(confirmationDetails, triggerActionEnum);
            } else {
                firePageAlert(confirmationDetails, triggerActionEnum);
            }
        }
        
        function handleRedStopAction() {
            return Swal.fire({
                icon: "error",
                text: l("GrantApplication:ActionButton.RedStopWarning"),
                confirmButtonText: 'Ok',
                customClass: {
                    confirmButton: 'btn btn-primary'
                }
            }).then(() => {
                widgetManager.refresh();
            });
        }
        
        function handleCompleteAssessment(confirmationDetails, triggerActionEnum) {
            unity.grantManager.assessments.assessment.getDisplayList(widgetAppId)
                .then(function (response) {
                    updateConfirmationDetails(response, confirmationDetails, triggerActionEnum);
                });
        }
        
        function updateConfirmationDetails(response, confirmationDetails, triggerActionEnum) {
            if (response.data.some(item => item.status !== "COMPLETED")) {
                confirmationDetails = {
                    isConfirmationRequired: true,
                    title: 'Confirm Action',
                    text: 'One or more assessment records are incomplete. Are you sure you want to complete the assessment of the application?',
                    confirmButtonText: 'Confirm'
                };
            } else {
                confirmationDetails = {
                    isConfirmationRequired: true,
                    title: 'Confirm Action',
                    text: 'Are you sure you want to complete the assessment of the application?',
                    confirmButtonText: 'Confirm'
                };
            }
        
            firePageAlert(confirmationDetails, triggerActionEnum);
        }

        function firePageAlert(confirmationDetails, triggerActionEnum) {
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
                        triggerStatusAction(triggerActionEnum);
                    }
                    else {
                        widgetManager.refresh();
                    }
                });

            }
            else {
                triggerStatusAction(triggerActionEnum);
            }
        }

        function getConfirmationText(triggerActionEnum) {
            switch (triggerActionEnum) {
                case 'Approve':
                    return { isConfirmationRequired: true, title: 'Confirm Action', text: 'Are you sure you want to approve the application?', confirmButtonText: 'Confirm', };
                case 'Deny':
                    return { isConfirmationRequired: true, title: 'Confirm Action', text: 'Are you sure you want to decline the application?', confirmButtonText: 'Confirm' };
                case 'Withdraw':
                    return { isConfirmationRequired: true, title: 'Confirm Action', text: 'Are you sure you want to Withdraw the application?', confirmButtonText: 'Confirm' };
                case 'Close':
                    return { isConfirmationRequired: true, title: 'Confirm Action', text: 'Are you sure you want to Close the application?', confirmButtonText: 'Confirm' };
                case 'CompleteReview':
                    return { isConfirmationRequired: true, title: 'Confirm Action', text: 'Are you sure you want to complete the review of the application?', confirmButtonText: 'Confirm' };
                default:
                    return { isConfirmationRequired: false };
            }
        }

        function triggerStatusAction(triggerActionEnum) {
            unity.grantManager.grantApplications.grantApplication
                .triggerAction(widgetAppId, triggerActionEnum, {})
                .then(function (_) {
                    widgetManager.refresh();
                    abp.notify.success(
                        l(`Enum:GrantApplicationAction.Message.${triggerActionEnum}`),
                        "Application Status Changed"
                    );
                    PubSub.publish("application_status_changed", triggerActionEnum);
                    PubSub.publish("refresh_detail_panel_summary");
                    PubSub.publish("init_date_pickers");
                })
                .catch(function () { widgetManager.refresh(); });
        }
        return {
            init: init,
            getFilters: getFilters
        };
    };
})();