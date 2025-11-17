$(function () {
    let viewRoleAssignmentModal = new abp.ModalManager({
        viewUrl: 'Configuration/ViewRoleAssignmentModal'
    });

    // Handle assign role to views button
    $('#assignRoleToViewsButton').on("click", function () {
        const viewRole = $('#Configuration_ViewRole').val();
        if (!viewRole || viewRole.trim() === '') {
            abp.notify.warn('Please configure a View Role before assigning it to views.');
            return;
        }
        viewRoleAssignmentModal.open();
    });
    
    viewRoleAssignmentModal.onResult(function () {
        abp.notify.success('Role assignment jobs have been queued successfully.');
    });

    // Handle configuration form submission
    $('#ReportingConfigurationForm').on('submit', function (e) {
        e.preventDefault();
        
        const form = $(this);
        const formData = new FormData(this);
        const saveButton = $('#saveConfigButton');
        const originalText = saveButton.text();
        
        saveButton.prop('disabled', true).text('Saving...');
        
        abp.ajax({
            url: form.attr('action') || window.location.pathname,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        }).done(function() {
            abp.notify.success('Configuration saved successfully');
        }).always(function() {
            saveButton.prop('disabled', false).text(originalText);
        });
    });
});
