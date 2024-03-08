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

    $('#attachments-tab').one('click', function () {
        dataTable.columns.adjust();
    });
});