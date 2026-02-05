$(function () {
    const FormHierarchyType = {
        None: 0,
        Parent: 1,
        Child: 2
    };
    const parentFormPageSize = 20;
    const l = abp.localization.getResource('GrantManager');

    const UIElements = {
        btnSave: $('#btn-save-payment-configuration'),
        btnBack: $('#btn-back-payment-configuration'),
        appFormId: $('#applicationFormId'),
        accountCode: $('#AccountCode'),
        preventPayment: $('#PreventAutomaticPaymentToCAS'),
        payable: $('#Payable'),
        hasEditPermission: $('#HasEditFormPaymentConfiguration'),
        paymentApprovalThreshold: $('#PaymentApprovalThreshold'),
        paymentThresholdForm: $('#PaymentThresholdForm'),
        defaultPaymentGroup: $('#DefaultPaymentGroup'),
        defaultPaymentGroupRow: $('#default-payment-group-row'),
        formHierarchySection: $('#form-hierarchy-section'),
        formHierarchy: $('#FormHierarchy'),
        parentFormColumn: $('#parent-form-column'),
        parentFormSelect: $('#ParentForm')
    };

    init();

    function init() {
        bindUIEvents();
        initializeParentFormLookup();
        toggleFormHierarchySection();
        toggleParentFormSection();
        toggleDefaultPaymentGroupRow(UIElements.payable.is(':checked'));

        toastr.options.positionClass = 'toast-top-center';
        UIElements.btnSave.prop('disabled', true);
    }

    function bindUIEvents() {
        UIElements.accountCode.on('change', enableSaveButton);
        UIElements.preventPayment.on('change', enableSaveButton);
        UIElements.payable.on('change', function () {
            toggleFormHierarchySection();
            toggleDefaultPaymentGroupRow(UIElements.payable.is(':checked'));
            enableSaveButton();
        });
        UIElements.paymentApprovalThreshold.on('change', enableSaveButton);
        UIElements.formHierarchy.on('change', function () {
            toggleParentFormSection();
            enableSaveButton();
        });
        UIElements.defaultPaymentGroup.on('change', enableSaveButton);

        UIElements.btnSave.on('click', saveButtonAction);
        UIElements.btnBack.on('click', backButtonAction);

        UIElements.paymentApprovalThreshold.on('keypress', preventNegativeKeyPress);
        UIElements.paymentApprovalThreshold.on('input', preventDecimalKeyPress);
    }

    function initializeParentFormLookup() {
        UIElements.parentFormSelect.select2({
            width: '100%',
            allowClear: true,
            placeholder: UIElements.parentFormSelect.data('placeholder') || 'Please choose...',
            minimumInputLength: 2,
            ajax: {
                transport: function (params, success, failure) {
                    const currentPage = params.data.page || 1;
                    unity.grantManager.applicationForms.applicationForm.getParentFormLookup({
                        filter: params.data.term || '',
                        skipCount: (currentPage - 1) * parentFormPageSize,
                        maxResultCount: parentFormPageSize,
                        excludeFormId: UIElements.appFormId.val()
                    }).then(success).catch(failure);
                },
                delay: 300,
                processResults: function (data, params) {
                    params.page = params.page || 1;
                    const items = (data.items || []).map(function (item) {
                        const displayText = item.category
                            ? `${item.applicationFormName} - ${item.category}`
                            : item.applicationFormName;
                        return {
                            id: item.applicationFormId,
                            text: displayText
                        };
                    });

                    return {
                        results: items,
                        pagination: {
                            more: (params.page * parentFormPageSize) < (data.totalCount || 0)
                        }
                    };
                }
            }
        });

        UIElements.parentFormSelect.on('select2:select', function (e) {
            enableSaveButton();
        });

        UIElements.parentFormSelect.on('select2:clear', function () {
            enableSaveButton();
        });

        UIElements.parentFormSelect.on('change', enableSaveButton);
    }

    function toggleFormHierarchySection() {
        const isPayable = UIElements.payable.is(':checked');
        if (isPayable) {
            UIElements.formHierarchySection.removeClass('d-none');
        } else {
            UIElements.formHierarchySection.addClass('d-none');
            UIElements.formHierarchy.val('').trigger('change');
            resetParentFormSelection();
        }
        toggleParentFormSection();
    }

    function toggleDefaultPaymentGroupRow(isPayable) {
        if (isPayable) {
            UIElements.defaultPaymentGroupRow.removeClass('d-none');
            if (!UIElements.defaultPaymentGroup.val()) {
                UIElements.defaultPaymentGroup.val('1');
            }
        } else {
            UIElements.defaultPaymentGroupRow.addClass('d-none');
            UIElements.defaultPaymentGroup.val('');
        }
    }

    function toggleParentFormSection() {
        const hierarchyValue = UIElements.formHierarchy.val();
        if (hierarchyValue === String(FormHierarchyType.Child)) {
            UIElements.parentFormColumn.removeClass('d-none');
        } else {
            UIElements.parentFormColumn.addClass('d-none');
            resetParentFormSelection();
        }
    }

    function resetParentFormSelection() {
        UIElements.parentFormSelect.val(null).trigger('change');
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

    function validateFormHierarchyBeforeSave() {
        if (!UIElements.payable.is(':checked')) {
            return true;
        }

        const hierarchyValue = UIElements.formHierarchy.val();
        if (!hierarchyValue) {
            abp.notify.error(l('GrantManager:PayableFormRequiresHierarchy'));
            return false;
        }

        if (hierarchyValue === String(FormHierarchyType.Child)) {
            if (!UIElements.parentFormSelect.val()) {
                abp.notify.error(l('GrantManager:ChildFormRequiresParentForm'));
                return false;
            }
        }

        return true;
    }

    function saveButtonAction() {
        if (!validateFormHierarchyBeforeSave()) {
            return;
        }

        const hierarchyValue = UIElements.formHierarchy.val();
        const formHierarchy = hierarchyValue ? parseInt(hierarchyValue, 10) : null;
        const parentFormId = UIElements.parentFormSelect.val();
        const defaultPaymentGroupValue = UIElements.payable.is(':checked')
            ? (UIElements.defaultPaymentGroup.val() || '1')
            : null;

        unity.grantManager.applicationForms.applicationForm.savePaymentConfiguration(
            {
                accountCodingId: UIElements.accountCode.val(),
                applicationFormId: UIElements.appFormId.val(),
                preventPayment: UIElements.preventPayment.is(':checked'),
                payable: UIElements.payable.is(':checked'),
                paymentApprovalThreshold: UIElements.paymentApprovalThreshold.val() === '' ? null : UIElements.paymentApprovalThreshold.val(),
                formHierarchy: Number.isNaN(formHierarchy) ? null : formHierarchy,
                parentFormId: parentFormId || null,
                defaultPaymentGroup: defaultPaymentGroupValue
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
