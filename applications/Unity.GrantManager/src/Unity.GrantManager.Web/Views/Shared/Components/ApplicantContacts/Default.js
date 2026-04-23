$(function () {
    const LAYOUT_NOTIFICATION_DELAYS = [0, 120, 300, 700];

    let widgetRoot = $();
    let applicantId = null;
    let canEdit = false;
    let contactsData = [];
    let roleLabelMap = {};
    let localizedTexts = {};
    let contactsTable = null;
    let savedOrder = null;
    let editContactModal = null;

    function t(key, fallback) {
        return localizedTexts[key] || fallback;
    }

    function format(template, value) { // NOSONAR - intentionally scoped here; helper is only used within this widget scenario and should remain inside the closure
        return (template || '').replace('{0}', value);
    }

    function ensureEditContactModal() {
        if (editContactModal) {
            return editContactModal;
        }

        editContactModal = new abp.ModalManager(abp.appPath + 'ApplicantContact/EditModal');
        editContactModal.onResult(function () {
            abp.notify.success(t('contactSaved', 'Contact saved.'));
            refreshWidget();
        });

        return editContactModal;
    }

    function readWidgetState() {
        widgetRoot = $('.applicant-contacts-widget');
        applicantId = widgetRoot.data('applicant-id');
        canEdit = widgetRoot.data('can-edit') === true || widgetRoot.data('can-edit') === 'true';

        contactsData = safeParse($('#ApplicantContacts_Data').val()).map(toCamelCase);
        localizedTexts = safeParse($('#ApplicantContacts_Texts').val());

        roleLabelMap = {};
        safeParse($('#ApplicantContacts_RoleOptions').val()).forEach(function (option) {
            const normalized = toCamelCase(option);
            roleLabelMap[normalized.value] = normalized.label;
        });
    }

    function pickCaseInsensitive(row, names) { // NOSONAR - intentionally scoped here; closure context is needed for widget encapsulation
        if (!row) { return undefined; }
        for (const name of names) {
            if (row[name] !== undefined && row[name] !== null) { return row[name]; }
            const lower = name.charAt(0).toLowerCase() + name.slice(1);
            if (row[lower] !== undefined && row[lower] !== null) { return row[lower]; }
            const upper = name.charAt(0).toUpperCase() + name.slice(1);
            if (row[upper] !== undefined && row[upper] !== null) { return row[upper]; }
        }
        return undefined;
    }

    function renderReferenceLink(data, type, row) {
        const appId = pickCaseInsensitive(row, ['applicationId']);
        const refNo = data || pickCaseInsensitive(row, ['referenceNo']);
        const hasAppId = !!appId && appId !== '00000000-0000-0000-0000-000000000000';
        if (!hasAppId) {
            return t('nullPlaceholder', '—');
        }
        const label = refNo || t('view', 'View');
        return `<a href="/GrantApplications/Details?ApplicationId=${appId}">${label}</a>`;
    }

    function renderPrimaryBadge(row) { // NOSONAR - intentionally scoped here; closure context is needed for widget encapsulation
        if (!row.isPrimary) {
            return '';
        }
        const title = row.isPrimaryInferred
            ? t('primaryInferredTooltip', 'Primary contact (auto-selected by most recent timestamp; not explicitly set).')
            : t('primaryExplicitTooltip', 'Primary contact');
        return `<span class="applicant-contact-primary-badge"
                      data-bs-toggle="tooltip"
                      data-bs-placement="left"
                      title="${title}">
                    <i class="fl fl-checkmark text-success" aria-hidden="true"></i>
                    <span class="visually-hidden">${t('primaryContactVisuallyHidden', 'Primary contact')}</span>
                </span>`;
    }

    function renderActions(data, type, row) {
        if (row.contactType !== 'Applicant') {
            let sourceInfoMessage;
            if (row.contactType === 'Application') {
                sourceInfoMessage = t('sourceInfoApplication', 'Sourced from the Application submission. Managed on the Application Details form and cannot be edited here.');
            } else if (row.contactType === 'ApplicantAgent') {
                sourceInfoMessage = t('sourceInfoApplicantAgent', 'Sourced from the Applicant Agent on the CHEFS submission. Captured at intake and cannot be edited here.');
            } else {
                sourceInfoMessage = format(t('sourceInfoGeneric', 'Sourced from {0} and cannot be edited here.'), row.contactType || 'another record');
            }

            const message = sourceInfoMessage;
            const escaped = $('<div/>').text(message).html();
            return `<span class="applicant-contact-source-info"
                          data-bs-toggle="tooltip"
                          data-bs-placement="left"
                          title="${escaped}">
                        <i class="fa fa-info-circle text-muted" aria-hidden="true"></i>
                        <span class="visually-hidden">${escaped}</span>
                    </span>`;
        }
        if (!canEdit) {
            return '';
        }
        const setPrimaryDisabled = row.isPrimary ? 'disabled' : '';
        return `<div class="dropdown applicant-contact-actions">
                    <button type="button"
                            class="btn btn-sm btn-link p-0 applicant-contact-menu-btn"
                            data-bs-toggle="dropdown"
                            data-bs-strategy="fixed"
                            aria-expanded="false"
                            data-contact-id="${row.contactId}">
                        <i class="fa fa-ellipsis-v" aria-hidden="true"></i>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end">
                        <li>
                            <button class="dropdown-item applicant-contact-edit-btn"
                                    data-contact-id="${row.contactId}">${t('edit', 'Edit')}</button>
                        </li>
                        <li>
                            <button class="dropdown-item applicant-contact-set-primary-btn"
                                    data-contact-id="${row.contactId}" ${setPrimaryDisabled}>${t('setAsPrimary', 'Set as Primary')}</button>
                        </li>
                    </ul>
                </div>`;
    }

    function setAsPrimary(contact) {
        const service = unity?.grantManager?.applicantProfile?.applicantContact;
        if (!service) {
            abp.notify.error(t('serviceUnavailable', 'Applicant contact service is not available.'));
            return;
        }
        service.setPrimary(applicantId, contact.contactId)
            .done(function () {
                abp.notify.success(t('contactSetPrimary', 'Contact set as primary.'));
                refreshWidget();
            })
            .fail(function () {
                abp.notify.error(t('setPrimaryFailed', 'Failed to set contact as primary.'));
            });
    }

    function initializeContactsTable(order) {
        if (!$.fn.DataTable || !$('#ApplicantContactsTable').length) {
            return null;
        }

        if ($.fn.DataTable.isDataTable('#ApplicantContactsTable')) {
            $('#ApplicantContactsTable').DataTable().destroy();
        }

        return $('#ApplicantContactsTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                data: contactsData,
                serverSide: false,
                order: order || [[0, 'asc']],
                searching: true,
                paging: true,
                pageLength: 10,
                select: false,
                info: true,
                processing: true,
                scrollX: true,
                stateSave: true,
                stateDuration: 0,
                stateSaveCallback: function (settings, data) {
                    try {
                        localStorage.setItem(
                            'DataTables_ApplicantContactsTable_' + (applicantId || 'none'),
                            JSON.stringify(data));
                    } catch (e) { console.error('Failed to save DataTables state to localStorage.', e); }
                },
                stateLoadCallback: function () {
                    try {
                        const raw = localStorage.getItem(
                            'DataTables_ApplicantContactsTable_' + (applicantId || 'none'));
                        return raw ? JSON.parse(raw) : null;
                    } catch (e) { console.error('Failed to load DataTables state from localStorage.', e); return null; }
                },
                drawCallback: function () {
                    this.api().columns.adjust();
                    $('#ApplicantContactsTable [data-bs-toggle="tooltip"]').each(function () {
                        const existing = bootstrap.Tooltip.getInstance(this);
                        if (existing) { existing.dispose(); }
                        bootstrap.Tooltip.getOrCreateInstance(this);
                    });
                },
                lengthMenu: [[10, 25, 50], [10, 25, 50]],
                columnDefs: [
                    {
                        title: t('columnName', 'Name'),
                        data: 'name',
                        width: '18%',
                        render: (data, type, row) => {
                            const name = data || t('nullPlaceholder', '—');
                            if (type !== 'display') {
                                return name;
                            }
                            return renderPrimaryBadge(row) + name;
                        },
                        targets: 0
                    },
                    {
                        title: t('columnType', 'Type'),
                        data: 'role',
                        width: '13%',
                        render: (data) => roleLabelMap[data] || data || t('nullPlaceholder', '—'),
                        targets: 1
                    },
                    {
                        title: t('columnEmail', 'Email'),
                        data: 'email',
                        width: '22%',
                        render: (data) => data || t('nullPlaceholder', '—'),
                        targets: 2
                    },
                    {
                        title: t('columnPhone', 'Phone'),
                        data: null,
                        width: '13%',
                        render: (data, type, row) => {
                            const phone = row.workPhoneNumber || row.mobilePhoneNumber;
                            return phone || t('nullPlaceholder', '—');
                        },
                        targets: 3
                    },
                    {
                        title: t('columnTitle', 'Title'),
                        data: 'title',
                        width: '18%',
                        render: (data) => data || t('nullPlaceholder', '—'),
                        targets: 4
                    },
                    {
                        title: t('columnSubmission', 'Submission #'),
                        data: 'referenceNo',
                        width: '10%',
                        render: renderReferenceLink,
                        targets: 5
                    },
                    {
                        title: t('columnActions', ''),
                        data: null,
                        orderable: false,
                        searchable: false,
                        width: '48px',
                        className: 'text-center',
                        render: renderActions,
                        targets: 6
                    }
                ]
            })
        );
    }

    function scheduleLayoutNotifications() {
        LAYOUT_NOTIFICATION_DELAYS.forEach((delay) => {
            setTimeout(notifyApplicantContactsLayoutChange, delay);
        });
    }

    function refreshWidget() {
        if (contactsTable) {
            try {
                savedOrder = contactsTable.order();
                contactsTable.processing(true);
            } catch (e) { console.error('Failed to enable DataTables processing indicator.', e); }
        }

        $.ajax({
            url: abp.appPath + 'Widget/ApplicantContacts/Refresh',
            type: 'GET',
            dataType: 'html',
            data: { applicantId: applicantId },
            success: function (html) {
                const container = widgetRoot.parent();
                container.html(html);
                bindWidget(savedOrder);
                savedOrder = null;
                abp.event.trigger('applicant-contacts-refreshed');
            },
            error: function () {
                if (contactsTable) {
                    try { contactsTable.processing(false); } catch (e) { console.error('Failed to disable DataTables processing indicator.', e); }
                }
            }
        });
    }

    function bindWidget(order) {
        readWidgetState();
        contactsTable = initializeContactsTable(order);
        scheduleLayoutNotifications();
    }

    $(document).on('click', '.applicant-contact-edit-btn', function () {
        const id = $(this).data('contact-id');
        ensureEditContactModal().open({
            id: id,
            applicantId: applicantId
        });
    });

    $(document).on('click', '.applicant-contact-set-primary-btn', function () {
        const id = $(this).data('contact-id');
        const contact = contactsData.find((c) => c.contactId === id);
        if (!contact) return;
        setAsPrimary(contact);
    });

    bindWidget();
});

function safeParse(value) {
    try {
        return JSON.parse(value || '[]');
    } catch (error) {
        console.warn('Unable to parse ApplicantContacts data.', error);
        return [];
    }
}

function toCamelCase(obj) {
    if (obj === null || typeof obj !== 'object') {
        return obj;
    }
    const result = {};
    Object.keys(obj).forEach(function (key) {
        const camelKey = key.length > 0
            ? key.charAt(0).toLowerCase() + key.slice(1)
            : key;
        result[camelKey] = obj[key];
    });
    return result;
}

function notifyApplicantContactsLayoutChange() {
    globalThis.dispatchEvent(new CustomEvent('applicant-contacts-layout-changed'));
}
