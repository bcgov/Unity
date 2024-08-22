$(function () {
    $('body').on('click', '#saveAssessmentResultBtn', function () {
        let applicationId = document.getElementById('AssessmentResultViewApplicationId').value;
        let formData = $("#assessmentResultForm").serializeArray();
        let assessmentResultObj = {};
        let formVersionId = $("#ApplicationFormVersionId").val();     
        let worksheetId = $("#WorksheetId").val();       

        $.each(formData, function (_, input) {
            if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                Flex.includeCustomFieldObj(assessmentResultObj, input);
            }
            else if ((input.name == "AssessmentResults.ProjectSummary") || (input.name == "AssessmentResults.Notes")) {
                assessmentResultObj[input.name.split(".")[1]] = input.value;
            } else {
                let inputElement = $('[name="' + input.name + '"]');
                // This will not work if the culture is different and uses a different decimal separator
                if (inputElement.hasClass('unity-currency-input')) {
                    assessmentResultObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');
                }
                else {
                    assessmentResultObj[input.name.split(".")[1]] = input.value;
                }

                if (isNumberField(input)) {
                    if (assessmentResultObj[input.name.split(".")[1]] == '') {
                        assessmentResultObj[input.name.split(".")[1]] = 0;
                    } else if (assessmentResultObj[input.name.split(".")[1]] > getMaxNumberField(input)) {
                        assessmentResultObj[input.name.split(".")[1]] = getMaxNumberField(input);
                    }
                }
            }
        });

        // Update checkboxes which are serialized if unchecked
        $(`#assessmentResultForm input:checkbox`).each(function () {
            assessmentResultObj[this.name] = (this.checked).toString();
        });

        try {
            assessmentResultObj['correlationId'] = formVersionId;
            assessmentResultObj['worksheetId'] = worksheetId;
            unity.grantManager.grantApplications.grantApplication
                .updateAssessmentResults(applicationId, assessmentResultObj)
                .done(function () {
                    abp.notify.success(
                        'The application has been updated.'
                    );
                    $('#saveAssessmentResultBtn').prop('disabled', true);

                    PubSub.publish('application_assessment_results_saved', assessmentResultObj);
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

    PubSub.subscribe(
        'init_date_pickers',
        async (msg, data) => {
            initDatePicker();
        }
    );

    PubSub.subscribe('project_info_saved',
        (msg, data) => { 
            if (data.RequestedAmount) {
                $('#RequestedAmountInputAR')?.prop("value", data?.RequestedAmount);
                $('#RequestedAmountInputAR').maskMoney('mask');
            }
            if (data.TotalProjectBudget) {
                $('#TotalBudgetInputAR')?.prop("value", data?.TotalProjectBudget);
                $('#TotalBudgetInputAR').maskMoney('mask');
            } 
        }
    );

    PubSub.subscribe(
        'fields_assessmentinfo',
        () => {
            enableResultSaveBtn();
        }
    );

    $('.unity-currency-input').maskMoney();
});

let dueDateHasChanged = false;
let decisionDateHasChanged = false;
let notificationDateHasChanged = false;

function validateDueDate() {
    dueDateHasChanged = true;
    enableResultSaveBtn();
}

function validateNotificationDate() {
    notificationDateHasChanged = true;
    enableResultSaveBtn();
}

function validateDecisionDate() {
    decisionDateHasChanged = true;
    enableResultSaveBtn();
}

function hasInvalidExplicitValidations() {
    let explicitChangedValueValidations = [
        {
            flag: dueDateHasChanged,
            name: 'AssessmentResults_DueDate'
        },
        {
            flag: decisionDateHasChanged,
            name: 'AssessmentResults_FinalDecisionDate'
        },
        {
            flag: notificationDateHasChanged,
            name: 'AssessmentResults_NotificationDate'
        }
    ];

    for (const element of explicitChangedValueValidations) {
        let obj = element;
        let result = flaggedFieldIsValid(obj.flag, obj.name);
        if (!result) {
            return true; // validation error, exit
        }
    }

    return false;
}

function flaggedFieldIsValid(flag, name) {
    if (flag === true) {
        if (document.getElementById(name).value && !document.getElementById(name).validity.valid) {
            return false;
        }
    }

    return true;
}

function hasInvalidCustomFields() {        
    let invalidFieldsFound = false;
    $("input[id^='custom']:visible").each(function (i, el) {  
        let $field = $(this);
        if ($field.hasClass('custom-currency-input')) {
            if (!isValidCurrencyCustomField($field)) {
                invalidFieldsFound = true;
            }
        } else {
            let fieldValidity = document.getElementById(el.id).validity.valid;
            if (!fieldValidity) {
                invalidFieldsFound = true;
            }
        }
        
    });

    return invalidFieldsFound;
}

function enableResultSaveBtn() {
    if (hasInvalidCustomFields()) {
        $('#saveAssessmentResultBtn').prop('disabled', true);
        return;
    }

    if (hasInvalidExplicitValidations()) {
        $('#saveAssessmentResultBtn').prop('disabled', true);
        return;
    }

    $('#saveAssessmentResultBtn').prop('disabled', false);
}




