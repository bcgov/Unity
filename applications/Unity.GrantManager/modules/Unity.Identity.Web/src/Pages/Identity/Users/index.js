(function ($) {
    let l = abp.localization.getResource('AbpIdentity');

    let _identityUserAppService = volo.abp.identity.identityUser;
    let _userImportService = unity.grantManager.identity.userImport;

    let inputAction = function (requestData, dataTableSettings) {
        return {
            directory: 'IDIR',
            firstName: $('#import-user-firstName').val(),
            lastName: $('#import-user-lastName').val()
        };
    };

    let responseCallback = function (result) {
        return {
            recordsTotal: result.length,
            recordsFiltered: result.length,
            data: result
        };
    };

    let _filterDataTable = null;

    let setupImportModal = function () {
        let _$filterTable = $('#UserSearchTable');
        _filterDataTable = _$filterTable.DataTable(
            abp.libs.datatables.normalizeConfiguration(
                {
                    order: [[0, 'asc']],
                    processing: true,
                    serverSide: false,
                    scrollX: true,
                    paging: true,
                    searching: false,
                    ajax: abp.libs.datatables.createAjax(
                        _userImportService.search,
                        inputAction,
                        responseCallback
                    ),
                    select: {
                        style: 'single',
                    },
                    columnDefs: [{
                        title: 'First Name',
                        name: 'firstName',
                        data: 'firstName',
                        className: 'data-table-header',
                    },
                    {
                        title: 'Last Name',
                        name: 'lastName',
                        data: 'lastName',
                        className: 'data-table-header'
                        },
                    {
                        title: 'Display Name',
                        name: 'displayName',
                        data: 'displayName',
                        className: 'data-table-header'
                    }],
                })
        )

        $('#ImportUserSearchButton').click(function (e) {
            e.preventDefault();            
            _filterDataTable.ajax.reloadEx();
            $('#import-user-btn').attr('disabled', true);
        });        

        $('#cancel-import-btn').click(function (e) {
            _importModal.close();
        });

        _filterDataTable.on('select', function (e, dt, type, indexes) {            
            if (type === 'row') {
                let selectedData = _filterDataTable.row(indexes).data();                
                $('#import-user-id').val(selectedData.userGuid);
                $('#import-user-btn').removeAttr('disabled');
            }            
        });

        _filterDataTable.on('deselect', function (e, dt, type, indexes) {
            $('#import-user-id').val();
            $('#import-user-btn').attr('disabled', true);
        });     

        _importModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });
    }

    abp.modals.importUser = function () {
        let initModal = function (publicApi, args) {
            setupImportModal();
        };
        return { initModal: initModal };
    }

    abp.modals.editUser = function () {
        let initModal = function (publicApi, args) { /* Intentionally left empty */ };
        return { initModal: initModal };
    }

    let _editModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Identity/Users/EditModal',
        modalClass: "editUser"
    });
    let _permissionsModal = new abp.ModalManager(
        abp.appPath + 'AbpPermissionManagement/PermissionManagementModal'
    );
    let _importModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Identity/Users/ImportModal',
        modalClass: "importUser"
    });

    let _dataTable = null;

    abp.ui.extensions.entityActions.get('identity.user').addContributor(
        function (actionList) {
            return actionList.addManyTail(
                [
                    {
                        text: l('Edit'),
                        visible: abp.auth.isGranted(
                            'AbpIdentity.Users.Update'
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
                            'AbpIdentity.Users.ManagePermissions'
                        ),
                        action: function (data) {
                            _permissionsModal.open({
                                providerName: 'U',
                                providerKey: data.record.id,
                                providerKeyDisplayName: data.record.userName
                            });
                        },
                    },
                    {
                        text: l('Delete'),
                        visible: function (data) {
                            return abp.auth.isGranted('AbpIdentity.Users.Delete') && abp.currentUser.id !== data.id;
                        },
                        confirmMessage: function (data) {
                            return l(
                                'UserDeletionConfirmationMessage',
                                data.record.userName
                            );
                        },
                        action: function (data) {
                            _identityUserAppService
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

    abp.ui.extensions.tableColumns.get('identity.user').addContributor(
        function (columnList) {
            columnList.addManyTail(
                [
                    {
                        title: l("Actions"),
                        rowAction: {
                            items: abp.ui.extensions.entityActions.get('identity.user').actions.toArray()
                        }
                    },
                    {
                        title: l('UserName'),
                        data: 'userName',
                        render: function (data, type, row) {
                            row.userName = $.fn.dataTable.render.text().display(row.userName);
                            if (!row.isActive) {
                                return '<i data-toggle="tooltip" data-placement="top" title="' +
                                    l('ThisUserIsNotActiveMessage') +
                                    '" class="fa fa-ban text-danger"></i> ' +
                                    '<span class="opc-65">' + row.userName + '</span>';
                            }

                            return row.userName;
                        }
                    },
                    {
                        title: l('EmailAddress'),
                        data: 'email',
                    }
                ]
            );
        },
        0 //adds as the first contributor
    );

    $('#AbpContentToolbar button[name=ImportUser]').click(function (e) {
        e.preventDefault();
        _importModal.open();
    });

    _importModal.onOpen(function () {
        setTimeout(() => {
            _filterDataTable.columns.adjust().draw();
        });   
    });

    $(function () {
        let _$table = $('#UsersTable');
        _dataTable = _$table.DataTable(
            abp.libs.datatables.normalizeConfiguration({
                order: [[1, 'asc']],
                processing: true,
                serverSide: true,
                scrollX: true,
                paging: true,
                ajax: abp.libs.datatables.createAjax(
                    _identityUserAppService.getList
                ),
                columnDefs: abp.ui.extensions.tableColumns.get('identity.user').columns.toArray()
            })
        );

        _editModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });
    });
})(jQuery);
