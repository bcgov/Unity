$(function () {    
    $('.currency-input').maskMoney();

    $('body').on('click', '#saveApplicantInfoBtn', function () {
        let applicationId = document.getElementById('ApplicantInfoViewApplicationId').value;
        let formData = $("#ApplicantInfoForm").serializeArray();
        let ApplicantInfoObj = {};
        $.each(formData, function (key, input) {
           
                // This will not work if the culture is different and uses a different decimal separator
                ApplicantInfoObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');

                if (isNumberField(input)) {
                    if (ApplicantInfoObj[input.name.split(".")[1]] == '') {
                        ApplicantInfoObj[input.name.split(".")[1]] = 0;
                    } else if (ApplicantInfoObj[input.name.split(".")[1]] > getMaxNumberField(input)) {
                        ApplicantInfoObj[input.name.split(".")[1]] = getMaxNumberField(input);
                    }
                }
                else if (ApplicantInfoObj[input.name.split(".")[1]] == '') {
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

    function getMaxNumberField(input) {
        const maxCurrency = 10000000000000000000000000000;
        const maxScore = 2147483647;
        if (isCurrencyField(input))
            return maxCurrency;
        else
            return maxScore;
    }

    function isNumberField(input) {
        return isCurrencyField(input) || isPercentageField(input);
    }

    function isCurrencyField(input) {
        const currencyFields = ['ApplicantInfo.RequestedAmount',
            'ApplicantInfo.TotalProjectBudget',
            'ApplicantInfo.ProjectFundingTotal'];
        return currencyFields.includes(input.name);
    }

    function isPercentageField(input) {
        return input.name == 'ApplicantInfo.PercentageTotalProjectBudget';
    }

    $('#startDate').on('apply.daterangepicker', function(event, picker) {
        console.log(event, picker);
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



    function initializeDroplist(dropListId) {
        let initializedDropList = $(dropListId);
        initializedDropList.empty();
        initializedDropList.append($('<option>', {
            value: '',
            text: 'Please Choose...'
        }));

        return initializedDropList;
    }

    $('.remove-leading-zeros').on('input', function () {
        let inputValue = $(this).val();
        let newValue = inputValue.replace(/^0+(?!$)/, '');
        $(this).val(newValue);
    });
});


function enableSaveBtn(inputText) {
    if (!$("#ApplicantInfoForm").valid()) {
        $('#saveApplicantInfoBtn').prop('disabled', true);
        return;
    }
    if (!document.getElementById("ApplicantInfo_ContactEmail").validity.valid ||
        !document.getElementById("ApplicantInfo_ContactBusinessPhone").checkValidity() ||
        !document.getElementById("ApplicantInfo_ContactCellPhone").checkValidity()) {
        $('#saveApplicantInfoBtn').prop('disabled', true);
        return;
    } 

    $('#saveApplicantInfoBtn').prop('disabled', false);
}

function calculatePercentage() {
    const requestedAmount = parseFloat(document.getElementById("ApplicantInfo_RequestedAmount").value.replace(/,/g, ''));
    const totalProjectBudget = parseFloat(document.getElementById("ApplicantInfo_TotalProjectBudget").value.replace(/,/g, ''));
    if (isNaN(requestedAmount) || isNaN(totalProjectBudget) || totalProjectBudget == 0) {
        document.getElementById("ApplicantInfo_PercentageTotalProjectBudget").value = 0;
        return;
    }
    const percentage = (requestedAmount / totalProjectBudget) * 100.00;
    document.getElementById("ApplicantInfo_PercentageTotalProjectBudget").value = percentage.toFixed(2);
}
