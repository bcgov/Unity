$(function () {
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });

    const l = abp.localization.getResource('GrantManager');
    const dataTable = $('#GrantApplicationsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, 'asc']],
            searching: false,
            scrollX: true,
            select: 'multi',
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.grantApplication.getList
            ),
            columnDefs: [
                {
                    title: '',
                    className: 'select-checkbox',
                    render: function (data) {
                        return '';
                    },
                },
                {
                    title: l('ProjectName'),
                    data: 'projectName',
                    className: 'data-table-header',
                },
                {
                    title: l('ReferenceNo'),
                    data: 'referenceNo',
                    className: 'data-table-header',
                },
                {
                    title: l('EligibleAmount'),
                    data: 'eligibleAmount',
                    className: 'data-table-header',
                    render: function (data) {
                        return formatter.format(data);
                    },
                },
                {
                    title: l('RequestedAmount'),
                    data: 'requestedAmount',
                    className: 'data-table-header',
                    render: function (data) {
                        return formatter.format(data);
                    },
                },
                {
                    title: l('Assignee'),
                    data: 'assignees',
                    className: 'data-table-header',
                    render: function (data) {
                        return !data || data.length === 0
                            ? null
                            : data.length > 1
                            ? l('Multiple assignees')
                            : data[0].assigneeDisplayName;
                    },
                },
                {
                    title: l('Probability'),
                    data: 'probability',
                    className: 'data-table-header',
                },
                {
                    title: l('GrantApplicationStatus'),
                    data: "status",              
                    className: 'data-table-header',
                },
                {
                    title: l('ProposalDate'),
                    data: 'proposalDate',
                    className: 'data-table-header',
                    render: function (data) {
                        return luxon.DateTime.fromISO(data, {
                            locale: abp.localization.currentCulture.name,
                        }).toLocaleString();
                    },
                },
                {
                    title: l('SubmissionDate'),
                    data: 'submissionDate',
                    className: 'data-table-header',
                    render: function (data) {
                        return luxon.DateTime.fromISO(data, {
                            locale: abp.localization.currentCulture.name,
                        }).toLocaleString();
                    },
                },
            ],
        })
    );

    dataTable.on('select', function (e, dt, type, indexes) {
        if (type === 'row') {
            var selectedData = dataTable.row(indexes).data();
            console.log('Selected Data:', selectedData);
            PubSub.publish('select_application', selectedData);
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            var deselectedData = dataTable.row(indexes).data();
            PubSub.publish('deselect_application', deselectedData);
        }
    });
    dataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });

    const refresh_application_list_subscription = PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload();
            PubSub.publish('clear_selected_application');
        }
    );
});
