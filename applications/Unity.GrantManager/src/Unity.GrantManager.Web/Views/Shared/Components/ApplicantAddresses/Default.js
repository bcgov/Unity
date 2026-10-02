$(function () {
    const LAYOUT_NOTIFICATION_DELAYS = [0, 120, 300, 700];
    const l = abp.localization.getResource('GrantManager');

    const nullPlaceholder = '—';
    let widgetRoot = $();
    let applicantId = null;
    let canEdit = false;
    let addressesData = [];
    let addressesTable = null;
    let zoneForm = null;
    let isSaving = false;
    let savedOrder = null;
    let editAddressModal = null;
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

    function scheduleLayoutNotifications() {
        LAYOUT_NOTIFICATION_DELAYS.forEach((delay) => {
            setTimeout(notifyApplicantAddressesLayoutChange, delay);
        });
    }

    function readWidgetState() {
        widgetRoot = $('.applicant-addresses-widget');
        applicantId = $('#ApplicantAddresses_ApplicantId').val();
        canEdit = widgetRoot.data('can-edit') === true || widgetRoot.data('can-edit') === 'true';
        addressesData = safeParse($('#ApplicantAddresses_Data').val());
    }

    function buildColumnDefs() {
        return [
            {
                title: l('ApplicantAddresses:ColumnAddressType'),
                data: 'addressType',
                width: '13%',
                render: renderTableText,
                targets: 0
            },
            {
                title: l('ApplicantAddresses:ColumnAddress'),
                data: 'street',
                width: '22%',
                render: (data, type, row) =>
                    renderTableText([data, row.street2].filter(Boolean).join(', '), type),
                targets: 1
            },
            {
                title: l('ApplicantAddresses:ColumnUnit'),
                data: 'unit',
                width: '8%',
                render: renderTableText,
                targets: 2
            },
            {
                title: l('ApplicantAddresses:ColumnCity'),
                data: 'city',
                width: '14%',
                render: renderTableText,
                targets: 3
            },
            {
                title: l('ApplicantAddresses:ColumnProvince'),
                data: 'province',
                width: '14%',
                render: renderTableText,
                targets: 4
            },
            {
                title: l('ApplicantAddresses:ColumnPostalCode'),
                data: 'postal',
                width: '10%',
                render: renderTableText,
                targets: 5
            },
            {
                title: l('ApplicantAddresses:ColumnSubmission'),
                data: 'referenceNo',
                width: '13%',
                render: renderTableLink,
                targets: 6
            },
            {
                title: l('ApplicantAddresses:ColumnPrimary'),
                data: null,
                orderable: false,
                searchable: false,
                width: '10%',
                render: renderPrimary,
                targets: 7
            },
            {
                title: '',
                data: null,
                orderable: false,
                searchable: false,
                width: '48px',
                className: 'text-center',
                render: renderActions,
                targets: 8
            }
        ];
    }

    function renderPrimary(data, type, row) {
        if (!row.isPrimary) {
            return '';
        }

        const label = l('ApplicantAddresses:PrimaryBadge', row.addressType);

        if (type !== 'display') {
            return label;
        }

        return `<span class="badge applicant-address-primary-badge">${abp.utils.htmlEscape(label)}</span>`;
    }

    function renderActions(data, type, row) {
        if (!canEdit) {
            return row.isEditable ? '' : renderSourceInfo();
        }

        const editLabel = abp.utils.htmlEscape(l('Common:Command:Edit'));
        // A disabled button receives no hover or focus, so the explanation sits on a focusable
        // wrapper carrying the same Bootstrap tooltip as renderSourceInfo.
        const editItem = row.isEditable
            ? `<button class="dropdown-item applicant-address-edit-btn"
                                    data-address-id="${row.id}">${editLabel}</button>`
            : `<span class="d-block" tabindex="0"
                                  data-bs-toggle="tooltip"
                                  data-bs-placement="left"
                                  title="${abp.utils.htmlEscape(l('GrantManager:AddressNotEditable'))}">
                                <button class="dropdown-item applicant-address-edit-btn"
                                        data-address-id="${row.id}" disabled>${editLabel}</button>
                            </span>`;

        const setPrimaryLabel = l('ApplicantAddresses:SetAsPrimary', row.addressType);
        const setPrimaryDisabled = row.isPrimary ? 'disabled' : '';

        return `<div class="dropdown applicant-address-actions">
                    <button type="button"
                            class="btn btn-sm btn-link p-0 applicant-address-menu-btn"
                            data-bs-toggle="dropdown"
                            aria-expanded="false"
                            data-address-id="${row.id}">
                        <i class="fa-solid fa-ellipsis-vertical" aria-hidden="true"></i>
                        <span class="visually-hidden">${abp.utils.htmlEscape(l('ApplicantAddresses:AddressActions'))}</span>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end">
                        <li>
                            ${editItem}
                        </li>
                        <li>
                            <button class="dropdown-item applicant-address-set-primary-btn"
                                    data-address-id="${row.id}" ${setPrimaryDisabled}>${abp.utils.htmlEscape(setPrimaryLabel)}</button>
                        </li>
                    </ul>
                </div>`;
    }

    function renderSourceInfo() {
        const escaped = abp.utils.htmlEscape(l('GrantManager:AddressNotEditable'));
        return `<span class="applicant-address-source-info"
                      data-bs-toggle="tooltip"
                      data-bs-placement="left"
                      title="${escaped}">
                    <i class="fa-solid fa-circle-info text-muted" aria-hidden="true"></i>
                    <span class="visually-hidden">${escaped}</span>
                </span>`;
    }

    function ensureEditAddressModal() {
        if (editAddressModal) {
            return editAddressModal;
        }

        editAddressModal = new abp.ModalManager(abp.appPath + 'ApplicantAddress/EditModal');
        editAddressModal.onResult(function () {
            abp.notify.success(l('ApplicantAddresses:AddressSaved'));
            refreshWidget();
        });

        return editAddressModal;
    }

    function initializeAddressesTable(order) {
        if (!$.fn.DataTable || !$('#ApplicantAddressesTable').length) {
            return null;
        }

        if ($.fn.DataTable.isDataTable('#ApplicantAddressesTable')) {
            $('#ApplicantAddressesTable').DataTable().destroy();
        }

        return $('#ApplicantAddressesTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                data: addressesData,
                serverSide: false,
                order: order || [[0, 'asc']],
                searching: true,
                paging: true,
                pageLength: 10,
                select: false,
                info: true,
                scrollX: true,
                drawCallback: function () {
                    this.api().columns.adjust();
                    $('#ApplicantAddressesTable [data-bs-toggle="tooltip"]').each(function () {
                        const existing = bootstrap.Tooltip.getInstance(this);
                        if (existing) { existing.dispose(); }
                        bootstrap.Tooltip.getOrCreateInstance(this);
                    });
                    // The table's scroll body clips an absolutely positioned menu, so use Popper's fixed strategy.
                    $('#ApplicantAddressesTable .applicant-address-menu-btn').each(function () {
                        bootstrap.Dropdown.getOrCreateInstance(this, {
                            popperConfig: (config) => ({ ...config, strategy: 'fixed' })
                        });
                    });
                },
                columnDefs: buildColumnDefs()
            })
        );
    }

    function bindZoneForm() {
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
    }

    function bindWidget(order) {
        readWidgetState();
        addressesTable = initializeAddressesTable(order);
        bindZoneForm();
        scheduleLayoutNotifications();
    }

    function refreshWidget() {
        if (addressesTable) {
            try {
                savedOrder = addressesTable.order();
                addressesTable.processing(true);
            } catch (e) { console.error('Failed to enable DataTables processing indicator.', e); }
        }

        $.ajax({
            url: abp.appPath + 'Widget/ApplicantAddresses/Refresh',
            type: 'GET',
            dataType: 'html',
            data: { applicantId: applicantId },
            success: function (html) {
                if (addressesTable) {
                    addressesTable.destroy();
                    addressesTable = null;
                }
                widgetRoot.parent().html(html);
                bindWidget(savedOrder);
                savedOrder = null;
                abp.event.trigger('applicant-addresses-refreshed');
            },
            error: function () {
                savedOrder = null;
                if (addressesTable) {
                    try { addressesTable.processing(false); } catch (e) { console.error('Failed to disable DataTables processing indicator.', e); }
                }
                abp.notify.error(l('ApplicantAddresses:RefreshFailed'));
            }
        });
    }

    bindWidget();

    $(document).on('click', '.applicant-address-edit-btn', function () {
        ensureEditAddressModal().open({
            id: $(this).data('address-id'),
            applicantId: applicantId
        });
    });

    $(document).on('click', '.applicant-address-set-primary-btn', function () {
        const addressId = $(this).data('address-id');
        const service = globalThis.unity?.grantManager?.applicantProfile?.applicantAddress;

        if (!service) {
            abp.notify.error(l('ApplicantAddresses:ServiceUnavailable'));
            return;
        }

        service.setPrimary(applicantId, addressId)
            .done(function () {
                abp.notify.success(l('ApplicantAddresses:AddressSetPrimary'));
                refreshWidget();
            })
            .fail(function () {
                abp.notify.error(l('ApplicantAddresses:SetPrimaryFailed'));
            });
    });

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
