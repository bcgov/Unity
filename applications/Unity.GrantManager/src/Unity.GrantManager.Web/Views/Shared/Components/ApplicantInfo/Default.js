$(function () {
    $('.numeric-mask').maskMoney({ precision: 0 });
    $('.numeric-mask').each(function () {
        $(this).maskMoney('mask', this.value);
    });

    const $unityAppId = $('#applicantInfoUnityApplicantId');
    let previousUnityAppId = $unityAppId.val();

    $unityAppId.on('input', function () {
        const currentUnityAppId = $(this).val().trim();
        $('#saveApplicantInfoBtn').prop('disabled', currentUnityAppId === previousUnityAppId);
    });

    $('body').on('click', '#saveApplicantInfoBtn', function () {        
        let applicationId = document.getElementById('ApplicantInfoViewApplicationId').value;
        let formData = $("#ApplicantInfoForm").serializeArray();
        let ApplicantInfoObj = {};
        let formVersionId = $("#ApplicationFormVersionId").val();
        let worksheetId = $("#ApplicantInfo_WorksheetId").val();

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

        // Update checkboxes which are serialized if unchecked
        $(`#ApplicantInfoForm input:checkbox`).each(function () {
            ApplicantInfoObj[this.name] = (this.checked).toString();
        });

        // Make sure all the custom fields are set in the custom fields object
        if (typeof Flex === 'function') {
            Flex?.setCustomFields(ApplicantInfoObj);
        }

        try {
            if (ApplicantInfoObj["SupplierNumber"] + "" != "undefined"
                && ApplicantInfoObj["SupplierNumber"] + "" != ""
                && ApplicantInfoObj["SupplierNumber"] + "" != ApplicantInfoObj["OriginalSupplierNumber"] + "") {
                $('.cas-spinner').show();
            }

            const orgName = $('#OrgName').val();
            ApplicantInfoObj['orgName'] = orgName;
            const orgNumber = $('#OrgNumber').val();
            ApplicantInfoObj['orgNumber'] = orgNumber;
            const orgStatus = $('#orgBookStatusDropdown').val();
            ApplicantInfoObj['orgStatus'] = orgStatus;
            const organizationType = $('#orgTypeDropdown').val();
            ApplicantInfoObj['organizationType'] = organizationType;
            const indigenousOrgInd = $('#indigenousOrgInd').is(":checked");
            if (indigenousOrgInd) {
                ApplicantInfoObj['IndigenousOrgInd'] = "Yes";
            }
            else {
                ApplicantInfoObj['IndigenousOrgInd'] = "No";
            }



            ApplicantInfoObj['correlationId'] = formVersionId;
            ApplicantInfoObj['worksheetId'] = worksheetId;

            let currentUnityAppId = ApplicantInfoObj['UnityApplicantId'];

            if (currentUnityAppId !== null) {
                if (previousUnityAppId !== currentUnityAppId) {
                    checkUnityApplicantIdExist(currentUnityAppId, applicationId, ApplicantInfoObj);
                } else {
                    updateApplicantInfo(applicationId, ApplicantInfoObj);
                }
            } else {
                updateApplicantInfo(applicationId, ApplicantInfoObj);
            }

            previousUnityAppId = currentUnityAppId;
            $('#saveApplicantInfoBtn').prop('disabled', true);
            PubSub.publish("applicant_info_updated", ApplicantInfoObj);

        }
        catch (error) {
            $('.cas-spinner').hide();
            console.log(error);
            $('#saveApplicantInfoBtn').prop('disabled', false);
        }
    });

    $('#orgSectorDropdown').change(function () {
        const selectedValue = $(this).val();
        let sectorList = JSON.parse($('#orgApplicationSectorList').text());

        let childDropdown = $('#orgSubSectorDropdown');
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

    $unityAppId.on('change', function () {
        if ($unityAppId.val().trim() !== previousUnityAppId) {
            $('#saveApplicantInfoBtn').prop('disabled', false);
        } else {
            $('#saveApplicantInfoBtn').prop('disabled', true);
        }
    })

    PubSub.subscribe(
        'fields_applicantinfo',
        () => {
            enableApplicantInfoSaveBtn();
        }
    );

    $('.unity-currency-input').maskMoney();

    $('#orgBookSelect').on('select2:select', function (e) {
        let selectedData = e.params.data;
        let orgBookId = selectedData.id;

        abp.ajax({
            url: '/api/app/org-book/org-book-details-query/' + orgBookId,
            type: 'GET'
        }).done(function (response) {

            $('#OrgName').val(response.names[0].text);
            $('#OrgNumber').val(orgBookId);
            let entry_status = getAttributeObjectByType("entity_status", response.attributes);
            let org_status = entry_status.value == "HIS" ? "HISTORICAL" : "ACTIVE";
            $('#orgBookStatusDropdown').val(org_status);
            let entity_type = getAttributeObjectByType("entity_type", response.attributes);
            $('#orgTypeDropdown').val(entity_type.value);


            enableApplicantInfoSaveBtn();

        });
    });

    function getAttributeObjectByType(type, attributes) {
        return attributes.find(attr => attr.type === type);
    }

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
            UnityApplicantId: getVal('applicantInfoUnityApplicantId'),
            ApplicantName: $('.application-details-breadcrumb .applicant-name').text(),
            OrgName: getVal('OrgName'),
            OrgNumber: getVal('OrgNumber'),
            NonRegOrgName: getVal('NonRegOrgName'),
            OrganizationType: getVal('orgTypeDropdown'),
            OrganizationSize: getVal('OrganizationSize'),
            OrgStatus: getVal('orgBookStatusDropdown'),
            IndigenousOrgInd: $('#indigenousOrgInd').is(':checked') ? 'Yes' : 'No',
            Sector: getVal('orgSectorDropdown'),
            SubSector: getVal('orgSubSectorDropdown'),
            SectorSubSectorIndustryDesc: getVal('SectorSubSectorIndustryDesc'),
            FiscalDay: getVal('FiscalDay'),
            FiscalMonth: getVal('FiscalMonth')
        };

        let newData = {
            ApplicantId: selectedData.id || '',
            UnityApplicantId: selectedData.UnityApplicantId || '',
            ApplicantName: selectedData.ApplicantName || '',
            OrgName: selectedData.OrgName || '',
            OrgNumber: selectedData.OrgNumber || '',
            NonRegOrgName: selectedData.NonRegOrgName || '',
            OrganizationType: selectedData.OrganizationType || '',
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
            $('.cas-spinner').show();
            $('#mergeApplicantsSpinner').show();

            let selectedPrincipal = $('input[name="merge_ApplicantId"]:checked').val();
            let principalApplicantId = selectedPrincipal === 'existing' ? existing.ApplicantId : newData.ApplicantId;
            let nonPrincipalApplicantId = selectedPrincipal === 'existing' ? newData.ApplicantId : existing.ApplicantId;
            let applicationId = $('#ApplicantInfoViewApplicationId').val();

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
                    ApplicantInfoObj[input.name.split(".")[1]] = input.value;
                    if (ApplicantInfoObj[input.name.split(".")[1]] == '') {
                        ApplicantInfoObj[input.name.split(".")[1]] = null;
                    }
                    if (input.name == 'ApplicantId' || input.name == 'SupplierNumber' || input.name == 'OriginalSupplierNumber') {
                        ApplicantInfoObj[input.name] = input.value;
                    }
                });

                $(`#ApplicantInfoForm input:checkbox`).each(function () {
                    ApplicantInfoObj[this.name] = (this.checked).toString();
                });
                if (typeof Flex === 'function') {
                    Flex?.setCustomFields(ApplicantInfoObj);
                }

                if (ApplicantInfoObj["SupplierNumber"] + "" != "undefined"
                    && ApplicantInfoObj["SupplierNumber"] + "" != ""
                    && ApplicantInfoObj["SupplierNumber"] + "" != ApplicantInfoObj["OriginalSupplierNumber"] + "") {
                    $('.cas-spinner').show();
                }

                const orgName = $('#OrgName').val();
                ApplicantInfoObj['OrgName'] = orgName;
                const orgNumber = $('#OrgNumber').val();
                ApplicantInfoObj['OrgNumber'] = orgNumber;
                const orgStatus = $('#orgBookStatusDropdown').val();
                ApplicantInfoObj['OrgStatus'] = orgStatus;
                const organizationType = $('#orgTypeDropdown').val();
                ApplicantInfoObj['OrganizationType'] = organizationType;
                const indigenousOrgInd = $('#indigenousOrgInd').is(":checked");
                ApplicantInfoObj['IndigenousOrgInd'] = indigenousOrgInd ? "Yes" : "No";
                ApplicantInfoObj['correlationId'] = formVersionId;
                ApplicantInfoObj['worksheetId'] = worksheetId;
                ApplicantInfoObj.ApplicantId = principalApplicantId;
                Object.assign(ApplicantInfoObj, mergedApplicantInfo);


                try {
                    await handleApplicantMerge(applicationId, principalApplicantId, nonPrincipalApplicantId, newData, ApplicantInfoObj);
                } catch (err) {
                    console.error(err);
                }
            }

            // Update form fields with merged values
            for (const key in mergedApplicantInfo) {
                switch (key) {
                    case 'ApplicantId':
                        $('#ApplicantId').val(mergedApplicantInfo[key]);
                        $('#ApplicantInfoViewApplicantId').val(mergedApplicantInfo[key]);
                        break;
                    case 'UnityApplicantId':
                        $('#applicantInfoUnityApplicantId').val(mergedApplicantInfo[key]);
                        break;
                    case 'ApplicantName':
                        $('.application-details-breadcrumb .applicant-name').val(mergedApplicantInfo[key]);
                        $('.application-details-breadcrumb .applicant-name').text(mergedApplicantInfo[key]);
                        break;
                    case 'OrgName':
                        $('#OrgName').val(mergedApplicantInfo[key]);
                        break;
                    case 'OrgNumber':
                        $('#OrgNumber').val(mergedApplicantInfo[key]);
                        break;
                    case 'NonRegOrgName':
                        $('#NonRegOrgName').val(mergedApplicantInfo[key]);
                        break;
                    case 'OrganizationType':
                        $('#orgTypeDropdown').val(mergedApplicantInfo[key]);
                        break;
                    case 'OrganizationSize':
                        $('#OrganizationSize').val(mergedApplicantInfo[key]);
                        break;
                    case 'OrgStatus':
                        $('#orgBookStatusDropdown').val(mergedApplicantInfo[key]);
                        break;
                    case 'IndigenousOrgInd':
                        $('#indigenousOrgInd').prop('checked', mergedApplicantInfo[key] === 'Yes');
                        break;
                    case 'Sector':
                        $('#orgSectorDropdown').val(mergedApplicantInfo[key]);
                        break;
                    case 'SubSector':
                        $('#orgSubSectorDropdown').val(mergedApplicantInfo[key]);
                        break;
                    case 'SectorSubSectorIndustryDesc':
                        $('#SectorSubSectorIndustryDesc').val(mergedApplicantInfo[key]);
                        break;
                    case 'FiscalDay':
                        $('#FiscalDay').val(mergedApplicantInfo[key]);
                        break;
                    case 'FiscalMonth':
                        $('#FiscalMonth').val(mergedApplicantInfo[key]);
                        break;
                }
            }

            $('.cas-spinner').hide();
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

    $('[data-bs-toggle="tooltip"]').tooltip();
    setElectoralDistrictLockState(true);

    // Listen for changes in physical address fields
    $('.physical-address-fields-group').on('change', 'input', function () {
        if (
            $('#ApplicantElectoralAddressType').val() === "PhysicalAddress" &&
            !electoralDistrictLocked
        ) {
            refreshApplicantElectoralDistrict();
        }
    });

    // Listen for changes in mailing address fields
    $('.mailing-address-fields-group').on('change', 'input', function () {
        if (
            $('#ApplicantElectoralAddressType').val() === "MailingAddress" &&
            !electoralDistrictLocked
        ) {
            refreshApplicantElectoralDistrict();
        }
    });

});

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
    const street = $(`#${prefix}Street`).val() || '';
    const city = $(`#${prefix}City`).val() || '';
    const province = $(`#${prefix}Province`).val() || '';
    const postal = $(`#${prefix}Postal`).val() || '';
    const country = $(`#${prefix}Country`).val() || '';

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
        updatePrincipalApplicant(applicationId, principalApplicantId);
    }

    updateApplicantInfo(applicationId, ApplicantInfoObj);
}

