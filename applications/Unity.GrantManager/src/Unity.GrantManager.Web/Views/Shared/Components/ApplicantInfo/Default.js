$(function () {    
    $('.currency-input').maskMoney();

    $('body').on('click', '#saveApplicantInfoBtn', function () {
        let applicationId = document.getElementById('ApplicantInfoViewApplicationId').value;
        let formData = $("#ApplicantInfoForm").serializeArray();
        let ApplicantInfoObj = {};
        $.each(formData, function (key, input) {
           
                // This will not work if the culture is different and uses a different decimal separator
                ApplicantInfoObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');

                
                if (ApplicantInfoObj[input.name.split(".")[1]] == '') {
                    ApplicantInfoObj[input.name.split(".")[1]] = null;
                }

            if (input.name == 'ApplicantId' || input.name == 'SupplierNumber') {
                ApplicantInfoObj[input.name] = input.value;
            }
            
        });
        try {
            unity.grantManager.grantApplications.grantApplication
                .updateProjectApplicantInfo(applicationId, ApplicantInfoObj)
                .done(function () {
                    abp.notify.success(
                        'The Applicant info has been updated.'
                    );
                    $('#saveApplicantInfoBtn').prop('disabled', true);
                    PubSub.publish("refresh_detail_panel_summary");
                    PubSub.publish('project_info_saved');
                    refreshSupplierInfoWidget();
                });
        }
        catch (error) {
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
            })
            .catch(error => {
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

});


function enableSaveBtn(inputText) {
    if (!$("#ApplicantInfoForm").valid()) {
        $('#saveApplicantInfoBtn').prop('disabled', true);
        return;
    }
    $('#saveApplicantInfoBtn').prop('disabled', false);
}


