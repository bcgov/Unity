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
            result = result.map((item, index) => ({
                ...item,
                rowCount: index,
            }));
            setTimeout(function () {
                PubSub.publish('update_application_attachment_count', { files: result.length });
            }, 10);
        }

        return {
            data: result
        };
    };

    const $downloadButton = $('#application_attachment_download_btn');
    const $deleteButton = $('#application_attachment_delete_btn');
    const $uploadButton = $('#application_attachment_upload_btn');
    const $uploadInput = $('#application_attachment_upload');
    const selectedAttachments = [];
    const selectAllClass = 'select-all-application-files';
    const tableWrapperSelector = '#ApplicationAttachmentsTable_wrapper';

    const dataTable = $('#ApplicationAttachmentsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[1, 'asc']],
            searching: true,
            paging: false,
            orderSequence: ['asc', 'desc'],
            select: {
                style: 'multi',
                selector: 'td.select-checkbox-cell',
            },
            info: false,
            autoWidth: false,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.attachments.attachment.getAttachments, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    ...getAttachmentSelectColumn(selectAllClass),
                    className:
                        'notexport dt-checkboxes-cell attachment-select-cell select-checkbox-cell text-center',
                },
                {
                    title: 'Filename',
                    data: 'fileName',
                    className: 'data-table-header text-break',
                    width: '30%',
                },
                {
                    title: 'Label',
                    data: 'displayName',
                    className: 'data-table-header text-break',
                    width: '20%',
                    render: function (data, type, row) {
                        return renderAttachmentLabelCell(
                            data ?? nullPlaceholder,
                            row.id
                        );
                    }
                },
                {
                    title: 'Date',
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
            ],
            externalFilterButtonId: 'btn-toggle-filter-uploads',
            headerCallback: buildAttachmentHeaderCallback(selectAllClass),
        })
    );

    initializeFilterRowPlugin(dataTable, 'btn-toggle-filter-uploads');

    dataTable.on('click', 'td button.edit-button', function (event) {
        event.stopPropagation();
        const attachmentId = $(event.currentTarget).data('attachment-id');
        updateAttachmentMetadata('Application', attachmentId);
    });

    function syncSelectedAttachments(index, isSelected) {
        const rowData = dataTable.row(index).data();
        if (!rowData) {
            return;
        }

        const rowKey = rowData.s3ObjectKey;
        const existingIndex = selectedAttachments.findIndex(
            (item) => item.s3ObjectKey === rowKey
        );

        if (isSelected && existingIndex === -1) {
            selectedAttachments.push(rowData);
        }

        if (!isSelected && existingIndex > -1) {
            selectedAttachments.splice(existingIndex, 1);
        }

        updateBulkButtons();
    }

    function updateBulkButtons() {
        const hasSelection = dataTable.rows({ selected: true }).count() > 0;
        $downloadButton.prop('disabled', !hasSelection);
        $deleteButton.prop('disabled', !hasSelection);
    }

    function getSelectedDownloadUrl(rowData) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return `/api/app/attachment/application/${encodeURIComponent(applicationId)}/download/${encodeURIComponent(rowData.fileName)}`;
    }

    async function downloadSelectedAttachments() {
        if (selectedAttachments.length === 0) {
            return;
        }

        const zip = new JSZip();
        const refNo = document.getElementsByClassName('reference-no')[0].textContent;
        const existingHTML = $downloadButton.html();

        try {
            $downloadButton
                .html(
                    '<span class="button-content"><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span><span>Downloading...</span></span>'
                )
                .prop('disabled', true);

            for (const rowData of selectedAttachments) {
                const response = await fetch(getSelectedDownloadUrl(rowData), {
                    credentials: 'same-origin',
                });

                if (!response.ok) {
                    throw new Error('Failed to download ' + rowData.fileName);
                }

                const blob = await response.blob();
                zip.file(rowData.fileName, blob);
            }

            const content = await zip.generateAsync({ type: 'blob' });
            const link = document.createElement('a');
            link.href = URL.createObjectURL(content);
            link.download = `${refNo}-Selected_Attachments.zip`;
            link.click();
            abp.notify.success('', 'The files have been downloaded successfully.');
        } catch (error) {
            console.error('Error downloading selected application attachments:', error);
            abp.notify.error('', 'An error occurred while downloading the selected files.');
        } finally {
            $downloadButton.html(existingHTML);
            updateBulkButtons();
        }
    }

    async function deleteSelectedAttachments() {
        if (selectedAttachments.length === 0) {
            return;
        }

        const confirmed = await abp.message.confirm(
            `Delete ${selectedAttachments.length} selected attachment(s)?`,
            'Delete Attachments'
        );

        if (!confirmed) {
            return;
        }

        const existingHTML = $deleteButton.html();

        try {
            $deleteButton
                .html(
                    '<span class="button-content"><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span><span>Deleting...</span></span>'
                )
                .prop('disabled', true);

            for (const rowData of selectedAttachments.slice()) {
                await abp.ajax({
                    url: '/Attachments/DeleteAttachmentModal?handler=OnPostAsync',
                    type: 'POST',
                    data: {
                        S3ObjectKey: rowData.s3ObjectKey,
                        FileName: rowData.fileName,
                        AttachmentType: 'Application',
                        AttachmentTypeId: rowData.applicationId || $('#DetailsViewApplicationId').val(),
                    },
                });
            }

            selectedAttachments.length = 0;
            dataTable.rows({ selected: true }).deselect();
            dataTable.ajax.reload();
            abp.notify.success('', 'The selected attachments have been deleted successfully.');
        } catch (error) {
            console.error('Error deleting selected application attachments:', error);
            abp.notify.error('', 'An error occurred while deleting the selected files.');
        } finally {
            $deleteButton.html(existingHTML);
            updateBulkButtons();
        }
    }

    $downloadButton.on('click', downloadSelectedAttachments);
    $deleteButton.on('click', deleteSelectedAttachments);
    $uploadButton.on('click', function () {
        $uploadInput.trigger('click');
    });

    function adjustApplicationAttachmentsTable() {
        dataTable.columns.adjust();
    }

    bindAttachmentSelectionBehavior({
        dataTable,
        tableWrapper: tableWrapperSelector,
        selectAllClass,
        onSelect: (index) => syncSelectedAttachments(index, true),
        onDeselect: (index) => syncSelectedAttachments(index, false),
    });

    updateBulkButtons();

    dataTable.on('draw', function () {
        selectedAttachments.length = 0;
        updateBulkButtons();
    });

    observeAttachmentTableResize(tableWrapperSelector, adjustApplicationAttachmentsTable);
    $('#attachments-tab').on('shown.bs.tab', adjustApplicationAttachmentsTable);

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
