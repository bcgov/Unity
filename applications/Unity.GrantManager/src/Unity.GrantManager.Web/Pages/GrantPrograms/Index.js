$(function () {
    const l = abp.localization.getResource('GrantManager');

    let responseCallback = function (result) {
        return {
            recordsTotal: result.length,
            recordsFiltered: result.length,
            data: result
        };
    };
    
    let submitTenantSwap = function (data) {
        $('#SwapTenantId').val(data);
        tenantSwapForm.submit();
    };
    
    /**
     * Users Grant Programs: List All
     */
    let listColumns = [
        {
            title: l('Name'),
            name: 'tenantName',
            data: 'tenantName',
            index: 0
        },
        {
            title: l('Actions'),
            orderable: false,
            className: 'notexport text-center',
            name: 'rowActions',
            data: 'tenantId',
            index: 1,
            rowAction: {
                items:
                    [
                        {
                            text: l('Common:Command:Select'),
                            action: (data) => submitTenantSwap(data.record.tenantId)
                        }
                    ]
            }
        }
    ];

    Unity.DataTables.create('#UserGrantProgramsTable', {
        listColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 0,
        dataEndpoint: unity.grantManager.identity.userTenant.getList,
        data: {},
        responseCallback,
        customButtons: [...commonTableActionButtons('Grant Programs')],
        serverSideEnabled: false,
        pagingEnabled: true,
        reorderEnabled: false,
        languageSetValues: {},
        externalSearchId: 'search-grant-programs',
        disableColumnSelect: true,
        exportTitle: 'Grant Programs'
    });
});

