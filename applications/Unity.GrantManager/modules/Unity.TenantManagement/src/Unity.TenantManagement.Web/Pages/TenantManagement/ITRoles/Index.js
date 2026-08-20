(function () {
    let lGm = abp.localization.getResource('GrantManager');
    let _hostRoleService = unity.grantManager.identity.hostRole;
    let _userImportService = unity.grantManager.identity.userImport;

    let _assignModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'TenantManagement/ITRoles/AssignModal',
        modalClass: 'assignRole'
    });

    let _dataTable = null;
    let _assignRoleFilterDataTable = null;

    // ─── Modal setup: Assign Role ──────────────────────────────────────────────

    let _setupAssignRoleModal = function () {
        let _$filterTable = $('#AssignRoleUserSearchTable');
        _assignRoleFilterDataTable = _$filterTable.DataTable(
            abp.libs.datatables.normalizeConfiguration({
                order: [[0, 'asc']],
                processing: true,
                serverSide: false,
                scrollX: true,
                paging: true,
                searching: false,
                ajax: abp.libs.datatables.createAjax(
                    _userImportService.search,
                    function () {
                        return {
                            directory: 'IDIR',
                            firstName: $('#assign-role-firstName').val(),
                            lastName: $('#assign-role-lastName').val()
                        };
                    },
                    function (result) {
                        return { recordsTotal: result.length, recordsFiltered: result.length, data: result };
                    }
                ),
                select: { style: 'single' },
                columnDefs: [
                    { title: 'First Name', name: 'firstName', data: 'firstName', className: 'data-table-header' },
                    { title: 'Last Name', name: 'lastName', data: 'lastName', className: 'data-table-header' },
                    { title: 'Display Name', name: 'displayName', data: 'displayName', className: 'data-table-header' }
                ]
            })
        );

        $('#AssignRoleSearchButton').click(function (e) {
            e.preventDefault();
            _assignRoleFilterDataTable.ajax.reload();
            $('#assign-role-btn').attr('disabled', true);
        });

        $('#cancel-assign-role-btn').click(function () {
            _assignModal.close();
        });

        _assignRoleFilterDataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row') {
                let selectedData = _assignRoleFilterDataTable.row(indexes).data();
                $('#assign-role-user-id').val(selectedData.userGuid);
                $('#assign-role-btn').removeAttr('disabled');
            }
        });

        _assignRoleFilterDataTable.on('deselect', function () {
            $('#assign-role-user-id').val();
            $('#assign-role-btn').attr('disabled', true);
        });
    };

    _assignModal.onOpen(function () {
        setTimeout(() => {
            _assignRoleFilterDataTable.columns.adjust().draw();
        });
    });

    abp.modals.assignRole = function () {
        return { initModal: _setupAssignRoleModal };
    };

    // One row per user - each role that user currently holds renders as a badge with its own "x"
    // remove control, so ITAdministrator and ITOperations can be revoked independently without
    // needing a separate row (or a separate actions column) per role.
    // Role name literals match IdentityConsts.ITAdminRoleName/ITOperationsRoleName - hardcoded here
    // the same way existing code (e.g. Tenants/Index.js's abp.auth.isGranted('ITOperations') check)
    // already references these role names as plain strings on the JS side.
    let _roleLabels = {
        'ITAdministrator': lGm('ITRoles:ITAdministrator'),
        'ITOperations': lGm('ITRoles:ITOperations')
    };

    function _buildRolesCell(id, roleNames) {
        return (roleNames || []).map(function (roleName) {
            let label = $('<span>').text(_roleLabels[roleName] || roleName).html();
            return '<span class="badge bg-secondary me-1 p-2">' + label +
                ' <a href="javascript:;" class="it-role-badge-remove text-white" data-id="' + id +
                '" data-role="' + roleName + '" aria-label="' + lGm('ITRoles:RevokeAction') + '">&times;</a></span>';
        }).join('');
    }

    let listColumns = [
        { title: lGm('ITRoles:DisplayName'), data: 'displayName', name: 'displayName', index: 0 },
        { title: lGm('ITRoles:Username'), data: 'username', name: 'username', index: 1 },
        { title: lGm('ITRoles:Email'), data: 'email', name: 'email', index: 2 },
        {
            title: lGm('ITRoles:RoleName'),
            name: 'roleNames',
            data: 'roleNames',
            orderable: false,
            index: 3,
            render: function (data, type, row) {
                return type === 'display' ? _buildRolesCell(row.id, data) : (data || []).join(', ');
            }
        }
    ];

    let defaultVisibleColumns = ['displayName', 'username', 'email', 'roleNames'];

    let responseCallback = function (result) {
        return {
            recordsTotal: result.length,
            recordsFiltered: result.length,
            data: result
        };
    };

    function _onRevokeConfirmed(id, roleName) {
        return function (confirmed) {
            if (confirmed) {
                _hostRoleService.revokeRole(id, roleName).then(function () {
                    _dataTable.ajax.reload();
                    abp.notify.success(lGm('ITRoles:SuccessfullyRevoked'));
                });
            }
        };
    }

    $(function () {
        _dataTable = initializeDataTable({
            dt: $('#ITRolesTable'),
            listColumns: listColumns,
            defaultVisibleColumns: defaultVisibleColumns,
            defaultSortColumn: 0,
            dataEndpoint: _hostRoleService.getList,
            responseCallback: responseCallback,
            actionButtons: commonTableActionButtons('ITRoles').filter(function (b) { return b.id !== 'btn-toggle-filter'; }),
            serverSideEnabled: false,
            pagingEnabled: true,
            reorderEnabled: true,
            languageSetValues: {},
            externalSearchId: 'search',
            fixedHeaders: true
        });

        _assignModal.onResult(function () {
            _dataTable.ajax.reload();
        });

        // Relocate the page-toolbar "Assign Role" button into the action bar, matching the
        // Tenants list layout.
        $('#assignRoleButtonContainer').append($('#AbpContentToolbar button[name=AssignRole]'));

        $('#assignRoleButtonContainer button[name=AssignRole]').click(function (e) {
            e.preventDefault();
            _assignModal.open();
        });

        $(document).on('click', '.it-role-badge-remove', function (e) {
            e.preventDefault();
            e.stopPropagation();
            abp.message.confirm(
                lGm('ITRoles:RevokeConfirmationMessage'),
                _onRevokeConfirmed($(this).data('id'), $(this).data('role')));
        });
    });
})();
