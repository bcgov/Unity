(function ($) {
    let l = abp.localization.getResource('AbpIdentity');

    let _identityRoleAppService = volo.abp.identity.identityRole;
    let _permissionsModal = new abp.ModalManager(
        abp.appPath + 'AbpPermissionManagement/PermissionManagementModal'
    );
    let _editModal = new abp.ModalManager(
        abp.appPath + 'Identity/Roles/EditModal'
    );
    let _createModal = new abp.ModalManager(
        abp.appPath + 'Identity/Roles/CreateModal'
    );

    let _dataTable = null;

    abp.ui.extensions.entityActions.get('identity.role').addContributor(
        function(actionList) {
            return actionList.addManyTail(
                [
                    {
                        text: l('Edit'),
                        visible: abp.auth.isGranted(
                            'AbpIdentity.Roles.Update'
                        ),
                        action: function (data) {
                            _editModal.open({
                                id: data.record.id,
                            });
                        },
                    },
                    {
                        text: l('Permissions'),
                        visible: abp.auth.isGranted(
                            'AbpIdentity.Roles.ManagePermissions'
                        ),
                        action: function (data) {
                            _permissionsModal.open({
                                providerName: 'R',
                                providerKey: data.record.name,
                                providerKeyDisplayName: data.record.name
                            });
                        },
                    },
                    {
                        text: l('Delete'),
                        visible: function (data) {
                            return (
                                !data.isStatic &&
                                abp.auth.isGranted(
                                    'AbpIdentity.Roles.Delete'
                                )
                            );
                        },
                        confirmMessage: function (data) {
                            return l(
                                'RoleDeletionConfirmationMessage',
                                data.record.name
                            );
                        },
                        action: function (data) {
                            _identityRoleAppService
                                .delete(data.record.id)
                                .then(function () {
                                    _dataTable.ajax.reloadEx();
                                    abp.notify.success(l('SuccessfullyDeleted'));
                                });
                        },
                    }
                ]
            );
        }
    );

    abp.ui.extensions.tableColumns.get('identity.role').addContributor(
        function (columnList) {
            columnList.addManyTail(
                [
                    {
                        title: l("Actions"),
                        rowAction: {
                            items: abp.ui.extensions.entityActions.get('identity.role').actions.toArray()
                        }
                    },
                    {
                        title: l('RoleName'),
                        data: 'name',
                        render: function (data, type, row) {
                            let name = '<span>' + $.fn.dataTable.render.text().display(data) + '</span>'; //prevent against possible XSS
                            if (row.isDefault) {
                                name +=
                                    '<span class="badge rounded-pill bg-success ms-1">' +
                                    l('DisplayName:IsDefault') +
                                    '</span>';
                            }
                            if (row.isPublic) {
                                name +=
                                    '<span class="badge rounded-pill bg-info ms-1">' +
                                    l('DisplayName:IsPublic') +
                                    '</span>';
                            }
                            if (row.isStatic) {
                                name +=
                                    '<span class="badge rounded-pill bg-warning ms-1">' +
                                    l('DisplayName:IsStatic') +
                                    '</span>';
                            }
                            return name;
                        },
                    }
                ]
            );
        },
        0 //adds as the first contributor
    );
    
    $(function () {
        let _$table = $('#IdentityRolesTable');        

        _dataTable = _$table.DataTable(
            abp.libs.datatables.normalizeConfiguration({
                order: [[1, 'asc']],
                searching: true,
                processing: true,
                serverSide: true,
                scrollX: true,
                paging: true,
                ajax: abp.libs.datatables.createAjax(
                    _identityRoleAppService.getList
                ),
                columnDefs: abp.ui.extensions.tableColumns.get('identity.role').columns.toArray()
            })
        );

        _createModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });

        _editModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });

        $('#AbpContentToolbar button[name=CreateRole]').click(function (e) {
            e.preventDefault();
            _createModal.open();
        });
    });
})(jQuery);
