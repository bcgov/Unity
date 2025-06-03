$(function () {
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
                        orderable: false,
                        className: 'notexport text-center',
                        name: 'rowActions',
                        data: 'id',
                        index: 0,
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

    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn flex-none';
    let actionButtons = [
        {
            text: '<i class="fl fl-multi-select align-middle"></i><span>View Role Matrix</span>',
            className: 'btn-light rounded-1',
            action: function (e, dt, button, config) {
                window.location = '/Identity/Roles/PermissionRoleMatrix'
            }
        },
        {
            text: '<i class="fl fl-add-to align-middle"></i> <span>' + l('NewRole') + '</span>',
            titleAttr: l('NewRole'),
            id: 'CreateRoleButton',
            className: 'btn-light rounded-1',
            available: () => abp.auth.isGranted('AbpIdentity.Roles.Create'),
            action: (e, dt, node, config) => {
                e.preventDefault();
                _createModal.open();
            }
        },
        ...commonTableActionButtons(l('Roles'))
    ];

    let dt = $('#IdentityRolesTable');
    let listColumns = abp.ui.extensions.tableColumns.get('identity.role').columns.toArray();
    let defaultVisibleColumns = listColumns.map((item) => { return item['data']; });

    _dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 1,
        dataEndpoint: _identityRoleAppService.getList,
        data: {},
        responseCallback: function (result) {
            return {
                recordsTotal: result.totalCount,
                recordsFiltered: result.items.length,
                data: result.items
            };
        },
        actionButtons,
        serverSideEnabled: false,
        pagingEnabled: true,
        reorderEnabled: false,
        languageSetValues: {},
        dataTableName: 'IdentityRolesTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        externalSearchId: 'search-roles'
    });

    _createModal.onResult(function () {
        _dataTable.ajax.reloadEx();
    });

    _editModal.onResult(function () {
        _dataTable.ajax.reloadEx();
    });
});
