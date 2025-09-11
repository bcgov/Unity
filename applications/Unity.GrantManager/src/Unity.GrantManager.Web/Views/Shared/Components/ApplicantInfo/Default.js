abp.widgets.ApplicantInfo = function ($wrapper) {
    let widgetManager = $wrapper.data('abp-widget-manager');

    let widgetApi = {
        getFilters: function () {
            return {
                applicationId: $wrapper.find('#ApplicantInfo_ApplicationId').val(),
                applicationFormVersionId: $wrapper.find("#ApplicantInfo_ApplicationFormVersionId").val()
            };
        },
        init: function (filters) {
            let $widgetForm = $wrapper.find('form');

            // Create a new form instance and store it on the widget API
            this.zoneForm = new UnityZoneForm($widgetForm, {
                saveButtonSelector: '#saveApplicantInfoBtn'
            });

            console.log("Applicant Info Initialized");

            this.zoneForm.init();

            // Set up additional event handlers here
            this.setupEventHandlers();
            registerElectoralDistrictControls(this.zoneForm.form);
            registerApplicantInfoSummaryDropdowns(this.zoneForm.form);
        },
        refresh: function () {            
            const currentFilters = this.getFilters();
            widgetManager.refresh($wrapper, currentFilters);
        },
        setupEventHandlers: function () {
            const self = this;

            PubSub.subscribe(
                'applicant_info_merged',
                () => {
                    self.refresh();
                }
            );

            // Save button handler
            self.zoneForm.saveButton.on('click', function () {
                let applicationId = document.getElementById('ApplicantInfo_ApplicationId').value;
                let applicantInfoSubmission = self.getPartialUpdate();
                try {
                    unity.grantManager.grantApplications.applicationApplicant
                        .updatePartialApplicantInfo(applicationId, applicantInfoSubmission)
                        .done(function () {
                            abp.notify.success('The Applicant Info has been updated.');
                            self.zoneForm.resetTracking();
                            PubSub.publish("refresh_detail_panel_summary");
                            PubSub.publish('applicant_info_updated', applicantInfoSubmission);
                        })
                        .fail(function (error) {
                            abp.notify.error('Failed to update Applicant Info.');
                            console.log(error);
                        });
                } catch (error) {
                    abp.notify.error('An unexpected error occurred.');
                    console.log(error);
                }
            });
        },
        getPartialUpdate: function () {
            let submissionPayload = this.serializeWidget();

            const customIncludes = new Set();

            if (typeof Flex === 'function' && Object.keys(submissionPayload.CustomFields || {}).length > 0) {
                // Add Worksheet Metadata and filter conditions
                submissionPayload.CorrelationId = $("#ApplicantInfo_ApplicationFormVersionId").val();
                // Check for worksheet scenario - multiple vs single
                let multipleWorksheetsIds = $("#ApplicantInfo_WorksheetIds").val();
                let singleWorksheetId = $("#ApplicantInfo_WorksheetId").val();
                
                // Set correct payload property based on worksheet scenario
                if (multipleWorksheetsIds) {
                    // Multiple worksheets scenario - send as WorksheetIds array
                    submissionPayload.WorksheetIds = multipleWorksheetsIds.split(',').map(id => id.trim());
                } else if (singleWorksheetId) {
                    // Single worksheet scenario - send as WorksheetId
                    submissionPayload.WorksheetId = singleWorksheetId.trim();
                }

                // Normalize checkboxes to string for custom worksheets
                $(`#Unity_GrantManager_ApplicationManagement_Applicant_Worksheet input:checkbox`).each(function () {
                    submissionPayload.CustomFields[this.name] = (this.checked).toString();
                });

                customIncludes
                    .add('CustomFields')
                    .add('CorrelationId');
                    
                    // Add appropriate worksheet ID field based on scenario
                    if(multipleWorksheetsIds) {
                        customIncludes.add('WorksheetIds');
                    } else if(singleWorksheetId) {
                        customIncludes.add('WorksheetId');
                    }
            }

            customIncludes.add('ApplicantId');

            let modifiedFieldData = Object.fromEntries(
                Object.entries(submissionPayload).filter(([key, _]) => {
                    // Check if it's a modified widget field
                    return this.zoneForm.modifiedFields.has(key) || customIncludes.has(key) || key.startsWith('custom_');
                })
            );

            let partialSubmissionPayload = {
                modifiedFields: Array.from(this.zoneForm.modifiedFields),
                data: unflattenObject(modifiedFieldData)
            };

            return partialSubmissionPayload;
        },
        serializeWidget: function () {
            let formData = this.zoneForm.serializeZoneArray();
            let submissionPayload = {};

            // Process all form fields
            $.each(formData, (_, input) => {
                this.processFormField(submissionPayload, input);
            });

            return submissionPayload;
        },
        processFormField: function (submissionPayload, input) {
            const fieldName = input.name;
            const inputElement = $(`[name="${fieldName}"]`);

            // Handle checkboxes explicitly
            if (inputElement.length && inputElement.attr('type') === 'checkbox') {
                // Only process the actual checkbox, not the hidden field
                // If multiple elements with the same name, pick the checkbox
                const checkbox = inputElement.filter('[type="checkbox"]');
                if (checkbox.length) {
                    if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                        Flex.includeCustomFieldObj(submissionPayload, input);
                    } else {
                        submissionPayload[fieldName] = checkbox.is(':checked');
                    }
                    return;
                }
            }

            // Existing logic for custom fields
            if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                Flex.includeCustomFieldObj(submissionPayload, input);
                return;
            }

            let fieldValue = input.value;

            if (inputElement.hasClass('unity-currency-input') || inputElement.hasClass('numeric-mask')) {
                fieldValue = fieldValue.replace(/,/g, '');
            }

            if (fieldName.startsWith('ApplicantInfo.')) {
                const propertyName = fieldName.split('.')[1];
                submissionPayload[propertyName] = fieldValue;
            } else {
                submissionPayload[fieldName] = fieldValue;
            }
        }
    }

    return widgetApi;
}

