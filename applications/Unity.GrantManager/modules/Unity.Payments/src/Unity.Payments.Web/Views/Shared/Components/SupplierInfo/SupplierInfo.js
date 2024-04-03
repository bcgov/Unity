$(function () {
    const l = abp.localization.getResource('PaymentsResource');
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }
    let responseCallback = function (result) {
        

        return {
            data: result
        };
    };

    const dataTable = $('#SiteInfoTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.payments.supplierInfo.supplierInfo.getSites, inputAction, responseCallback
            ),
            columnDefs: [                
                {
                    //title: l('AssessmentResultAttachments:DocumentName'),
                    title:'sample title',
                    data: 'mailingAddress',
                    className: 'data-table-header',
                },
                {
                    //title: l('AssessmentResultAttachments:AttachedBy'),
                    title: 'number ',
                    data: 'number',
                    className: 'data-table-header',
                },
                {
                    title: '<i class="fl fl-paperclip" ></i>',
                    render: function (data) {
                        return '<i class="fl fl-paperclip" ></i>';
                    },
                    orderable: false
                },
            ],
        })
    );

    dataTable.columns.adjust();

    PubSub.subscribe(
        'refresh_sites_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
    
});

