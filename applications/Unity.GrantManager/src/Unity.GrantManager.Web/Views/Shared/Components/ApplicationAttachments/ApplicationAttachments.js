// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    const l = abp.localization.getResource('GrantManager');
    const nullPlaceholder = '—';

    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return {
            attachmentType: 'APPLICATION',
            attachedResourceId: applicationId
        };
    };

    let responseCallback = function (result) {
        if (result) {            
            setTimeout(function () {
                PubSub.publish('update_application_attachment_count', { files: result.length });
            }, 10);
        }

        return {
            data: result
        };
    };

    const dataTable = $('#ApplicationAttachmentsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.attachments.attachment.getAttachments, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    title: '<i class="fl fl-paperclip" ></i>',
                    render: function (data) {
                        return '<i class="fl fl-paperclip" ></i>';
                    },
                    orderable: false
                },
                {
                    title: l('AssessmentResultAttachments:DocumentName'),
                    data: 'fileName',
                    className: 'data-table-header',
                },
                {
                    title: 'Label',
                    data: 'displayName',
                    className: 'data-table-header',
                    render: function (data) {
                        return data ?? nullPlaceholder;
                    }
                },
                {
                    title: l('AssessmentResultAttachments:UploadedDate'),
                    data: 'time',
                    className: 'data-table-header',
                    render: function (data, type) {
                        if (type === 'display') {
                            return new Date(data).toDateString();
                        }
                        return data;
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
                        return generateAttachmentButtonContent(data, type, full, meta, 'Application');
                    },
                    orderable: false
                }
            ],
        })
    );

    dataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });

    PubSub.subscribe(
        'refresh_application_attachment_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
    $('#attachments-tab').one('click', function () {
        dataTable.columns.adjust();
    });
});
