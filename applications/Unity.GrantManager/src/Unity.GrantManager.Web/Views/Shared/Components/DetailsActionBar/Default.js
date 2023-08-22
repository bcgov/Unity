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
            applicationIds: selectedApplicationIds,
            operation: 'GRANT_APPROVED',
            message: 'Are you sure you want to approve this application?',
            title: 'Approve Applications',
        });
    });
    $('#dontApproveApplicationsDetails').click(function () {
        dontApproveApplicationsModal.open({
            applicationIds: selectedApplicationIds,
            operation: 'GRANT_NOT_APPROVED',
            message: 'Are you sure you want to disapprove this application?', 
            title: 'Not Approve Applications',
        });
    });
    
        
});