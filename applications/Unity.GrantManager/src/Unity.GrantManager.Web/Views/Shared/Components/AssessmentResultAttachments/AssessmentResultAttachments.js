// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    const l = abp.localization.getResource('GrantManager');
    const dt = $('#AssessmentResultAttachmentsTable');

    let inputAction = function (requestData, dataTableSettings) {
        let assessmentId = decodeURIComponent($("#AssessmentId").val());
        return {
            attachmentType: 'ASSESSMENT',
            attachedResourceId: assessmentId ?? "00000000-0000-0000-0000-000000000000"
        };
    };

    let responseCallback = function (result) {
        return {
            data: result
        };
    };

    function getColumns() {
        return [
            {
                title: '<i class="fl fl-paperclip" ></i>',
                data: null,
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
                    return generateAttachmentButtonContent(data, type, full, meta, 'Assessment');
                },
                orderable: false
            }
        ];
    }

    const filterButtonId = getFilterButtonId(dt);
    const dataTable = initializeDataTable({
        dt,
        listColumns: getColumns(),
        dataEndpoint: unity.grantManager.attachments.attachment.getAttachments,
        data: inputAction,
        responseCallback,
        actionButtons: [...commonTableActionButtons('Assessment Attachments', filterButtonId)],
        pagingEnabled: false,
        reorderEnabled: false,
        defaultSortColumn: 2,
        defaultSortDirection: 'asc',
        selectType: false,
        dataTableName: dt[0].id,
        dynamicButtonContainerId: 'AssessmentResultAttachments_ButtonContainerId',
        externalSearchId: 'AssessmentResultAttachments_SearchId',
        disableColumnSelect: true
    });

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