$(function () {
    // Initialize widget through ABP's widget system instead of global object
    abp.zones = abp.zones || {};
    abp.zones.applicantInfo = $('[data-widget-name="ApplicantInfo"]')
        .data('abp-widget-api') || null;

    $('#applicantLookupSelect').select2({
        ajax: {
            url: '/api/app/applicant/applicant-look-up-autocomplete-query',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return { applicantLookUpQuery: params.term };
            },
            processResults: function (data) {
                return {
                    results: data.map(function (item) {
                        const res = {
                            id: item.Id,
                            text: `${item.UnityApplicantId?.trim() ? item.UnityApplicantId : 'None'} / ${item.ApplicantName}`,
                            ApplicantName: item.ApplicantName,
                            OrgName: item.OrgName,
                            OrgNumber: item.OrgNumber,
                            NonRegOrgName: item.NonRegOrgName,
                            OrganizationType: item.OrganizationType,
                            OrganizationSize: item.OrganizationSize,
                            OrgStatus: item.OrgStatus,
                            IndigenousOrgInd: item.IndigenousOrgInd,
                            Sector: item.Sector,
                            SubSector: item.SubSector,
                            SectorSubSectorIndustryDesc: item.SectorSubSectorIndustryDesc,
                            FiscalDay: item.FiscalDay,
                            FiscalMonth: item.FiscalMonth,
                            UnityApplicantId: item.UnityApplicantId
                        };
                        return res
                    })
                };
            }
        },
        minimumInputLength: 3,
        placeholder: 'Start typing applicant name or number to search for applicant...'
    });

    $('#applicantLookupSelect').on('select2:select', function (e) {
        $('#mergeApplicantsMergeBtn').prop('disabled', false);
        let selectedData = e.params.data;

        // Gather existing values from the form
        let getVal = id => $(`#${id}`).val() || '';
        let existing = {
            ApplicantId: getVal('ApplicantId'),
            UnityApplicantId: getVal('ApplicantSummary_UnityApplicantId'),
            ApplicantName: $('.application-details-breadcrumb .applicant-name').text(),
            OrgName: getVal('ApplicantSummary_OrgName'),
            OrgNumber: getVal('ApplicantSummary_OrgNumber'),
            NonRegOrgName: getVal('ApplicantSummary_NonRegOrgName'),
            OrganizationType: getVal('ApplicantSummary_OrganizationType'),
            BusinessNumber: getVal('ApplicantSummary_BusinessNumber'),
            OrganizationSize: getVal('ApplicantSummary_OrganizationSize'),
            OrgStatus: getVal('ApplicantSummary_OrgStatus'),
            IndigenousOrgInd: $('#ApplicantSummary_IndigenousOrgInd').is(':checked') ? 'Yes' : 'No',
            Sector: getVal('ApplicantSummary_Sector'),
            SubSector: getVal('ApplicantSummary_SubSector'),
            SectorSubSectorIndustryDesc: getVal('ApplicantSummary_SectorSubSectorIndustryDesc'),
            FiscalDay: getVal('ApplicantSummary_FiscalDay'),
            FiscalMonth: getVal('ApplicantSummary_FiscalMonth')
        };

        let newData = {
            ApplicantId: selectedData.id || '',
            UnityApplicantId: selectedData.UnityApplicantId || '',
            ApplicantName: selectedData.ApplicantName || '',
            OrgName: selectedData.OrgName || '',
            OrgNumber: selectedData.OrgNumber || '',
            NonRegOrgName: selectedData.NonRegOrgName || '',
            OrganizationType: selectedData.OrganizationType || '',
            BusinessNumber: selectedData.BusinessNumber || '',
            OrganizationSize: selectedData.OrganizationSize || '',
            OrgStatus: selectedData.OrgStatus || '',
            IndigenousOrgInd: selectedData.IndigenousOrgInd || '',
            Sector: selectedData.Sector || '',
            SubSector: selectedData.SubSector || '',
            SectorSubSectorIndustryDesc: selectedData.SectorSubSectorIndustryDesc || '',
            FiscalDay: selectedData.FiscalDay || '',
            FiscalMonth: selectedData.FiscalMonth || ''
        };

        $('#existing_ApplicantNameHeader').text(existing.ApplicantName);
        $('#new_ApplicantNameHeader').text(newData.ApplicantName);

        // Fill modal fields
        for (const key in existing) {
            $(`#existing_${key}`).text(existing[key]);
            $(`#new_${key}`).text(newData[key]);
            $(`input[name="merge_${key}"][value="existing"]`).prop('checked', true);
        }

        // Show step 1, hide step 2
        $('#mergeApplicantsStep1').show();
        $('#mergeApplicantsStep2').hide();

        // Remove previous handlers
        $('#mergeApplicantsNextBtn').off('click');
        $('#mergeApplicantsBackBtn').off('click');
        $('#mergeApplicantsMergeBtn').off('click');
        $('#mergeDuplicateApplicantsModal').off('hidden.bs.modal');

        // Next button: go to confirmation
        $('#mergeApplicantsNextBtn').on('click', function () {
            $('#mergeApplicantsStep1').hide();
            $('#mergeApplicantsStep2').show();
        });

        // Back button: return to comparison
        $('#mergeApplicantsBackBtn').on('click', function () {
            $('#mergeApplicantsStep2').hide();
            $('#mergeApplicantsStep1').show();
        });

        // Merge button: apply selected values to form
        $('#mergeApplicantsMergeBtn').on('click', async function () {
            $('#mergeApplicantsMergeBtn').prop('disabled', true);
            $('#mergeApplicantsSpinner').show();

            let selectedPrincipal = $('input[name="merge_ApplicantId"]:checked').val();
            let principalApplicantId = selectedPrincipal === 'existing' ? existing.ApplicantId : newData.ApplicantId;
            let nonPrincipalApplicantId = selectedPrincipal === 'existing' ? newData.ApplicantId : existing.ApplicantId;
            let applicationId = $('#ApplicantInfo_ApplicationId').val();

            // Merge and update applicant info
            let mergedApplicantInfo = {};
            if (principalApplicantId) {
                mergedApplicantInfo = getMergedApplicantInfo(existing, newData);
                mergedApplicantInfo.ApplicantId = principalApplicantId;

                let formData = $("#ApplicantInfoForm").serializeArray();
                let ApplicantInfoObj = {};
                let formVersionId = $("#ApplicationFormVersionId").val();
                let worksheetId = $("#WorksheetId").val();
                
                $.each(formData, function (_, input) {
                    if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                        Flex.includeCustomFieldObj(ApplicantInfoObj, input);
                    }
                    else {
                        ApplicantInfoObj[input.name] = input.value;

                        if (ApplicantInfoObj[input.name] == '') {
                            ApplicantInfoObj[input.name] = null;
                        }
                    }
                });

                $(`#ApplicantInfoForm input:checkbox`).each(function () {
                    ApplicantInfoObj[this.name] = (this.checked).toString();
                });
                if (typeof Flex === 'function') {
                    Flex?.setCustomFields(ApplicantInfoObj);
                }

                Object.assign(ApplicantInfoObj, mergedApplicantInfo);
                Object.keys(ApplicantInfoObj).forEach(key => {
                    if (ApplicantInfoObj[key] === "") {
                        ApplicantInfoObj[key] = null;
                    }
                });

                const orgName = $('#ApplicantSummary_OrgName').val();
                ApplicantInfoObj['ApplicantSummary.OrgName'] = orgName;
                const orgNumber = $('#ApplicantSummary_OrgNumber').val();
                ApplicantInfoObj['ApplicantSummary.OrgNumber'] = orgNumber;
                const orgStatus = $('#ApplicantSummary_OrgStatus').val();
                ApplicantInfoObj['ApplicantSummary.OrgStatus'] = orgStatus;

                ApplicantInfoObj['correlationId'] = formVersionId;
                ApplicantInfoObj['worksheetId'] = worksheetId;
                ApplicantInfoObj.ApplicantId = principalApplicantId;
              
                try {
                    await handleApplicantMerge(applicationId, principalApplicantId, nonPrincipalApplicantId, newData, ApplicantInfoObj);
                } catch (err) {
                    console.error(err);
                }
            }

            $('#mergeApplicantsSpinner').hide();
            $('#mergeDuplicateApplicantsModal').modal('hide');
        });

        // On modal close, clear the ApplicantLookUp field
        $('#mergeDuplicateApplicantsModal').on('hidden.bs.modal', function () {
            $('#applicantLookupSelect').val(null).trigger('change');
        });

        // Show the modal
        $('#mergeDuplicateApplicantsModal').modal('show');

    });


    $('#selectAllExistingBtn').on('click', function () {
        $('#mergeApplicantsStep1 input[type="radio"][value="existing"]').each(function () {
            $(this).prop('checked', true);
        });
    });

    $('#selectAllNewBtn').on('click', function () {
        $('#mergeApplicantsStep1 input[type="radio"][value="new"]').each(function () {
            $(this).prop('checked', true);
        });
    });
});

