// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    const l = abp.localization.getResource('GrantManager');
    const dt = $('#ApplicationAttachmentsTable');

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
                    return generateAttachmentButtonContent(data, type, full, meta, 'Application');
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
        actionButtons: [...commonTableActionButtons('Application Attachments', filterButtonId)],
        pagingEnabled: false,
        reorderEnabled: false,
        defaultSortColumn: 2,
        defaultSortDirection: 'asc',
        selectType: false,
        dataTableName: dt[0].id,
        dynamicButtonContainerId: 'ApplicationAttachments_ButtonContainerId',
        externalSearchId: 'ApplicationAttachments_SearchId',
        disableColumnSelect: true
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

    $('#attachments-tab').one('click', function () {
        dataTable.columns.adjust();
    });
});
