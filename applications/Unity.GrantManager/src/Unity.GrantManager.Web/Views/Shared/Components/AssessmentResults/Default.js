$(function () {    
    $('.unity-currency-input').maskMoney();

    $('body').on('click', '#saveAssessmentResultBtn', function () {       
        $("#approvedAmountInput").attr("disabled", false);
        let applicationId = document.getElementById('AssessmentResultViewApplicationId').value;
        let formData = $("#assessmentResultForm").serializeArray();
        let assessmentResultObj = {};
        $.each(formData, function (key, input) {
            if ((input.name == "AssessmentResults.ProjectSummary") || (input.name == "AssessmentResults.Notes")) {
                assessmentResultObj[input.name.split(".")[1]] = input.value;
            } else {
                // This will not work if the culture is different and uses a different decimal separator
                assessmentResultObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');

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
                    $('#saveAssessmentResultBtn').prop('disabled', true);
                    PubSub.publish('application_assessment_results_saved');
                    PubSub.publish('refresh_detail_panel_summary');
                    initDatePicker();
                });
        }
        catch (error) {
            console.log(error);
            $('#saveAssessmentResultBtn').prop('disabled', false);
            initDatePicker();
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

    function initDatePicker() {
        setTimeout(function () {
            let dtToday = new Date();
            let month = dtToday.getMonth() + 1;
            let day = dtToday.getDate();
            let year = dtToday.getFullYear();
            if (month < 10)
                month = '0' + month.toString();
            if (day < 10)
                day = '0' + day.toString();
            let todayDate = year + '-' + month + '-' + day;
            $('#AssessmentResults_FinalDecisionDate').attr({ 'max': todayDate });
            $('#AssessmentResults_DueDate').attr({ 'min': todayDate }); 
        }, 500)
    }
    initDatePicker();
});

let dueDateHasChanged = false;
let decisionDateHasChanged = false;

function validateDueDate() {
    dueDateHasChanged = true;
    enableResultSaveBtn('dueDate');
}

function validateDecisionDate() {
    decisionDateHasChanged = true;
    enableResultSaveBtn('decisionDate');
}

function areLegacyDatesInvalid() {
    if (dueDateHasChanged) {
        if (document.getElementById('AssessmentResults_DueDate').value && !document.getElementById('AssessmentResults_DueDate').validity.valid) {
            $('#saveAssessmentResultBtn').prop('disabled', true);
            return true;
        }
    }
    if (decisionDateHasChanged) {
        if (document.getElementById('AssessmentResults_FinalDecisionDate').value && !document.getElementById('AssessmentResults_FinalDecisionDate').validity.valid) {
            $('#saveAssessmentResultBtn').prop('disabled', true);
            return true;
        }
    }

    return false;
}

function enableResultSaveBtn(inputText) {
    if (areLegacyDatesInvalid()) {
        return;
    }
    $('#saveAssessmentResultBtn').prop('disabled', false);
}


