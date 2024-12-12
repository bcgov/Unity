$(function () {
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }
    let responseCallback = function (result) {
        if (result) {
            setTimeout(function () {
                PubSub.publish('update_application_emails_count', { itemCount: result.length });
            }, 10);
        }

        return {
            data: result
        };
    };

    let emailHistoryDataTable = $('#EmailHistoryTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'desc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.notifications.emailNotifications.emailNotification.getHistoryByApplicationId, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    className: 'dt-control',
                    orderable: false,
                    data: null,
                    defaultContent: ''
                },
                {
                    title: 'Subject',
                    data: 'subject',
                    className: 'data-table-header'
                },
                {
                    title: 'Sent',
                    data: 'creationTime',
                    className: 'data-table-header',
                    render: function (data) {
                        return data != null ? luxon.DateTime.fromISO(data, {
                            locale: abp.localization.currentCulture.name,
                        }).toLocaleString({
                            day: "numeric",
                            year: "numeric",
                            month: "numeric",
                            hour: "numeric",
                            minute: "numeric",
                            second: "numeric"
                        }) : '';
                    }
                },
                {
                    title: 'Sent By',
                    data: 'sentBy',
                    className: 'data-table-header',
                    render: function (data) {
                        return data.name + ' ' + data.surname;
                    },
                }
            ],
        })
    );

    emailHistoryDataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });

    function rowFormat(d) {
        return '<div class="multi-line">' + d.body + '</div>';
    }

    // Add event listener for opening and closing details
    emailHistoryDataTable.on('click', 'td.dt-control', function (e) {
        let tr = e.target.closest('tr');
        let row = emailHistoryDataTable.row(tr);

        if (row.child.isShown()) {
            // This row is already open - close it
            row.child.hide();
        }
        else {
            // Open this row
            row.child(rowFormat(row.data())).show();
        }
    });

    PubSub.subscribe('refresh_application_emails', (msg, data) => {
        emailHistoryDataTable.ajax.reload();
    });

    $('#emails-tab').on('click', function () {
        emailHistoryDataTable.columns.adjust().draw();
    });
});
