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
                        title: '',
                        render: function (data) {
                            return '<i class="fl fl-attachment" ></i>';
                        }
                    },
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
                        data: 'attachedBy',
                        className: 'data-table-header',                        
                    },
                    {
                        title: '',
                        data: 's3Guid',
                        render: function (data, type, full, meta) {
                            var html = '<a href="/download?S3Guid=' + encodeURIComponent(data) + '&Name=' + encodeURIComponent(full.fileName);
                            html += '" target="_blank" download="' + data + '" style="text-decoration:none">';
                            html += '<button class="btn btn-light" type="submit"><i class="fl fl-attachment-more" ></i></button>';
                            html += '</a > ';
                            return html;
                        }
                    }
                ],
            })
        );


    dataTable.on('select', function (e, dt, type, indexes) {
        if (type === 'row') {
            const selectedData = dataTable.row(indexes).data();
            console.log('Selected Data:', selectedData);
            //PubSub.publish('select_application', selectedData);
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            const deselectedData = dataTable.row(indexes).data();
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
