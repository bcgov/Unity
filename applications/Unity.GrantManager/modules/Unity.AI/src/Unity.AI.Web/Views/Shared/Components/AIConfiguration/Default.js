$(function () {
    const UIElements = {
        btnSave: $('#btn-save-ai-config'),
        btnCancel: $('#btn-cancel-ai-config'),
        btnBack: $('#btn-back-ai-config'),
        formId: $('#aiConfigFormId'),
        automaticCheckbox: $('#AutomaticallyGenerateAIAnalysis'),
        manualCheckbox: $('#ManuallyInitiateAIAnalysis')
    };

    let lastSavedAIValues = {
        automaticallyGenerateAIAnalysis: UIElements.automaticCheckbox.is(':checked'),
        manuallyInitiateAIAnalysis: UIElements.manualCheckbox.is(':checked')
    };

    init();

    function init() {
        bindUIEvents();
    }

    function bindUIEvents() {
        UIElements.btnSave.on('click', handleSave);
        UIElements.btnCancel.on('click', handleCancel);
        UIElements.btnBack.on('click', function () {
            location.href = '/ApplicationForms';
        });
    }

    function handleSave() {
        const automaticEnabled = UIElements.automaticCheckbox.is(':checked');
        const manualEnabled = UIElements.manualCheckbox.is(':checked');
        const turningOn = (automaticEnabled && !lastSavedAIValues.automaticallyGenerateAIAnalysis) ||
            (manualEnabled && !lastSavedAIValues.manuallyInitiateAIAnalysis);

        unity.aI.legalDisclaimer.confirmIfNeeded(turningOn, function () {
            saveAiConfig(automaticEnabled, manualEnabled);
        });
    }

    function saveAiConfig(automaticEnabled, manualEnabled) {
        UIElements.btnSave.prop('disabled', true);

        abp.ajax({
            url: `/api/app/application-form/${UIElements.formId.val()}/ai-config`,
            type: 'PATCH',
            data: JSON.stringify({
                automaticallyGenerateAIAnalysis: automaticEnabled,
                manuallyInitiateAIAnalysis: manualEnabled
            }),
            contentType: 'application/json'
        })
            .done(function () {
                lastSavedAIValues = {
                    automaticallyGenerateAIAnalysis: automaticEnabled,
                    manuallyInitiateAIAnalysis: manualEnabled
                };
                abp.notify.success('AI configuration saved successfully.');
            })
            .fail(function () {
                abp.notify.error('Failed to save AI configuration.');
            })
            .always(function () {
                UIElements.btnSave.prop('disabled', false);
            });
    }

    function handleCancel() {
        UIElements.automaticCheckbox.prop('checked', lastSavedAIValues.automaticallyGenerateAIAnalysis);
        UIElements.manualCheckbox.prop('checked', lastSavedAIValues.manuallyInitiateAIAnalysis);
    }
});
