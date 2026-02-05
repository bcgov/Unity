/* ApplicantOrganizationInfo Component Scripts */

(function () {
    const sectorDataElement = document.getElementById('ApplicantOrganizationInfo_SectorData');
    const sectorSelect = document.getElementById('ApplicantOrganizationInfo_Sector');
    const subSectorSelect = document.getElementById('ApplicantOrganizationInfo_SubSector');
    const form = $('#ApplicantOrganizationInfoForm');
    const saveButton = $('#saveApplicantOrganizationInfoBtn');
    const persistableFields = new Set([
        'UnityApplicantId',
        'ApplicantName',
        'OrgName',
        'OrgNumber',
        'BusinessNumber',
        'OrgStatus',
        'OrganizationType',
        'OrganizationSize',
        'NonRegOrgName',
        'Sector',
        'SubSector',
        'IndigenousOrgInd',
        'SectorSubSectorIndustryDesc',
        'FiscalMonth',
        'FiscalDay',
        'OrganizationOperationLength',
        'RedStop'
    ]);

    let zoneForm = null;

    if (form.length && saveButton.length && typeof UnityZoneForm === 'function') {
        zoneForm = new UnityZoneForm(form, {
            saveButtonSelector: '#saveApplicantOrganizationInfoBtn'
        });

        zoneForm.init();

        saveButton.on('click', function (event) {
            event.preventDefault();

            if (!zoneForm || zoneForm.modifiedFields.size === 0) {
                return;
            }

            const applicantId = $('#ApplicantOrganizationInfo_ApplicantId').val();
            if (!applicantId) {
                abp.notify.warn('Applicant identifier is missing.');
                return;
            }

            const payload = buildPartialUpdatePayload();
            if (!payload) {
                return;
            }

            unity.grantManager.applicants.applicant
                .partialUpdateApplicantSummary(applicantId, payload)
                .done(function () {
                    abp.notify.success('Applicant organization information updated.');
                    zoneForm.resetTracking();
                })
                .fail(function (error) {
                    console.error('Failed to update applicant organization information.', error);
                    abp.notify.error('Failed to update applicant organization information.');
                });
        });
    }

    function buildPartialUpdatePayload() {
        if (!zoneForm) {
            return null;
        }

        const modifiedFields = Array
            .from(zoneForm.modifiedFields ?? [])
            .map(field => field.split('.').pop() ?? field)
            .filter(field => persistableFields.has(field));

        if (modifiedFields.length === 0) {
            return null;
        }

        const fieldValues = {};

        modifiedFields.forEach(fieldName => {
            const $inputs = form.find(`[name="${fieldName}"]`);
            let $input = $inputs.filter(':not([type="hidden"])');

            if ($input.length === 0) {
                $input = $inputs.first();
            }

            if ($input.length === 0) {
                return;
            }

            let value;
            const inputType = ($input.attr('type') || '').toLowerCase();

            if (inputType === 'checkbox') {
                value = $input.is(':checked');
            } else if (inputType === 'radio') {
                const $checkedRadio = form.find(`input[name="${fieldName}"]:checked`);
                value = $checkedRadio.length ? $checkedRadio.val() : null;
            } else {
                value = $input.val();
            }

            if (typeof value === 'string') {
                if ($input.hasClass('unity-currency-input') || $input.hasClass('numeric-mask')) {
                    value = value.replace(/,/g, '');
                }

                const trimmed = value.trim();
                value = trimmed === '' ? null : trimmed;
            }

            fieldValues[fieldName] = value;
        });

        return {
            modifiedFields,
            data: fieldValues
        };
    }

    let sectors = [];

    if (sectorDataElement) {
        try {
            const json = sectorDataElement.textContent?.trim() ?? '[]';
            sectors = JSON.parse(json);
        } catch (error) {
            console.warn('Unable to parse ApplicantOrganizationInfo sector data.', error);
            sectors = [];
        }
    }

    const getSectorName = (sector) => sector?.sectorName ?? sector?.SectorName ?? '';
    const getSectorSubSectors = (sector) => sector?.subSectors ?? sector?.SubSectors ?? [];
    const getSubSectorName = (subSector) => subSector?.subSectorName ?? subSector?.SubSectorName ?? '';

    const renderSubSectors = (selectedSector) => {
        if (!subSectorSelect) {
            return;
        }

        const currentValue = subSectorSelect.value;
        subSectorSelect.innerHTML = '';

        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = 'Please choose...';
        subSectorSelect.appendChild(defaultOption);

        if (!Array.isArray(sectors) || !selectedSector || !sectorSelect) {
            return;
        }

        const sector = sectors.find(item => getSectorName(item) === selectedSector);
        const subSectors = getSectorSubSectors(sector);

        if (!Array.isArray(subSectors)) {
            return;
        }

        subSectors.forEach(subSector => {
            const option = document.createElement('option');
            option.value = getSubSectorName(subSector);
            option.textContent = getSubSectorName(subSector);
            subSectorSelect.appendChild(option);
        });

        if (Array.from(subSectorSelect.options).some(option => option.value === currentValue)) {
            subSectorSelect.value = currentValue;
        }
    };

    if (sectorSelect) {
        sectorSelect.addEventListener('change', () => renderSubSectors(sectorSelect.value));
        renderSubSectors(sectorSelect.value);
    }

    const orgBookSelect = $('#ApplicantOrganizationInfo_OrgBookSelect');
    const clearOrgBookButton = $('#ApplicantOrganizationInfo_ClearOrgBook');

    const fieldSelectors = {
        orgName: '#ApplicantOrganizationInfo_OrgName',
        orgNumber: '#ApplicantOrganizationInfo_OrgNumber',
        orgStatus: '#ApplicantOrganizationInfo_OrgStatus',
        organizationType: '#ApplicantOrganizationInfo_OrganizationType',
        businessNumber: '#ApplicantOrganizationInfo_BusinessNumber'
    };

    const getAttributeObjectByType = (type, attributes) => {
        if (!Array.isArray(attributes)) {
            return { type: '', value: '', text: '' };
        }

        return attributes.find(attr => attr.type === type) || { type: '', value: '', text: '' };
    };

    const setInputValue = (selector, value) => {
        const $field = $(selector);
        if ($field.length) {
            $field.val(value ?? '').trigger('change');
        }
    };

    const setSelectValue = (selector, value) => {
        const $field = $(selector);
        if (!$field.length) {
            return;
        }

        const normalizedValue = value ?? '';

        if (normalizedValue && !$field.find(`option[value="${normalizedValue}"]`).length) {
            $field.append(new Option(normalizedValue, normalizedValue));
        }

        $field.val(normalizedValue).trigger('change');
    };

    const populateRegisteredFields = (orgName, orgNumber, orgStatus, organizationType, businessNumber) => {
        setInputValue(fieldSelectors.orgName, orgName);
        setInputValue(fieldSelectors.orgNumber, orgNumber);
        setSelectValue(fieldSelectors.orgStatus, orgStatus);
        setSelectValue(fieldSelectors.organizationType, organizationType);
        setInputValue(fieldSelectors.businessNumber, businessNumber);
    };

    if (orgBookSelect.length) {
        if (orgBookSelect.hasClass('select2-hidden-accessible')) {
            orgBookSelect.select2('destroy');
        }

        orgBookSelect.select2({
            ajax: {
                url: '/api/app/org-book/org-book-autocomplete-query',
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return { orgBookQuery: params.term };
                },
                processResults: function (data) {
                    return {
                        results: data.map(function (item) {
                            return {
                                id: item.topic_source_id ?? item.Id ?? '',
                                text: item.value ?? item.Text ?? '',
                                value: item.value ?? item.Text ?? ''
                            };
                        })
                    };
                }
            },
            minimumInputLength: 3,
            placeholder: 'Start typing a name or number to search...'
        });

        orgBookSelect.on('select2:select', function (e) {
            const selectedData = e.params?.data;
            const orgBookId = selectedData?.id;

            if (!orgBookId) {
                return;
            }

            abp.ajax({
                url: `/api/app/org-book/org-book-details-query/${orgBookId}`,
                type: 'GET'
            }).done(function (response) {
                const entryStatus = getAttributeObjectByType('entity_status', response?.attributes);
                const orgStatus = entryStatus.value === 'HIS' ? 'HISTORICAL' : 'ACTIVE';
                const entityType = getAttributeObjectByType('entity_type', response?.attributes);
                const businessNumber = getAttributeObjectByType('business_number', response?.names);
                const organizationName = (response?.names && response.names[0]?.text) ? response.names[0].text : '';

                populateRegisteredFields(
                    organizationName,
                    orgBookId,
                    orgStatus,
                    entityType.value ?? '',
                    businessNumber.text ?? ''
                );
            }).fail(function (error) {
                console.error('Failed to retrieve organization details from BC Registries.', error);
            });
        });

        clearOrgBookButton.on('click', function () {
            orgBookSelect.val(null).trigger('change');
            populateRegisteredFields('', '', '', '', '');
        });
    }
})();
