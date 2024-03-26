
function removeApplicationPayment(applicationId) {
    $('#' + applicationId).remove();
    if (!$('div.single-payment').length) {
        $('#no-payment-msg').css("display", "block");
        $("#payment-modal").find('button[type="submit"]').prop("disabled", true);
    }
    else {
        $('#no-payment-msg').css("display", "none");
    }
}

function closePaymentModal() {

    $('#payment-modal').modal('hide');
}