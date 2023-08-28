$(function () {
    console.log('Script loaded');
    const l = abp.localization.getResource('GrantManager');
    var jsonData = {
        data: [
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                DocumentName: 'Document 1',
                UploadedDate: new Date(),
                AttachedBy: 'Melisa C',
            },
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                DocumentName: 'Document 2',
                UploadedDate: new Date(),
                AttachedBy: 'Jack H',
            },
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                DocumentName: 'Document 3',
                UploadedDate: new Date(),
                AttachedBy: 'Chris M',
            },
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                DocumentName: 'Document 4',
                UploadedDate: new Date(),
                AttachedBy: 'Don',
            },
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                DocumentName: 'Document N',
                UploadedDate: new Date(),
                AttachedBy: 'Honey',
            },
        ],
    };
    setTimeout(function () {
        const dataTable = $('#ApplicationAttachmentsTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                serverSide: true,
                order: [[1, 'asc']],
                searching: false,
                paging: false,
                select: false,
                info: false,
                scrollX: true,
                ajax: function (data, callback, settings) {
                    callback(jsonData);
                },
                columnDefs: [
                    {
                        title: l('AssessmentResultAttachments:DocumentName'),
                        data: 'DocumentName',
                        className: 'data-table-header',
                    },
                    {
                        title: l('AssessmentResultAttachments:UploadedDate'),
                        data: 'UploadedDate',
                        className: 'data-table-header',
                        render: function (data) {
                            return new Date(data).toDateString();
                        },
                    },
                    {
                        title: l('AssessmentResultAttachments:AttachedBy'),
                        data: 'AttachedBy',
                        className: 'data-table-header',
                    },
                ],
            })
        );

        dataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row') {
                var selectedData = dataTable.row(indexes).data();
                console.log('Selected Data:', selectedData);
                //PubSub.publish('select_application', selectedData);
            }
        });

        dataTable.on('deselect', function (e, dt, type, indexes) {
            if (type === 'row') {
                var deselectedData = dataTable.row(indexes).data();
                //PubSub.publish('deselect_application', deselectedData);
            }
        });
        dataTable.on('click', 'tbody tr', function (e) {
            e.currentTarget.classList.toggle('selected');
        });
    }, 3000);
    const refresh_application_list_subscription = PubSub.subscribe(
        'refresh_reviewer_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
});
