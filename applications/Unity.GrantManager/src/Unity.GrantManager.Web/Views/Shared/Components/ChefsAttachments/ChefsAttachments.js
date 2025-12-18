// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    const downloadAll = $('#downloadAll');
    const dt = $('#ChefsAttachmentsTable');
    let chefsDataTable;
    let selectedAtttachments = [];
    const nullPlaceholder = '—';

    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString())
            .searchParams;
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

            if (result.length === 0 || selectedAtttachments.length === 0) {
                $(downloadAll).prop('disabled', true);

                if (document.getElementById('generateAiSummaries')) {
                    $generateAISummariesButton.prop('disabled', true);
                }
            }

            // Check if any attachments have AI summaries and enable/disable toggle button
            const hasAISummaries = result.some(
                (item) => item.aiSummary && item.aiSummary.trim() !== ''
            );
            const $toggleButton = $('#toggleAllAISummaries');
            if ($toggleButton.length > 0) {
                $toggleButton.prop('disabled', !hasAISummaries);
                if (!hasAISummaries) {
                    $toggleButton.attr('title', 'No AI summaries available');
                } else {
                    $toggleButton.attr('title', 'Toggle AI Summaries');
                }
            }
        }
        return {
            data: formatItems(result),
        };
    };

    function getColumns() {
        return [
            getSelectColumn('Select Attachment', 'rowCount', 'chefs-files'),
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
                let $cellWrapper = $('<div>').addClass(
                    'd-flex align-items-center'
                );
                let $textWrapper = $('<div>')
                    .addClass('w-100')
                    .append(data ?? nullPlaceholder);
                let $buttonWrapper = $('<div>').addClass('flex-shrink-1');

                let $editButton = $('<button>')
                    .addClass('btn btn-sm edit-button px-0 float-end')
                    .attr({
                        'aria-label': 'Edit',
                        title: 'Edit',
                    })
                    .append($('<i>').addClass('fl fl-edit'));

                $cellWrapper.append($textWrapper);
                $buttonWrapper.append($editButton);
                $cellWrapper.append($buttonWrapper);

                return $cellWrapper.prop('outerHTML');
            },
        };
    }

    function getChefsFileDownloadColumn() {
        return {
            title: '',
            name: 'chefsFileDownload',
            data: 'chefsFileId',
            width: '150px',
            className: 'text-nowrap',
            render: function (data, type, full, meta) {
                let html =
                    '<button class="btn px-2" name="chefs-download-btn" type="button"' +
                    ' chefs-submission-id=' +
                    encodeURIComponent(full.chefsSumbissionId) +
                    ' chefs-data=' +
                    encodeURIComponent(data) +
                    ' chefs-file-name=' +
                    encodeURIComponent(full.fileName) +
                    ' onclick="downloadChefsFile(event)">' +
                    '<i class="fl fl-download"></i>' +
                    '<span>Download</span>' +
                    '</button>';
                return html;
            },
            orderable: false,
            index: 2,
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
            select: {
                style: 'multiple',
                selector: 'td:not(:nth-child(4))',
            },
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
                            ).html(data.aiSummary)
                        );
                    $(row).after(summaryRow);
                }
            },
        })
    );

    PubSub.subscribe('refresh_chefs_attachment_list', (msg, data) => {
        chefsDataTable.ajax.reload();
    });

    chefsDataTable.on(
        'click',
        'td button.edit-button',
        function (event, dt, type, indexes) {
            event.stopPropagation();
            let rowData = chefsDataTable.row(event.target.closest('tr')).data();
            updateAttachmentMetadata('CHEFS', rowData.id);
        }
    );
    //Generate AI summaries for attachments
    const $generateAISummariesButton = $('#generateAiSummaries');
    if ($generateAISummariesButton.length > 0) {
        $generateAISummariesButton.on('click', function () {
            const $button = $(this);
            const selectedRows = chefsDataTable.rows({ selected: true }).data();

            if (selectedRows.length === 0) {
                abp.message.warn(
                    'Please select at least one attachment to generate summaries.'
                );
                return;
            }

            // Get attachment IDs from selected rows
            const attachmentIds = selectedRows.toArray().map((row) => row.id);

            const existingHTML = $button.html();

            // Call the backend API
            $.ajax({
                url: '/api/app/attachment/generate-aISummaries-attachments',
                data: JSON.stringify(attachmentIds),
                contentType: 'application/json',
                type: 'POST',
                beforeSend: function () {
                    $button
                        .html(
                            '<span class="spinner-border spinner-border-sm mr-2" role="status" aria-hidden="true"></span> Generating...'
                        )
                        .prop('disabled', true);
                },
                success: function (summaries) {
                    abp.notify.success(
                        'AI summaries generated successfully for ' +
                            summaries.length +
                            ' attachment(s).'
                    );

                    // Reload the table to show new summaries
                    chefsDataTable.ajax.reload();

                    // Enable the toggle button now that we have summaries
                    $('#toggleAllAISummaries').prop('disabled', false);

                    $button.html(existingHTML).prop('disabled', false);
                },
                error: function (error) {
                    console.error('Error generating AI summaries:', error);
                    abp.notify.error(
                        'An error occurred while generating AI summaries. Please try again.'
                    );
                    $button.html(existingHTML).prop('disabled', false);
                },
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
            const $text = $button.contents().filter(function () {
                return this.nodeType === 3;
            });

            // Don't do anything if button is disabled
            if ($button.prop('disabled')) {
                return;
            }

            if (allAISummariesExpanded) {
                // Collapse all
                chefsDataTable.rows().every(function () {
                    const row = this;
                    if (row.child.isShown()) {
                        const $childRow = $(row.child());
                        const $summaryRow = $childRow.find('.ai-summary-row');

                        // Add fade-out class to the summary row
                        $summaryRow.removeClass('fade-in').addClass('fade-out');

                        // Wait for animation to complete before hiding
                        setTimeout(function () {
                            row.child.hide();
                            $(row.node()).removeClass('shown');
                            $summaryRow.removeClass('fade-out');
                        }, 500); // Match animation duration
                    }
                });
                $icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
                $text.replaceWith('Show Summaries');
                $button.attr('title', 'Show AI Summaries');
                allAISummariesExpanded = false;
            } else {
                // Expand all
                chefsDataTable.rows().every(function () {
                    const row = this;
                    const rowData = row.data();

                    // Only expand if there's an AI summary
                    if (rowData.aiSummary && rowData.aiSummary.trim() !== '') {
                        // Create the summary HTML
                        const summaryHtml = formatAISummary(rowData);

                        // Show the child row
                        row.child(summaryHtml).show();
                        $(row.node()).addClass('shown');

                        // Add fade-in animation after DOM is ready
                        setTimeout(function () {
                            const $childRow = $(row.child());
                            $childRow
                                .find('.ai-summary-row')
                                .addClass('fade-in');
                        }, 10);
                    }
                });
                $icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
                $text.replaceWith('Hide Summaries');
                $button.attr('title', 'Hide AI Summaries');
                allAISummariesExpanded = true;
            }
        });
    }

    // Reset AI summary expansion state when table is reloaded
    chefsDataTable.on('draw.dt', function () {
        if (allAISummariesExpanded) {
            const $button = $('#toggleAllAISummaries');
            const $icon = $button.find('i');
            const $text = $button.contents().filter(function () {
                return this.nodeType === 3;
            });
            $icon.removeClass('fl-chevron-up').addClass('fa-wand-sparkles');
            $text.replaceWith('Show Summaries');
            $button.attr('title', 'Show AI Summaries');
            allAISummariesExpanded = false;
        }
    });

    function formatAISummary(data) {
        return (
            '<div class="ai-summary-row">' +
            '<div class="ai-summary-content">' +
            '<strong><i class="unt-icon-sm fa-solid fa-wand-sparkles"></i> Summary:</strong> ' +
            '<p class="mt-2">' +
            (data.aiSummary || 'No summary available') +
            '</p>' +
            '</div>' +
            '</div>'
        );
    }

    chefsDataTable.on('select', function (e, dt, type, indexes) {
        if (indexes?.length) {
            indexes.forEach((index) => {
                $('#row_' + index).prop('checked', true);
                if ($('.chkbox:checked').length == $('.chkbox').length) {
                    $('.select-all-chefs-files').prop('checked', true);
                }
                selectAttachment(type, index, 'select_chefs_file');
            });
        }
    });

    chefsDataTable.on('deselect', function (e, dt, type, indexes) {
        if (indexes?.length) {
            indexes.forEach((index) => {
                $('#row_' + index).prop('checked', false);
                if ($('.chkbox:checked').length != $('.chkbox').length) {
                    $('.select-all-chefs-files').prop('checked', false);
                }
                selectAttachment(type, index, 'deselect_chefs_file');
            });
        }
    });

    function selectAttachment(type, indexes, action) {
        if (type === 'row') {
            let data = chefsDataTable.row(indexes).data();
            PubSub.publish(action, data);

            if (action == 'select_chefs_file') {
                const found = selectedAtttachments.some(
                    (item) => item.chefsFileId === data.chefsFileId
                );
                if (!found) {
                    selectedAtttachments.push({
                        FormSubmissionId: data.chefsSumbissionId,
                        ChefsFileId: data.chefsFileId,
                        Filename: data.fileName,
                    });
                }
            } else if (action == 'deselect_chefs_file') {
                const filtedItems = selectedAtttachments.filter(
                    (item) => item.ChefsFileId !== data.chefsFileId
                );
                selectedAtttachments = filtedItems;
            }

            if (selectedAtttachments.length > 0) {
                $(downloadAll).prop('disabled', false);
            } else {
                $(downloadAll).prop('disabled', true);
            }

            if (
                document.getElementById('generateAiSummaries') &&
                selectedAtttachments.length > 0
            ) {
                $generateAISummariesButton.prop('disabled', false);
            } else {
                $generateAISummariesButton.prop('disabled', true);
            }
        }
    }

    $('.select-all-chefs-files').on('click', function () {
        if ($(this).is(':checked')) {
            chefsDataTable.rows({ page: 'current' }).select();
        } else {
            chefsDataTable.rows({ page: 'current' }).deselect();
        }
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

    $(downloadAll).on('click', function () {
        const refNo =
            document.getElementsByClassName('reference-no')[0].textContent;
        const _this = $(this);
        const existingHTML = _this.html();
        const zip = new JSZip();
        const tempFiles = selectedAtttachments;

        if (tempFiles.length > 0) {
            //Calls an endpoint
            $.ajax({
                url: '/api/app/attachment/chefs/download-all',
                data: JSON.stringify(tempFiles),
                contentType: 'application/json',
                type: 'POST',
                beforeSend: function () {
                    //Add loading spinner
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

                    zip.generateAsync({ type: 'blob' }).then(function (
                        content
                    ) {
                        const link = document.createElement('a');
                        link.href = URL.createObjectURL(content);
                        link.download = `${refNo}-All_Attachments.zip`;
                        link.click();
                    });

                    abp.notify.success(
                        '',
                        'The files have been downloaded successfully.'
                    );
                    //show original HTML and enable
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

    //Calls an endpoint
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
            // Download file by navigating to the endpoint
            const downloadUrl =
                '/api/app/attachment/chefs/' +
                encodeURIComponent(chefsSubmissionId) +
                '/download/' +
                encodeURIComponent(chefsFileId) +
                '/' +
                encodeURIComponent(chefsFileName);

            // Create a temporary link and trigger download
            const link = document.createElement('a');
            link.href = downloadUrl;
            link.download = chefsFileName;
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            abp.notify.success(
                '',
                'The file has been downloaded successfully.'
            );
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
