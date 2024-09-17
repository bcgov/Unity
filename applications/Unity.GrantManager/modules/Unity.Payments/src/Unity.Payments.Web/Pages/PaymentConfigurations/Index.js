$(function () {
    const UIElements = {
        inputMinistryClient: $('input[name="PaymentConfiguration.MinistryClient"]'),
        inputResponsibility: $('input[name="PaymentConfiguration.Responsibility"]'),
        inputServiceLine: $('input[name="PaymentConfiguration.ServiceLine"]'),
        inputStob: $('input[name="PaymentConfiguration.Stob"]'),
        inputProjectNumber: $('input[name="PaymentConfiguration.ProjectNumber"]'),
        readOnlyAccountCoding: $('#account-coding'),
        statusMessage: $('#status-message'),
        inputPaymentIdPrefix: $('#PaymentIdPrefix')
    };

    init();

    function init() {
        bindUIEvents();
        setAccountCodingDisplay();
        displayStatusMessage();
        bindLimitedInputFields(/^[a-zA-Z0-9]+$/, [UIElements.inputPaymentIdPrefix[0].id]);
    }

    function displayStatusMessage() {
        let statusMessage = UIElements.statusMessage.val();
        if (statusMessage != "") {
            if (statusMessage.indexOf('error') > 0) {
                abp.notify.error(
                    UIElements.statusMessage.val(),
                    'Error Occurred'
                );
            } else {
                abp.notify.success(
                    UIElements.statusMessage.val(),
                    'Save Successful'
                );
            }
        }
    }

    function bindUIEvents() {
        UIElements.inputMinistryClient.on('keyup', setAccountCodingDisplay);
        UIElements.inputResponsibility.on('keyup', setAccountCodingDisplay);
        UIElements.inputServiceLine.on('keyup', setAccountCodingDisplay);
        UIElements.inputStob.on('keyup', setAccountCodingDisplay);
        UIElements.inputProjectNumber.on('keyup', setAccountCodingDisplay);
    }

    function bindLimitedInputFields(regex, fieldIds) {
        fieldIds.forEach((id) => {
            let inputElement = document.getElementById(id);
            inputElement.addEventListener('input', function (_) {
                let value = inputElement.value;
                let lastChar = value.charAt(value.length - 1);

                if (!regex.test(lastChar)) {
                    inputElement.value = value.slice(0, -1);
                }

                inputElement.value = inputElement.value.toUpperCase();
            });
        });
    }

    function setAccountCodingDisplay() {
        let currentAccount = $(UIElements.inputMinistryClient).val() + "." +
            $(UIElements.inputResponsibility).val() + "." +
            $(UIElements.inputServiceLine).val() + "." +
            $(UIElements.inputStob).val() + "." +
            $(UIElements.inputProjectNumber).val();
        $(UIElements.readOnlyAccountCoding).val(currentAccount);
    }
    $('#resetButton').click(function () {
        $('#paymentConfigForm')[0].reset();
        setAccountCodingDisplay();
    });
});
