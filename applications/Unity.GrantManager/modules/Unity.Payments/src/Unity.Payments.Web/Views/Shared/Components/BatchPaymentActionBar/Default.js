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
        if (selectedBatchPaymentIds.length == 1) {
            $('#viewBatchPaymentDetails').prop('disabled', false);
        }
        else {
            $('#viewBatchPaymentDetails').prop('disabled', true);

        }
    }

});

