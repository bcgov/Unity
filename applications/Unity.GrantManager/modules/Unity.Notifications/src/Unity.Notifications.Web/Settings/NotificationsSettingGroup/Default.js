(function ($) {
    $(function () {
        console.info("Notifications Setting Management UI Loaded");

        const NotificationUiElements = {
            settingForm: $("#NotificationsSettingsForm"),
            saveButton: $("#NotificationsSaveButton"),
            discardButton: $("#NotificationsDiscardButton")
        }

        let initialFormState = NotificationUiElements.settingForm.serialize();

        function checkFormChanges() {
            let currentFormState = NotificationUiElements.settingForm.serialize();
            let isFormChanged = currentFormState !== initialFormState;

            NotificationUiElements.saveButton.prop('disabled', !isFormChanged);
            NotificationUiElements.discardButton.prop('disabled', !isFormChanged);
        }

        NotificationUiElements.settingForm.on('input change', function () {
            checkFormChanges();
        });

        NotificationUiElements.settingForm.on('submit', function (event) {
            event.preventDefault();

            if (!$(this).valid()) {
                return;
            }

            // Add email validation

            let form = $(this).serializeFormToObject();
            unity.notifications.emailNotifications.emailNotification.updateSettings(form).then(function (result) {
                $(document).trigger("AbpSettingSaved");
                initialFormState = NotificationUiElements.settingForm.serialize();
                checkFormChanges();
            });

        });

        NotificationUiElements.discardButton.on('click', function () {
            NotificationUiElements.settingForm[0].reset();
            initialFormState = NotificationUiElements.settingForm.serialize();
            checkFormChanges();
        });

        checkFormChanges();
    });
})(jQuery);