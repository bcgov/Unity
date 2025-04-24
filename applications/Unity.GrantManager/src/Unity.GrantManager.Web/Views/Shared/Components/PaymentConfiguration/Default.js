$(function () {
    const UIElements = {
        btnSave: $('#btn-save-payment-configuration'),
        btnBack: $('#btn-back-payment-configuration'),
        appFormId: $('#applicationFormId').val(),
        accountCode: $('#AccountCode'),
        preventPayment: $('#PreventAutomaticPaymentToCAS'),
        payable: $('#Payable'),
        hasEditPermission: $('#HasEditFormPaymentConfiguration').val()
    };

    function bindUIEvents() {
        UIElements.accountCode.on('change', enableSaveButton);
        UIElements.preventPayment.on('change', enableSaveButton);
        UIElements.payable.on('change', enableSaveButton);
        
        UIElements.btnSave.on('click', saveButtonAction);
        UIElements.btnBack.on('click', backButtonAction);
    }

    init();

    function init() {
        bindUIEvents();
        console.log('wtf');
        toastr.options.positionClass = 'toast-top-center';
        UIElements.btnSave.prop('disabled', true);
    }

    function saveButtonAction() {
        let applicationFormId = UIElements.appFormId;
        let accountCodingId = UIElements.accountCode.val();
        let preventPayment = UIElements.preventPayment.is(':checked');
        let payable = UIElements.payable.is(':checked');

        unity.grantManager.applicationForms.applicationForm.savePaymentConfiguration(
        {
            accountCodingId: accountCodingId,
            applicationFormId: applicationFormId,
            preventPayment: preventPayment,
            payable: payable
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
                confirmButtonText:'Yes',
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
        if (UIElements.hasEditPermission === 'True') {
            UIElements.btnSave.prop('disabled', false);
        }
    }
});

