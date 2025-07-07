$(function () {
    let selectedPaymentIds = [];
    let tagPaymentModal = new abp.ModalManager({
        viewUrl: 'PaymentTags/PaymentTagsSelectionModal',
    });

    tagPaymentModal.onOpen(function () {
        let tagInput = new TagsInput({
            selector: 'SelectedTags',
            duplicate: false,
            max: 50
        });
        let suggestionsArray = [];
        let uncommonTags = JSON.parse($('#UncommonTags').val());
        let commonTags = JSON.parse($('#CommonTags').val());
        let allTags = JSON.parse($('#AllTags').val());
        if (allTags) {
            suggestionsArray = allTags;
        }
        tagInput.setSuggestions(suggestionsArray);

        let tagInputArray = [];

        if (uncommonTags && uncommonTags.length != 0) {
            tagInputArray.push({ tagId: '00000000-0000-0000-0000-000000000000', Name: 'Uncommon Tags', class: 'tags-uncommon', Id: '00000000-0000-0000-0000-000000000000' })

        }
        if (commonTags && commonTags.length) {
            commonTags.forEach(function (item, index) {

                tagInputArray.push({ tagId: item.Id, Name: item.Name, class: 'tags-common', Id: item.Id })
            });
        }
        tagInput.addData(tagInputArray);
    });

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
            $('.action-bar').addClass('disabled');
            $('#tagPayment').prop('disabled', true); 
        }
        else {
            $('*[data-selector="batch-payment-table-actions"]').prop('disabled', false);
            $('*[data-selector="batch-payment-table-actions"]').removeClass('action-bar-btn-unavailable');
            $('.action-bar').addClass('active');
            $('#tagPayment').removeClass('disabled');
            $('#tagPayment').prop('disabled', false);

        }
    }

    $('#tagPayment').click(function () {
        tagPaymentModal.open({
            paymentRequestIds: JSON.stringify(selectedPaymentIds),
            actionType: 'Add'
        });
    });


    tagPaymentModal.onResult(function () {
        abp.notify.success(
            'The payment tags have been successfully updated.',
            'Payment Tags'
        );
        PubSub.publish("refresh_payment_list");
    });

});

