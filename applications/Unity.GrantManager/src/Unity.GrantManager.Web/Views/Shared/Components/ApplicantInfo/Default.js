$(function () {    
    $('.currency-input').maskMoney();

    $('body').on('click', '#saveApplicantInfoBtn', function () {
        let applicationId = document.getElementById('ApplicantInfoViewApplicationId').value;
        let formData = $("#ApplicantInfoForm").serializeArray();
        let ApplicantInfoObj = {};
        $.each(formData, function (key, input) {
            // This will not work if the culture is different and uses a different decimal separator
            if (input.name === 'ApplicantInfo.SiteNumbers') {
                ApplicantInfoObj[input.name.split(".")[1]] = input.value;
            } else {
                ApplicantInfoObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');
            }
                
            if (ApplicantInfoObj[input.name.split(".")[1]] == '') {
                ApplicantInfoObj[input.name.split(".")[1]] = null;
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
                });
        }
        catch (error) {
            console.log(error);
            $('#saveApplicantInfoBtn').prop('disabled', false);
        }
    });


    let tagInput = new TagsInput({
        selector: 'SiteNumbers',
        duplicate: false,
        max: 50
    });
    let siteNumbersArray = [];
    let inputArray = [];
    let siteNumbers = $('#SiteNumbers').val();
    if (siteNumbers) {
        siteNumbersArray = siteNumbers.split(',');
    }
    if (siteNumbersArray.length) {
        siteNumbersArray.forEach(function (item, index) {
            inputArray.push({ text: item, class: 'tags-common', id: index + 1 })
        });
    }
    tagInput.addData(inputArray);
    tagInput.callback = enableSaveBtn;

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


