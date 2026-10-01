(function () {
    const l = abp.localization.getResource('GrantManager');

    // Reusable, config-driven required-field prompts for actions blocked on missing data (e.g. Approve/Deny).
    function getActionPromptConfig() {
        return {
            Approve: {
                banner: l('GrantApplication:ActionPrompt.SelectDecisionDateBanner'),
                confirmText: l('GrantApplication:ActionPrompt.ConfirmApprove'),
                fields: [
                    { name: 'finalDecisionDate', label: l('GrantApplication:ActionPrompt.DecisionDate'), type: 'date', required: true, currentValueElementId: 'actionWidget_FinalDecisionDate' }
                ]
            },
            Deny: {
                banner: l('GrantApplication:ActionPrompt.SelectDeclineDetailsBanner'),
                confirmText: l('GrantApplication:ActionPrompt.ConfirmDecline'),
                fields: [
                    { name: 'declineRational', label: l('GrantApplication:ActionPrompt.DeclineRational'), type: 'select', required: true, currentValueElementId: 'actionWidget_DeclineRational', optionsTemplateId: 'actionWidget_DeclineRationalOptionsTemplate' },
                    { name: 'finalDecisionDate', label: l('GrantApplication:ActionPrompt.DecisionDate'), type: 'date', required: true, currentValueElementId: 'actionWidget_FinalDecisionDate' }
                ]
            }
        };
    }

    function fieldInputId(field) {
        return `actionPrompt_${field.name}`;
    }

    function todayDateString() {
        let today = new Date();
        let month = (today.getMonth() + 1).toString().padStart(2, '0');
        let day = today.getDate().toString().padStart(2, '0');
        return `${today.getFullYear()}-${month}-${day}`;
    }

    abp.widgets.ApplicationActionWidget = function ($wrapper) {

        let widgetManager = $wrapper.data('abp-widget-manager');
        let $actionButtons = $wrapper.find('.details-dropdown-action');
        let widgetAppId = decodeURIComponent(document.querySelector("#DetailsViewApplicationId").value);

        function init() {
            $actionButtons.each(function () {
                let $button = $(this);
                attachClickEvent($button);
            });
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
            let isRedStop = document.getElementById('actionWidget_ApplicantIsRedStop')?.value === 'true';

            if (isRedStop) {
                return handleRedStopAction();
            }

            let promptConfig = getActionPromptConfig()[triggerActionEnum];
            if (promptConfig) {
                return showActionFieldPrompt(triggerActionEnum, promptConfig);
            }

            let confirmationDetails = getConfirmationText(triggerActionEnum);
            if (triggerActionEnum === 'CompleteAssessment') {
                handleCompleteAssessment(confirmationDetails, triggerActionEnum);
            } else {
                firePageAlert(confirmationDetails, triggerActionEnum);
            }
        }

        // Reusable, config-driven required-field prompts for actions blocked on missing data (e.g. Approve/Deny).
        function buildPromptFieldHtml(field) {
            let currentValue = document.getElementById(field.currentValueElementId)?.value || '';
            let requiredMark = field.required ? ' <span class="text-danger">*</span>' : '';
            let inputHtml = field.type === 'select'
                ? `<select id="${fieldInputId(field)}" class="form-select">${document.getElementById(field.optionsTemplateId)?.innerHTML || ''}</select>`
                : `<input type="date" id="${fieldInputId(field)}" class="form-control" max="${todayDateString()}" value="${currentValue}" />`;

            return `<div class="mb-3 text-start">
                        <label for="${fieldInputId(field)}" class="form-label fw-bold">${field.label}${requiredMark}</label>
                        ${inputHtml}
                    </div>`;
        }

        function buildPromptFieldsHtml(promptConfig) {
            let bannerHtml = promptConfig.banner ? `<div class="alert alert-info text-start" role="alert">${promptConfig.banner}</div>` : '';
            let fieldsHtml = promptConfig.fields.map(buildPromptFieldHtml).join('');
            return `${bannerHtml}${fieldsHtml}<p>${promptConfig.confirmText}</p>`;
        }

        function collectAndValidatePromptFields(fields) {
            let values = {};
            for (const field of fields) {
                let el = document.getElementById(fieldInputId(field));
                let value = el ? el.value : '';
                if (field.required && !value) {
                    Swal.showValidationMessage(l('GrantApplication:ActionPrompt.FieldRequired', field.label));
                    return false;
                }
                values[field.name] = value || null;
            }
            return values;
        }

        function showActionFieldPrompt(triggerActionEnum, promptConfig) {
            Swal.fire({
                title: 'Confirm Action',
                html: buildPromptFieldsHtml(promptConfig),
                showCancelButton: true,
                confirmButtonText: 'Confirm',
                focusConfirm: false,
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-secondary'
                },
                didOpen: (modalEl) => {
                    promptConfig.fields.forEach((field) => {
                        if (field.type === 'select') {
                            let el = modalEl.querySelector(`#${fieldInputId(field)}`);
                            if (el) {
                                el.value = document.getElementById(field.currentValueElementId)?.value || '';
                            }
                        }
                    });
                },
                preConfirm: () => collectAndValidatePromptFields(promptConfig.fields)
            }).then((result) => {
                if (result.isConfirmed) {
                    triggerStatusAction(triggerActionEnum, result.value);
                } else {
                    widgetManager.refresh();
                }
            });
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

        function triggerStatusAction(triggerActionEnum, inputValues = {}) {
            unity.grantManager.grantApplications.grantApplication
                .triggerAction(widgetAppId, triggerActionEnum, inputValues)
                .then(function (_) {
                    widgetManager.refresh();
                    abp.notify.success(
                        l(`Enum:GrantApplicationAction.Message.${triggerActionEnum}`),
                        "Application Status Changed"
                    );
                    PubSub.publish("application_status_changed", triggerActionEnum);
                    PubSub.publish("refresh_detail_panel_summary");
                    PubSub.publish("init_date_pickers");
                    PubSub.publish('ApplicationHistory_refresh');
                })
                .catch(function () { widgetManager.refresh(); });
        }
        return {
            init: init,
            getFilters: getFilters
        };
    };
})();