$(function () {
    const LAYOUT_NOTIFICATION_DELAYS = [0, 120, 300, 700];
    const contactsRaw = $('#ApplicantContacts_Data').val();
    const contactsData = safeParse(contactsRaw);

    const nullPlaceholder = '—';
    let contactsTable = null;
    let zoneForm = null;

    function renderTableLink(data, row) {
        if (!data || !row.applicationId) {
            return nullPlaceholder;
        }

        return `<a href="/GrantApplications/Details?ApplicationId=${row.applicationId}">${data}</a>`;
    }

    function initializeContactsTable(selector, data, columnDefs, extraConfig = {}) {
        if (!$.fn.DataTable || !$(selector).length) {
            return null;
        }

        return $(selector).DataTable(
            abp.libs.datatables.normalizeConfiguration({
                data: data,
                serverSide: false,
                order: [[0, 'asc']],
                searching: true,
                paging: true,
                pageLength: 10,
                select: false,
                info: true,
                scrollX: true,
                drawCallback: function () {
                    this.api().columns.adjust();
                },
                ...extraConfig,
                columnDefs: columnDefs
            })
        );
    }

    function scheduleLayoutNotifications() {
        LAYOUT_NOTIFICATION_DELAYS.forEach((delay) => {
            setTimeout(notifyApplicantContactsLayoutChange, delay);
        });
    }

    contactsTable = initializeContactsTable(
        '#ApplicantContactsTable',
        contactsData,
        [
            {
                title: 'Name',
                data: 'name',
                width: '18%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Email',
                data: 'email',
                width: '22%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Phone',
                data: 'phone',
                width: '13%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Title',
                data: 'title',
                width: '17%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Type',
                data: 'type',
                width: '10%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Submission #',
                data: 'referenceNo',
                width: '15%',
                render: (data, type, row) => renderTableLink(data, row)
            }
        ],
        {
            lengthMenu: [[10, 25, 50], [10, 25, 50]]
        }
    );

    scheduleLayoutNotifications();

    const form = $('#ApplicantContactsForm');
    const saveButton = $('#saveApplicantContactsBtn');

    if (form.length && saveButton.length && typeof UnityZoneForm === 'function') {
        zoneForm = new UnityZoneForm(form, {
            saveButtonSelector: '#saveApplicantContactsBtn'
        });

        zoneForm.init();

        saveButton.on('click', function (event) {
            event.preventDefault();

            if (!zoneForm || zoneForm.modifiedFields.size === 0) {
                return;
            }

            const applicantId = $('#ApplicantContacts_ApplicantId').val();
            if (!applicantId) {
                abp.notify.warn('Applicant identifier is missing.');
                return;
            }

            const modifiedFields = Array.from(zoneForm.modifiedFields ?? []);
            const contactDirty = modifiedFields.some((field) => field.startsWith('PrimaryContact.'));

            if (!contactDirty) {
                return;
            }

            const contactId = $('#ApplicantContacts_PrimaryContactId').val();
            if (isGuidEmpty(contactId)) {
                return;
            }

            const payload = {
                primaryContact: {
                    id: contactId,
                    fullName: form.find('[name="PrimaryContact.FullName"]').val(),
                    title: form.find('[name="PrimaryContact.Title"]').val(),
                    email: form.find('[name="PrimaryContact.Email"]').val(),
                    businessPhone: form.find('[name="PrimaryContact.BusinessPhone"]').val(),
                    cellPhone: form.find('[name="PrimaryContact.CellPhone"]').val()
                }
            };

            unity.grantManager.applicants.applicant
                .updateApplicantContactAddresses(applicantId, payload)
                .done(function () {
                    abp.notify.success('Contact updated.');
                    zoneForm.resetTracking();
                    updateContactTableAfterSave(payload.primaryContact, contactsTable);
                })
                .fail(function () {
                    abp.notify.error('Failed to update contact.');
                });
        });
    }
});

function safeParse(value) {
    try {
        return JSON.parse(value || '[]');
    } catch (error) {
        console.warn('Unable to parse ApplicantContacts data.', error);
        return [];
    }
}

function notifyApplicantContactsLayoutChange() {
    globalThis.dispatchEvent(new CustomEvent('applicant-contacts-layout-changed'));
}

function isGuidEmpty(value) {
    return !value || value === '00000000-0000-0000-0000-000000000000';
}

function updateContactTableAfterSave(contactPayload, contactsDt) {
    if (!contactsDt || !contactPayload) {
        return;
    }

    contactsDt.rows().every(function () {
        const rowData = this.data();
        if (rowData.id === contactPayload.id) {
            rowData.name = contactPayload.fullName || '';
            rowData.email = contactPayload.email || '';
            rowData.phone = contactPayload.businessPhone || contactPayload.cellPhone || '';
            rowData.title = contactPayload.title || '';
            this.data(rowData);
        }
    });

    contactsDt.rows().invalidate().draw(false);
}
