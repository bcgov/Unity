$(function () {
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    const l = abp.localization.getResource('GrantManager');
    const dataTable = $('#GrantApplicationsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]],
            searching: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(unity.grantManager.grantApplications.grantApplication.getList),
            columnDefs: [
                {
                    title: "",
                    className: 'select-checkbox',
                    targets: 0,
                    orderable: false,
                    data: ""
                },                
                {
                    title: l('ProjectName'),
                    data: "projectName"
                },
                {
                    title: l('ReferenceNo'),
                    data: "referenceNo",
                },
                {
                    title: l('EligibleAmount'),
                    data: "eligibleAmount",
                    render: function (data) {
                        return formatter.format(data)
                    }
                },
                {
                    title: l('RequestedAmount'),
                    data: "requestedAmount",
                    render: function (data) {
                        return formatter.format(data)
                    }
                },
                {
                    title: l('Assignee'),
                    data: "assignees",
                    render: function (data) {
                        return (!data || data.length === 0) ? null : data.length > 1 ? l('Multiple') : data[0].assigneeDisplayName;                        
                    }
                },
                {
                    title: l('Probability'),
                    data: "probability",
                },
                {
                    title: l('GrantApplicationStatus'),
                    data: "status",              
                },
                {
                    title: l('ProposalDate'),
                    data: "proposalDate",
                    render: function (data) {
                        return luxon
                            .DateTime
                            .fromISO(data, {
                                locale: abp.localization.currentCulture.name
                            }).toLocaleString();
                    }
                },
                {
                    title: l('SubmissionDate'),
                    data: "submissionDate",
                    render: function (data) {
                        return luxon
                            .DateTime
                            .fromISO(data, {
                                locale: abp.localization.currentCulture.name
                            }).toLocaleString();
                    }
                },
            ]
        })
    );

    dataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });
});
