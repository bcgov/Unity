$(function () {
    let selectedApplicationIds = decodeURIComponent($("#DetailsViewApplicationId").val());    
    
    let approveApplicationsModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    let dontApproveApplicationsModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });       

    let tagApplicationModal = new abp.ModalManager({
        viewUrl: '../ApplicationTags/ApplicationTagsSelectionModal',

    });
    
    approveApplicationsModal.onResult(function () {
        abp.notify.success(
            'This application has been successfully approved',
            'Approve Application'
        );        
    });
    dontApproveApplicationsModal.onResult(function () {
        abp.notify.success(
            'This application has now been disapproved',
            'Not Approve Application'
        );        
    });
           
    $('#approveApplicationsDetails').click(function () {
        approveApplicationsModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'GRANT_APPROVED',
            message: 'Are you sure you want to approve this application?',
            title: 'Approve Applications',
        });
    });
    $('#disApproveApplicationsDetails').click(function () {
        dontApproveApplicationsModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'GRANT_NOT_APPROVED',
            message: 'Are you sure you want to disapprove this application?', 
            title: 'Not Approve Applications',
        });
    });

    let startAssessmentModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    startAssessmentModal.onResult(function () {
        abp.notify.success(
            'Assessment is now started for this application',
            'Start Assessment'
        );
    });

    $('#startAssessment').click(function () {
        startAssessmentModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'UNDER_ASSESSMENT',
            message: 'Are you sure you want to start assessment for this application?',
            title: 'Start Assessment',
        });
    });

    let completeAssessmentModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    completeAssessmentModal.onResult(function () {
        abp.notify.success(
            'Assessment is now completed for this application',
            'Completed Assessment'
        );
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
            tagInputArray.push({ text: 'Uncommon Tags', class: 'uncommon', id: 0 })

        }
        const commonTagsArray = commonTags.split(',');
        if (commonTags && commonTagsArray.length) {

            if (commonTagsArray.length) {

                commonTagsArray.forEach(function (item, index) {

                    tagInputArray.push({ text: item, class: 'common', id: index + 1 })
                });

            }
        }
        tagInput.addData(tagInputArray);
    });
    tagApplicationModal.onResult(function () {
        abp.notify.success(
            'The application tags has been successfully updated.',
            'Application Tags'
        );
        PubSub.publish("ApplicationTags_refresh");

    });

    $('#completeAssessment').click(function () {
        completeAssessmentModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'ASSESSMENT_COMPLETED',
            message: 'Are you sure you want to complete assessment for this application?',
            title: 'Complete Assessment',
        });
    });

    $('#tagApplication').click(function () {
        tagApplicationModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            actionType: 'Add'
        });
    });
});