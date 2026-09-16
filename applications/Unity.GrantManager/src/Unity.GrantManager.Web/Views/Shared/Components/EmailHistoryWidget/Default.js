$(function () {

    let inputAction = function () {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }

    let hasReceivedInitialResponse = false;
    let hasLoadedEmptyState = false;

    let responseCallback = function (result) {
        const normalizedResult = (result || []).map(item => ({
            ...item,
            templateName: resolveTemplateName(item)
        }));

        if (!hasReceivedInitialResponse) {
            hasReceivedInitialResponse = true;
            hasLoadedEmptyState = normalizedResult.length === 0;
        }

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
            dom: 'Bfrtip',
            serverSide: false,
            order: [[2, 'desc']],
            searching: false,
            paging: false,
            select: {
                style: 'single'
            },
            buttons: [
                {
                    extend: 'selectedSingle',
                    text: 'Print',
                    action: function (e, dt, node, config) {
                        let rowData = dt.row({ selected: true }).data();
                        if (rowData) {
                            printEmailHistoryRow(rowData);
                        }
                    }
                }
            ],
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
                    render: function (data, type, full) {
                        if (full.sendOnDateTime) return '—';
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
                        // Only needed when the Scheduled Send column is shown, and only for
                        // tables that started out empty (see hasLoadedEmptyState above).
                        const addWidthClass = enableEmailDelay && hasLoadedEmptyState;

                        // Show delete button for drafts
                        if (data === 'Draft' && abp.auth.isGranted('Notifications.Email.DeleteDraft')) {
                            return generateDeleteButtonContent(full, meta.row, addWidthClass);
                        }
                        // Show cancel button for scheduled sends that haven't passed yet
                        else if (full.sendOnDateTime && abp.auth.isGranted('Notifications.Email.CancelScheduled')) {
                            const sendOnDateTime = parseUtcDateTime(full.sendOnDateTime);
                            const now = luxon.DateTime.utc();
                            if (sendOnDateTime && sendOnDateTime > now) {
                                return generateCancelScheduledButtonContent(full, meta.row, addWidthClass);
                            }
                        }
                        return '';
                    },
                    orderable: false
                }
            ],
        })
    );

    function generateDeleteButtonContent(full, row, addWidthClass) {
        const widthClass = addWidthClass ? ' btn-w30' : '';
        return `<button class="btn btn-delete-draft${widthClass}" type="button" onclick="deleteDraftEmail('${full.id}', '${row}')"><i class="fl fl-cancel"></i></button>`;
    }

    // Move the DataTables print button out of the table toolbar and into the header row
    emailHistoryDataTable.buttons(0, null).container().appendTo('#EmailHistoryButtonSection');

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
            row.child(emailHistoryHandlebars(row.data())).show();
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
        emailHistoryDataTable.ajax.reload(() => {
            emailHistoryDataTable.columns.adjust().draw();
        }, false);
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

