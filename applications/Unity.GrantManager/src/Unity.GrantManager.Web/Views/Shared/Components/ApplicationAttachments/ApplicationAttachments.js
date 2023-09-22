$(function () {
    console.log('Script loaded');
    const l = abp.localization.getResource('GrantManager');
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }
    let responseCallback = function (result) {

        // your custom code.
        console.log(result)
        if (result && result.length) {
            PubSub.publish('update_application_attachment_count', result.length);  
        }
       
       
        return {
            data: result
        };
    };
   
        const dataTable = $('#ApplicationAttachmentsTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                serverSide: true,
                order: [[1, 'asc']],
                searching: false,
                paging: false,
                select: false,
                info: false,
                scrollX: true,
                ajax: abp.libs.datatables.createAjax(
                    unity.grantManager.grantApplications.applicationAttachment.getList, inputAction, responseCallback
                ),
                columnDefs: [
                    {
                        title: l('AssessmentResultAttachments:DocumentName'),
                        data: 'fileName',
                        className: 'data-table-header',
                    },
                    {
                        title: l('AssessmentResultAttachments:UploadedDate'),
                        data: 'time',
                        className: 'data-table-header',
                        render: function (data) {
                            return new Date(data).toDateString();
                        },
                    },
                    {
                        title: l('AssessmentResultAttachments:AttachedBy'),
                        data: 'creatorId',
                        className: 'data-table-header',
                        render: function (data) {
                            return 'Reviewer Name';
                        },
                    },
                ],
            })
        );

        dataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row') {
                var selectedData = dataTable.row(indexes).data();
                console.log('Selected Data:', selectedData);
                //PubSub.publish('select_application', selectedData);
            }
        });

        dataTable.on('deselect', function (e, dt, type, indexes) {
            if (type === 'row') {
                var deselectedData = dataTable.row(indexes).data();
                //PubSub.publish('deselect_application', deselectedData);
            }
        });
        dataTable.on('click', 'tbody tr', function (e) {
            e.currentTarget.classList.toggle('selected');
        });
   
     PubSub.subscribe(
        'refresh_application_attachment_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
});
