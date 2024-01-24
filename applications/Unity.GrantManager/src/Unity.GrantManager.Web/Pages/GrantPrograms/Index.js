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
    $('#UserGrantProgramsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: false,
            order: [[0, "asc"]],
            searching: true,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(unity.grantManager.identity.userTenant.getList, null, responseCallback),
            columnDefs: [
                {
                    title: l('Name'),
                    data: "tenantName"
                },
                {        
                    orderable: false,
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
            ]
        })
    );    
});