$(document).on('click', '#btnClearOrgbook', function (e) {
    e.preventDefault();
    const $f = $('#ApplicantInfoForm');

    if ($f.find('#ApplicantSummary_OrgName').val()) $('#saveApplicantInfoBtn').prop('disabled', false);

    $f.find('#ApplicantSummary_OrgName').val('').trigger('change');
    $f.find('#ApplicantSummary_OrgNumber').val('').trigger('change');
    $f.find('#ApplicantSummary_OrgStatus').val('').trigger('change');
    $f.find('#ApplicantSummary_OrganizationType').val('').trigger('change');
    $f.find('#ApplicantSummary_BusinessNumber').val('').trigger('change');

    $('#orgBookSelect').val(null).trigger('change');
});

// Move to zone-extensions
function unflattenObject(flatObj) {
    const result = {};
    for (const flatKey in flatObj) {
        const value = flatObj[flatKey];
        if (!flatKey) continue;
        const keys = flatKey.split('.');
        let cur = result;
        for (let i = 0; i < keys.length; i++) {
            const k = keys[i];
            if (i === keys.length - 1) {
                cur[k] = value;
            } else {
                cur[k] = cur[k] || {};
                cur = cur[k];
            }
        }
    }
    return result;
}

function registerElectoralDistrictControls($container) {
    $container.find('[data-bs-toggle="tooltip"]').tooltip();
    setElectoralDistrictLockState(true);

    const $electoralType = $('#ApplicantElectoralAddressType');

    // Delegate change event for both address groups
    $container.on('change', '.physical-address-fields-group input, .mailing-address-fields-group input', function () {
        const type = $electoralType.val();
        if (
            ((type === "PhysicalAddress" && $(this).closest('.physical-address-fields-group').length) ||
                (type === "MailingAddress" && $(this).closest('.mailing-address-fields-group').length)) &&
            !electoralDistrictLocked
        ) {
            refreshApplicantElectoralDistrict();
        }
    });
}

