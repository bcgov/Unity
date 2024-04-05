
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
        $("#payment-modal").find('button[type="submit"]').prop("disabled", true);
    }
    else {
        $('#no-payment-msg').css("display", "none");
    }
}

function closePaymentModal() {

    $('#payment-modal').modal('hide');
}

function checkMaxValue(applicationId,input) {

    let maxValue = $('#PaymentThreshold').val();
    let applicationCount = $('#ApplicationCount').val();
    let enteredValue = parseFloat(input.value);
    if (applicationCount > 1 && maxValue) {
        let errorId = "#" + applicationId + "_maxerror";
        if (enteredValue > maxValue) {
            $(errorId).css("display", "block");
        } else {
            $(errorId).css("display", "none");
        }
    }
   
}
