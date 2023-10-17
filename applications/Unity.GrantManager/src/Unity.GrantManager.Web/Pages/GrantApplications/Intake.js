$(function () {
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    const l = abp.localization.getResource('GrantManager');

    const placeholderText = function () {
        return "<span class=\"badge text-bg-secondary\">PLACEHOLDER</span>";
    }

    let inputAction = function (requestData, dataTableSettings) {
        return document.getElementById('PassFormIdToJavaScript').value;
    }

    $('#GrantApplicationsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]],
            searching: false,
            scrollX: true,
            ajax:
                abp.libs.datatables.createAjax(
                    unity.grantManager.intakes.submission.getSubmissionsList, inputAction),
            columnDefs: [
                {
                    title: l('ProjectName'),
                    data: "projectTitle"
                },
                {
                    title: l('ReferenceNo'),
                    data: "confirmationId",
                    render: function (data) {
                        return '<a href="#">' + data + '</a>';
                    }
                },
                {
                    title: l('EligibleAmount'),
                    data: "totalRequestToMjf",
                    render: function (data) {
                        return formatter.format(data)
                    }
                },
                {
                    title: l('RequestedAmount'),
                    data: "eligibleCost",
                    render: function (data) {
                        return formatter.format(data)
                    }
                },
                {
                    title: l('Assignee'),
                    data: "assignees",
                    render: placeholderText
                    //render: function (data) {
                    //    return (!data || data.length === 0) ? null : data.length > 1 ? l('Multiple') : data[0].username;
                    //}
                },
                {
                    title: l('Probability'),
                    data: "probability",
                    render: placeholderText
                },
                {
                    title: l('GrantApplicationStatus'),
                    data: "status",
                    render: placeholderText
                    //render: (data) => l('Enum:GrantApplicationStatus.' + data)
                },
                {
                    title: l('ProposalDate'),
                    data: "proposalDate",
                    render: placeholderText
                },
                {
                    title: l('ProposalDate'),
                    data: "proposalDate",
                    render: placeholderText
                    //render: function (data) {
                    //    return luxon
                    //        .DateTime
                    //        .fromISO(data, {
                    //            locale: abp.localization.currentCulture.name
                    //        }).toLocaleString();
                    //}
                },
                {
                    title: l('SubmissionDate'),
                    data: "createdAt",
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
});
