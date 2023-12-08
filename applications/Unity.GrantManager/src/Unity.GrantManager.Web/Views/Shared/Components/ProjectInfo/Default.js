$(function () {    
    $('.currency-input').maskMoney();

    $('body').on('click', '#saveProjectInfoBtn', function () {       
        let applicationId = document.getElementById('ProjectInfoViewApplicationId').value;
        let formData = $("#projectInfoForm").serializeArray();
        let projectInfoObj = {};
        $.each(formData, function (key, input) {
            if ((input.name == "ProjectInfo.ProjectName") || (input.name == "ProjectInfo.ProjectSummary") || (input.name == "ProjectInfo.Community")) {
                projectInfoObj[input.name.split(".")[1]] = input.value;
            } else {
                // This will not work if the culture is different and uses a different decimal separator
                projectInfoObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');

                if (isNumberField(input)) {
                    if (projectInfoObj[input.name.split(".")[1]] == '') {
                        projectInfoObj[input.name.split(".")[1]] = 0;
                    } else if (projectInfoObj[input.name.split(".")[1]] > getMaxNumberField(input)) {
                        projectInfoObj[input.name.split(".")[1]] = getMaxNumberField(input);
                    }
                }
            }
        });
        try {
            unity.grantManager.grantApplications.grantApplication
                .updateProjectInfo(applicationId, projectInfoObj)
                .done(function () {
                    abp.notify.success(
                        'The project info has been updated.'
                    );
                    $('#saveProjectInfoBtn').prop('disabled', true);
                    PubSub.publish('project_info_saved');                    
                });
        }
        catch (error) {
            console.log(error);
            $('#saveProjectInfoBtn').prop('disabled', false);
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
        const currencyFields = ['ProjectInfo.RequestedAmount',
            'ProjectInfo.TotalProjectBudget',
            'ProjectInfo.ProjectFundingTotal'];
        return currencyFields.includes(input.name);
    }

    function isPercentageField(input) {
        return input.name == 'ProjectInfo.PercentageTotalProjectBudget';
    }

    $('#startDate').on('apply.daterangepicker', function(event, picker) {
        console.log(event, picker);
      });

      $('#sectorDropdown').change(function () {
        var selectedValue = $(this).val();

        let sectorList = JSON.parse($('#applicationSectorList').text());

        var childDropdown = $('#subSectorDropdown');
        childDropdown.empty();

        let subSectors = sectorList.find(sector => (sector.sectorCode === selectedValue))?.subSectors;

        $.each(subSectors, function (index, item) {
            childDropdown.append($('<option>', {
                value: item.subSectorCode,
                text: item.subSectorName
            }));
        });
    });
});


function enableSaveBtn(inputText) {
    if (inputText?.value?.trim() != "") {
        $('#saveProjectInfoBtn').prop('disabled', false);
    } else {
        $('#saveProjectInfoBtn').prop('disabled', true);
    }
}