function generateCancelScheduledButtonContent(full, row, addWidthClass) {
    const widthClass = addWidthClass ? ' btn-w30' : '';
    return `<button class="btn btn-delete-delayed${widthClass}" type="button" onclick="cancelScheduledEmail('${full.id}', '${row}')"><i class="fl fl-cancel"></i></button>`;
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

const emailHistoryTemplate = `<div class="emailHistoryPreview">
    <dl class="row">
        <dt class="text-nowrap col-1">From:</dt>
        <dd class="col-11">{{fromAddress}}</dd>
        <dt class="text-nowrap col-1">Sent:</dt>
        <dd class="col-11">{{default (formatDateTime sentDateTime) 'Not Sent'}}</dd>
        <dt class="text-nowrap col-1">To:</dt>
        <dd class="col-11">{{csvList toAddress}}</dd>
        {{#if cc}}
        <dt class="text-nowrap col-1">CC:</dt>
        <dd class="col-11">{{csvList cc}}</dd>
        {{/if}}
        {{#if bcc}}
        <dt class="text-nowrap col-1">BCC:</dt>
        <dd class="col-11">{{csvList bcc}}</dd>
        {{/if}}
        <dt class="text-nowrap col-1">Subject:</dt>
        <dd class="col-11">{{subject}}</dd>
    </dl>
    <div class="row">
    {{safeHtml body}}
    </div>
</div>`;

Handlebars.registerHelper("csvList", function (listText) {
    return listText.replaceAll(",", "; ");
});

// Persisted email bodies are user/template-authored HTML and are not trusted;
// sanitize with DOMPurify's HTML allowlist before rendering as markup.
Handlebars.registerHelper('safeHtml', function (html) {
    if (typeof DOMPurify === 'undefined') {
        return Handlebars.escapeExpression(html || '');
    }

    const sanitized = DOMPurify.sanitize(html || '', { USE_PROFILES: { html: true } });
    return new Handlebars.SafeString(sanitized);
});

Handlebars.registerHelper('default', function (value, fallback) {
    return (value !== undefined && value !== null && value !== '') ? value : fallback;
});

// Mirrors the DataTable's Sent Date column rendering, plus the timezone abbreviation in brackets
Handlebars.registerHelper('formatDateTime', function (value) {
    if (!value) {
        return null;
    }

    const dateTime = luxon.DateTime.fromISO(value, {
        locale: abp.localization.currentCulture.name,
    });

    if (!dateTime.isValid) {
        return value;
    }

    const formatted = dateTime.toLocaleString({
        day: 'numeric',
        year: 'numeric',
        month: 'numeric',
        hour: 'numeric',
        minute: 'numeric'
    });

    const zoneName = dateTime.offsetNameShort;
    return zoneName ? `${formatted} (${zoneName})` : formatted;
});

const emailHistoryHandlebars = Handlebars.compile(emailHistoryTemplate);

const emailPrintTemplate = `<div class="email-print-container">
    <dl class="email-print-header row">
        <dt class="text-nowrap col-1">From:</dt>
        <dd class="col-11">{{fromAddress}}</dd>
        <dt class="text-nowrap col-1">Sent:</dt>
        <dd class="col-11">{{default (formatDateTime sentDateTime) 'Not Sent'}}</dd>
        <dt class="text-nowrap col-1">To:</dt>
        <dd class="col-11">{{csvList toAddress}}</dd>
        {{#if cc}}
        <dt class="text-nowrap col-1">CC:</dt>
        <dd class="col-11">{{csvList cc}}</dd>
        {{/if}}
        {{#if bcc}}
        <dt class="text-nowrap col-1">BCC:</dt>
        <dd class="col-11">{{csvList bcc}}</dd>
        {{/if}}
        <dt class="text-nowrap col-1">Subject:</dt>
        <dd class="col-11">{{subject}}</dd>
    </dl>
    <hr />
    <div class="email-print-body">
    {{safeHtml body}}
    </div>
</div>`;

const emailPrintHandlebars = Handlebars.compile(emailPrintTemplate);

function printEmailHistoryRow(rowData) {
    const referenceNo = $('#applicationBreadcrumbWidget .reference-no').text().trim();
    const applicantName = $('#applicationBreadcrumbWidget .applicant-name').text().trim();
    const printTitle = buildEmailPrintTitle(referenceNo, applicantName);

    openEmailPrintInNewTab(emailPrintHandlebars(rowData), printTitle);
}

function buildEmailPrintTitle(referenceNo, applicantName) {
    const parts = [referenceNo, applicantName, 'Notification'].filter(Boolean);
    // Strip characters that are invalid in downloaded file names
    return parts.join('-').replace(/[\\/:*?"<>|]/g, '') || 'Notification';
}

function openEmailPrintInNewTab(emailPrintHtml, printTitle) {
    const newTab = globalThis.open('', '_blank');
    const doc = newTab.document;

    doc.open();
    doc.close();
    doc.title = printTitle;

    const stylesheets = [
        { href: '/libs/bootstrap/css/bootstrap.min.css' },
        { href: '/Views/Shared/Components/EmailHistoryWidget/EmailPrint.css' }
    ];

    const stylesReady = Promise.all(stylesheets.map(({ href }) => new Promise((resolve, reject) => {
        const link = doc.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        link.onload = resolve;
        link.onerror = reject;
        doc.head.appendChild(link);
    })));

    doc.body.innerHTML = emailPrintHtml;

    // Chain script.onload directly instead of relying on the popup's window load
    // event, which does not reliably fire after doc.open/close plus a body rewrite.
    const jqueryScript = doc.createElement('script');
    jqueryScript.src = '/libs/jquery/jquery.js';
    stylesReady.then(() => {
        jqueryScript.onload = () => {
            const printScript = doc.createElement('script');
            printScript.src = '/Views/Shared/Components/EmailHistoryWidget/loadEmailPrint.js';
            printScript.onload = () => newTab.executeOperations();
            doc.head.appendChild(printScript);
        };
        doc.head.appendChild(jqueryScript);
    });
}