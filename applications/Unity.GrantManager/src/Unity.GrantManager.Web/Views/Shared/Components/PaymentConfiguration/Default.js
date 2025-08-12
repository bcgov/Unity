$(function () {
    const UIElements = {
        btnSave: $('#btn-save-payment-configuration'),
        btnBack: $('#btn-back-payment-configuration'),
        appFormId: $('#applicationFormId'),
        accountCode: $('#AccountCode'),
        preventPayment: $('#PreventAutomaticPaymentToCAS'),
        payable: $('#Payable'),
        hasEditPermission: $('#HasEditFormPaymentConfiguration'),
        paymentApprovalThreshold: $('#PaymentApprovalThreshold'),
        paymentThresholdForm: $('#PaymentThresholdForm')
    };

    function bindUIEvents() {
        UIElements.accountCode.on('change', enableSaveButton);
        UIElements.preventPayment.on('change', enableSaveButton);
        UIElements.payable.on('change', enableSaveButton);
        UIElements.paymentApprovalThreshold.on('change', enableSaveButton);

        UIElements.btnSave.on('click', saveButtonAction);
        UIElements.btnBack.on('click', backButtonAction);

        UIElements.paymentApprovalThreshold.on('keypress', preventNegativeKeyPress);
        UIElements.paymentApprovalThreshold.on('input', preventDecimalKeyPress);
    }

    init();

    function init() {
        bindUIEvents();
        toastr.options.positionClass = 'toast-top-center';
        UIElements.btnSave.prop('disabled', true);
    }
    
    function preventDecimalKeyPress(e) {
        const input = e.target;
        const cursorPosition = input.selectionStart;
        const decimalMatch = input.value.match(/\.(\d+)/);

        // Limit to two decimal places
        if (decimalMatch && decimalMatch[1].length > 2) {
            input.value = input.value.replace(/\.(\d{2}).*/, '.$1');
            input.setSelectionRange(cursorPosition, cursorPosition); // Restore cursor position
        }
    }

    function preventNegativeKeyPress(e) {
        if (e.key === '-' || e.keyCode === 45) {
            e.preventDefault();
        }
        const input = e.target;

        if (input.value.length > 17) {
            e.preventDefault();
        }
    }

    function saveButtonAction() {

        unity.grantManager.applicationForms.applicationForm.savePaymentConfiguration(
            {
                accountCodingId: UIElements.accountCode.val(),
                applicationFormId: UIElements.appFormId.val(),
                preventPayment: UIElements.preventPayment.is(':checked'),
                payable: UIElements.payable.is(':checked'),
                paymentApprovalThreshold: UIElements.paymentApprovalThreshold.val() === '' ? null : UIElements.paymentApprovalThreshold.val()
            })
            .then(() => {
                UIElements.btnSave.prop('disabled', true);
                abp.notify.success(
                    'Payment Configuration is successfully saved.',
                    'Form Payment Configuration'
                );

                Swal.fire({
                    title: "Note",
                    text: "Please note that any changes made to the payment configuration will not impact payment requests that have already been created.",
                    confirmButtonText: 'Ok',
                    customClass: {
                        confirmButton: 'btn btn-primary'
                    }
                });
            });
    }

    function backButtonAction() {

        // If the save is enabled show a sweet alert to confirm the back action
        if (UIElements.btnSave.prop('disabled') === false) {

            Swal.fire({
                title: "Are you sure?",
                text: "You have unsaved changes.",
                showCancelButton: true,
                confirmButtonText: 'Yes',
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-secondary'
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    location.href = '/ApplicationForms';
                }
                else {
                    return;
                }
            });
        } else {
            location.href = '/ApplicationForms';
        }
    }

    function enableSaveButton() {
        if (UIElements.hasEditPermission.val() === 'True') {
            UIElements.btnSave.prop('disabled', false);
        }
    }
});

