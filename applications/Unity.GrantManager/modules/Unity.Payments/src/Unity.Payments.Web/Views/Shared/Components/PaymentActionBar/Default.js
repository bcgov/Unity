$(function () {
    let selectedPaymentIds = [];
    let tagPaymentModal = new abp.ModalManager({
        viewUrl: 'PaymentTags/PaymentTagsSelectionModal',
    });

    tagPaymentModal.onOpen(async function () {
        let tagInput = new TagsInput({
            selector: 'SelectedTags',
            duplicate: false,
            max: 50
        });
        let selectedIds = $('#SelectedPaymentRequestIds').val();
        let paymentRequestIds = JSON.parse(selectedIds);
        let cacheKey = $('#CacheKey').val();

        if (!paymentRequestIds || paymentRequestIds.length === 0) return;
        if (!cacheKey) {
            console.error("Cache key is missing");
            abp.notify.error('Failed to load payment tags. Please try again.');
            return;
        }

        try {
            let commonTags = [];
            let uncommonTags = [];
            let allTags = [];
            let groupedTags = {};


            allTags = await unity.grantManager.globalTag.tags.getList();

            // Use cache key to avoid URL length limits with many payment IDs
            let tags = await unity.payments.paymentTags.paymentTag.getListWithCacheKey(cacheKey);


            tags.forEach(function (item) {
                if (!item.tag) return;
                let paymentId = item.paymentRequestId;
                if (!groupedTags[paymentId]) {
                    groupedTags[paymentId] = [];
                }

                let exists = groupedTags[paymentId].some(t => t.id === item.tag.id);
                if (!exists) {
                    groupedTags[paymentId].push(item.tag);
                }
            });

            paymentRequestIds.forEach(function (id) {
                if (!groupedTags.hasOwnProperty(id)) {
                    groupedTags[id] = [];
                }
            });


            let groupedValues = Object.values(groupedTags);
            if (groupedValues.length > 0) {
                commonTags = groupedValues.reduce(function (prev, next) {
                    return prev.filter(p => next.some(n => n.id === p.id));
                });
            }
            let alltags = Object.entries(groupedTags).map(([paymentId, tagList]) => {
                let uncommon = tagList.filter(tag => !commonTags.some(ct => ct.id === tag.id));

                return {
                    paymentRequestId : paymentId,
                    commonTags: [...commonTags].sort((a, b) => a.name.localeCompare(b.name)),
                    uncommonTags: uncommon.sort((a, b) => a.name.localeCompare(b.name))
                };
            });


            $('#TagsJson').val(JSON.stringify(alltags));

            let tagInputArray = [];


            Object.entries(groupedTags).forEach(function ([paymentId, tagList]) {
                let uncommon = tagList.filter(tag => !commonTags.some(ct => ct.id === tag.id));
                uncommonTags = uncommonTags.concat(uncommon);


            });


            if (uncommonTags.length > 0) {
                tagInputArray.unshift({
                    tagId: '00000000-0000-0000-0000-000000000000',
                    name: 'Uncommon Tags',
                    class: 'tags-uncommon',
                    id: '00000000-0000-0000-0000-000000000000'
                });
            }


            if (commonTags.length > 0) {
                commonTags.forEach(function (tag) {
                    tagInputArray.push({
                        tagId: tag.id,
                        name: tag.name,
                        class: 'tags-common',
                        id: tag.id
                    });
                });
            }

            tagInput.setSuggestions(
                (allTags || []).filter((value, index, self) =>
                    index === self.findIndex(t => t.id === value.id)
                ).sort((a, b) => a.name.localeCompare(b.name))
            );

            tagInput.addData(tagInputArray);
        } catch (error) {
            console.error("Error loading tag select list", error);
        }
        
    });

    PubSub.subscribe("select_batchpayment_application", (msg, data) => {
        if (!selectedPaymentIds.includes(data.id)) {
            selectedPaymentIds.push(data.id);
        }
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
        // Store payment IDs in distributed cache to avoid URL length limits
        unity.payments.paymentRequests.paymentBulkActions
            .storePaymentIds({ paymentRequestIds: selectedPaymentIds })
            .then(function(response) {
                tagPaymentModal.open({
                    cacheKey: response.cacheKey,
                    actionType: 'Add'
                });
            })
            .catch(function(error) {
                abp.notify.error('Failed to prepare tag selection. Please try again.');
                console.error('Error storing payment IDs:', error);
            });
    });


    tagPaymentModal.onResult(function () {
        abp.notify.success(
            'The payment tags have been successfully updated.',
            'Payment Tags'
        );
        selectedPaymentIds = [];
        PubSub.publish("refresh_payment_list");
    });

});

