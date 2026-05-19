function removeHistoricalPaymentRequest(applicationId) {
    let $container = $('#' + applicationId);
    let $parentGroup = $container.closest('.parent-child-group');
    $container.remove();

    if ($('div.single-payment').length) {
        $('#no-payment-msg').css('display', 'none');
    } else {
        $('#no-payment-msg').css('display', 'block');
        $('#historical-payment-modal').find('#btnSubmitHistoricalPayment').prop('disabled', true);
    }

    if ($parentGroup.length && $parentGroup.find('.single-payment').length === 0) {
        $parentGroup.remove();
    }

    validateAllHistoricalPaymentAmounts();
}

function closeHistoricalPaymentModal() {
    $('#historical-payment-modal').modal('hide');
}

function checkHistoricalMaxValueRequest(applicationId, input, amountRemaining) {
    if (isPartOfParentChildGroup(applicationId)) {
        validateParentChildAmounts(applicationId);
    } else {
        let enteredValue = Number.parseFloat(input.value.replaceAll(',', ''));
        let remainingErrorId = '#error_column_' + applicationId;
        if (amountRemaining < enteredValue) {
            $(remainingErrorId).css('display', 'block');
        } else {
            $(remainingErrorId).css('display', 'none');
        }
    }
}

function validateAllHistoricalPaymentAmounts() {
    $('input[name*=".CorrelationId"]').each(function () {
        let correlationId = $(this).val();
        let index = getIndexByCorrelationId(correlationId);
        let isPartOfGroup =
            $(`input[name="ApplicationPaymentRequestForm[${index}].IsPartOfParentChildGroup"]`).val() === 'True';

        if (isPartOfGroup) {
            validateParentChildAmounts(correlationId);
        } else {
            let amountInput = $(`input[name="ApplicationPaymentRequestForm[${index}].Amount"]`);
            let remainingAmount = Number.parseFloat(
                $(`input[name="ApplicationPaymentRequestForm[${index}].RemainingAmount"]`).val()
            );
            let enteredValue = Number.parseFloat(amountInput.val().replaceAll(',', '')) || 0;
            let remainingErrorId = `#error_column_${correlationId}`;

            if (enteredValue > remainingAmount) {
                $(remainingErrorId).css('display', 'block');
            } else {
                $(remainingErrorId).css('display', 'none');
            }
        }
    });
}

function submitHistoricalPayments() {
    validateAllHistoricalPaymentAmounts();

    let validationFailed = $('.payment-error-column:visible').length > 0;

    if (validationFailed) {
        abp.notify.error(
            '',
            'There are payment requests that are in error. Please remove or fix them before submitting.'
        );
        return false;
    } else {
        let form = document.getElementById('historicalpaymentform');
        if (!form.reportValidity()) {
            return false;
        }
        $('#historicalpaymentform').submit();
    }
}

$(function () {
    validateAllHistoricalPaymentAmounts();
});
