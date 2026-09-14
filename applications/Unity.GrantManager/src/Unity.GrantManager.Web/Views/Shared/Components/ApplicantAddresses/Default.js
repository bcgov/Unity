$(function () {
    const LAYOUT_NOTIFICATION_DELAYS = [0, 120, 300, 700];
    const addressesRaw = $('#ApplicantAddresses_Data').val();
    const addressesData = safeParse(addressesRaw);

    const nullPlaceholder = '—';
    let addressesTable = null;
    let zoneForm = null;
    let isSaving = false;
    const PHYSICAL_ADDRESS_FIELDS = new Set([
        'PrimaryPhysicalAddress.Street',
        'PrimaryPhysicalAddress.Street2',
        'PrimaryPhysicalAddress.Unit',
        'PrimaryPhysicalAddress.City',
        'PrimaryPhysicalAddress.Province',
        'PrimaryPhysicalAddress.PostalCode'
    ]);
    const MAILING_ADDRESS_FIELDS = new Set([
        'PrimaryMailingAddress.Street',
        'PrimaryMailingAddress.Street2',
        'PrimaryMailingAddress.Unit',
        'PrimaryMailingAddress.City',
        'PrimaryMailingAddress.Province',
        'PrimaryMailingAddress.PostalCode'
    ]);


    function renderTableText(data, type) {
        const value = data || nullPlaceholder;
        return type === 'display' ? abp.utils.htmlEscape(value) : value;
    }

    function renderTableLink(data, type, row) {
        if (type !== 'display' || !data || !row.applicationId) {
            return renderTableText(data, type);
        }

        return `<a href="/GrantApplications/Details?ApplicationId=${encodeURIComponent(row.applicationId)}">${abp.utils.htmlEscape(data)}</a>`;
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

    addressesTable = initializeApplicantTable(
        '#ApplicantAddressesTable',
        addressesData,
        [
            {
                title: 'Address Type',
                data: 'addressType',
                width: '13%',
                render: renderTableText
            },
            {
                title: 'Address',
                data: 'street',
                width: '22%',
                render: (data, type, row) => renderTableText([data, row.street2].filter(Boolean).join(', '), type)
            },
            {
                title: 'Unit',
                data: 'unit',
                width: '8%',
                render: renderTableText
            },
            {
                title: 'City',
                data: 'city',
                width: '14%',
                render: renderTableText
            },
            {
                title: 'Province',
                data: 'province',
                width: '14%',
                render: renderTableText
            },
            {
                title: 'Postal Code',
                data: 'postal',
                width: '10%',
                render: renderTableText
            },
            {
                title: 'Submission #',
                data: 'referenceNo',
                width: '13%',
                render: renderTableLink
            }
        ]
    );

    scheduleLayoutNotifications();

    const form = $('#ApplicantAddressesForm');
    const saveButton = $('#saveApplicantAddressesBtn');

    if (form.length && saveButton.length && typeof UnityZoneForm === 'function') {
        zoneForm = new UnityZoneForm(form, {
            saveButtonSelector: '#saveApplicantAddressesBtn'
        });

        zoneForm.init();

        // The shared tracker handles change/blur; also track typing and pasting immediately.
        form.find('input, textarea').on('input', function () {
            if (isSaving) {
                return;
            }

            const field = $(this);
            const name = field.attr('name');
            if (name) {
                zoneForm.checkFieldModified(field, name);
            }
        });

        saveButton.on('click', function (event) {
            event.preventDefault();

            if (isSaving || !zoneForm || zoneForm.modifiedFields.size === 0) {
                return;
            }

            let payload;
            try {
                payload = buildSavePayload(zoneForm, form);
            } catch (error) {
                abp.notify.warn(error.message);
                return;
            }
            if (!payload) {
                return;
            }

            const applicantId = $('#ApplicantAddresses_ApplicantId').val();
            if (!applicantId) {
                abp.notify.warn('Applicant identifier is missing.');
                return;
            }

            isSaving = true;
            zoneForm.setSaving(true);
            const editableFields = form.find('input:enabled:not([type="hidden"]), select:enabled, textarea:enabled');
            editableFields.prop('disabled', true);

            unity.grantManager.applicants.applicant
                .updateApplicantContactAddresses(applicantId, payload, { abpHandleError: false })
                .done(function (result) {
                    applySavedApplicantAddresses(result, form);
                    updateAddressTableAfterSave(result, addressesTable);
                    zoneForm.resetTracking();
                    abp.notify.success('Addresses saved.');
                    scheduleLayoutNotifications();
                })
                .fail(function (error) {
                    abp.notify.error(error?.message || 'Failed to save addresses.');
                })
                .always(function () {
                    editableFields.prop('disabled', false);
                    isSaving = false;
                    zoneForm.setSaving(false);
                });
        });
    }


    function buildSavePayload(zoneFormInstance, $form) {
        const modifiedFields = Array.from(zoneFormInstance.modifiedFields ?? []);

        const physicalModified = modifiedFields.filter((field) => PHYSICAL_ADDRESS_FIELDS.has(field));
        const mailingModified = modifiedFields.filter((field) => MAILING_ADDRESS_FIELDS.has(field));

        const payload = {};

        if (physicalModified.length > 0) {
            const addressId = $('#ApplicantAddresses_PrimaryPhysicalAddressId').val();
            payload.primaryPhysicalAddress = buildAddressPayload(addressId, 'PrimaryPhysicalAddress', $form);
        }

        if (mailingModified.length > 0) {
            const addressId = $('#ApplicantAddresses_PrimaryMailingAddressId').val();
            payload.primaryMailingAddress = buildAddressPayload(addressId, 'PrimaryMailingAddress', $form);
        }

        if (!payload.primaryPhysicalAddress && !payload.primaryMailingAddress) {
            return null;
        }

        if ([payload.primaryPhysicalAddress, payload.primaryMailingAddress]
            .some(address => address && isGuidEmpty(address.id))) {
            const applicationId = $('#ApplicantAddresses_ExpectedApplicationId').val();
            if (isGuidEmpty(applicationId)) {
                throw new Error('A submission is required before you can add an address.');
            }
            payload.expectedApplicationId = applicationId;
        }

        return payload;
    }

});

function safeParse(value) {
    try {
        return JSON.parse(value || '[]');
    } catch (error) {
        console.warn('Unable to parse ApplicantAddresses data.', error);
        return [];
    }
}

function notifyApplicantAddressesLayoutChange() {
    globalThis.dispatchEvent(new CustomEvent('applicant-addresses-layout-changed'));
}

function isGuidEmpty(value) {
    return !value || value === '00000000-0000-0000-0000-000000000000';
}

function buildAddressPayload(addressId, prefix, $form) {
    const readField = name => ($form.find(`[name="${prefix}.${name}"]`).val() || '').trim();
    const address = {
        id: addressId || '00000000-0000-0000-0000-000000000000',
        street: readField('Street'),
        street2: readField('Street2'),
        unit: readField('Unit'),
        city: readField('City'),
        province: readField('Province'),
        postalCode: readField('PostalCode')
    };

    if (isGuidEmpty(address.id)) {
        const hasContent = [address.street, address.street2, address.unit, address.city, address.province, address.postalCode]
            .some(value => value.length > 0);
        if (!hasContent) {
            return null;
        }
        if (!address.street && !address.street2) {
            const label = prefix === 'PrimaryPhysicalAddress' ? 'physical' : 'mailing';
            throw new Error(`Enter Street or Street 2 for the new ${label} address.`);
        }
    }

    return address;
}

function applySavedApplicantAddresses(result, $form) {
    ['PrimaryPhysicalAddress', 'PrimaryMailingAddress'].forEach(prefix => {
        const key = prefix.charAt(0).toLowerCase() + prefix.slice(1);
        const address = result[key];
        if (!address) {
            return;
        }

        $form.find(`#ApplicantAddresses_${prefix}Id`).val(address.id);
        const fields = {
            Street: address.street,
            Street2: address.street2,
            Unit: address.unit,
            City: address.city,
            Province: address.province,
            PostalCode: address.postal
        };
        Object.entries(fields).forEach(([name, value]) => {
            $form.find(`[name="${prefix}.${name}"]`).val(value || '');
        });
        $form.find(`#${prefix}_CreationHint`).remove();
    });
}

function updateAddressTableAfterSave(payload, addressesDt) {
    if (!addressesDt) {
        return;
    }

    ['primaryPhysicalAddress', 'primaryMailingAddress'].forEach((key) => {
        const addressPayload = payload[key];
        if (!addressPayload) {
            return;
        }
        let found = false;
        const savedRow = {
            id: addressPayload.id,
            addressType: key === 'primaryPhysicalAddress' ? 'Physical' : 'Mailing',
            applicationId: addressPayload.applicationId,
            referenceNo: addressPayload.referenceNo || '',
            street: addressPayload.street || '',
            street2: addressPayload.street2 || '',
            unit: addressPayload.unit || '',
            city: addressPayload.city || '',
            province: addressPayload.province || '',
            postal: addressPayload.postal || '',
            country: addressPayload.country || ''
        };
        addressesDt.rows().every(function () {
            const rowData = this.data();
            if (rowData.id === addressPayload.id) {
                this.data(savedRow);
                found = true;
            }
        });
        if (!found) {
            addressesDt.row.add(savedRow);
        }
    });

    addressesDt.rows().invalidate().draw(false);
}
