
function removeApplicationPayment(applicationId) {
    $('#' + applicationId).remove();
    let applicationCount = $('#ApplicationCount').val();
    $('#ApplicationCount').val(applicationCount - 1);
    if ((applicationCount - 1) == 1) {
        $('.max-error').css("display", "none");
        $('.payment-divider').css("display", "none");
    }
    if (!$('div.single-payment').length) {
        $('#no-payment-msg').css("display", "block");
        $("#payment-modal").find('#btnSubmitPayment').prop("disabled", true);
    }
    else {
        $('#no-payment-msg').css("display", "none");
    }
}

function closePaymentModal() {
    $('#payment-modal').modal('hide');
}

function checkMaxValue(applicationId, input, amountRemaining) {
    let enteredValue = parseFloat(input.value.replace(/,/g, ""));
    let remainingErrorId = "#column_" + applicationId + "_remaining_error";
    if (amountRemaining < enteredValue) {
        $(remainingErrorId).css("display", "block");
    } else {
        $(remainingErrorId).css("display", "none");
    }
}

function submitPayments() {
    // check for error class divs
    let validationFailed = $(".payment-error-column:visible").length > 0;

    if (validationFailed) {
        abp.notify.error(
            '',
            'There are payment requests that are in error please remove or fix them before submitting.'
        );
        return false;
    } else {
        $('#paymentform').submit();
    }
};


