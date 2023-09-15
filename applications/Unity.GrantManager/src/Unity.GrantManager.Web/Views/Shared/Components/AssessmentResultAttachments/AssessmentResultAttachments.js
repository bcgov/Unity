$(function () {
    console.log('Script loaded');
    const l = abp.localization.getResource('GrantManager');  
    
    let inputAction = function (requestData, dataTableSettings) { 
        var assessmentId = decodeURIComponent($("#AssessmentId").val());
        if (!assessmentId) {
            return "00000000-0000-0000-0000-000000000000";
        }
        return assessmentId;
    }

    let responseCallback = function (result) {        
        console.log(result); 
        return {
            data: result
        };
    };
    
    const dataTable = $('#AssessmentResultAttachmentsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            ordering: false,
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.adjudicationAttachment.getList, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    title: '<i class="fl fl-paperclip" ></i>',
                    render: function (data) {
                        return '<i class="fl fl-paperclip" ></i>';
                    },
                },
                {
                    title: l('AssessmentResultAttachments:DocumentName'),
                    data: 'fileName',
                    className: 'data-table-header',
                },
                {
                    title: l('AssessmentResultAttachments:UploadedDate'),
                    data: 'time',
                    className: 'data-table-header',
                    render: function (data) {
                        return new Date(data).toDateString();
                    },
                },
                {
                    title: l('AssessmentResultAttachments:AttachedBy'),
                    data: 'attachedBy',
                    className: 'data-table-header',
                },
            ],
        })
    );

    dataTable.on('select', function (e, dt, type, indexes) {
        if (type === 'row') {
            const selectedData = dataTable.row(indexes).data();
            console.log('Selected Data:', selectedData);
            //PubSub.publish('select_application', selectedData);
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            const deselectedData = dataTable.row(indexes).data();
            //PubSub.publish('deselect_application', deselectedData);
        }
    });
    dataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });

    PubSub.subscribe(
        'refresh_adjudication_attachment_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
});
