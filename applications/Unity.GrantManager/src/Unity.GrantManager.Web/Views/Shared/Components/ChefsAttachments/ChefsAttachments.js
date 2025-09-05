// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
$(function () {
    const downloadAll = $("#downloadAll");
    const dt = $('#ChefsAttachmentsTable');
    let chefsDataTable;
    let selectedAtttachments = [];
    const nullPlaceholder = '—';

    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }

    let responseCallback = function (result) {
        const formattedData = formatItems(result);
        
        if (formattedData.length <= 0) {
            $('.dataTables_paginate').hide();
        }

        if (formattedData) {
            setTimeout(function () {
                PubSub.publish('update_application_attachment_count', { chefs: formattedData.length });
            }, 10);

            if (formattedData.length === 0 || selectedAtttachments.length === 0) {
                $(downloadAll).prop("disabled", true);
            }
        }
        return {
            data: formattedData
        };
    };

    function getColumns() {
        return [
            getSelectColumn('Select Attachment', 'rowCount', 'chefs-files'),
            getChefsFileNameColumn(),
            getChefsLabelColumn(),
            getChefsFileDownloadColumn(),
            getChefsExpandColumn(),
        ]
    }

    function getChefsFileNameColumn() {
        return {
            title: 'Document Name',
            name: 'chefsFileName',
            data: 'fileName',
            className: 'data-table-header text-break',
            index: 1,
            orderable: false,
            width: "50%"
        };
    }

    function getChefsLabelColumn() {
        return {
            title: 'Label',
            data: 'displayName',
            className: 'data-table-header text-break',
            width: "50%",
            render: function (data) {
                let $cellWrapper   = $('<div>').addClass('d-flex align-items-center');
                let $textWrapper   = $('<div>').addClass('w-100').append(data ?? nullPlaceholder);
                let $buttonWrapper = $('<div>').addClass('flex-shrink-1');

                let $editButton = $('<button>')
                    .addClass('btn btn-sm edit-button px-0 float-end')
                    .attr({
                        'aria-label': 'Edit',
                        'title': 'Edit'
                    }).append($('<i>').addClass('fl fl-edit'));

                $cellWrapper.append($textWrapper);
                $buttonWrapper.append($editButton);
                $cellWrapper.append($buttonWrapper);

                return $cellWrapper.prop('outerHTML');
            }
        }
    }

    function getChefsFileDownloadColumn() {
        return {
            title: '',
            name: 'chefsFileDownload',
            data: 'chefsFileId',
            render: function (data, type, full, meta) {
                let html = '<a href="/api/app/attachment/chefs/' + encodeURIComponent(full.chefsSumbissionId) + '/download/' + encodeURIComponent(data) + '/' + encodeURIComponent(full.fileName) + '" target = "_blank" download = "' + full.fileName + '" >';
                html += '<button class="btn" type="button"><i class="fl fl-download"></i><span>Download</span></button></a>';
                return html;
            },
            orderable: false,
            index: 2,
        };
    }

    function getChefsExpandColumn() {
        return {
            title: '',
            name: 'chefsExpand',
            data: null,
            render: function (data, type, full, meta) {
                return '<button class="btn btn-sm ai-toggle-btn" data-row="' + meta.row + '" style="border: none; background: transparent; padding: 4px 8px;" title="View AI Summary"><i class="fl fl-search" style="transition: opacity 0.3s;"></i></button>';
            },
            orderable: false,
            width: '50px',
            className: 'text-center'
        };
    }

    let formatItems = function (items) {
        const hardcodedData = {
            'Document.pdf': {
                chefsFileId: 'CHF-2025-001234',
                chefsSumbissionId: 'SUB-2025-001234',
                createdDate: '2025-01-10T09:30:00',
                fileSize: 2048576,
                displayName: '',
                aiSummary: 'This document contains detailed project specifications including budget breakdowns, timeline milestones, and stakeholder agreements for the proposed community development initiative.'
            },
            'Final_v3.docx': {
                chefsFileId: 'CHF-2025-001235',
                chefsSumbissionId: 'SUB-2025-001235',
                createdDate: '2025-01-11T14:15:00',
                fileSize: 1536000,
                displayName: '',
                aiSummary: 'Program narrative (5 pages) describing objectives, outputs, and evaluation plan; includes a logic model but no budget or timeline.'
            },
            'IMG_2025-01-12_143322.jpg': {
                chefsFileId: 'CHF-2025-001236',
                chefsSumbissionId: 'SUB-2025-001236',
                createdDate: '2025-01-12T14:33:22',
                fileSize: 4194304,
                displayName: '',
                aiSummary: 'Photograph showing construction progress at the northwest corner of the facility, demonstrating completed foundation work and initial framing structure.'
            },
            'Notes.docx': {
                chefsFileId: 'CHF-2025-001237',
                chefsSumbissionId: 'SUB-2025-001237',
                createdDate: '2025-01-13T10:45:00',
                fileSize: 512000,
                displayName: '',
                aiSummary: 'Internal meeting notes (2 pages) summarizing roles, risks, and next steps; not an official endorsement, but useful context.'
            }
        };

        // If no items, return hardcoded data as array
        if (!items || items.length === 0) {
            const hardcodedArray = Object.keys(hardcodedData).map((fileName, index) => ({
                fileName: fileName,
                ...hardcodedData[fileName],
                rowCount: index
            }));
            return hardcodedArray;
        }

        const newData = items.map((item, index) => {
            const hardcoded = hardcodedData[item.fileName] || {};
            return {
                ...item,
                ...hardcoded,
                // Use the real AI summary from the database, fallback to hardcoded demo data, then to placeholder
                aiSummary: item.aiSummary || hardcoded.aiSummary || 'AI analysis not yet available for this attachment.',
                rowCount: index
            };
        });
        return newData;
    }

    chefsDataTable = dt.DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: true,
            order: [[1, 'desc']],
            searching: true,
            iDisplayLength: 25,
            lengthMenu: [10, 25, 50, 100],
            scrollX: true,
            scrollCollapse: true,
            processing: true,
            autoWidth: true,
            select: {
                style: 'multiple',
                selector: 'td:not(:nth-child(5)):not(:last-child)',
            },
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.attachments.attachment.getApplicationChefsFileAttachments,
                inputAction,
                responseCallback
            ),
            columnDefs: getColumns(),
            createdRow: function(row, data, dataIndex) {
                if (data.aiSummary) {
                    var summaryRow = $('<tr class="ai-summary-row" data-parent-row="' + dataIndex + '" style="background-color: #f8f9fa; display: none;">')
                        .append($('<td>'))
                        .append($('<td colspan="4" style="font-size: 1em; color: #6c757d; font-style: italic;">')
                            .html(data.aiSummary));
                    $(row).after(summaryRow);
                }
            }
        })
    );

    PubSub.subscribe(
        'refresh_chefs_attachment_list',
        (msg, data) => {
            chefsDataTable.ajax.reload();
        }
    );

    chefsDataTable.on('click', 'td button.edit-button', function (event, dt, type, indexes) {
        event.stopPropagation();
        let rowData = chefsDataTable.row(event.target.closest('tr')).data();
        updateAttachmentMetadata('CHEFS', rowData.id);
    });

    chefsDataTable.on('select', function (e, dt, type, indexes) {
        if (indexes?.length) {
            indexes.forEach(index => {
                $("#row_" + index).prop("checked", true);
                if ($(".chkbox:checked").length == $(".chkbox").length) {
                    $(".select-all-chefs-files").prop("checked", true);
                }
                selectAttachment(type, index, 'select_chefs_file');
            });
        }
    });

    // Hide/show AI summary rows during DataTable operations
    chefsDataTable.on('draw', function() {
        $('.ai-summary-row').remove();
        chefsDataTable.rows().every(function(rowIdx) {
            var data = this.data();
            var row = this.node();
            if (data && data.aiSummary) {
                var summaryRow = $('<tr class="ai-summary-row" data-parent-row="' + rowIdx + '" style="background-color: #f8f9fa; display: none;">')
                    .append($('<td>'))
                    .append($('<td colspan="4" style="font-size: 1em; color: #6c757d; font-style: italic;">')
                        .html(data.aiSummary));
                $(row).after(summaryRow);
            }
        });
    });

    // Toggle AI summary on magnifying glass click with accordion behavior
    $(document).on('click', '.ai-toggle-btn', function(e) {
        e.stopPropagation();
        var $btn = $(this);
        var $icon = $btn.find('i');
        var rowIdx = $btn.data('row');
        var $summaryRow = $('.ai-summary-row[data-parent-row="' + rowIdx + '"]');
        
        if ($summaryRow.is(':visible')) {
            // Close current row
            $summaryRow.hide();
            $icon.css('opacity', '0.6');
            $btn.attr('title', 'View AI Summary');
        } else {
            // Close all other open rows first (accordion behavior)
            $('.ai-summary-row:visible').hide();
            $('.ai-toggle-btn i').css('opacity', '0.6');
            $('.ai-toggle-btn').attr('title', 'View AI Summary');
            
            // Open the clicked row
            $summaryRow.show();
            $icon.css('opacity', '1');
            $btn.attr('title', 'Hide AI Summary');
        }
    });

    chefsDataTable.on('deselect', function (e, dt, type, indexes) {
        if (indexes?.length) {
            indexes.forEach(index => {
                $("#row_" + index).prop("checked", false);
                if ($(".chkbox:checked").length != $(".chkbox").length) {
                    $(".select-all-chefs-files").prop("checked", false);
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
                const found = selectedAtttachments.some(item => item.chefsFileId === data.chefsFileId);
                if (!found) {
                    selectedAtttachments.push({
                        FormSubmissionId: data.chefsSumbissionId,
                        ChefsFileId: data.chefsFileId,
                        Filename: data.fileName
                    });
                }
            }
            else if (action == 'deselect_chefs_file') {
                const filtedItems = selectedAtttachments.filter(item => item.ChefsFileId !== data.chefsFileId);
                selectedAtttachments = filtedItems;
            }

            if (selectedAtttachments.length > 0) {
                $(downloadAll).prop("disabled", false);
            } else {
                $(downloadAll).prop("disabled", true);
            }
        }
    }

    $('.select-all-chefs-files').on('click', function () {
        if ($(this).is(':checked')) {
            chefsDataTable.rows({ 'page': 'current' }).select();
        }
        else {
            chefsDataTable.rows({ 'page': 'current' }).deselect();
        }
    });

    $('#resyncSubmissionAttachments').on('click', function () {
        let applicationId = document.getElementById('AssessmentResultViewApplicationId').value;
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
        }
        catch (error) {
            console.log(error);
        }
    });

    $('#attachments-tab').on('click', function () {
        chefsDataTable.columns.adjust();
    });

    $(downloadAll).on('click', function () {
        const refNo = document.getElementsByClassName('reference-no')[0].textContent;
        const _this = $(this);
        const existingHTML = _this.html();
        const zip = new JSZip();
        const tempFiles = selectedAtttachments;

        if (tempFiles.length > 0) {
            //Calls an endpoint
            $.ajax({
                url: "/api/app/attachment/chefs/download-all",
                data: JSON.stringify(tempFiles),
                contentType: "application/json",
                type: "POST",
                beforeSend: function () {
                    //Add loading spinner
                    $(_this).html('<div class="spinner-loading"><span class="spinner-border spinner-border-sm mr-2" role="status" aria-hidden="true"></span> Downloading...</div>').prop('disabled', true);
                },
                success: function (data) {
                    data.forEach(file => {
                        zip.file(file.fileDownloadName, file.fileContents, { base64: true });
                    });

                    zip.generateAsync({ type: "blob" })
                        .then(function (content) {
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
                error: function () {
                    abp.notify.error(
                        '',
                        'The selected files exceed more than 80MB download limit. Please deselect some files and try again.'
                    );
                    $(_this).html(existingHTML).prop('disabled', false);
                }
            });
        }

    });

});