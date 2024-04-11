$(function () {
    const UIElements = {
        inputMinistryClient: $('input[name="PaymentSettings.MinistryClient"]'),
        inputResponsibility: $('input[name="PaymentSettings.Responsibility"]'),
        inputServiceLine: $('input[name="PaymentSettings.ServiceLine"]'),
        inputStob: $('input[name="PaymentSettings.Stob"]'),
        inputProjectNumber: $('input[name="PaymentSettings.ProjectNumber"]'),
        readOnlyAccountCoding: $('#account-coding'),
        statusMessage: $('#status-message')
    };

    init();

    function init() {
        bindUIEvents();
        setAccountCodinDisplay();
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
        UIElements.inputMinistryClient.on('keyup', setAccountCodinDisplay);
        UIElements.inputResponsibility.on('keyup', setAccountCodinDisplay);
        UIElements.inputServiceLine.on('keyup', setAccountCodinDisplay);
        UIElements.inputStob.on('keyup', setAccountCodinDisplay);
        UIElements.inputProjectNumber.on('keyup', setAccountCodinDisplay);
    }

    function setAccountCodinDisplay() {
        let currentAccount = $(UIElements.inputMinistryClient).val() + "." +
        $(UIElements.inputResponsibility).val() + "." +
        $(UIElements.inputServiceLine).val() + "." +
        $(UIElements.inputStob).val() + "." +
        $(UIElements.inputProjectNumber).val();
        $(UIElements.readOnlyAccountCoding).val(currentAccount);
    }
});
