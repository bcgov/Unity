﻿(function ($) {
    $(function () {
        $('.form-check').addClass('form-switch');

        const TabsUiElements = {
            settingForm: $("#ApplicationTabsSettingsForm"),
            saveButton: $("#ApplicationTabsSaveButton"),
            discardButton: $("#ApplicationTabsDiscardButton")
        }

        let initialFormState = TabsUiElements.settingForm.serialize();

        function checkFormChanges() {
            let currentFormState = TabsUiElements.settingForm.serialize();
            let isFormChanged = currentFormState !== initialFormState;

            TabsUiElements.saveButton.prop('disabled', !isFormChanged);
            TabsUiElements.discardButton.prop('disabled', !isFormChanged);
        }

        TabsUiElements.settingForm.on('input change', function () {
            checkFormChanges();
        });

        TabsUiElements.settingForm.on('submit', function (event) {
            event.preventDefault();

            if (!$(this).valid()) {
                return;
            }

            let form = $(this).serializeFormToObject();
            unity.grantManager.settingManagement.applicationUiSettings.update(form).then(function (result) {
                $(document).trigger("AbpSettingSaved");
                initialFormState = TabsUiElements.settingForm.serialize();
                checkFormChanges();
            });

        });

        TabsUiElements.discardButton.on('click', function () {
            TabsUiElements.settingForm[0].reset();
            initialFormState = TabsUiElements.settingForm.serialize();
            checkFormChanges();
        });

        checkFormChanges();
    });
})(jQuery);