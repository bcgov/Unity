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
            
        });
        try {
            unity.grantManager.grantApplications.grantApplication
                .updateProjectApplicantInfo(applicationId, ApplicantInfoObj)
                .done(function () {
                    abp.notify.success(
                        'The project info has been updated.'
                    );
                    $('#saveApplicantInfoBtn').prop('disabled', true);
                    PubSub.publish('project_info_saved');
                });
        }
        catch (error) {
            console.log(error);
            $('#saveApplicantInfoBtn').prop('disabled', false);
        }
    });



 

    $('#orgSectorDropdown').change(function () {
        const selectedValue = $(this).val();
        let sectorList = JSON.parse($('#applicationSectorList').text());

        let childDropdown = $('#orgSubSectorDropdown');
        childDropdown.empty();

        let subSectors = sectorList.find(sector => (sector.sectorName === selectedValue))?.subSectors;
        childDropdown.append($('<option>', {
            value: '',
            text: 'Please Choose...'
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


