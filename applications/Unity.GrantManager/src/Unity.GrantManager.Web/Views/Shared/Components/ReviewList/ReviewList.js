$(function () {
    console.log('Script loaded');
    const l = abp.localization.getResource('GrantManager');
    var jsonData = {
        data: [
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                ReviewerName: 'Reviewer 1',
                StartDate: new Date(),
                EndDate: new Date(),
                Status: {
                    Id: 'njsdnf-sjd f-sn fd',
                    InternalStatus: 'Completed',
                },
                Recommended: true,
            },
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                ReviewerName: 'Reviewer 2',
                StartDate: new Date(),
                EndDate: new Date(),
                Status: {
                    Id: 'njsdnf-sjd f-sn fd',
                    InternalStatus: 'In progress',
                },
                Recommended: false,
            },
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                ReviewerName: 'Reviewer 3',
                StartDate: new Date(),
                EndDate: new Date(),
                Status: {
                    Id: 'njsdnf-sjd f-sn fd',
                    InternalStatus: 'Completed',
                },
                Recommended: true,
            },
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                ReviewerName: 'Reviewer 4',
                StartDate: new Date(),
                EndDate: new Date(),
                Status: {
                    Id: 'njsdnf-sjd f-sn fd',
                    InternalStatus: 'Under Team Lead Review',
                },
                Recommended: true,
            },
            {
                Id: '79071522-3c7c-4206-93ef-64538afb00a5',
                ReviewerName: 'Reviewer N',
                StartDate: new Date(),
                EndDate: new Date(),
                Status: {
                    Id: 'njsdnf-sjd f-sn fd',
                    InternalStatus: 'Completed',
                },
                Recommended: true,
            },
        ],
    };

    const dataTable = $('#ReviewListTable').DataTable(
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
                    title: l('ReviewerList:ReviewerName'),
                    data: 'ReviewerName',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:StartDate'),
                    data: 'StartDate',
                    className: 'data-table-header',
                    render: function (data) {
                        return new Date(data).toDateString();
                    },
                },
                {
                    title: l('ReviewerList:EndDate'),
                    data: 'EndDate',
                    className: 'data-table-header',
                    render: function (data) {
                        return new Date(data).toDateString();
                    },
                },
                {
                    title: l('ReviewerList:Status'),
                    data: 'Status',
                    className: 'data-table-header',
                    render: function (data) {
                        return data.InternalStatus;
                    },
                },
                {
                    title: l('ReviewerList:Recommended'),
                    data: 'Recommended',
                    className: 'data-table-header',
                    render: function (data) {
                        return data === true ? 'Recommended for Approval' : 'Recommended for Denial';
                    },
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

    dataTable.on('draw', function () {
        // This code will execute when the DataTable completes drawing
        console.log('DataTable has completed drawing.');
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

    const refresh_review_list_subscription = PubSub.subscribe(
        'refresh_reviewer_list',
        (msg, data) => {
            dataTable.ajax.reload();
        }
    );
});
