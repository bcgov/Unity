$(function () {
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }
    let responseCallback = function (result) {
        if (result) {
            setTimeout(function () {
                PubSub.publish('update_application_attachment_count', { chefs: result.length });
            }, 10);

            if (result.length === 0) { 
                $('#downloadAll').prop("disabled", true);
            }
        }
        
        return {
            data: result
        };
    };
    const dataTable = $('#ChefsAttachmentsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.attachment.getApplicationChefsFileAttachments, inputAction, responseCallback
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
                    data: 'name',
                    className: 'data-table-header',
                },
                {
                    title: '',
                    data: 'chefsFileId',
                    render: function (data, type, full, meta) {
                        let html = '<a href="/api/app/attachment/chefs/' + encodeURIComponent(full.chefsSumbissionId) + '/download/' + encodeURIComponent(data) + '/' + encodeURIComponent(full.name) + '" target = "_blank" download = "' + full.name + '" >';
                        html += '<button class="btn" type="button"><i class="fl fl-download"></i><span>Download</span></button></a>';
                        return html;
                    },
                    orderable: false
                }
            ],
        })
    );

    $('#resyncSubmissionAttachments').click(function () {
        let applicationId = document.getElementById('AssessmentResultViewApplicationId').value;
        try {
            unity.grantManager.grantApplications.attachment
                .resyncSubmissionAttachments(applicationId)
                .done(function () {
                    abp.notify.success(
                        'Submission Attachment/s has been resynced.'
                    );
                    dataTable.ajax.reload();
                    dataTable.columns.adjust();
                });
        }
        catch (error) {
            console.log(error);
        }
    });

    $('#attachments-tab').on('click', function () {
        dataTable.columns.adjust();
    });

    $('#downloadAll').on('click', function () {
        const _this = $(this);
        const existingHTML = _this.html();
        const zip = new JSZip();
        const chefsAttactmentsTable = document.getElementById('ChefsAttachmentsTable');
        const anchorTags = chefsAttactmentsTable.querySelectorAll('a');
        const tempFiles = [];

        if (anchorTags.length > 0) { 
            anchorTags.forEach(item => {
                if (item !== null) {
                    let tempFileName = item.pathname.split('/').pop();
                    let tempChefsFileId = item.pathname.split('/').slice(-2)[0];
                    let tempFormSubmissionId = item.pathname.split('/').slice(-4)[0];

                    tempFiles.push({
                        FormSubmissionId: tempFormSubmissionId,
                        ChefsFileId: tempChefsFileId,
                        Filename: tempFileName
                    });
                }
            });
        } 

        ////Calls an endpoint
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
                        link.download = 'Submission_Files.zip';
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
                    'Error downloading the files.'
                );
                $(_this).html(existingHTML).prop('disabled', false);
            }
        });

    });
});