let createPaymentNumberFormatter = createNumberFormatter();

function removeApplicationPaymentRequest(applicationId) {
    let $container = $('#' + applicationId);
    $container.remove();

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

    // Always recalculate the total after removal
    calculateTotalAmount();
}

function closePaymentModal() {
    $('#payment-modal').modal('hide');
}

function checkMaxValueRequest(applicationId, input, amountRemaining) {
    let enteredValue = parseFloat(input.value.replace(/,/g, ""));
    let remainingErrorId = "#column_" + applicationId + "_remaining_error";
    if (amountRemaining < enteredValue) {
        $(remainingErrorId).css("display", "block");
    } else {
        $(remainingErrorId).css("display", "none");
    }

    // Update the total amount after checking the value
    calculateTotalAmount();
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

function calculateTotalAmount() {
    let total = 0;
    $('.amount').each(function () {
        let value = parseFloat($(this).val().replace(/,/g, '')) || 0;
        total += value;
    });
 
    let totalFormatted = createPaymentNumberFormatter.format(total);
   $('#TotalAmount').val(totalFormatted);
}
