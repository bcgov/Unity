$(function () {
    const LAYOUT_NOTIFICATION_DELAYS = [0, 120, 300, 700];
    const contactsRaw = $('#ApplicantContacts_Data').val();
    const addressesRaw = $('#ApplicantAddresses_Data').val();
    const contactsData = safeParse(contactsRaw);
    const addressesData = safeParse(addressesRaw);

    const nullPlaceholder = '—';
    let contactsTable = null;
    let addressesTable = null;
    let zoneForm = null;

    function notifyApplicantAddressesLayoutChange() {
        window.dispatchEvent(new CustomEvent('applicant-addresses-layout-changed'));
    }

    function renderTableLink(data, row) {
        if (!data || !row.applicationId) {
            return nullPlaceholder;
        }

        return `<a href="/GrantApplications/Details?ApplicationId=${row.applicationId}">${data}</a>`;
    }

    function initializeApplicantTable(selector, data, columnDefs, extraConfig = {}) {
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
            setTimeout(notifyApplicantAddressesLayoutChange, delay);
        });
    }

    contactsTable = initializeApplicantTable(
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

    addressesTable = initializeApplicantTable(
        '#ApplicantAddressesTable',
        addressesData,
        [
            {
                title: 'Address Type',
                data: 'addressType',
                width: '13%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Address',
                data: 'street',
                width: '22%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Unit',
                data: 'unit',
                width: '8%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'City',
                data: 'city',
                width: '14%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Province',
                data: 'province',
                width: '14%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Postal Code',
                data: 'postal',
                width: '10%',
                render: (data) => data || nullPlaceholder
            },
            {
                title: 'Submission #',
                data: 'referenceNo',
                width: '13%',
                render: (data, type, row) => renderTableLink(data, row)
            }
        ]
    );

    $('#contactsAddressesSubTabs button').on('shown.bs.tab', function (e) {
        const target = $(e.target).data('bsTarget');
        if (target === '#contactsSubTabPane' && contactsTable) contactsTable.columns.adjust().draw(false);
        if (target === '#addressesSubTabPane' && addressesTable) addressesTable.columns.adjust().draw(false);
        notifyApplicantAddressesLayoutChange();
        requestAnimationFrame(() => notifyApplicantAddressesLayoutChange());
    });

    scheduleLayoutNotifications();

    const form = $('#ApplicantAddressesForm');
    const saveButton = $('#saveApplicantAddressesBtn');

    if (form.length && saveButton.length && typeof UnityZoneForm === 'function') {
        zoneForm = new UnityZoneForm(form, {
            saveButtonSelector: '#saveApplicantAddressesBtn'
        });

        zoneForm.init();

        saveButton.on('click', function (event) {
            event.preventDefault();

            if (!zoneForm || zoneForm.modifiedFields.size === 0) {
                return;
            }

            const payload = buildSavePayload(zoneForm, form);
            if (!payload) {
                return;
            }

            const applicantId = $('#ApplicantAddresses_ApplicantId').val();
            if (!applicantId) {
                abp.notify.warn('Applicant identifier is missing.');
                return;
            }

            unity.grantManager.applicants.applicant
                .updateApplicantContactAddresses(applicantId, payload)
                .done(function () {
                    abp.notify.success('Contacts and addresses updated.');
                    zoneForm.resetTracking();
                    updateTablesAfterSave(payload, contactsTable, addressesTable);
                })
                .fail(function () {
                    abp.notify.error('Failed to update contacts and addresses.');
                });
        });
    }

    function safeParse(value) {
        try {
            return JSON.parse(value || '[]');
        } catch (error) {
            console.warn('Unable to parse ApplicantAddresses data.', error);
            return [];
        }
    }

    function buildSavePayload(zoneFormInstance, $form) {
        const modifiedFields = Array.from(zoneFormInstance.modifiedFields ?? []);

        const contactDirty = modifiedFields.some((field) => field.startsWith('PrimaryContact.'));
        const physicalDirty = modifiedFields.some((field) => field.startsWith('PrimaryPhysicalAddress.'));
        const mailingDirty = modifiedFields.some((field) => field.startsWith('PrimaryMailingAddress.'));

        const payload = {};

        if (contactDirty) {
            const contactId = $('#ApplicantAddresses_PrimaryContactId').val();
            if (!isGuidEmpty(contactId)) {
                payload.primaryContact = {
                    id: contactId,
                    fullName: $form.find('[name="PrimaryContact.FullName"]').val(),
                    title: $form.find('[name="PrimaryContact.Title"]').val(),
                    email: $form.find('[name="PrimaryContact.Email"]').val(),
                    businessPhone: $form.find('[name="PrimaryContact.BusinessPhone"]').val(),
                    cellPhone: $form.find('[name="PrimaryContact.CellPhone"]').val()
                };
            }
        }

        if (physicalDirty) {
            const addressId = $('#ApplicantAddresses_PrimaryPhysicalAddressId').val();
            if (!isGuidEmpty(addressId)) {
                payload.primaryPhysicalAddress = buildAddressPayload(addressId, 'PrimaryPhysicalAddress', $form);
            }
        }

        if (mailingDirty) {
            const addressId = $('#ApplicantAddresses_PrimaryMailingAddressId').val();
            if (!isGuidEmpty(addressId)) {
                payload.primaryMailingAddress = buildAddressPayload(addressId, 'PrimaryMailingAddress', $form);
            }
        }

        if (!payload.primaryContact && !payload.primaryPhysicalAddress && !payload.primaryMailingAddress) {
            return null;
        }

        return payload;
    }

    function isGuidEmpty(value) {
        return !value || value === '00000000-0000-0000-0000-000000000000';
    }

    function buildAddressPayload(addressId, prefix, $form) {
        return {
            id: addressId,
            street: $form.find(`[name="${prefix}.Street"]`).val(),
            street2: $form.find(`[name="${prefix}.Street2"]`).val(),
            unit: $form.find(`[name="${prefix}.Unit"]`).val(),
            city: $form.find(`[name="${prefix}.City"]`).val(),
            province: $form.find(`[name="${prefix}.Province"]`).val(),
            postalCode: $form.find(`[name="${prefix}.PostalCode"]`).val()
        };
    }

    function updateTablesAfterSave(payload, contactsDt, addressesDt) {
        if (contactsDt && payload.primaryContact) {
            contactsDt.rows().every(function () {
                const rowData = this.data();
                if (rowData.id === payload.primaryContact.id) {
                    rowData.name = payload.primaryContact.fullName || '';
                    rowData.email = payload.primaryContact.email || '';
                    rowData.phone = payload.primaryContact.businessPhone || payload.primaryContact.cellPhone || '';
                    rowData.title = payload.primaryContact.title || '';
                    this.data(rowData);
                }
            });
            contactsDt.rows().invalidate().draw(false);
        }

        if (addressesDt) {
            ['primaryPhysicalAddress', 'primaryMailingAddress'].forEach((key) => {
                const addressPayload = payload[key];
                if (!addressPayload) {
                    return;
                }
                addressesDt.rows().every(function () {
                    const rowData = this.data();
                    if (rowData.id === addressPayload.id) {
                        rowData.street = addressPayload.street || '';
                        rowData.street2 = addressPayload.street2 || '';
                        rowData.unit = addressPayload.unit || '';
                        rowData.city = addressPayload.city || '';
                        rowData.province = addressPayload.province || '';
                        rowData.postal = addressPayload.postalCode || '';
                        this.data(rowData);
                    }
                });
            });

            addressesDt.rows().invalidate().draw(false);
        }
    }
});
