$(function () {    
    $("#PaymentsSettingsForm").on('submit', function (event) {
        event.preventDefault();

        if (!$(this).valid()) {
            return;
        }

        let form = $(this).serializeFormToObject();

        debugger;
        unity.payments.settings.paymentsSettings.update(form).then(function (result) {
            $(document).trigger("AbpSettingSaved");
        });
    });
});

