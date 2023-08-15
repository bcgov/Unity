$(function () {
    var assignApplicationModal = new abp.ModalManager({
        viewUrl: 'AssigneeSelection/AssigneeSelectionModal'
    });
    var statusUpdateModal = new abp.ModalManager({
        viewUrl: 'StatusUpdate/StatusUpdateModal'
    });



    $('#assignApplication').click(function () {
        assignApplicationModal.open({
            applicationIds: JSON.stringify(['1525df4d-fcc0-4ad0-9ae4-a0f49cfe8c02']),
        });
    });

    $('#statusUpdate').click(function () {
        statusUpdateModal.open({
            applicationIds: JSON.stringify(['1525df4d-fcc0-4ad0-9ae4-a0f49cfe8c02']),
        });
    });


});