function saveAssessmentScores() {
    try {        
        let data = {
            "financialAnalysis": parseScoreValueInput("financialAnalysis"),
            "economicImpact": parseScoreValueInput("economicImpact"),
            "inclusiveGrowth": parseScoreValueInput("inclusiveGrowth"),
            "cleanGrowth": parseScoreValueInput("cleanGrowth"),
            "assessmentId": $("#AssessmentId").val(),
        }        
        unity.grantManager.assessments.assessment.updateAssessmentScore(data)
            .done(function () {
                abp.notify.success(
                    'Assessment scores has been updated.'
                );
                PubSub.publish('refresh_assessment_scores', null);
                PubSub.publish('refresh_review_list_without_select', $("#AssessmentId").val());
            });

    }
    catch (error) {
        console.log(error);
    }
};

function parseScoreValueInput(name) {    
    let control = "#" + name;
    return $(control).val() == '' ? 0 : Math.min($(control).attr('max'), $(control).val())
}

function enableSaveButton(inputText) {
    if (inputText.value.trim() != "") {
        $('#saveAssessmentScoresBtn').prop('disabled', false);
    } else {
        $('#saveAssessmentScoresBtn').prop('disabled', true);
    }    
    updateSum();
}

function updateSum() {
    let financialAnalysis = $('#financialAnalysis').val() || 0;
    let inclusiveGrowth = $('#inclusiveGrowth').val() || 0;
    let cleanGrowth = $('#cleanGrowth').val() || 0;
    let economicImpact = $('#economicImpact').val() || 0;
    let sum = parseInt(financialAnalysis) + parseInt(inclusiveGrowth) + parseInt(cleanGrowth) + parseInt(economicImpact);
    $('#subTotal').val(sum);
}

function positiveIntegersOnly(e) {    
    if (e.keyCode === 9
        || e.keyCode === 8
        || e.keyCode === 37
        || e.keyCode === 39) {
        return true;
    }
    if(e.target?.value?.length >= 2 ) {
        return false;
    }
    if (!((e.keyCode > 95 && e.keyCode < 106)
        || (e.keyCode > 47 && e.keyCode < 58)
        || e.keyCode == 8)) {
        return false;
    }
}

function handleInputChange(questionId, inputFieldPrefix, saveButtonPrefix, discardButtonPrefix) {
    const inputField = document.getElementById(inputFieldPrefix + questionId);
    const saveButton = document.getElementById(saveButtonPrefix + questionId);
    const discardButton = document.getElementById(discardButtonPrefix + questionId);
    const errorMessage = document.getElementById('error-message-' + questionId);
    const originalValue = inputField.getAttribute('data-original-value');

    let valid = true;   

    if (inputFieldPrefix == 'answer-number-') {
        valid = validateNumericField(inputField, errorMessage);
    } else if (inputFieldPrefix == 'answer-text-') {
        valid = validateTextField(inputField, errorMessage);
    }      

    if (inputField.value !== originalValue && valid) {
        saveButton.disabled = false;
        discardButton.disabled = false;
    } else if (inputField.value !== originalValue && !valid) {
        saveButton.disabled = true;
        discardButton.disabled = false;
    } else {
        saveButton.disabled = true;
    }
}

function validateTextField(textInputField, errorMessage) {
    if (textInputField.validity.tooShort) {
        errorMessage.textContent = 'The answer is too short. Minimum length is ' + textInputField.minLength + ' characters.';
        return false;
    } else if (textInputField.validity.tooLong) {
        errorMessage.textContent = 'The answer is too long. Maximum length is ' + textInputField.maxLength + ' characters.';
        return false;
    } else {
        errorMessage.textContent = ''; 
        return true;
    }
}

function validateNumericField(numericInputField, errorMessage) {
    if (numericInputField.validity.rangeOverflow) {
        errorMessage.textContent = `Value must be less than or equal to ${numericInputField.max}.`;
        return false;
    } else if (numericInputField.validity.rangeUnderflow) {
        errorMessage.textContent = `Value must be greater than or equal to ${numericInputField.min}.`;
        return false;
    } else {
        errorMessage.textContent = '';
        return true;
    }
}

function updateSubtotal() {
    setTimeout(function () {
        let subtotal = 0;

        // Handle number inputs
        const numberInputs = document.querySelectorAll('.answer-number-input');
        numberInputs.forEach(input => {
            subtotal += parseFloat(input.value) || 0;
        });

        // Handle Yes/No inputs
        const yesNoInputs = document.querySelectorAll('.answer-yesno-input');
        yesNoInputs.forEach(input => {
            let value = 0;
            if (input.value === "Yes") {
                value = parseFloat(input.getAttribute('data-yes-numeric-value')) || 0;
            } else if (input.value === "No") {
                value = parseFloat(input.getAttribute('data-no-numeric-value')) || 0;
            }
            subtotal += value;
        });

        // Handle select list inputs
        const selectListInputs = document.querySelectorAll('.answer-selectlist-input');
        selectListInputs.forEach(select => {
            const selectedOption = select.options[select.selectedIndex];
            const numericValue = parseFloat(selectedOption.getAttribute('data-numeric-value')) || 0;
            subtotal += numericValue;
        });

        // Update the subtotal field
        const subTotalField = document.getElementById('scoresheetSubtotal');
        if (subTotalField) {
            subTotalField.value = subtotal;
        }
    }, 500);
}


function saveChanges(questionId, inputFieldPrefix, saveButtonPrefix, questionType, discardButtonPrefix) {
    const inputField = document.getElementById(inputFieldPrefix + questionId);
    const saveButton = document.getElementById(saveButtonPrefix + questionId);
    const discardButton = document.getElementById(discardButtonPrefix + questionId);
    const assessmentId = $("#AssessmentId").val();
    let answerValue = inputField.value;
    if (questionType == 1 && !answerValue) {
        answerValue = 0;
    }
    unity.grantManager.assessments.assessment.saveScoresheetAnswer(assessmentId, questionId, answerValue, questionType)
        .then(response => {
            abp.notify.success(
                'Answer is successfully saved.',
                'Save Answer'
            );
            inputField.setAttribute('data-original-value', inputField.value);
            saveButton.disabled = true;
            discardButton.disabled = true;
            updateSubtotal();
            PubSub.publish('refresh_review_list_without_select', assessmentId);
        });

}

function discardChanges(questionId, inputFieldPrefix, saveButtonPrefix, discardButtonPrefix) {
    const inputField = document.getElementById(inputFieldPrefix + questionId);
    const saveButton = document.getElementById(saveButtonPrefix + questionId);    
    const discardButton = document.getElementById(discardButtonPrefix + questionId); 

    const originalValue = inputField.getAttribute('data-original-value');
    inputField.value = originalValue;

    saveButton.disabled = true;
    discardButton.disabled = true;

    if (inputFieldPrefix == 'answer-number-' || inputFieldPrefix == 'answer-text-') {
        const errorMessage = document.getElementById('error-message-' + questionId);
        errorMessage.textContent = '';
    } 
}

function expandAllAccordions(divId) {
    const accordions = document.querySelectorAll('#' + divId + ' .accordion-collapse');
    accordions.forEach(accordion => {
        accordion.classList.add('show');
        accordion.previousElementSibling.querySelector('.accordion-button').classList.remove('collapsed');
    });
}

function collapseAllAccordions(divId) {
    const accordions = document.querySelectorAll('#' + divId + ' .accordion-collapse');
    accordions.forEach(accordion => {
        accordion.classList.remove('show');
        accordion.previousElementSibling.querySelector('.accordion-button').classList.add('collapsed');
    });
}
