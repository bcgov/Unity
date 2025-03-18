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
                // This will not work if the culture is different and uses a different decimal separator
                ApplicantInfoObj[input.name.split(".")[1]] = input.value;

                if (ApplicantInfoObj[input.name.split(".")[1]] == '') {
                    ApplicantInfoObj[input.name.split(".")[1]] = null;
                }

                if (input.name == 'ApplicantId' || input.name == 'SupplierNumber' || input.name == 'OriginalSupplierNumber') {
                    ApplicantInfoObj[input.name] = input.value;
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

            const orgName = $('#ApplicantInfo_OrgName').val();
            ApplicantInfoObj['orgName'] = orgName;
            const orgNumber = $('#ApplicantInfo_OrgNumber').val();
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
                ApplicantInfoObj['IndigenousOrgInd'] =  "No";
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

    let $orgBookSelect = $('.auto-complete-select');

    $orgBookSelect.on('select2:select', function (e) {
        let selectedData = e.params.data;
        let orgBookId = selectedData.id;

        abp.ajax({
            url: '/api/app/org-book/org-book-details-query/' + orgBookId,
            type: 'GET'
        }).done(function (response) {
           
            $('#ApplicantInfo_OrgName').val(response.names[0].text);
            $('#ApplicantInfo_OrgNumber').val(orgBookId);
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
});


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

async function checkUnityApplicantIdExist(unityAppId, appId, appInfoObj ) {
    try {
        let existingApplicant = await unity.grantManager.applicants.applicant.getExistingApplicant(unityAppId);

        if (existingApplicant) {
            Swal.fire({
                icon: "error",
                text: "Applicatn ID already exists. Please enter a unique ID.",
                confirmButtonText: 'Ok',
                customClass: {
                    confirmButton: 'btn btn-primary'
                }
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

