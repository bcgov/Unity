$(function () {

    let inputAction = function () {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }

    let responseCallback = function (result) {
        const normalizedResult = (result || []).map(item => ({
            ...item,
            templateName: resolveTemplateName(item)
        }));

        if (result) {
            setTimeout(function () {
                PubSub.publish('update_application_emails_count', { itemCount: normalizedResult.length });
            }, 10);
        }

        return {
            data: normalizedResult
        };
    };

    const enableEmailDelay = $('#EmailHistoryTable').data('enable-email-delay') === true
        || $('#EmailHistoryTable').data('enable-email-delay') === 'true';

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
            scrollX: false,
            ajax: abp.libs.datatables.createAjax(
                unity.notifications.emailNotifications.emailNotification.getHistoryByApplicationId, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    className: 'dt-control',
                    orderable: false,
                    data: null,
                    width: '2%',
                    defaultContent: ''
                },
                {
                    title: 'Subject',
                    data: 'subject',
                    className: 'data-table-header text-break',
                    width: '38%'
                },
                {
                    title: 'Status',
                    data: 'status',
                    className: 'data-table-header',
                    width: '12%'
                },
                {
                    title: 'Created',
                    data: 'creationTime',
                    className: 'data-table-header',
                    width: '12%',
                    render: function (data) {
                        return data ? luxon.DateTime.fromISO(data, {
                            locale: abp.localization.currentCulture.name,
                        }).toLocaleString({
                            day: "numeric",
                            year: "numeric",
                            month: "numeric",
                            hour: "numeric",
                            minute: "numeric"
                        }) : '—';
                    }
                },
                {
                    title: 'Sent Date',
                    data: 'sentDateTime',
                    className: 'data-table-header',
                    width: '12%',
                    render: function (data) {
                        return data ? luxon.DateTime.fromISO(data, {
                            locale: abp.localization.currentCulture.name,
                        }).toLocaleString({
                            day: "numeric",
                            year: "numeric",
                            month: "numeric",
                            hour: "numeric",
                            minute: "numeric"
                        }) : '—';
                    }
                },
                {
                    title: 'Sent By',
                    data: 'sentBy',
                    className: 'data-table-header',
                    width: enableEmailDelay ? '10%' : '16%',
                    render: function (data, type, full) {
                        if (full.scheduledNotificationId && full.scheduledNotificationId !== '00000000-0000-0000-0000-000000000000') {
                            return 'Automated Notification';
                        }
                        return data ? data.name + ' ' + data.surname : '—';
                    },
                },
                {
                    title: 'Scheduled Send',
                    data: 'sendOnDateTime',
                    className: 'data-table-header text-center',
                    width: '10%',
                    visible: enableEmailDelay,
                    render: function (data, type) {
                        if (!data) return '—';
                        return formatScheduledSendDateTimeUtcToPacific(data, type) || '—';
                    }
                },
                {
                    title: 'To Address',
                    data: 'toAddress',
                    visible: false,
                    className: 'data-table-header'
                },
                {
                    title: 'From Address',
                    data: 'fromAddress',
                    visible: false,
                    className: 'data-table-header'
                },
                {
                    title: 'Body',
                    data: 'body',
                    visible: false,
                    className: 'data-table-header'
                },
                {
                    title: 'Template Name',
                    data: 'templateName',
                    visible: false,
                    className: 'data-table-header'
                },
                {
                    title: 'Scheduled Notification ID',
                    data: 'scheduledNotificationId',
                    visible: false,
                    className: 'data-table-header',
                    defaultContent: ''
                },
                {
                    data: 'status',
                    width: '8%',
                    className: 'text-center',
                    render: function (data, _, full, meta) {
                        // Show delete button for drafts
                        if (data === 'Draft' && abp.auth.isGranted('Notifications.Email.DeleteDraft')) {
                            return generateDeleteButtonContent(full, meta.row);
                        }
                        // Show cancel button for scheduled sends that haven't passed yet
                        else if (full.sendOnDateTime && abp.auth.isGranted('Notifications.Email.CancelScheduled')) {
                            const sendOnDateTime = parseUtcDateTime(full.sendOnDateTime);
                            const now = luxon.DateTime.utc();
                            if (sendOnDateTime && sendOnDateTime > now) {
                                return generateCancelScheduledButtonContent(full, meta.row);
                            }
                        }
                        return '';
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
        let column = emailHistoryDataTable.column(this);

        if (column.index() > 0 && column.index() < 4) {
            const data = row.data();
            const normalizedSelectedRow = {
                ...data,
                templateName: resolveTemplateName(data)
            };

            PubSub.publish('email_selected', normalizedSelectedRow);
        }
    });

    PubSub.subscribe('refresh_application_emails', () => {
        emailHistoryDataTable.ajax.reload();
    });

    $('#emails-tab').on('click', function () {
        emailHistoryDataTable.columns.adjust().draw();
    });
});

