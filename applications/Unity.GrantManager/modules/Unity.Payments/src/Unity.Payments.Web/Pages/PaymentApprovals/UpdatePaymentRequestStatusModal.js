
function removeApplicationPayment(applicationId,groupId) {
    $('#' + applicationId).remove();
    let applicationCount = $('#ApplicationCount').val();
    let groupCount = $(`#${groupId}_count`).val();
    $(`#${groupId}_count`).val(groupCount - 1);
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
    if (groupCount - 1 == 0) {
        
        $(`#${groupId}_container .payment-status-transition`).css("display", "none");
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

function submitPaymentApprovals() {
    // check for error class divs

        $('#paymentRequestStatus').submit();
    
};

function getStatusText(data) {
    switch (data) {

        case "L1Pending":
            return "L1 Pending";

        case "L1Approved":
            return "L1 Approved";

        case "L1Declined":
            return "L1 Declined";

        case "L2Pending":
            return "L2 Pending";

        case "L2Approved":
            return "L2 Approved";

        case "L2Declined":
            return "L2 Declined";

        case "L3Pending":
            return "L3 Pending";

        case "L3Approved":
            return "L3 Approved";

        case "L3Declined":
            return "L3 Declined";

        case "Submitted":
            return "Submitted";

        case "Paid":
            return "Paid";

        case "PaymentFailed":
            return "Payment Failed"


        default:
            return "Created";
    }
}




