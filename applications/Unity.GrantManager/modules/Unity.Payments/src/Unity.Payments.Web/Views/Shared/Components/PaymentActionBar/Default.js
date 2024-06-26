$(function () {
    let selectedPaymentIds = [];
    
  

    PubSub.subscribe("select_batchpayment_application", (msg, data) => {
        selectedPaymentIds.push(data.id);
        manageActionButtons();
    });

    PubSub.subscribe("deselect_batchpayment_application", (msg, data) => {
        if (data === "reset_data") {
            selectedPaymentIds = [];
        } else {
            selectedPaymentIds = selectedPaymentIds.filter(item => item !== data.id);
        }
        manageActionButtons();
    });

    function manageActionButtons() {
        if (selectedPaymentIds.length == 0) {
            $('*[data-selector="batch-payment-table-actions"]').prop('disabled', true);
            $('*[data-selector="batch-payment-table-actions"]').addClass('action-bar-btn-unavailable');
            $('.action-bar').removeClass('active');


        }
        else {
            $('*[data-selector="batch-payment-table-actions"]').prop('disabled', false);
            $('*[data-selector="batch-payment-table-actions"]').removeClass('action-bar-btn-unavailable');
            $('.action-bar').addClass('active');
         
        }
    }

});

