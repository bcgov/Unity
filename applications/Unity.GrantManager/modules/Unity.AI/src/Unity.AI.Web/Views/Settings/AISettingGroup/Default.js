$(function () {
    const uiElements = {
        settingForm: $('#AISettingsForm'),
        saveButton: $('#AISettingsSaveButton'),
        discardButton: $('#AISettingsDiscardButton')
    };

    let initialFormState = uiElements.settingForm.serialize();

    function checkFormChanges() {
        let isFormChanged = uiElements.settingForm.serialize() !== initialFormState;
        uiElements.saveButton.prop('disabled', !isFormChanged);
        uiElements.discardButton.prop('disabled', !isFormChanged);
    }

    uiElements.settingForm.on('change', function () {
        checkFormChanges();
    });

    uiElements.settingForm.on('submit', function (event) {
        event.preventDefault();

        const scoringEnabled = $('#ScoringAssistantEnabled').is(':checked');

        unity.aI.settings.aIConfiguration.updateScoringSettings({
            scoringAssistantEnabled: scoringEnabled
        }).then(function () {
            $(document).trigger('AbpSettingSaved');
            initialFormState = uiElements.settingForm.serialize();
            checkFormChanges();
        });
    });

    uiElements.discardButton.on('click', function () {
        uiElements.settingForm[0].reset();
        initialFormState = uiElements.settingForm.serialize();
        checkFormChanges();
    });

    checkFormChanges();
});
