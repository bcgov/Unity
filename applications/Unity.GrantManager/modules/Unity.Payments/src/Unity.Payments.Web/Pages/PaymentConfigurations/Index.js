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
        UIElements.inputPaymentIdPrefix.on('keyup', upperCaseTextOnly);
        UIElements.inputMinistryClient.on('keyup', setAccountCodingDisplay);
        UIElements.inputResponsibility.on('keyup', setAccountCodingDisplay);
        UIElements.inputServiceLine.on('keyup', setAccountCodingDisplay);
        UIElements.inputStob.on('keyup', setAccountCodingDisplay);
        UIElements.inputProjectNumber.on('keyup', setAccountCodingDisplay);
    }

    function upperCaseTextOnly(e) {
        var regex = new RegExp("^[a-zA-Z0-9]+$");
        var key = String.fromCharCode(!e.charCode ? e.which : e.charCode);
        if (!regex.test(key)) {
           e.preventDefault();
           return false;
        }   

        let currentValue = e.currentTarget.value;
        e.currentTarget.value = currentValue.toUpperCase();
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
