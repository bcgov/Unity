$(function () {
    console.log('Script loaded');
    const l = abp.localization.getResource('GrantManager');  
    
    let inputAction = function (requestData, dataTableSettings) { 
        let assessmentId = decodeURIComponent($("#AssessmentId").val());
        if (!assessmentId) {
            return "00000000-0000-0000-0000-000000000000";
        }
        return assessmentId;
    }

    let responseCallback = function (result) {        
        console.log(result); 
        return {
            data: result
        };
    };
    
    const dataTable = $('#AssessmentResultAttachmentsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            ordering: false,
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.adjudicationAttachment.getList, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    title: '<i class="fl fl-paperclip" ></i>',
                    render: function (data) {
                        return '<i class="fl fl-paperclip" ></i>';
                    },
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
                        let html = '<div class="dropdown" style="float:right;">';
                        html += '<button class="btn btn-light dropbtn" type="button"><i class="fl fl-attachment-more" ></i></button>';
                        html += '<div class="dropdown-content">';
                        html += '<a href="/download?S3ObjectKey=' + encodeURIComponent(data) + '&Name=' + encodeURIComponent(full.fileName);
                        html += '" target="_blank" download="' + data + '" class="fullwidth">';
                        html += '<button class="btn fullWidth" style="margin:20px" type="button"><i class="fl fl-download"></i><span>Download Attachment</span></button></a>';
                        html += '<button class="btn fullWidth" style="margin:20px" type="button" onclick="deleteAdjudicationAttachment(\'' + data;
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
            
        }
    });
    dataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });

    PubSub.subscribe(
        'refresh_adjudication_attachment_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
});

let deleteAdjudicationAttachmentModal = new abp.ModalManager({
    viewUrl: '../Attachments/DeleteAttachmentModal'
});

function deleteAdjudicationAttachment(s3ObjectKey, fileName) {
    let assessmentId = decodeURIComponent($("#AssessmentId").val());
    deleteAdjudicationAttachmentModal.open({
        s3ObjectKey: s3ObjectKey,
        fileName: fileName,
        attachmentType: 'Adjudication',
        attachmentTypeId: assessmentId,
    });
}

deleteAdjudicationAttachmentModal.onResult(function () {
    abp.notify.success(
        'Attachment is successfully deleted.',
        'Delete Attachment'
    );
    PubSub.publish('refresh_adjudication_attachment_list');
});