function registerApplicantInfoSummaryDropdowns($container) {
    $container.find('#ApplicantSummary_Sector').on('change', function () {
        const selectedValue = $(this).val();
        let sectorList = JSON.parse($('#orgApplicationSectorList').text());

        let childDropdown = $('#ApplicantSummary_SubSector');
        childDropdown.empty();

        let subSectors = sectorList.find(sector => (sector.sectorName === selectedValue))?.subSectors;
        childDropdown.append($('<option>', {
            value: '',
            text: 'Please choose...'
        }));

        $.each(subSectors, function (index, item) {
            childDropdown.append($('<option>', {
                value: item.subSectorName,
                text: item.subSectorName
            }));
        });
    });

    $container.find('#orgBookSelect').on('select2:select', function (e) {
        let selectedData = e.params.data;
        let orgBookId = selectedData.id;

        abp.ajax({
            url: '/api/app/org-book/org-book-details-query/' + orgBookId,
            type: 'GET'
        }).done(function (response) {
            let entry_status = getAttributeObjectByType("entity_status", response.attributes);
            let org_status = entry_status.value == "HIS" ? "HISTORICAL" : "ACTIVE";
            let entity_type = getAttributeObjectByType("entity_type", response.attributes);
            let business_number = getAttributeObjectByType("business_number", response.names);

            $container.find('#ApplicantSummary_OrgName').val(response.names[0].text).trigger('change');
            $container.find('#ApplicantSummary_OrgNumber').val(orgBookId).trigger('change');
            $container.find('#ApplicantSummary_OrgStatus').val(org_status).trigger('change');
            $container.find('#ApplicantSummary_OrganizationType').val(entity_type.value).trigger('change');
            $container.find('#ApplicantSummary_BusinessNumber').val(business_number.text).trigger('change');
        });
    });
}

