$(function () {
    var selectedApplicationIds = decodeURIComponent($("#DetailsViewApplicationId").val());    
    
    var approveApplicationsModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });
    var dontApproveApplicationsModal = new abp.ModalManager({
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

    var startAdjudicationModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    startAdjudicationModal.onResult(function () {
        abp.notify.success(
            'Adjudication is now started for this application',
            'Start Adjudication'
        );
    });

    $('#startAdjudication').click(function () {
        startAdjudicationModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'UNDER_ADJUDICATION',
            message: 'Are you sure you want to start adjudication for this application?',
            title: 'Start Adjudication',
        });
    });

    var completeAdjudicationModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    completeAdjudicationModal.onResult(function () {
        abp.notify.success(
            'Adjudication is now completed for this application',
            'Completed Adjudication'
        );
    });

    $('#completeAdjudication').click(function () {
        completeAdjudicationModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'ADJUDICATION_COMPLETED',
            message: 'Are you sure you want to complete adjudication for this application?',
            title: 'Complete Adjudication',
        });
    });
        
});