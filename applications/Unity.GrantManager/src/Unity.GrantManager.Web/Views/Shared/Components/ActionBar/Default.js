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

    tagApplicationModal.onOpen(async function () {
        let tagInput = new TagsInput({
            selector: 'SelectedTags',
            duplicate: false,
            max: 50
        });
        let selectedIds = $('#SelectedApplicationIds').val(); 
        let applicationIds = JSON.parse(selectedIds);

        if (!applicationIds || applicationIds.length === 0) return;

        try {
            let commonTags = [];
            let uncommonTags = [];
            let allTags = [];
            let groupedTags = {};

           
            allTags = await unity.grantManager.globalTag.tags.getList();

            let tags = await unity.grantManager.grantApplications.applicationTags.getListWithApplicationIds(applicationIds);
            
            
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

            
            let groupedValues = Object.values(groupedTags);
            if (groupedValues.length > 0) {
                commonTags = groupedValues.reduce(function (prev, next) {
                    return prev.filter(p => next.some(n => n.id === p.id));
                });
            }

            let alltags = Object.entries(groupedTags).map(([appId, tagList]) => {
                let uncommon = tagList.filter(tag => !commonTags.some(ct => ct.id === tag.id));

                return {
                    applicationId: appId,
                    commonTags: [...commonTags].sort((a, b) => a.name.localeCompare(b.name)),
                    uncommonTags: uncommon.sort((a, b) => a.name.localeCompare(b.name))
                };
            });

           
            $('#TagsJson').val(JSON.stringify(alltags));
            let tagInputArray = [];

            
            Object.entries(groupedTags).forEach(function ([appId, tagList]) {
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

    assignApplicationModal.onOpen(function () {
        let userTagsInput = new UserTagsInput({
            selector: 'SelectedAssignees',
            duplicate: false,
            max: 50
        });
        let suggestionsArray = [];
        let uncommonTags = $('#UnCommonAssigneeList').val();
        let commonTags = $('#CommonAssigneeList').val();
        let allTags = $('#AllAssignees').val();
        if (allTags) {
            suggestionsArray = JSON.parse(allTags);
            console.log(suggestionsArray)
        }
        userTagsInput.setSuggestions(suggestionsArray);

        let tagInputArray = [];

        if (uncommonTags && uncommonTags != null && uncommonTags != "[]") {
            tagInputArray.push({ FullName: 'Uncommon Assignees', class: 'tags-uncommon', Id: 'uncommonAssignees', Role: 'Various Roles' })

        }

        if (commonTags && commonTags != null && commonTags != "[]") {
            const commonTagsArray = JSON.parse(commonTags);
            if (commonTagsArray.length) {

                commonTagsArray.forEach(function (item) {

                    tagInputArray.push(item)
                });

            }
        }
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
        approveApplicationsModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds)
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

    $('#assignApplication').click(function () {
        assignApplicationModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
            actionType: 'Add'
        });
    });

    $('#unAssignApplication').click(function () {
        unAssignApplicationModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
            actionType: 'Remove'
        });
    });

    $('#statusUpdate').click(function () {
        statusUpdateModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
        });
    });

    $('#applicationLink').click(function () {
        const summaryCanvas = document.getElementById('applicationAsssessmentSummary');
        const rightSideCanvas = new bootstrap.Offcanvas(summaryCanvas);
        rightSideCanvas.show();
    });

    $('#externalLink').click(function () {
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


    $('#tagApplication').click(function () {
        tagApplicationModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
            actionType: 'Add'
        });
    });

    $('.spinner-grow').hide();

    $('#applicationPaymentRequest').click(function () {
        applicationPaymentRequestModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
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

