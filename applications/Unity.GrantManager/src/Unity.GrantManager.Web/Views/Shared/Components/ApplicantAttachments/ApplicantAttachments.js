// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    const l = abp.localization.getResource('GrantManager');
    const nullPlaceholder = '—';

    let inputAction = function (requestData, dataTableSettings) {
        const applicantId = decodeURIComponent($('#DetailsViewApplicantId').val());
        return {
            attachmentType: 'APPLICANT',
            attachedResourceId: applicantId
        };
    };

    const dataTable = $('#ApplicantAttachmentsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'asc']],
            searching: true,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.attachments.attachment.getAttachments, inputAction, attachmentCountResponseCallback
            ),
            columnDefs: [
                {
                    title: '<i class="fl fl-paperclip" ></i>',
                    width: '40px',
                    className: 'text-center',
                    render: function (data) {
                        return '<i class="fl fl-paperclip" ></i>';
                    },
                    orderable: false
                },
                {
                    title: l('AssessmentResultAttachments:DocumentName'),
                    data: 'fileName',
                    className: 'data-table-header text-break',
                    width: '30%',
                },
                {
                    title: 'Label',
                    data: 'displayName',
                    className: 'data-table-header text-break',
                    width: '20%',
                    render: function (data) {
                        return data ?? nullPlaceholder;
                    }
                },
                {
                    title: l('AssessmentResultAttachments:UploadedDate'),
                    data: 'time',
                    className: 'data-table-header',
                    width: '140px',
                    render: function (data, type) {
                     if (type === 'display' || type === 'filter') {
                            return new Date(data).toDateString();
                        }
                        return data;
                    },
                },
                {
                    title: l('AssessmentResultAttachments:AttachedBy'),
                    data: 'attachedBy',
                    className: 'data-table-header',
                    width: '25%',
                },
                {
                    title: '',
                    data: 's3ObjectKey',
                    width: '80px',
                    className: 'text-center',
                    render: function (data, type, full, meta) {
                        return generateAttachmentButtonContent(data, type, full, meta, 'Applicant');
                    },
                    orderable: false
                }
            ],
            externalFilterButtonId: 'btn-toggle-filter-applicant-uploads',
        })
    );

    initializeFilterRowPlugin(dataTable, 'btn-toggle-filter-applicant-uploads');

    dataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });

    PubSub.subscribe(
        'refresh_applicant_attachment_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
    $('#attachments-tab').one('click', function () {
        dataTable.columns.adjust();
    });

    $('#applicant_attachment_upload_btn').on('click', function () {
        $('#applicant_attachment_upload').click();
    });
});

function attachmentCountResponseCallback(result) {
    if (result) {
        setTimeout(function () {
            PubSub.publish('update_applicant_attachment_count', { files: result.length });
        }, 10);
    }

    return {
        data: result
    };
}