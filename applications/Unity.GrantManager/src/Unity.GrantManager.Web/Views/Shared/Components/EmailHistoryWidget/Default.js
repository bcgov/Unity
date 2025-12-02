$(function () {

    let inputAction = function() {
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
            select: {
                style: 'single'
            },
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
                    width: '30px',
                    defaultContent: ''
                },
                {
                    title: 'Subject',
                    data: 'subject',
                    className: 'data-table-header text-break',
                    width: '30%'
                },
                {
                    title: 'Status',
                    data: 'status',
                    className: 'data-table-header',
                    width: '15%'
                },
                {
                    title: 'Created',
                    data: 'creationTime',
                    className: 'data-table-header',
                    width: '180px',
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
                    width: '20%',
                    render: function (data) {
                        return data ? data.name + ' ' + data.surname : '—';
                    },
                },
                {
                    title: 'To Address',
                    data: 'toAddress',
                    visible : false ,
                    className: 'data-table-header'
                },
                {
                    title: 'From Address',
                    data: 'fromAddress',
                    visible : false ,
                    className: 'data-table-header'
                },
                {
                    title: 'Body',
                    data: 'body',
                    visible : false ,
                    className: 'data-table-header'
                },
                {
                    data: 'status',
                    width: '60px',
                    className: 'text-center',
                    render: function (data, _, full, meta) {
                        if (data === 'Draft' && abp.auth.isGranted('Notifications.Email.Send')) {
                            return generateDeleteButtonContent(full, meta.row);
                        } else {
                            return '';
                        }
                    },
                    orderable: false
                }
            ],
        })
    );

    function generateDeleteButtonContent(full, row) {
        return `<button class="btn btn-delete-draft" type="button" onclick="deleteDraftEmail('${full.id}', '${row}')"><i class="fl fl-cancel"></i></button>`;
    }

    function rowFormat(d) {
        return '<div class="multi-line">' + d.body + '</div>';
    }

    // Add event listener for opening and closing details
    emailHistoryDataTable.on('click', 'td.dt-control', (e) => {
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

    emailHistoryDataTable.on('click', 'tr td', function (e) {
        let tr = e.target.closest('tr');
        let row = emailHistoryDataTable.row(tr);
        let column = emailHistoryDataTable.column( this );

        if(column.index() > 0 && column.index() < 4) {
            let data = row.data();
            PubSub.publish('email_selected', data);
        }
    });

    PubSub.subscribe('refresh_application_emails', () => {
        emailHistoryDataTable.ajax.reload();
    });

    $('#emails-tab').on('click', function () {
        emailHistoryDataTable.columns.adjust().draw();
    });
});

function deleteDraftEmail(id, rowIndex) {
    Swal.fire({
        title: "Delete Draft Email",
        text: "Are you sure you want to delete this draft email?",
        showCancelButton: true,
        confirmButtonText: "Confirm",
        customClass: {
            confirmButton: 'btn btn-primary',
            cancelButton: 'btn btn-secondary'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax(
                {
                    url: `/api/app/email-notification/${id}/email`,
                    type: "DELETE",
            })
            .then(response => {
                abp.notify.success('Draft email is successfully deleted.', 'Delete Draft Email');
                PubSub.publish('refresh_application_emails');
            })
            .catch(error => {
                console.error('There was a problem with the fetch operation:', error);
            });
        }
    });
}


