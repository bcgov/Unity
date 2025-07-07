
function removeApplicationPaymentRequest(applicationId) {
    var $container = $('#' + applicationId);

    // Get the amount value inside this container before removing it
    var amountValue = $container.find('.amount').val();
    var amount = parseFloat((amountValue || "0").replace(/,/g, ''));

    // Update the total amount
    var $totalInput = $('.totalAmount');
    var currentTotal = parseFloat(($totalInput.val() || "0").replace(/,/g, '')) || 0;
    var newTotal = currentTotal - amount;
    if (newTotal < 0) newTotal = 0;
    $totalInput.val(newTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));

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