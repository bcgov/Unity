$(function () {
    const uiElements = {
        settingForm: $('#AISettingsForm'),
        saveButton: $('#AISettingsSaveButton'),
        discardButton: $('#AISettingsDiscardButton')
    };

    let initialFormState = uiElements.settingForm.serialize();

    let lastSavedValues = {
        automaticGenerationEnabled: $('#AutomaticGenerationEnabled').is(':checked'),
        manualGenerationEnabled: $('#ManualGenerationEnabled').is(':checked')
    };

    function checkFormChanges() {
        let isFormChanged = uiElements.settingForm.serialize() !== initialFormState;
        uiElements.saveButton.prop('disabled', !isFormChanged);
        uiElements.discardButton.prop('disabled', !isFormChanged);
    }

    function saveSettings(automaticEnabled, manualEnabled) {
        unity.aI.settings.aIConfiguration.updateTenantConfiguration({
            automaticGenerationEnabled: automaticEnabled,
            manualGenerationEnabled: manualEnabled
        }).then(function () {
            lastSavedValues = {
                automaticGenerationEnabled: automaticEnabled,
                manualGenerationEnabled: manualEnabled
            };
            $(document).trigger('AbpSettingSaved');
            initialFormState = uiElements.settingForm.serialize();
            checkFormChanges();
        });
    }

    uiElements.settingForm.on('change', function () {
        checkFormChanges();
    });

    uiElements.settingForm.on('submit', function (event) {
        event.preventDefault();

        const automaticEnabled = $('#AutomaticGenerationEnabled').is(':checked');
        const manualEnabled = $('#ManualGenerationEnabled').is(':checked');
        const turningOn = (automaticEnabled && !lastSavedValues.automaticGenerationEnabled) ||
            (manualEnabled && !lastSavedValues.manualGenerationEnabled);

        unity.aI.legalDisclaimer.confirmIfNeeded(turningOn, function () {
            saveSettings(automaticEnabled, manualEnabled);
        });
    });

    uiElements.discardButton.on('click', function () {
        uiElements.settingForm[0].reset();
        initialFormState = uiElements.settingForm.serialize();
        checkFormChanges();
    });

    checkFormChanges();
});