function getAttributeObjectByType(type, attributes) {
    if (!Array.isArray(attributes))  return { type: '', value: '', text: '' };
    return attributes.find(attr => attr.type === type) || { type: '', value: '', text: '' };
}

let electoralDistrictLocked = true; // Default: locked
function setElectoralDistrictLockState(locked) {
    $('#btn-toggle-lock-electoral').tooltip('hide');

    electoralDistrictLocked = locked;

    // Toggle "disabled" look and interaction for the select
    const $select = $('#ElectoralDistrict');
    if (locked) {
        $select.addClass('select-disabled');
        $select.on('mousedown.electoralLock touchstart.electoralLock', function (e) { e.preventDefault(); });
        $select.on('focus.electoralLock', function (e) { $(this).blur(); });
    } else {
        $select.removeClass('select-disabled');
        $select.off('.electoralLock');
    }

    $('#btn-refresh-electoral').prop('disabled', locked);

    // Toggle icon
    const $icon = $('#btn-toggle-lock-electoral i');
    if (locked) {
        $icon.removeClass('fa-unlock').addClass('fa-lock');
    } else {
        $icon.removeClass('fa-lock').addClass('fa-unlock');
    }
};

async function refreshApplicantElectoralDistrict() {
    try {
        let address = extractAddressInfo();
        let addressDetails = await unity.grantManager.integrations.geocoder.geocoderApi.getAddressDetails(address);
        let electoralDistrict = await unity.grantManager.integrations.geocoder.geocoderApi.getElectoralDistrict(addressDetails?.coordinates);
        if (electoralDistrict?.name) {
            $('#ElectoralDistrict').val(electoralDistrict.name).trigger('change');
        }
    }
    catch (error) {
        console.error(error);
    }
};

function toggleElectoralDistrictLockState() {
    setElectoralDistrictLockState(!electoralDistrictLocked);
};

function extractAddressInfo() {
    // Determine which address to use based on the flag    
    const isPhysical = $('#ApplicantElectoralAddressType').val() === "PhysicalAddress";

    // Define the field prefixes
    const prefix = isPhysical ? 'PhysicalAddress' : 'MailingAddress';

    // Collect address parts
    const street   = $(`#${prefix}_Street`).val() || '';
    const city     = $(`#${prefix}_City`).val() || '';
    const province = $(`#${prefix}_Province`).val() || '';
    const postal   = $(`#${prefix}_Postal`).val() || '';
    const country  = $(`#${prefix}_Country`).val() || '';

    // Concatenate address parts, filtering out empty values
    return [street, city, province, postal, country]
        .filter(part => part.trim() !== '')
        .join(', ');
};

