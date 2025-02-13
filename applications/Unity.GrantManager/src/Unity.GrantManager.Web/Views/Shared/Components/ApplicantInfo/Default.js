$(function () {
    $('.numeric-mask').maskMoney({ precision: 0 });
    $('.numeric-mask').each(function () {
        $(this).maskMoney('mask', this.value);
    });

    const $unityAppId = $('#applicantInfoUnityApplicantId');
    let previousUnityAppId = $unityAppId.val();
    console.log(previousUnityAppId);

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

            ApplicantInfoObj['correlationId'] = formVersionId;
            ApplicantInfoObj['worksheetId'] = worksheetId;

            if (ApplicantInfoObj['UnityApplicantId'] !== null) {
                if (previousUnityAppId !== ApplicantInfoObj['UnityApplicantId']) {
                    checkUnityApplicantIdExist(ApplicantInfoObj['UnityApplicantId'], applicationId, ApplicantInfoObj);
                } else {
                    updateApplicantInfo(applicationId, ApplicantInfoObj);
                    }
            } else { 
                updateApplicantInfo(applicationId, ApplicantInfoObj);
            }
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
});


async function generateUnityApplicantIdBtn() {
    try {
        let nextUnityApplicantId = await unity.grantManager.applicants.applicant.getNextUnityApplicantId().then(data => {
            return data;
        });
        document.getElementById('applicantInfoUnityApplicantId').value = nextUnityApplicantId;
        $('#saveApplicantInfoBtn').prop('disabled', false);
    }
    catch (error) {
        console.log(error);
    }
};

async function checkUnityApplicantIdExist(unityAppId, appId, appInfoObj ) {
    try {
        let existingApplicant = await unity.grantManager.applicants.applicant.getExistingApplicant(unityAppId).then(data => {
            return data;
        });

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

