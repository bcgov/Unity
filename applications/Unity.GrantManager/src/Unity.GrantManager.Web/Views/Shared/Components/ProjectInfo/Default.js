$(function () {    
    $('.currency-input').maskMoney();

    $('body').on('click', '#saveProjectInfoBtn', function () {       
        let applicationId = document.getElementById('ProjectInfoViewApplicationId').value;
        let formData = $("#projectInfoForm").serializeArray();
        let assessmentResultObj = {};
        $.each(formData, function (key, input) {
            if ((input.name == "AssessmentResults.ProjectSummary") || (input.name == "AssessmentResults.Notes")) {
                assessmentResultObj[input.name.split(".")[1]] = input.value;
            } else {
                // This will not work if the culture is different and uses a different decimal separator
                assessmentResultObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');
                console.log(input);

                if (isNumberField(input)) {
                    if (assessmentResultObj[input.name.split(".")[1]] == '') {
                        assessmentResultObj[input.name.split(".")[1]] = 0;
                    } else if (assessmentResultObj[input.name.split(".")[1]] > getMaxNumberField(input)) {
                        assessmentResultObj[input.name.split(".")[1]] = getMaxNumberField(input);
                    }
                }
            }
        });
        try {
            unity.grantManager.grantApplications.grantApplication
                .update(applicationId, assessmentResultObj)
                .done(function () {
                    abp.notify.success(
                        'The application has been updated.'
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
        return isCurrencyField(input) || isScoreField(input);
    }

    function isCurrencyField(input) {
        const currencyFields = ['AssessmentResults.RequestedAmount',
            'AssessmentResults.TotalProjectBudget',
            'AssessmentResults.RecommendedAmount',
            'AssessmentResults.ApprovedAmount'];
        return currencyFields.includes(input.name);
    }

    function isScoreField(input) {
        return input.name == 'AssessmentResults.TotalScore';
    }
});


function enableSaveBtn(inputText) {
    if (inputText?.value?.trim() != "") {
        $('#saveProjectInfoBtn').prop('disabled', false);
    } else {
        $('#saveProjectInfoBtn').prop('disabled', true);
    }
}
