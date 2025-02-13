﻿// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    const l = abp.localization.getResource('GrantManager');

    let inputAction = function (requestData, dataTableSettings) {
        let assessmentId = decodeURIComponent($("#AssessmentId").val());
        if (!assessmentId) {
            return "00000000-0000-0000-0000-000000000000";
        }
        return assessmentId;
    }

    let responseCallback = function (result) {
        return {
            data: result
        };
    };

    const dataTable = $('#AssessmentResultAttachmentsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.attachment.getAssessment, inputAction, responseCallback
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
                        return data ?? '—';
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
                        return generateAttachmentButtonContent(data, type, full, meta, 'Assessment');
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
        'refresh_assessment_attachment_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
});