function resolveTemplateName(emailRow) {
    const value = [
        emailRow?.templateName,
        emailRow?.emailTemplateName,
        emailRow?.template,
        emailRow?.TemplateName,
        emailRow?.EmailTemplateName,
        emailRow?.Template
    ].find(v => typeof v === 'string' && v.trim().length > 0);

    return (value || '').trim();
}

function parseUtcDateTime(value) {
    if (!value) {
        return null;
    }

    const normalized = String(value).trim().replace(' ', 'T');
    const withUtcSuffix = /([zZ]|[+-]\d{2}:?\d{2})$/.test(normalized)
        ? normalized
        : `${normalized}Z`;

    const dateTime = luxon.DateTime.fromISO(withUtcSuffix, { zone: 'utc' });
    return dateTime.isValid ? dateTime : null;
}

function formatScheduledSendDateTimeUtcToPacific(value, type) {
    if (type !== 'display' && type !== 'filter') {
        return value;
    }

    const utcDateTime = parseUtcDateTime(value);
    if (!utcDateTime) {
        return '—';
    }

    return utcDateTime
        .setZone('UTC-7')
        .toLocaleString({
            day: 'numeric',
            year: 'numeric',
            month: 'numeric',
            hour: 'numeric',
            minute: 'numeric'
        });
}

function generateCancelScheduledButtonContent(full, row) {
    return `<button class="btn btn-delete-delayed" type="button" onclick="cancelScheduledEmail('${full.id}', '${row}')"><i class="fl fl-cancel"></i></button>`;
}

function cancelScheduledEmail(id, rowIndex) {
    Swal.fire({
        title: "Cancel Scheduled Email",
        text: "Are you sure you want to cancel this scheduled email?",
        showCancelButton: true,
        confirmButtonText: "Confirm",
        customClass: {
            confirmButton: 'btn btn-primary',
            cancelButton: 'btn btn-secondary'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/api/app/email-notification/${id}/email`,
                type: "DELETE",
            })
                .then(response => {
                    abp.notify.success('Scheduled email has been cancelled.', 'Cancel Scheduled Email');
                    PubSub.publish('refresh_application_emails');
                    PubSub.publish('scheduled_email_cancelled', { id: id });
                })
                .catch(error => {
                    console.error('There was a problem with the fetch operation:', error);
                    
                    // Extract error message from API response
                    const errorMessage = error?.responseJSON?.error?.message || 'Failed to cancel scheduled email. Please try again.';
                    
                    abp.notify.error(errorMessage, 'Cancel Scheduled Email');
                });
        }
    });
}

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
                    PubSub.publish('draft_email_deleted', { id: id });
                })
                .catch(error => {
                    console.error('There was a problem with the fetch operation:', error);
                    
                    // Extract error message from API response
                    const errorMessage = error?.responseJSON?.error?.message || 'Failed to delete draft email. Please try again.';
                    
                    abp.notify.error(errorMessage, 'Delete Draft Email');
                });
        }
    });
}


