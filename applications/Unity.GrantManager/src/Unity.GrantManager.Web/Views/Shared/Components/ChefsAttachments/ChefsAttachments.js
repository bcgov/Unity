$(function () {
    const downloadAll = $("#downloadAll");
    const dt = $('#ChefsAttachmentsTable');
    let chefsDataTable;
    let selectedAtttachments = [];

    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }
    let responseCallback = function (result) {
        if (result.length <= 0) {
            $('.dataTables_paginate').hide();
        }

        if (result) {
            setTimeout(function () {
                PubSub.publish('update_application_attachment_count', { chefs: result.length });
            }, 10);

            if (result.length === 0 || selectedAtttachments.length === 0) {
                $(downloadAll).prop("disabled", true);
            }
        }
        return {
            data: formatItems(result)
        };
    };

    function getColumns() {
        return [
            getSelectColumn('Select Attachment', 'rowCount', 'chefs-files'),
            getChefsFileNameColumn(),
            getChefsFileDownloadColumn(),
        ]
    }

    function getChefsFileNameColumn() {
        return {
            title: l('AssessmentResultAttachments:DocumentName'),
            name: 'chefsFileName',
            data: 'name',
            className: 'data-table-header',
            index: 1,
            orderable: false,
        };
    }

    function getChefsFileDownloadColumn() {
        return {
            title: '',
            name: 'chefsFileDownload',
            data: 'chefsFileId',
            render: function (data, type, full, meta) {
                let html = '<a href="/api/app/attachment/chefs/' + encodeURIComponent(full.chefsSumbissionId) + '/download/' + encodeURIComponent(data) + '/' + encodeURIComponent(full.name) + '" target = "_blank" download = "' + full.name + '" >';
                html += '<button class="btn" type="button"><i class="fl fl-download"></i><span>Download</span></button></a>';
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
            select: {
                style: 'multiple',
                selector: 'td:not(:nth-child(8))',
            },
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.attachment.getApplicationChefsFileAttachments,
                inputAction,
                responseCallback
            ),
            columnDefs: getColumns()
        })
    );

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
                        Filename: data.name
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
            unity.grantManager.grantApplications.attachment
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