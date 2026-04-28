// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    globalThis.queueAttachmentSummary = function (triggerButton = null) {
        $('#generateAiSummaries')
            .data('trigger-button', triggerButton || null)
            .trigger('click');
    };

    const downloadAll = $('#downloadAll');
    const dt = $('#ChefsAttachmentsTable');
    let chefsDataTable;
    const aiSummaryPollIntervalMs = 15000;
    let aiSummaryPollTimeoutId = null;
    const nullPlaceholder = '—';

    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(globalThis.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
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

            $(downloadAll).prop('disabled', result.length === 0);

            const hasAISummaries = result.some(
                (item) => item.aiSummary && item.aiSummary.trim() !== ''
            );
            const $toggleButton = $('#toggleAllAISummaries');
            if ($toggleButton.length > 0) {
                $toggleButton.prop('disabled', !hasAISummaries);
                if (!hasAISummaries) {
                    $toggleButton.attr('title', 'No AI summaries available');
                } else {
                    $toggleButton.attr('title', 'Show AI Summaries');
                }
            }
        }
        return {
            data: formatItems(result),
        };
    };

    function getColumns() {
        return [
            getChefsIconColumn(),
            getChefsFileNameColumn(),
            getChefsLabelColumn(),
            getChefsFileDownloadColumn(),
        ];
    }

    function getChefsFileNameColumn() {
        return {
            title: 'Document Name',
            name: 'chefsFileName',
            data: 'fileName',
            className: 'data-table-header text-break',
            index: 1,
            orderable: false,
            width: '40%',
        };
    }

    function getChefsLabelColumn() {
        return {
            title: 'Label',
            data: 'displayName',
            className: 'data-table-header text-break',
            width: '35%',
            render: function (data) {
                return data ?? nullPlaceholder;
            },
        };
    }

    let formatItems = function (items) {
        const newData = items.map((item, index) => {
            return {
                ...item,
                rowCount: index,
            };
        });
        return newData;
    };

    chefsDataTable = dt.DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: true,
            order: [[1, 'desc']],
            searching: true,
            iDisplayLength: 25,
            lengthMenu: [10, 25, 50, 100],
            scrollX: true,
            scrollCollapse: false,
            processing: true,
            autoWidth: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.attachments.attachment
                    .getApplicationChefsFileAttachments,
                inputAction,
                responseCallback
            ),
            columnDefs: getColumns(),
            createdRow: function (row, data, dataIndex) {
                if (data.aiSummary) {
                    let summaryRow = $(
                        '<tr class="ai-summary-row" data-parent-row="' +
                            dataIndex +
                            '" style="background-color: #f8f9fa; display: none;">'
                    )
                        .append($('<td>'))
                        .append(
                            $(
                                '<td colspan="4" style="font-size: 1em; color: #6c757d; font-style: italic;">'
                            ).text(data.aiSummary)
                        );
                    $(row).after(summaryRow);
                }
            },
            externalFilterButtonId: 'btn-toggle-filter-submissions',
        })
    );

    initializeFilterRowPlugin(chefsDataTable, 'btn-toggle-filter-submissions');

    PubSub.subscribe('refresh_chefs_attachment_list', (msg, data) => {
        chefsDataTable.ajax.reload();
    });

    // Generate AI summaries for the current application attachments.
    const $generateAISummariesButton = $('#generateAiSummaries');
    const stopPolling = function () {
        if (aiSummaryPollTimeoutId) {
            clearTimeout(aiSummaryPollTimeoutId);
            aiSummaryPollTimeoutId = null;
        }
    };
    if ($generateAISummariesButton.length > 0) {
        $generateAISummariesButton.on('click', function () {
            const $button = $(this);
            const triggerButton = $button.data('trigger-button');
            const $activeButton = triggerButton ? $(triggerButton) : $button;
            const applicationId = new URL(globalThis.location.toLocaleString()).searchParams.get('ApplicationId');
            const promptVersion = globalThis.getSelectedPromptVersion?.() || null;

            $button.removeData('trigger-button');

            if (!applicationId) {
                abp.message.warn('No application was found for attachment summary generation.');
                return;
            }

            const existingHTML = $activeButton.html();
            const poll = function () {
                unity.grantManager.grantApplications.grantApplication
                    .getAIGenerationStatus(applicationId, 'attachment-summary', promptVersion)
                    .done(function (request) {
                        const status = globalThis.AIGenerationButtonState?.resolveStatus(request?.status) ?? '';

                        if (status === 'Failed') {
                            stopPolling();
                            abp.message.error(request?.failureReason || 'AI attachment summary generation failed.');
                            globalThis.AIGenerationButtonState?.restore($activeButton);
                            $activeButton.html(existingHTML).prop('disabled', false);
                            return;
                        }

                        if (!request || request.isActive === false || status === 'Completed') {
                            stopPolling();
                            globalThis.AIGenerationButtonState?.setCompleted($activeButton);
                            $activeButton.html('<span class="ai-button-content"><span>Completed</span></span>').prop('disabled', true);
                            chefsDataTable.ajax.reload();
                            return;
                        }

                        aiSummaryPollTimeoutId = setTimeout(poll, aiSummaryPollIntervalMs);
                    })
                    .fail(function () {
                        aiSummaryPollTimeoutId = setTimeout(poll, aiSummaryPollIntervalMs);
                    });
            };

            $activeButton
                .html(
                    '<span class="ai-button-content"><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span><span>Generating...</span></span>'
                )
                .prop('disabled', true);
            globalThis.AIGenerationButtonState?.setGenerating($activeButton);

            unity.grantManager.grantApplications.grantApplication
                .queueAttachmentSummary(applicationId, promptVersion)
                .done(function (request) {
                    if ((globalThis.AIGenerationButtonState?.resolveStatus(request?.status) ?? '') === 'Completed') {
                        $activeButton.html('<span class="ai-button-content"><span>Completed</span></span>').prop('disabled', true);
                        chefsDataTable.ajax.reload();
                        return;
                    }

                    aiSummaryPollTimeoutId = setTimeout(poll, 500);
                })
                .fail(function (error) {
                    if (aiSummaryPollTimeoutId) {
                        clearTimeout(aiSummaryPollTimeoutId);
                        aiSummaryPollTimeoutId = null;
                    }
                    console.error('Error queueing AI summaries:', error);
                    abp.message.error('An error occurred while queueing AI summaries. Please try again.');
                    globalThis.AIGenerationButtonState?.restore($activeButton);
                    $activeButton.html(existingHTML).prop('disabled', false);
                });
        });
    }

    // Toggle all AI summaries (only if feature is enabled)
    const $toggleAllAISummariesButton = $('#toggleAllAISummaries');
    let allAISummariesExpanded = false;

    if ($toggleAllAISummariesButton.length > 0) {
        $toggleAllAISummariesButton.on('click', function () {
            const $button = $(this);
            const $icon = $button.find('i');
            const $label = $button.find('.toggle-ai-summaries-label');

            if ($button.prop('disabled')) {
                return;
            }

            if (allAISummariesExpanded) {
                chefsDataTable.rows().every(function () {
                    const row = this;
                    if (row.child.isShown()) {
                        const $childRow = $(row.child());
                        const $summaryRow = $childRow.find('.ai-summary-row');

                        $summaryRow.removeClass('fade-in').addClass('fade-out');

                        setTimeout(function () {
                            row.child.hide();
                            $(row.node()).removeClass('shown');
                            $summaryRow.removeClass('fade-out');
                        }, 500);
                    }
                });
                $icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
                $label.text('Show Summaries');
                $button.attr('title', 'Show AI Summaries');
                allAISummariesExpanded = false;
            } else {
                chefsDataTable.rows().every(function () {
                    const row = this;
                    const rowData = row.data();

                    if (rowData.aiSummary && rowData.aiSummary.trim() !== '') {
                        const summaryHtml = formatAISummary(rowData);

                        row.child(summaryHtml).show();
                        $(row.node()).addClass('shown');

                        setTimeout(function () {
                            const $childRow = $(row.child());
                            $childRow.find('.ai-summary-row').addClass('fade-in');
                        }, 10);
                    }
                });
                $icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
                $label.text('Hide Summaries');
                $button.attr('title', 'Hide AI Summaries');
                allAISummariesExpanded = true;
            }
        });
    }

    chefsDataTable.on('draw.dt', function () {
        if (allAISummariesExpanded) {
            const $button = $('#toggleAllAISummaries');
            const $icon = $button.find('i');
            const $label = $button.find('.toggle-ai-summaries-label');
            $icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
            $label.text('Show Summaries');
            $button.attr('title', 'Show AI Summaries');
            allAISummariesExpanded = false;
        }
    });

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

    $('#resyncSubmissionAttachments').on('click', function () {
        let applicationId = document.getElementById(
            'AssessmentResultViewApplicationId'
        ).value;
        try {
            unity.grantManager.attachments.attachment
                .resyncSubmissionAttachments(applicationId)
                .done(function () {
                    abp.notify.success('Submission Attachment/s has been resynced.');
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

    $(downloadAll).on('click', function () {
        const refNo =
            document.getElementsByClassName('reference-no')[0].textContent;
        const _this = $(this);
        const existingHTML = _this.html();
        const zip = new JSZip();
        const tempFiles = chefsDataTable.rows().data().toArray().map((row) => ({
            FormSubmissionId: row.chefsSubmissionId,
            ChefsFileId: row.chefsFileId,
            Filename: row.fileName,
        }));

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
                        link.download = `${refNo}-All_Attachments.zip`;
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

function getChefsIconColumn() {
    return {
        title: '<i class="fl fl-paperclip"></i>',
        width: '40px',
        className: 'text-center',
        render: function () {
            return '<i class="fl fl-paperclip"></i>';
        },
        orderable: false,
    };
}

function getChefsFileDownloadColumn() {
    return {
        title: '',
        name: 'chefsFileDownload',
        data: 'chefsFileId',
        width: '60px',
        className: 'text-nowrap',
        render: function (data, type, full, meta) {
            let submissionId = encodeURIComponent(full.chefsSubmissionId);
            let fileId = encodeURIComponent(data);
            let fileName = full.fileName;
            let displayName = full.displayName || full.fileName;
            let html =
                '<div class="dropdown" style="float:right;">' +
                '<button class="btn btn-light dropbtn" type="button">' +
                '<i class="fl fl-attachment-more"></i>' +
                '</button>' +
                '<div class="dropdown-content">' +
                '<button class="btn fullWidth" style="margin:10px" type="button"' +
                ' chefs-submission-id="' + escapeHtmlAttribute(submissionId) + '"' +
                ' chefs-data="' + escapeHtmlAttribute(fileId) + '"' +
                ' chefs-file-name="' + escapeHtmlAttribute(fileName) + '"' +
                ' chefs-display-name="' + escapeHtmlAttribute(displayName) + '"' +
                ' onclick="previewChefsFile(event)">' +
                '<i class="fa fa-eye"></i><span>Preview Attachment</span>' +
                '</button>' +
                '<button class="btn fullWidth" style="margin:10px" type="button"' +
                ' chefs-submission-id="' + escapeHtmlAttribute(submissionId) + '"' +
                ' chefs-data="' + escapeHtmlAttribute(fileId) + '"' +
                ' chefs-file-name="' + escapeHtmlAttribute(fileName) + '"' +
                ' onclick="downloadChefsFile(event)">' +
                '<i class="fl fl-download"></i><span>Download Attachment</span>' +
                '</button>' +
                '<button class="btn fullWidth" style="margin:10px" type="button"' +
                ' onclick="updateAttachmentMetadata(\'CHEFS\',\'' + full.id + '\')">' +
                '<i class="fl fl-edit"></i><span>Edit Attachment</span>' +
                '</button>' +
                '</div>' +
                '</div>';
            return html;
        },
        orderable: false,
        index: 2,
    };
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function downloadChefsFile(event) {
    const button = event.currentTarget;
    const chefsFileId = button.getAttribute('chefs-data');
    const chefsSubmissionId = button.getAttribute('chefs-submission-id');
    const chefsFileName = button.getAttribute('chefs-file-name');
    console.log('Downloading CHEFS file:', {
        chefsFileId,
        chefsSubmissionId,
        chefsFileName,
    });

    $.ajax({
        url:
            '/api/app/attachment/chefs/' +
            chefsSubmissionId +
            '/download/' +
            chefsFileId +
            '/' +
            chefsFileName,
        type: 'GET',
        success: function (data) {
            const downloadUrl =
                '/api/app/attachment/chefs/' +
                encodeURIComponent(chefsSubmissionId) +
                '/download/' +
                encodeURIComponent(chefsFileId) +
                '/' +
                encodeURIComponent(chefsFileName);

            const link = document.createElement('a');
            link.href = downloadUrl;
            link.download = chefsFileName;
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            abp.notify.success('', 'The file has been downloaded successfully.');
        },
        error: function (error) {
            console.log('Error downloading CHEFS file:', error);
            if (error.responseText?.includes('You do not have access')) {
                showChefsAPIAccessError();
            } else {
                abp.notify.error(
                    '',
                    error.responseText ||
                        'An error occurred while downloading the file.'
                );
            }
        },
    });
}

function previewChefsFile(event) {
    const button = event.currentTarget;
    const chefsFileId = button.getAttribute('chefs-data');
    const chefsSubmissionId = button.getAttribute('chefs-submission-id');
    const chefsFileName = button.getAttribute('chefs-file-name');
    const chefsDisplayName = button.getAttribute('chefs-display-name');

    let previewModal = new abp.ModalManager({
        viewUrl: '../Attachments/PreviewAttachmentModal'
    });
    previewModal.open({
        attachmentType: 'chefs',
        ownerId: chefsSubmissionId,
        chefsFileId: chefsFileId,
        fileName: chefsFileName,
        displayName: chefsDisplayName
    });
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