async function generateUnityApplicantIdBtn() {
    try {
        let nextUnityApplicantId = await unity.grantManager.applicants.applicant.getNextUnityApplicantId();
        document.getElementById('applicantInfoUnityApplicantId').value = nextUnityApplicantId;
        $('#saveApplicantInfoBtn').prop('disabled', false);
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
function refreshSupplierInfoWidget() {
    const applicantId = $("#ApplicantInfoViewApplicantId").val();
    const url = `../Payments/Widget/SupplierInfo/Refresh?applicantId=${applicantId}`;
    fetch(url)
        .then(response => response.text())
        .then(data => {
            let supplierInfo = document.getElementById('supplier-info-widget');
            const parser = new DOMParser();
            const doc = parser.parseFromString(data, 'text/html');
            const siteIdValue = doc.querySelector('#SiteId').value;

            if (supplierInfo) {
                supplierInfo.innerHTML = data;
                PubSub.publish('reload_sites_list', siteIdValue);
            }
            $('.cas-spinner').hide();
        })
        .catch(error => {
            $('.cas-spinner').hide();
            console.error('Error refreshing supplier-info-widget:', error);
        });
}

function enableApplicantInfoSaveBtn(inputText) {
    if (!$("#ApplicantInfoForm").valid()
        || !abp.auth.isGranted('GrantApplicationManagement.ApplicantInfo.Update')
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
            refreshSupplierInfoWidget();
        })
        .then(function () {
            $('.cas-spinner').hide();
        }).catch(function () {
            $('.cas-spinner').hide();
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
    return setTimeout(function () {
        $.ajax({
            url: '/api/app/applicant/applicant-id',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                applicationId: applicationId,
                applicantId: principalApplicantId
            }),
            success: function () {
                abp.notify.success('Principal Applicant updated successfully.');
            },
            error: function (xhr, status) {
                abp.notify.error('Failed to update Principal Applicant.');
            }
        });
    }, 1000);
}
