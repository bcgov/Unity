$(function () {
    let _tenantViewRoleAppService = unity.reporting.tenantViewRole;

    // Initialize DataTable for tenant view role management
    let _dataTable = $('#TenantViewRoleTable').DataTable({
        order: [[0, 'asc']], // Sort by tenant name
        processing: false,
        serverSide: false,
        paging: true,
        searching: true,
        pageLength: 25,
        autoWidth: false,
        columnDefs: [
            {
                targets: [2, 3], // Assignment Status and Actions columns
                orderable: false
            }
        ],
        dom: 'frtip', // Show filter, table, info, pagination
        language: {
            emptyTable: "No tenants found",
            search: "Search tenants:",
            lengthMenu: "Show _MENU_ tenants per page",
            info: "Showing _START_ to _END_ of _TOTAL_ tenants"
        }
    });

    // Handle save role button click
    $(document).on('click', '.save-role-btn', function () {
        const button = $(this);
        const tenantId = button.data('tenant-id');
        const row = button.closest('tr');
        const viewRoleInput = row.find('.view-role-input');
        const viewRole = viewRoleInput.val().trim();

        if (!viewRole) {
            abp.notify.warn('Please enter a view role name.');
            return;
        }

        button.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Saving...');

        _tenantViewRoleAppService.update(tenantId, { viewRole: viewRole })
            .done(function (result) {
                // Update the assignment status
                const statusCell = row.find('.assignment-status-cell');
                if (result.isAssigned) {
                    statusCell.html('<span class="badge bg-success">Assigned</span>');
                } else {
                    statusCell.html('<span class="badge bg-warning">Pending</span>');
                }
                
                abp.notify.success('View role updated successfully.');
            })
            .fail(function () {
                abp.notify.error('Failed to update view role.');
            })
            .always(function () {
                button.prop('disabled', false).html('<i class="fa fa-save"></i> Save');
            });
    });

    // Handle assign role to views button click
    $(document).on('click', '.assign-role-btn', function () {
        const button = $(this);
        const tenantId = button.data('tenant-id');
        const tenantName = button.data('tenant-name');
        const row = button.closest('tr');
        const viewRole = row.find('.view-role-input').val().trim();

        if (!viewRole) {
            abp.notify.warn('Please save a view role first before assigning it to views.');
            return;
        }

        abp.message.confirm(
            `Are you sure you want to assign the role "${viewRole}" to all existing views for tenant "${tenantName}"? This operation will run in the background.`,
            'Confirm Role Assignment',
            function (isConfirmed) {
                if (isConfirmed) {
                    button.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Assigning...');

                    _tenantViewRoleAppService.assignRoleToViews(tenantId)
                        .done(function () {
                            // Update the assignment status to processing since it's a background operation
                            const statusCell = row.find('.assignment-status-cell');
                            statusCell.html('<span class="badge bg-info">Processing</span>');
                            
                            abp.notify.success(`Role assignment jobs have been queued for tenant "${tenantName}". The process will complete in the background.`);
                        })
                        .fail(function () {
                            abp.notify.error('Failed to queue role assignment jobs.');
                        })
                        .always(function () {
                            button.prop('disabled', false).html('<i class="fa fa-cogs"></i> Assign to Views');
                        });
                }
            }
        );
    });

    // Auto-save on Enter key in view role inputs
    $(document).on('keypress', '.view-role-input', function (e) {
        if (e.which === 13) { // Enter key
            const row = $(this).closest('tr');
            row.find('.save-role-btn').click();
        }
    });

    // Add refresh functionality
    window.refreshTenantTable = function() {
        window.location.reload();
    };
});