function getMergedApplicantInfo(existing, newData) {
    let merged = {};
    for (const key in existing) {
        let useExisting = $(`input[name="merge_${key}"][value="existing"]`).is(':checked');
        merged[key] = useExisting ? existing[key] : newData[key];
    }
    return merged;
}

async function handleApplicantMerge(applicationId, principalApplicantId, nonPrincipalApplicantId, newData, ApplicantInfoObj) {
    
    await setApplicantDuplicatedStatus(principalApplicantId, nonPrincipalApplicantId);

    if (principalApplicantId === newData.ApplicantId) {
        await updatePrincipalApplicant(applicationId, principalApplicantId);
    }
    
    await updateMergedApplicant(applicationId, ApplicantInfoObj);
}

function updateMergedApplicant(applicationId, appInfoObj) {
    return unity.grantManager.grantApplications.grantApplication
        .updateMergedApplicant(applicationId, appInfoObj)
        .done(function () {
            abp.notify.success(
                'The Applicant info has been updated.'
            );
            $('#saveApplicantInfoBtn').prop('disabled', true);
            PubSub.publish("refresh_detail_panel_summary");
            PubSub.publish('applicant_info_updated', appInfoObj);
            PubSub.publish('applicant_info_merged');
        });
}

async function generateUnityApplicantIdBtn() {
    try {
        let nextUnityApplicantId = await unity.grantManager.applicants.applicant.getNextUnityApplicantId();
        $('[data-widget-name="ApplicantInfo"]').find('#ApplicantSummary_UnityApplicantId').val(nextUnityApplicantId).trigger('change');
    }
    catch (error) {
        console.log(error);
    }
};

async function checkUnityApplicantIdExist(unityAppId, appId, appInfoObj) {
    try {
        let existingApplicant = await unity.grantManager.applicants.applicant.getExistingApplicant(unityAppId);

        if (existingApplicant) {
            Swal.fire({
                icon: "error",
                text: "Applicatn ID already exists. Please enter a unique ID.",
                confirmButtonText: 'Ok',
                customClass: { confirmButton: 'btn btn-primary' }
            });
        } else {
            updateApplicantInfo(appId, appInfoObj);
        }
    }
    catch (error) {
        console.log(error);
    }
}

function enableApplicantInfoSaveBtn(inputText) {
    if (!$("#ApplicantInfoForm").valid()
        || !abp.auth.isGranted('Unity.GrantManager.ApplicationManagement.Applicant') // Note: Will replace after worksheet permissions added
        || !abp.auth.isGranted('Unity.GrantManager.ApplicationManagement.Applicant.Summary.Update')
        || !abp.auth.isGranted('Unity.GrantManager.ApplicationManagement.Applicant.Authority.Update')
        || !abp.auth.isGranted('Unity.GrantManager.ApplicationManagement.Applicant.Location.Update')
        || !abp.auth.isGranted('Unity.GrantManager.ApplicationManagement.Applicant.Contact.Update')
        || formHasInvalidCurrencyCustomFields("ApplicantInfoForm")) {
        $('#saveApplicantInfoBtn').prop('disabled', true);
        return;
    }
    $('#saveApplicantInfoBtn').prop('disabled', false);
}

function updateApplicantInfo(appId, appInfoObj) {
    return unity.grantManager.grantApplications.grantApplication
        .updateProjectApplicantInfo(appId, appInfoObj)
        .done(function () {
            abp.notify.success(
                'The Applicant info has been updated.'
            );
            $('#saveApplicantInfoBtn').prop('disabled', true);
            PubSub.publish("refresh_detail_panel_summary");
            PubSub.publish('applicant_info_updated', appInfoObj);
        });
}

function setApplicantDuplicatedStatus(principalApplicantId, nonPrincipalApplicantId) {
    return $.ajax({
        url: '/api/app/applicant/set-duplicated',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            principalApplicantId: principalApplicantId,
            nonPrincipalApplicantId: nonPrincipalApplicantId
        })
    });
}

function updatePrincipalApplicant(applicationId, principalApplicantId) {
    return $.ajax({
            url: '/api/app/applicant/applicant-id',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                applicationId: applicationId,
                applicantId: principalApplicantId
            })
        })
        .done(function () {
            abp.notify.success('Principal Applicant updated successfully.');
        })
        .fail(function (xhr, status) {
            abp.notify.error('Failed to update Principal Applicant.');
         });
}
