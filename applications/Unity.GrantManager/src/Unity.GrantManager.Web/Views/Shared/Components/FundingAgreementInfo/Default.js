$(function () {
    $('.numeric-mask').maskMoney({ precision: 0 });
    $('.percentage-mask').maskMoney();
    $('.numeric-mask').each(function () {
        $(this).maskMoney('mask', this.value);
    });
    $('.percentage-mask').each(function () {
        $(this).maskMoney('mask', this.value);
    });
    $('body').on('click', '#saveFundingAgreementInfoBtn', function () {
        let applicationId = document.getElementById('FundingAgreementInfoViewApplicationId').value;
        let formData = $("#fundingAgreementInfoForm").serializeArray();
        let fundingAgreementInfoObj = {};
        let formVersionId = $("#ApplicationFormVersionId").val();
        let worksheetId = $("#ProjectInfo_WorksheetId").val();

        $.each(formData, function (_, input) {
            if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                Flex.includeCustomFieldObj(fundingAgreementInfoObj, input);
            }
            else if ((input.name == "ProjectInfo.ProjectName") || (input.name == "ProjectInfo.ProjectSummary") || (input.name == "ProjectInfo.Community")) {
                fundingAgreementInfoObj[input.name.split(".")[1]] = input.value;
            } else {
                buildFormData(fundingAgreementInfoObj, input)
            }
        });

        // Update checkboxes which are serialized if unchecked
        $(`#fundingAgreementInfoForm input:checkbox`).each(function () {
            fundingAgreementInfoObj[this.name] = (this.checked).toString();
        });

        fundingAgreementInfoObj['correlationId'] = formVersionId;
        fundingAgreementInfoObj['worksheetId'] = worksheetId;
        updateProjectInfo(applicationId, fundingAgreementInfoObj);
    });

    function buildFormData(fundingAgreementInfoObj, input) {

        let inputElement = $('[name="' + input.name + '"]');
        // This will not work if the culture is different and uses a different decimal separator
        if (inputElement.hasClass('unity-currency-input') || inputElement.hasClass('numeric-mask')) {
            fundingAgreementInfoObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');
        }
        else {
            fundingAgreementInfoObj[input.name.split(".")[1]] = input.value;
        }
        if (isNumberField(input)) {
            if (fundingAgreementInfoObj[input.name.split(".")[1]] == '') {
                fundingAgreementInfoObj[input.name.split(".")[1]] = 0;
            } else if (fundingAgreementInfoObj[input.name.split(".")[1]] > getMaxNumberField(input)) {
                fundingAgreementInfoObj[input.name.split(".")[1]] = getMaxNumberField(input);
            }
        }
        else if (fundingAgreementInfoObj[input.name.split(".")[1]] == '') {
            fundingAgreementInfoObj[input.name.split(".")[1]] = null;
        }
    }

    function updateProjectInfo(applicationId, fundingAgreementInfoObj) {
        try {
            unity.grantManager.grantApplications.grantApplication
                .updateProjectInfo(applicationId, fundingAgreementInfoObj)
                .done(function () {
                    abp.notify.success(
                        'The funding agreement info has been updated.'
                    );
                    $('#saveFundingAgreementInfoBtn').prop('disabled', true);
                    PubSub.publish('project_info_saved', fundingAgreementInfoObj);
                    PubSub.publish('refresh_detail_panel_summary');
                });
        }
        catch (error) {
            console.log(error);
            $('#saveFundingAgreementInfoBtn').prop('disabled', false);
        }
    }

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

    $('#startDate').on('apply.daterangepicker', function (event, picker) {
        console.log(event, picker);
    });


    $('#economicRegions').change(function () {

        const selectedValue = $(this).val();
        let allEconomicRegions = JSON.parse($('#allEconomicRegionList').text());
        let allRegionalDistricts = JSON.parse($('#allRegionalDistrictList').text());
        let selectedEconomicRegion = allEconomicRegions.find(d => d.economicRegionName == selectedValue);
        let childDropdown = initializeDroplist('#regionalDistricts');

        if (selectedValue) {
            let regionalDistricts = allRegionalDistricts.filter(d => d.economicRegionCode == selectedEconomicRegion.economicRegionCode);
            $.each(regionalDistricts, function (index, item) {
                childDropdown.append($('<option>', {
                    value: item.regionalDistrictName,
                    text: item.regionalDistrictName
                }));
            });
        }
        $('#regionalDistricts').change();
    });

    $('#regionalDistricts').change(function () {
        const selectedValue = $(this).val();
        let childDropdown = initializeDroplist('#communities');
        if (selectedValue) {
            let allSubdistricts = JSON.parse($('#allRegionalDistrictList').text());
            let allCommunities = JSON.parse($('#allCommunitiesList').text());
            let selectedSubDistrict = allSubdistricts.find(d => d.regionalDistrictName == selectedValue);
            let communities = allCommunities.filter(d => d.regionalDistrictCode == selectedSubDistrict.regionalDistrictCode);

            $.each(communities, function (index, item) {
                childDropdown.append($('<option>', {
                    value: item.name,
                    text: item.name
                }));
            });
        }
    });

    function initializeDroplist(dropListId) {
        let initializedDropList = $(dropListId);
        initializedDropList.empty();
        initializedDropList.append($('<option>', {
            value: '',
            text: 'Please choose...'
        }));

        return initializedDropList;
    }

    $('.remove-leading-zeros').on('input', function () {
        let inputValue = $(this).val();
        let newValue = inputValue.replace(/^0+(?!$)/, '');
        $(this).val(newValue);
    });

    PubSub.subscribe('application_assessment_results_saved',
        (msg, data) => {
            if (data.RequestedAmount) {
                $('#RequestedAmountInputPI').prop("value", data.RequestedAmount);
                $('#RequestedAmountInputPI').maskMoney('mask');
            }
            if (data.TotalProjectBudget) {
                $('#TotalBudgetInputPI').prop("value", data.TotalProjectBudget);
                $('#TotalBudgetInputPI').maskMoney('mask');
            }
        }
    );

    PubSub.subscribe(
        'fields_fundingagreementinfo',
        () => {
            enableFundingAgreementInfoSaveBtn();
        }
    );

    $('.unity-currency-input').maskMoney();
});


function enableFundingAgreementInfoSaveBtn(inputText) {
    if (!$("#fundingAgreementInfoForm").valid() || formHasInvalidCurrencyCustomFields("fundingAgreementInfoForm")) {
        $('#saveFundingAgreementInfoBtn').prop('disabled', true);
        return;
    }

    $('#saveFundingAgreementInfoBtn').prop('disabled', false);
}

//function calculatePercentage() {
//    const requestedAmount = parseFloat(document.getElementById("RequestedAmountInputPI")?.value.replace(/,/g, ''));
//    const totalProjectBudget = parseFloat(document.getElementById("TotalBudgetInputPI")?.value.replace(/,/g, ''));
//    if (isNaN(requestedAmount) || isNaN(totalProjectBudget) || totalProjectBudget == 0) {
//        document.getElementById("ProjectInfo_PercentageTotalProjectBudget").value = 0;
//        return;
//    }
//    const percentage = ((requestedAmount / totalProjectBudget) * 100.00).toFixed(2);
//    $("#ProjectInfo_PercentageTotalProjectBudget").maskMoney('mask', parseFloat(percentage));

//}
