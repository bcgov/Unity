$(function () {
    $('.numeric-mask').maskMoney({ precision: 0 });
    $('.numeric-mask').each(function () {
        $(this).maskMoney('mask', this.value);
    });

    $('body').on('click', '#saveApplicantInfoBtn', function () {
        let applicationId = document.getElementById('ApplicantInfoViewApplicationId').value;
        let formData = $("#ApplicantInfoForm").serializeArray();
        let ApplicantInfoObj = {};
        let formVersionId = $("#ApplicationFormVersionId").val(); 
        let worksheetId = $("#WorksheetId").val();

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

        try {

            if (ApplicantInfoObj["SupplierNumber"]+"" != "undefined" 
             && ApplicantInfoObj["SupplierNumber"]+"" != ""
             && ApplicantInfoObj["SupplierNumber"]+"" != ApplicantInfoObj["OriginalSupplierNumber"]+"")
            {
                $('.cas-spinner').show();
            }

            ApplicantInfoObj['correlationId'] = formVersionId;
            ApplicantInfoObj['worksheetId'] = worksheetId;
            unity.grantManager.grantApplications.grantApplication
                .updateProjectApplicantInfo(applicationId, ApplicantInfoObj)
                .done(function () {
                    abp.notify.success(
                        'The Applicant info has been updated.'
                    );
                    $('#saveApplicantInfoBtn').prop('disabled', true);
                    PubSub.publish("refresh_detail_panel_summary");                    
                    refreshSupplierInfoWidget();
                })
                .then(function () {
                    $('.cas-spinner').hide();
                }).catch(function(){
                    $('.cas-spinner').hide();
                });
        }
        catch (error) {
            $('.cas-spinner').hide();
            console.log(error);
            $('#saveApplicantInfoBtn').prop('disabled', false);
        }
    });

    function refreshSupplierInfoWidget() {
        const applicantId = $("#ApplicantInfoViewApplicantId").val();
        const url = `../Payments/Widget/SupplierInfo/Refresh?applicantId=${applicantId}`;
        fetch(url)
            .then(response => response.text())
            .then(data => {
                let supplierInfo = document.getElementById('supplier-info-widget');
                if (supplierInfo) {
                    supplierInfo.innerHTML = data;
                    PubSub.publish('reload_sites_list');
                }
                $('.cas-spinner').hide();
            })
            .catch(error => {
                $('.cas-spinner').hide();
                console.error('Error refreshing supplier-info-widget:', error);
            });
    }

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

    PubSub.subscribe(
        'fields_applicantinfo',
        () => {
            enableSaveBtn();
        }
    );

    $('.unity-currency-input').maskMoney();
});


function enableSaveBtn(inputText) {
    if (!$("#ApplicantInfoForm").valid() || formHasInvalidCurrencyCustomFields("ApplicantInfoForm")) {
        $('#saveApplicantInfoBtn').prop('disabled', true);
        return;
    }
    $('#saveApplicantInfoBtn').prop('disabled', false);
}


