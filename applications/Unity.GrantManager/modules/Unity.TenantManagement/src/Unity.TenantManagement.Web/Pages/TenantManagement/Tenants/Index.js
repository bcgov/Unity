(function () {
    let l = abp.localization.getResource('AbpTenantManagement');
    let _tenantAppService = unity.tenantManagement.tenant;
    let _userImportService = unity.grantManager.identity.userImport;

    let _editModal = new abp.ModalManager(
        abp.appPath + 'TenantManagement/Tenants/EditModal'
    );
    let _createModal = new abp.ModalManager({
            viewUrl: abp.appPath + 'TenantManagement/Tenants/CreateModal',
            modalClass: 'createTenant'
        }
    );
    let _featuresModal = new abp.ModalManager(
        abp.appPath + 'FeatureManagement/FeatureManagementModal'
    );
    let _assignManagerModal = new abp.ModalManager({
            viewUrl: abp.appPath + 'TenantManagement/Tenants/AssignManagerModal',
            modalClass: 'assignManager'
        }
    );

    let _dataTable = null;

    abp.ui.extensions.entityActions.get('tenantManagement.tenant').addContributor(
        function(actionList) {
            return actionList.addManyTail(
                [
                    {
                        text: l('Edit'),
                        visible: abp.auth.isGranted(
                            'UnityTenantManagement.Tenants.Update'
                        ),
                        action: function (data) {
                            _editModal.open({
                                id: data.record.id,
                            });
                        },
                    },
                    {
                        text: l('Assign Manager'),
                        visible: abp.auth.isGranted(
                            'UnityTenantManagement.Tenants.Create'
                        ),
                        action: function (data) {
                            _assignManagerModal.open({                                
                                id: data.record.id,
                            });
                        },
                    },
                    {
                        text: l('Features'),
                        visible: abp.auth.isGranted(
                            'AbpTenantManagement.Tenants.ManageFeatures'
                        ),
                        action: function (data) {
                            _featuresModal.open({
                                providerName: 'T',
                                providerKey: data.record.id,
                            });
                        },
                    },
                    {
                        text: l('Delete'),
                        visible: abp.auth.isGranted(
                            'UnityTenantManagement.Tenants.Delete'
                        ),
                        confirmMessage: function (data) {
                            return l(
                                'TenantDeletionConfirmationMessage',
                                data.record.name
                            );
                        },
                        action: function (data) {
                            _tenantAppService
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

    abp.ui.extensions.tableColumns.get('tenantManagement.tenant').addContributor(
        function (columnList) {
            columnList.addManyTail(
                [
                    {
                        title: l("Actions"),
                        rowAction: {
                            items: abp.ui.extensions.entityActions.get('tenantManagement.tenant').actions.toArray()
                        }
                    },
                    {
                        title: l("TenantName"),
                        data: 'name',
                    },
                    {
                        title: l("Id"),
                        data: 'id',
                    }
                ]
            );
        },
        0 //adds as the first contributor
    );

    let inputAction = function (requestData, dataTableSettings) {
        return {
            directory: 'IDIR',
            firstName: $('#create-tenant-firstName').val(),
            lastName: $('#create-tenant-lastName').val()
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

    let setupCreateTenantModal = function () {
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

        $('#TenantAdminSearchButton').click(function (e) {
            e.preventDefault();
            _filterDataTable.ajax.reloadEx();
            $('#create-tenant-btn').attr('disabled', true);
        });

        $('#cancel-tenant-btn').click(function (e) {
            _createModal.close();
            _assignManagerModal.close();
        });

        _filterDataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row') {
                let selectedData = _filterDataTable.row(indexes).data();
                $('#create-tenant-admin-id').val(selectedData.userGuid);
                $('#create-tenant-btn').removeAttr('disabled');
            }
        });

        _filterDataTable.on('deselect', function (e, dt, type, indexes) {
            $('#create-tenant-admin-id').val();
            $('#create-tenant-btn').attr('disabled', true);
        });

        _createModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });

        _assignManagerModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });
    }

    _createModal.onOpen(function () {
        setTimeout(() => {
            _filterDataTable.columns.adjust().draw();
        });
    });

    _assignManagerModal.onOpen(function () {
        setTimeout(() => {
            _filterDataTable.columns.adjust().draw();
        });
    });

    abp.modals.createTenant = function () {
        let initModal = function (publicApi, args) {
            setupCreateTenantModal();
        };
        return { initModal: initModal };
    }

    abp.modals.assignManager = function () {
        let initModal = function (publicApi, args) {
            setupCreateTenantModal();
        };
        return { initModal: initModal };
    }

    $(function () {
        let _$wrapper = $('#TenantsWrapper');

        _dataTable = _$wrapper.find('table').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                order: [[1, 'asc']],
                processing: true,
                paging: true,
                scrollX: true,
                serverSide: true,
                ajax: abp.libs.datatables.createAjax(_tenantAppService.getList),
                columnDefs: abp.ui.extensions.tableColumns.get('tenantManagement.tenant').columns.toArray(),
            })
        );

        _createModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });

        _editModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });

        $('#AbpContentToolbar button[name=CreateTenant]').click(function (e) {
            e.preventDefault();
            _createModal.open();
        });
    });
})();
