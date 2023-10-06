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
                    unity.grantManager.grantApplications.attachment.getApplication, inputAction, responseCallback
                ),
                columnDefs: [
                    {
                        title: '<i class="fl fl-paperclip" ></i>',
                        render: function (data) {
                            return '<i class="fl fl-paperclip" ></i>';
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
                        data: 's3ObjectKey',
                        render: function (data, type, full, meta) {
                            let applicationId = decodeURIComponent($("#DetailsViewApplicationId").val()); 
                            let html = '<div class="dropdown" style="float:right;">';
                            html += '<button class="btn btn-light dropbtn" type="button"><i class="fl fl-attachment-more" ></i></button>';
                            html += '<div class="dropdown-content">';
                            html += '<a href="/api/app/attachment/application/' + encodeURIComponent(applicationId) + '/download/' + encodeURIComponent(full.fileName);
                            html += '" target="_blank" download="' + data + '" class="fullwidth">';
                            html += '<button class="btn fullWidth" style="margin:10px" type="button"><i class="fl fl-download"></i><span>Download Attachment</span></button></a>';
                            html += '<button class="btn fullWidth" style="margin:10px" type="button" onclick="deleteApplicationAttachment(\'' + data;
                            html += '\',\'' + full.fileName + '\')"><i class="fl fl-cancel"></i><span>Delete Attachment</span></button>';
                            html += '</div>';
                            html += '</div>';
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
            
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            const deselectedData = dataTable.row(indexes).data();
            console.log('Selected Data:', deselectedData);
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

let deleteAttachmentModal = new abp.ModalManager({
    viewUrl: '../Attachments/DeleteAttachmentModal'
});

function deleteApplicationAttachment(s3ObjectKey, fileName) {
    let applicationId = decodeURIComponent($("#DetailsViewApplicationId").val()); 
    deleteAttachmentModal.open({
        s3ObjectKey: s3ObjectKey,
        fileName: fileName,
        attachmentType: 'Application',
        attachmentTypeId: applicationId,
    });
}

deleteAttachmentModal.onResult(function () {
    abp.notify.success(
        'Attachment is successfully deleted.',
        'Delete Attachment'
    );
    PubSub.publish('refresh_application_attachment_list');
});