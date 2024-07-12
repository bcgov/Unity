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
        viewUrl: 'Approve/ApproveApplicationsModal'
    });
    let dontApproveApplicationsModal = new abp.ModalManager({
        viewUrl: 'Approve/ApproveApplicationsModal'
    });

    let tagApplicationModal = new abp.ModalManager({
        viewUrl: 'ApplicationTags/ApplicationTagsSelectionModal',
    });

    let applicationPaymentRequestModal = new abp.ModalManager({
        viewUrl: 'PaymentRequests/CreatePaymentRequests',
    });

    tagApplicationModal.onOpen(function () {
        let tagInput = new TagsInput({
            selector: 'SelectedTags',
            duplicate: false,
            max: 50
        });
        let suggestionsArray = [];
        let uncommonTags = $('#UncommonTags').val();
        let commonTags = $('#CommonTags').val();
        let allTags = $('#AllTags').val();
        if (allTags) {
            suggestionsArray = allTags.split(',');
        }
        tagInput.setSuggestions(suggestionsArray);

        let tagInputArray = [];

        if (uncommonTags && uncommonTags != null) {
            tagInputArray.push({ text: 'Uncommon Tags', class: 'tags-uncommon', id: 0 })

        }
        const commonTagsArray = commonTags.split(',');
        if (commonTags && commonTagsArray.length) {

            if (commonTagsArray.length) {

                commonTagsArray.forEach(function (item, index) {

                    tagInputArray.push({ text: item, class: 'tags-common', id: index + 1 })
                });

            }
        }
        tagInput.addData(tagInputArray);


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
    approveApplicationsModal.onResult(function () {
        abp.notify.success(
            'The application/s has been successfully approved',
            'Approve Applications'
        );
        PubSub.publish("refresh_application_list");
    });
    dontApproveApplicationsModal.onResult(function () {
        abp.notify.success(
            'The application/s has now been disapproved',
            'Not Approve Applications'
        );
        PubSub.publish("refresh_application_list");
    });
    approveApplicationsModal.onClose(function () {
        PubSub.publish("refresh_application_list");
    });
    dontApproveApplicationsModal.onClose(function () {
        PubSub.publish("refresh_application_list");
    });

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
    $('#approveApplications').click(function () {
        approveApplicationsModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
            operation: 'GRANT_APPROVED',
            message: 'Are you sure you want to approve the selected application/s?',
            title: 'Approve Applications',
        });
    });
    $('#dontApproveApplications').click(function () {
        dontApproveApplicationsModal.open({
            applicationIds: JSON.stringify(selectedApplicationIds),
            operation: 'GRANT_NOT_APPROVED',
            message: 'Are you sure you want to disapprove the selected application/s?',
            title: 'Not Approve Applications',
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
});

