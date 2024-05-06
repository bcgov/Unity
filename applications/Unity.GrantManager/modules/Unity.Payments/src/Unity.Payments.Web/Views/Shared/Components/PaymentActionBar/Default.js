$(function () {
    let selectedBatchPaymentIds = [];
    $("#btn-toggle-filter").on("click", function () {
        // Toggle the visibility of the div
        $(".tr-toggle-filter").toggle();
    });
    $('#viewBatchPaymentDetails').click(function () {
        location.href =
            '/payments?BatchPaymentId=' +
        selectedBatchPaymentIds[0];
    });

    PubSub.subscribe("select_batchpayment_application", (msg, data) => {
        selectedBatchPaymentIds.push(data.id);
        manageActionButtons();
    });

    PubSub.subscribe("deselect_batchpayment_application", (msg, data) => {
        if (data === "reset_data") {
            selectedBatchPaymentIds = [];
        } else {
            selectedBatchPaymentIds = selectedBatchPaymentIds.filter(item => item !== data.id);
        }
        manageActionButtons();
    });

    function manageActionButtons() {
        if (selectedBatchPaymentIds.length == 0) {
            $('*[data-selector="batch-payment-table-actions"]').prop('disabled', true);
            $('*[data-selector="batch-payment-table-actions"]').addClass('action-bar-btn-unavailable');
            $('.action-bar').removeClass('active');


        }
        else {
            $('*[data-selector="batch-payment-table-actions"]').prop('disabled', false);
            $('*[data-selector="batch-payment-table-actions"]').removeClass('action-bar-btn-unavailable');
            $('.action-bar').addClass('active');

            $('#viewBatchPaymentDetails').addClass('action-bar-btn-unavailable');


            if (selectedBatchPaymentIds.length == 1) {
                $('#viewBatchPaymentDetails').removeClass('action-bar-btn-unavailable');



            }
        }
    }

});

