
function removeApplicationPayment(applicationId) {
    $.ajax({
        type: 'POST',
        url: 'Payment/CreatePaymentRequestModal?handler=RemoveItem',
        data: { applicationId: applicationId },
        success: function (response) {
           
            if (response.success) {
                let containerId = '#' + applicationId + '_container';
                $(containerId).remove();
            } else {
               
                console.error('Error occurred while removing the item.');
            }
        },
        error: function (xhr, status, error) {
          
            console.error('Error occurred while removing the item.');
        }
    });

}