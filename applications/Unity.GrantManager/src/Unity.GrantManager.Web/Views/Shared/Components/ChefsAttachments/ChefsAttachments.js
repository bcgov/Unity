// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    globalThis.queueAttachmentSummary = function (triggerButton = null) {
        $('#generateAiSummaries')
            .data('trigger-button', triggerButton || null)
            .trigger('click');
    };

    const downloadSelected = $('#downloadSelected');
    const dt = $('#ChefsAttachmentsTable');
    const selectAllClass = 'select-all-chefs-files';
    let chefsDataTable;
    let selectedAtttachments = [];
    const nullPlaceholder = '-';
    const tableWrapperSelector = '#ChefsAttachmentsTable_wrapper';

    let inputAction = function () {
        const urlParams = new URL(window.location.toLocaleString())
            .searchParams;
        return urlParams.get('ApplicationId');
    };

    let responseCallback = function (result) {
        if (result.length <= 0) {
            $('.dataTables_paginate').hide();
        }

        if (result) {
            setTimeout(function () {
                PubSub.publish('update_application_attachment_count', {
                    chefs: result.length,
                });
            }, 10);

            if (result.length === 0 || selectedAtttachments.length === 0) {
                $(downloadSelected).prop('disabled', true);

                if (document.getElementById('generateAiSummaries')) {
                    $generateAISummariesButton.prop('disabled', true);
                }
            }

        }

        return {
            data: formatItems(result),
        };
    };

    function getColumns() {
        return [
            {
                ...getAttachmentSelectColumn(selectAllClass),
                className:
                    'notexport dt-checkboxes-cell attachment-select-cell select-checkbox-cell text-center',
            },
            {
                title: 'Filename',
                name: 'chefsFileName',
                data: 'fileName',
                className: 'data-table-header text-break',
                index: 1,
                width: '40%',
            },
            {
                title: 'Label',
                data: 'displayName',
                className: 'data-table-header text-break',
                width: '35%',
                render: function (data, type, row) {
                    return renderAttachmentLabelCell(
                        data ?? nullPlaceholder,
                        row.id
                    );
                },
            },
        ];
    }

    function formatItems(items) {
        return items.map((item, index) => ({
            ...item,
            rowCount: index,
        }));
    }

    chefsDataTable = dt.DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: false,
            info: false,
            order: [[1, 'asc']],
            orderSequence: ['asc', 'desc'],
            searching: true,
            processing: true,
            autoWidth: false,
            select: {
                style: 'multiple',
                selector: 'td.select-checkbox-cell',
            },
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.attachments.attachment
                    .getApplicationChefsFileAttachments,
                inputAction,
                responseCallback
            ),
            columnDefs: getColumns(),
            headerCallback: buildAttachmentHeaderCallback(selectAllClass),
            createdRow: function (row, data, dataIndex) {
                if (data.aiSummary) {
                    let summaryRow = $(
                        '<tr class="ai-summary-row" data-parent-row="' +
                            dataIndex +
                            '" style="background-color: #f8f9fa; display: none;">'
                    ).append(
                        $('<td colspan="3">').append(
                            $('<div class="ai-summary-content">')
                                .append(
                                    $('<strong>')
                                        .append(
                                            '<i class="unt-icon-sm fa-solid fa-wand-sparkles"></i> Summary:'
                                        )
                                )
                                .append($('<p>').text(data.aiSummary))
                        )
                    );
                    $(row).after(summaryRow);
                }
            },
            externalFilterButtonId: 'btn-toggle-filter-submissions',
        })
    );

    const $generateAISummariesButton = $('#generateAiSummaries');

    initializeFilterRowPlugin(chefsDataTable, 'btn-toggle-filter-submissions');

    PubSub.subscribe('refresh_chefs_attachment_list', () => {
        chefsDataTable.ajax.reload();
    });

    chefsDataTable.on('click', 'td button.edit-button', function (event) {
        event.stopPropagation();
        let rowData = chefsDataTable.row(event.target.closest('tr')).data();
        updateAttachmentMetadata('CHEFS', rowData.id);
    });

    if ($generateAISummariesButton.length > 0) {
        function resetAttachmentSelectionState() {
            selectedAtttachments = [];
            $(tableWrapperSelector).find(`.${selectAllClass}`).prop('checked', false);
            $(tableWrapperSelector).find('.row-checkbox').prop('checked', false);
            $(downloadSelected).prop('disabled', true);
            $generateAISummariesButton.prop('disabled', true);
        }

        $generateAISummariesButton.on('click', function () {
            const $button = $(this);
            const triggerButton = $button.data('trigger-button');
            const $activeButton = triggerButton ? $(triggerButton) : $button;
            const rowsToProcess = triggerButton
                ? chefsDataTable.rows().data()
                : chefsDataTable.rows({ selected: true }).data();
            const promptVersion = globalThis.getSelectedPromptVersion?.() || null;

            $button.removeData('trigger-button');

            if (rowsToProcess.length === 0) {
                abp.message.warn(
                    triggerButton
                        ? 'No attachments were found to generate summaries for.'
                        : 'Please select at least one attachment to generate summaries.'
                );
                return;
            }

            const attachmentIds = rowsToProcess.toArray().map((row) => row.id);
            const existingHTML = $activeButton.html();

            $.ajax({
                url:
                    '/api/app/attachment-summary/generate-attachment-summaries' +
                    '?promptVersion=' +
                    encodeURIComponent(promptVersion || ''),
                data: JSON.stringify(attachmentIds),
                contentType: 'application/json',
                type: 'POST',
                beforeSend: function () {
                            $activeButton
                                .html(
                            '<span class="button-content"><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span><span>Queueing...</span></span>'
                                )
                        .prop('disabled', true);
                },
                success: function (summaries) {
                    abp.notify.success(
                        'AI summaries queued for ' +
                            summaries.length +
                            ' attachment(s). Refresh later to see updated results.'
                    );

                    resetAttachmentSelectionState();
                    $activeButton.html(existingHTML).prop('disabled', false);
                },
                error: function (error) {
                    console.error('Error generating AI summaries:', error);
                    abp.notify.error(
                        'An error occurred while queueing AI summaries. Please try again.'
                    );
                    $activeButton.html(existingHTML).prop('disabled', false);
                },
            });
        });
    }

    const $toggleAllAISummariesButton = $('#toggleAllAISummaries');
    let allAISummariesExpanded = true;

    function expandAllAISummaries() {
        chefsDataTable.rows().every(function () {
            const row = this;
            const rowData = row.data();

            if (rowData.aiSummary && rowData.aiSummary.trim() !== '') {
                const summaryHtml = formatAISummary(rowData);
                row.child(summaryHtml).show();
                $(row.child()).find('td').first().addClass('ai-summary-cell');
                $(row.node()).addClass('shown');
            }
        });
    }

    function collapseAllAISummaries() {
        chefsDataTable.rows().every(function () {
            const row = this;
            if (row.child.isShown()) {
                row.child.hide();
                $(row.node()).removeClass('shown');
            }
        });
    }

    function syncAISummaryToggleButton() {
        if (!$toggleAllAISummariesButton.length) {
            return;
        }

        const $icon = $toggleAllAISummariesButton.find('i');
        const $text = $toggleAllAISummariesButton.find('.toggle-ai-summaries-label');

        if (allAISummariesExpanded) {
            $icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
            $text.text('Hide Summaries');
            $toggleAllAISummariesButton.attr('title', 'Hide AI Summaries');
        } else {
            $icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
            $text.text('Show Summaries');
            $toggleAllAISummariesButton.attr('title', 'Show AI Summaries');
        }
    }

    if ($toggleAllAISummariesButton.length > 0) {
        $toggleAllAISummariesButton.on('click', function () {
            const $button = $(this);

            if ($button.prop('disabled')) {
                return;
            }

            if (allAISummariesExpanded) {
                allAISummariesExpanded = false;
                collapseAllAISummaries();
            } else {
                allAISummariesExpanded = true;
                expandAllAISummaries();
            }

            syncAISummaryToggleButton();
        });
    }

    chefsDataTable.on('draw.dt', function () {
        if (allAISummariesExpanded) {
            expandAllAISummaries();
        } else {
            collapseAllAISummaries();
        }

        syncAISummaryToggleButton();
    });

    function adjustChefsAttachmentsTable() {
        chefsDataTable.columns.adjust();
    }

    observeAttachmentTableResize(tableWrapperSelector, adjustChefsAttachmentsTable);
    $('#attachments-tab').on('shown.bs.tab', adjustChefsAttachmentsTable);

    function formatAISummary(data) {
        const safeSummary = escapeHtml(data.aiSummary || 'No summary available');
        return (
            '<div class="ai-summary-row">' +
            '<div class="ai-summary-content">' +
            '<strong><i class="unt-icon-sm fa-solid fa-wand-sparkles"></i> Summary:</strong> ' +
            '<p class="mt-2">' +
            safeSummary +
            '</p>' +
            '</div>' +
            '</div>'
        );
    }

    function selectAttachment(type, indexes, action) {
        if (type !== 'row') {
            return;
        }

        let data = chefsDataTable.row(indexes).data();
        PubSub.publish(action, data);

        if (action == 'select_chefs_file') {
            const found = selectedAtttachments.some(
                (item) => item.chefsFileId === data.chefsFileId
            );
            if (!found) {
                selectedAtttachments.push({
                    FormSubmissionId: data.chefsSubmissionId,
                    ChefsFileId: data.chefsFileId,
                    Filename: data.fileName,
                });
            }
        } else if (action == 'deselect_chefs_file') {
            selectedAtttachments = selectedAtttachments.filter(
                (item) => item.ChefsFileId !== data.chefsFileId
            );
        }

        const hasSelection = chefsDataTable.rows({ selected: true }).count() > 0;
        $(downloadSelected).prop('disabled', !hasSelection);

        if (document.getElementById('generateAiSummaries')) {
            $generateAISummariesButton.prop(
                'disabled',
                !hasSelection
            );
        }
    }

    bindAttachmentSelectionBehavior({
        dataTable: chefsDataTable,
        tableWrapper: tableWrapperSelector,
        selectAllClass,
        onSelect: (index) => selectAttachment('row', index, 'select_chefs_file'),
        onDeselect: (index) => selectAttachment('row', index, 'deselect_chefs_file'),
    });

    $('#resyncSubmissionAttachments').on('click', function () {
        let applicationId = document.getElementById(
            'AssessmentResultViewApplicationId'
        ).value;
        try {
            unity.grantManager.attachments.attachment
                .resyncSubmissionAttachments(applicationId)
                .done(function () {
                    abp.notify.success(
                        'Submission Attachment/s has been resynced.'
                    );
                    chefsDataTable.ajax.reload();
                    chefsDataTable.columns.adjust();
                });
        } catch (error) {
            console.log(error);
        }
    });

    $('#attachments-tab').on('click', function () {
        chefsDataTable.columns.adjust();
    });

    $(downloadSelected).on('click', function () {
        const refNo =
            document.getElementsByClassName('reference-no')[0].textContent;
        const _this = $(this);
        const existingHTML = _this.html();
        const zip = new JSZip();
        const tempFiles = selectedAtttachments;

        if (tempFiles.length > 0) {
            $.ajax({
                url: '/api/app/attachment/chefs/download-all',
                data: JSON.stringify(tempFiles),
                contentType: 'application/json',
                type: 'POST',
                beforeSend: function () {
                    $(_this)
                        .html(
                            '<div class="spinner-loading"><span class="spinner-border spinner-border-sm mr-2" role="status" aria-hidden="true"></span> Downloading...</div>'
                        )
                        .prop('disabled', true);
                },
                success: function (data) {
                    data.forEach((file) => {
                        zip.file(file.fileDownloadName, file.fileContents, {
                            base64: true,
                        });
                    });

                    zip.generateAsync({ type: 'blob' }).then(function (content) {
                        const link = document.createElement('a');
                        link.href = URL.createObjectURL(content);
                        link.download = `${refNo}-Selected_Attachments.zip`;
                        link.click();
                    });

                    abp.notify.success(
                        '',
                        'The files have been downloaded successfully.'
                    );
                    $(_this).html(existingHTML).prop('disabled', false);
                },
                error: function (error) {
                    if (error.status === 403) {
                        showChefsAPIAccessError();
                    } else {
                        abp.notify.error(
                            '',
                            'The selected files exceed more than 80MB download limit. Please deselect some files and try again.'
                        );
                    }

                    $(_this).html(existingHTML).prop('disabled', false);
                },
            });
        }
    });
});

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showChefsAPIAccessError() {
    const message =
        'Please check that the CHEFS checkbox is enabled for: ' +
        "'Allow this API key to access submitted files' in the related CHEFS form";

    Swal.fire({
        title: 'CHEFS is not allowing Unity access to the File Download',
        text: message,
        confirmButtonText: 'Ok',
        customClass: {
            confirmButton: 'btn btn-primary',
        },
    });
}
