$(function () {
    let selectedPaymentIds = [];
    let tagPaymentModal = new abp.ModalManager({
        viewUrl: 'PaymentTags/PaymentTagsSelectionModal',
    });

    tagPaymentModal.onOpen(async function () {
        let tagInput = new PaymentTagsInput({
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

            // Helper functions to reduce nesting depth
            function hasMatchingId(tagA, tagB) {
                return tagA.id === tagB.id;
            }

            function tagExistsInList(tag, tagList) {
                return tagList.some(t => hasMatchingId(t, tag));
            }

            function filterCommonTags(prev, next) {
                return prev.filter(p => tagExistsInList(p, next));
            }

            function getUncommonTags(tagList) {
                return tagList.filter(tag => !tagExistsInList(tag, commonTags));
            }

            function sortByName(a, b) {
                return a.name.localeCompare(b.name);
            }

            let groupedValues = Object.values(groupedTags);
            if (groupedValues.length > 0) {
                commonTags = groupedValues.reduce((prev, next) => filterCommonTags(prev, next), groupedValues[0]);
            }
            
            let allTagEntries = Object.entries(groupedTags).map(([paymentId, tagList]) => {
                let uncommon = getUncommonTags(tagList);

                return {
                    paymentRequestId : paymentId,
                    commonTags: [...commonTags].sort(sortByName),
                    uncommonTags: uncommon.sort(sortByName)
                };
            });


            $('#TagsJson').val(JSON.stringify(allTagEntries));

            let tagInputArray = [];


            Object.entries(groupedTags).forEach(function ([paymentId, tagList]) {
                let uncommon = getUncommonTags(tagList);
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

    // Owns only the standalone TAGS button. The DataTables-driven buttons
    // (Check Status, Approve, Decline, Cancel, History) are exclusively
    // owned by checkActionButtons() in PaymentRequests/Index.js — touching
    // them here would race with that logic, since PubSub.publish() defers
    // delivery to a later tick and could re-enable/unhide a button that
    // Index.js just disabled for a selected row.
    function manageActionButtons() {
        const hasSelection = selectedPaymentIds.length > 0;

        $('#tagPayment')
            .prop('disabled', !hasSelection)
            .toggleClass('action-bar-btn-unavailable', !hasSelection)
            .toggleClass('disabled', !hasSelection);

        $('.action-bar')
            .toggleClass('active', hasSelection)
            .toggleClass('disabled', !hasSelection);
    }

    $('#tagPayment').on('click', function () {
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
        manageActionButtons();
        PubSub.publish("refresh_payment_list");
    });

    // Initialize button states
    manageActionButtons();
});

