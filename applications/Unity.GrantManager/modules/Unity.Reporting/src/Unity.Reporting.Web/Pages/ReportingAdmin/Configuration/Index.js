$(function () {
    let _tenantViewRoleAppService = unity.reporting.configuration.tenantViewRole;
    let _databaseInfoModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'ReportingAdmin/Configuration/DatabaseInfoModal'
    });

    // Initialize tooltips for default role indicators
    function initializeTooltips() {
        $('[data-bs-toggle="tooltip"]').tooltip();
    }

    // Initialize DataTable for tenant view role management
    $('#TenantViewRoleTable').DataTable({
        order: [[0, 'asc']], // Sort by tenant name
        processing: false,
        serverSide: false,
        paging: true,
        searching: true,
        pageLength: 25,
        autoWidth: false,
        columnDefs: [
            {
                targets: [2], // Actions column
                orderable: false
            }
        ],
        dom: 'frtip', // Show filter, table, info, pagination
        language: {
            emptyTable: "No tenants found",
            search: "Search tenants:",
            lengthMenu: "Show _MENU_ tenants per page",
            info: "Showing _START_ to _END_ of _TOTAL_ tenants"
        },
        drawCallback: function() {
            // Reinitialize tooltips after table redraws
            initializeTooltips();
        }
    });

    // Initialize tooltips on page load
    initializeTooltips();

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
            .done(function (_) {
                // Remove the default indicator since it's now saved
                const indicator = row.find('.default-role-indicator');
                if (indicator.length) {
                    indicator.tooltip('dispose');
                    indicator.remove();
                }
                
                // Update the data attribute
                viewRoleInput.attr('data-is-default', 'false');
                
                abp.notify.success('View role saved successfully.');
            })
            .fail(function () {
                abp.notify.error('Failed to save view role.');
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
        const viewRoleInput = row.find('.view-role-input');
        const viewRole = viewRoleInput.val().trim();
        const isDefault = viewRoleInput.attr('data-is-default') === 'true';

        if (!viewRole) {
            abp.notify.warn('Please enter a view role name before assigning it to views.');
            return;
        }

        // Check if role needs to be saved first
        if (isDefault) {
            abp.message.confirm(
                `The role "${viewRole}" is using the default pattern and hasn't been saved yet. Would you like to save it first and then assign it to views?`,
                'Save and Assign Role',
                function (isConfirmed) {
                    if (isConfirmed) {
                        // Save first, then assign
                        saveAndAssignRole(tenantId, tenantName, viewRole, button, row);
                    }
                }
            );
        } else {
            // Role is already saved, proceed with assignment
            assignRoleToViews(tenantId, tenantName, viewRole, button);
        }
    });

    // Handle view database info button click
    $(document).on('click', '.view-database-info-btn', function () {
        const button = $(this);
        const tenantId = button.data('tenant-id');
        const tenantName = button.data('tenant-name');

        _databaseInfoModal.open({
            tenantId: tenantId,
            tenantName: tenantName
        });
    });

    // Function to save role and then assign to views
    function saveAndAssignRole(tenantId, tenantName, viewRole, button, row) {
        button.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Saving & Assigning...');

        _tenantViewRoleAppService.update(tenantId, { viewRole: viewRole })
            .done(function (result) {
                // Remove the default indicator
                const indicator = row.find('.default-role-indicator');
                if (indicator.length) {
                    indicator.tooltip('dispose');
                    indicator.remove();
                }
                
                // Update the data attribute
                row.find('.view-role-input').attr('data-is-default', 'false');
                
                // Now assign to views
                assignRoleToViews(tenantId, tenantName, viewRole, button);
            })
            .fail(function () {
                abp.notify.error('Failed to save view role.');
                button.prop('disabled', false).html('<i class="fa fa-cogs"></i> Assign to Views');
            });
    }

    // Function to assign role to views
    function assignRoleToViews(tenantId, tenantName, viewRole, button) {
        button.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Assigning...');

        _tenantViewRoleAppService.assignRoleToViews(tenantId)
            .done(function () {
                abp.notify.success(`Role assignment jobs have been queued for tenant "${tenantName}". The process will complete in the background.`);
            })
            .fail(function () {
                abp.notify.error('Failed to queue role assignment jobs.');
            })
            .always(function () {
                button.prop('disabled', false).html('<i class="fa fa-cogs"></i> Assign to Views');
            });
    }

    // Add refresh functionality
    window.refreshTenantTable = function() {
        window.location.reload();
    };
});
