(function ($) {
    $(function () {
        $('.form-check').addClass('form-switch');

        const syncSwitchAriaChecked = (el) => {
            $(el).attr('aria-checked', el.checked ? 'true' : 'false');
        };

        const $switchInputs = $('input[role="switch"]');
        $switchInputs.each(function () { syncSwitchAriaChecked(this); });
        $switchInputs.on('change', function () { syncSwitchAriaChecked(this); });

        $(document).on('reset', '#ApplicationTabsSettingsForm', function () {
            window.setTimeout(function () {
                $(this).find('input[role="switch"]').each(function () { syncSwitchAriaChecked(this); });
            }.bind(this), 0);
        });

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

            if (!TabsUiElements.settingForm.valid()) {
                return;
            }

            let form = TabsUiElements.settingForm.serializeFormToObject();
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