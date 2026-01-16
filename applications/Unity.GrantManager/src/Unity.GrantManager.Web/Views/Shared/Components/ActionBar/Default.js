$(function () {
    let selectedApplicationIds = [];
    let assignApplicationModal = new abp.ModalManager({
        viewUrl: 'AssigneeSelection/AssigneeSelectionModal'
    });
    let unAssignApplicationModal = new abp.ModalManager({
        viewUrl: 'AssigneeSelection/AssigneeSelectionModal'
    });
    let statusUpdateModal = new abp.ModalManager({
        viewUrl: 'StatusUpdate/StatusUpdateModal'
    });
    let approveApplicationsModal = new abp.ModalManager({
        viewUrl: 'BulkApprovals/ApproveApplicationsModal'
    });
    let approveApplicationsSummaryModal = new abp.ModalManager({
        viewUrl: 'BulkApprovals/ApproveApplicationsSummaryModal'
    });
    let tagApplicationModal = new abp.ModalManager({
        viewUrl: 'ApplicationTags/ApplicationTagsSelectionModal',
    });
    let applicationPaymentRequestModal = new abp.ModalManager({
        viewUrl: 'PaymentRequests/CreatePaymentRequests',
    });

    // Helper functions to reduce nesting depth
    function groupTagsByApplication(tags, applicationIds) {
        let groupedTags = {};
        
        tags.forEach(function (item) {
            if (!item.tag) return;
            let appId = item.applicationId;
            if (!groupedTags[appId]) {
                groupedTags[appId] = [];
            }

            let exists = groupedTags[appId].some(t => t.id === item.tag.id);
            if (!exists) {
                groupedTags[appId].push(item.tag);
            }
        });

        applicationIds.forEach(function (id) {
            if (!groupedTags.hasOwnProperty(id)) {
                groupedTags[id] = [];
            }
        });

        return groupedTags;
    }

    // Helper function for tag comparison
    function hasMatchingTagId(tag, tagList) {
        return tagList.some(t => t.id === tag.id);
    }

    function findCommonTags(groupedTags) {
        let groupedValues = Object.values(groupedTags);
        if (groupedValues.length === 0) return [];
        
        return groupedValues.reduce(function (prev, next) {
            return prev.filter(p => hasMatchingTagId(p, next));
        });
    }

    function filterUncommonTags(tagList, commonTags) {
        return tagList.filter(tag => !commonTags.some(ct => ct.id === tag.id));
    }

    function buildAllTagsData(groupedTags, commonTags) {
        return Object.entries(groupedTags).map(function([appId, tagList]) {
            let uncommon = filterUncommonTags(tagList, commonTags);

            return {
                applicationId: appId,
                commonTags: [...commonTags].sort((a, b) => a.name.localeCompare(b.name)),
                uncommonTags: uncommon.sort((a, b) => a.name.localeCompare(b.name))
            };
        });
    }

    function collectUncommonTags(groupedTags, commonTags) {
        let uncommonTags = [];
        
        Object.entries(groupedTags).forEach(function ([appId, tagList]) {
            let uncommon = filterUncommonTags(tagList, commonTags);
            uncommonTags.push(...uncommon);
        });
        
        // Remove duplicates by filtering based on tag ID
        uncommonTags = uncommonTags.filter((tag, index, self) => 
            index === self.findIndex(t => t.id === tag.id)
        );
        
        return uncommonTags;
    }

    function buildTagInputArray(commonTags, uncommonTags) {
        let tagInputArray = [];

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

        return tagInputArray;
    }

    function setupTagInput(tagInput, allTags, tagInputArray) {
        tagInput.setSuggestions(
            (allTags || []).filter((value, index, self) =>
                index === self.findIndex(t => t.id === value.id)
            ).sort((a, b) => a.name.localeCompare(b.name))
        );

        tagInput.addData(tagInputArray);
    }

    tagApplicationModal.onOpen(async function () {
        let tagInput = new TagsInput({
            selector: 'SelectedTags',
            duplicate: false,
            max: 50
        });

        let cacheKey = $('#CacheKey').val();
        let applicationIds = [];

        if (!cacheKey) {
            console.error("Cache key is missing");
            abp.notify.error('Failed to load application tags. Please try again.');
            return;
        }

        // Retrieve application IDs from hidden field (populated by modal code-behind)
        let selectedIds = $('#SelectedApplicationIds').val();
        applicationIds = JSON.parse(selectedIds);

        if (!applicationIds || applicationIds.length === 0) return;

        try {
            let globalTags = await unity.grantManager.globalTag.tags.getList();
            let tags = await unity.grantManager.grantApplications.applicationTags.getListWithCacheKey(cacheKey);
            
            let groupedTags = groupTagsByApplication(tags, applicationIds);
            let commonTags = findCommonTags(groupedTags);
            let allTags = buildAllTagsData(groupedTags, commonTags);
            let uncommonTags = collectUncommonTags(groupedTags, commonTags);
            
            $('#TagsJson').val(JSON.stringify(allTags));
            
            let tagInputArray = buildTagInputArray(commonTags, uncommonTags);
            setupTagInput(tagInput, globalTags, tagInputArray);
        } catch (error) {
            console.error("Error loading tag select list", error);
        }
    });

    // Helper functions for assignee modal
    function parseAssigneeData(uncommonTags, commonTags, allTags) {
        let suggestionsArray = [];
        let tagInputArray = [];

        if (allTags) {
            suggestionsArray = JSON.parse(allTags);            
        }

        if (uncommonTags && uncommonTags != "[]") {
            tagInputArray.push({ 
                FullName: 'Uncommon Assignees', 
                class: 'tags-uncommon', 
                Id: 'uncommonAssignees', 
                Role: 'Various Roles' 
            });
        }

        if (commonTags && commonTags != "[]") {
            const commonTagsArray = JSON.parse(commonTags);
            if (commonTagsArray.length) {
                commonTagsArray.forEach(function (item) {
                    tagInputArray.push(item);
                });
            }
        }

        return { suggestionsArray, tagInputArray };
    }

    assignApplicationModal.onOpen(function () {
        let userTagsInput = new UserTagsInput({
            selector: 'SelectedAssignees',
            duplicate: false,
            max: 50
        });
        
        let uncommonTags = $('#UnCommonAssigneeList').val();
        let commonTags = $('#CommonAssigneeList').val();
        let allTags = $('#AllAssignees').val();
        
        let { suggestionsArray, tagInputArray } = parseAssigneeData(uncommonTags, commonTags, allTags);
        
        userTagsInput.setSuggestions(suggestionsArray);
        userTagsInput.addData(tagInputArray);
        document.getElementById("user-tags-input").setAttribute("data-touched", "false");
    });
    tagApplicationModal.onResult(function () {
        abp.notify.success(
            'The application tags have been successfully updated.',
            'Application Tags'
        );
        PubSub.publish("refresh_application_list");
    });
    assignApplicationModal.onResult(function () {
        abp.notify.success(
            'The application assignee(s) have been successfully updated.',
            'Application Assignee'
        );
        PubSub.publish("refresh_application_list");
    });

    unAssignApplicationModal.onResult(function () {
        abp.notify.success(
            'The application assignee(s) have been successfully removed.',
            'Application Assignee'
        );
        PubSub.publish("refresh_application_list");
    });

    statusUpdateModal.onResult(function () {
        abp.notify.success(
            'The application status has been successfully updated',
            'Application Status'
        );
        PubSub.publish("refresh_application_list");
    });

    // Batch Approval Start
    $('#approveApplications').on("click", function () {
        // Store application IDs in distributed cache to avoid URL length limits
        unity.grantManager.applications.applicationBulkActions
            .storeApplicationIds({ applicationIds: selectedApplicationIds })
            .then(function(response) {
                // Open modal with cache key instead of application IDs array
                approveApplicationsModal.open({
                    cacheKey: response.cacheKey
                });
            })
            .catch(function(error) {
                abp.notify.error('Failed to prepare bulk approval. Please try again.');
                console.error('Error storing application IDs:', error);
            });
    });
    approveApplicationsModal.onResult(function (_, response) {                
        let transformedFailures = response.responseText.failures.map(failure => {
            return {
                Key: failure.key,
                Value: failure.value
            };
        });
        let summaryJson = JSON.stringify(
        {
            Successes: response.responseText.successes,
            Failures: transformedFailures
        });
        approveApplicationsSummaryModal.open({ summaryJson: summaryJson });
        PubSub.publish("refresh_application_list");
    });
    // Batch Approval End

    PubSub.subscribe("select_application", (msg, data) => {
        selectedApplicationIds.push(data.id);
        manageActionButtons();
    });

    PubSub.subscribe("deselect_application", (msg, data) => {
        if (data === "reset_data") {
            selectedApplicationIds = [];
        } else {
            selectedApplicationIds = selectedApplicationIds.filter(item => item !== data.id);
        }
        manageActionButtons();
    });

    PubSub.subscribe("clear_selected_application", (msg, data) => {
        selectedApplicationIds = [];
        manageActionButtons();
    });

    $('#assignApplication').on('click', function () {
        // Store application IDs in distributed cache to avoid URL length limits
        unity.grantManager.applications.applicationBulkActions
            .storeApplicationIds({ applicationIds: selectedApplicationIds })
            .then(function(response) {
                // Open modal with cache key instead of application IDs array
                assignApplicationModal.open({
                    cacheKey: response.cacheKey,
                    actionType: 'Add'
                });
            })
            .catch(function(error) {
                abp.notify.error('Failed to prepare assignee selection. Please try again.');
                console.error('Error storing application IDs:', error);
            });
    });

    $('#unAssignApplication').on('click', function () {
        unAssignApplicationModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
            actionType: 'Remove'
        });
    });

    $('#statusUpdate').on('click', function () {
        statusUpdateModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
        });
    });

    $('#applicationLink').on('click', function () {
        const summaryCanvas = document.getElementById('applicationAsssessmentSummary');
        const rightSideCanvas = new bootstrap.Offcanvas(summaryCanvas);
        rightSideCanvas.show();
    });

    $('#externalLink').on('click', function () {
        location.href =
            '/GrantApplications/Details?ApplicationId=' +
            selectedApplicationIds[0];
    });

    let summaryWidgetManager = new abp.WidgetManager({
        wrapper: '#summaryWidgetArea',
        filterCallback: function () {
            return {
                'applicationId': selectedApplicationIds.length == 1 ? selectedApplicationIds[0] : "00000000-0000-0000-0000-000000000000",
                'isReadOnly': true
            }
        }
    });
    function manageActionButtons() {
        if (selectedApplicationIds.length == 0) {
            $('*[data-selector="applications-table-actions"]').prop('disabled', true);
            $('*[data-selector="applications-table-actions"]').addClass('action-bar-btn-unavailable');
            $('.action-bar').removeClass('active');

            const summaryCanvas = document.getElementById('applicationAsssessmentSummary');
            summaryCanvas.classList.remove('show');
        }
        else {
            $('*[data-selector="applications-table-actions"]').prop('disabled', false);
            $('*[data-selector="applications-table-actions"]').removeClass('action-bar-btn-unavailable');
            $('.action-bar').addClass('active');

            $('#externalLink').addClass('action-bar-btn-unavailable');
            $('#applicationLink').addClass('action-bar-btn-unavailable');

            if (selectedApplicationIds.length == 1) {
                $('#externalLink').removeClass('action-bar-btn-unavailable');
                $('#applicationLink').removeClass('action-bar-btn-unavailable');

                summaryWidgetManager.refresh();
            }
        }
    }


    $('#tagApplication').on('click', function () {
        // Store application IDs in distributed cache to avoid URL length limits
        unity.grantManager.applications.applicationBulkActions
            .storeApplicationIds({ applicationIds: selectedApplicationIds })
            .then(function(response) {
                tagApplicationModal.open({
                    cacheKey: response.cacheKey,
                    actionType: 'Add'
                });
            })
            .catch(function(error) {
                abp.notify.error('Failed to prepare tag selection. Please try again.');
                console.error('Error storing application IDs:', error);
            });
    });

    $('.spinner-grow').hide();

    $('#applicationPaymentRequest').on('click', function () {
        // Store application IDs in distributed cache to avoid URL length limits
        unity.grantManager.applications.applicationBulkActions
            .storeApplicationIds({ applicationIds: selectedApplicationIds })
            .then(function(response) {
                applicationPaymentRequestModal.open({
                    cacheKey: response.cacheKey
                });
            })
            .catch(function(error) {
                abp.notify.error('Failed to prepare payment request. Please try again.');
                console.error('Error storing application IDs:', error);
            });
    });

    applicationPaymentRequestModal.onResult(function () {
        abp.notify.success(
            'The application/s payment request has been successfully submitted.',
            'Payment'
        );
        PubSub.publish("refresh_application_list");        
    });

    applicationPaymentRequestModal.onOpen(function () {
        calculateTotalAmount();     
    });    
});

