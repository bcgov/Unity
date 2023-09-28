$(function () {
    let selectedApplicationIds = decodeURIComponent($("#DetailsViewApplicationId").val());    
    
    let approveApplicationsModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    let dontApproveApplicationsModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
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
    $('#dontApproveApplicationsDetails').click(function () {
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

    $('#completeAssessment').click(function () {
        completeAssessmentModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'ASSESSMENT_COMPLETED',
            message: 'Are you sure you want to complete assessment for this application?',
            title: 'Complete Assessment',
        });
    });

    $('#addReview').click(function () {
        console.log("Add review")
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');

        try {
            unity.grantManager.assessments.assessment.create({ "applicationId": applicationId, "approvalRecommended": null, "startDate" : new Date() }, {})
                .done(function (data) {
                    PubSub.publish('add_review');
                    PubSub.publish('refresh_review_list',data.id);
                });

        } catch (error) {
            console.log(error);
        }
    }); 
    
    PubSub.subscribe(
        'add_review',
        (msg, data) => {
            $('#detailsTab a[href="#nav-review-and-assessment"]').tab('show');
        }
    );
});