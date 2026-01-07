function saveScoresSection(formId, sectionId) {
    const assessmentId = $('#AssessmentId').val();
    const secSaveButton = document.getElementById(
        'scoresheet-section-save-' + sectionId
    );
    const secDiscardButton = document.getElementById(
        'scoresheet-section-discard-' + sectionId
    );

    const assessmentAnswersArr = [];
    const inputFieldArr = [];
    const origAnswersArr = [];
    const formData = $(`#${formId}`).serializeArray();

    //Handle form object data
    $.each(formData, function (_, inputData) {
        buildFormData(
            assessmentAnswersArr,
            inputData,
            inputFieldArr,
            origAnswersArr
        );
    });

    const data = {
        AssessmentId: assessmentId,
        AssessmentAnswers: assessmentAnswersArr.map(
            ({ questionId, questionType, answer }) => ({
                questionId,
                questionType,
                answer,
            })
        ),
    };

    //Calls an enpoint and disabled buttons
    unity.grantManager.assessments.assessment
        .saveScoresheetSectionAnswers(data)
        .done(function () {
            abp.notify.success(
                'The answers have been saved successfully.',
                'Save Answers'
            );

            if (inputFieldArr.length > 0) {
                for (let item of inputFieldArr) {
                    const inputField = document.getElementById(item);
                    inputField.setAttribute(
                        'data-original-value',
                        inputField.value
                    );
                }
            }

            secSaveButton.disabled = true;
            secDiscardButton.disabled = true;

            updateSubtotal();
            PubSub.publish(
                'refresh_review_list_without_sidepanel',
                assessmentId
            );
        });
}

function markAsHumanConfirmed(inputElement) {
    console.log('markAsHumanConfirmed inputElement', inputElement);
    // Check if this was an AI-generated answer
    const isHumanConfirmed =
        inputElement.getAttribute('data-is-human-confirmed') === 'true';

    if (!isHumanConfirmed) {
        // Mark as human confirmed
        inputElement.setAttribute('data-is-human-confirmed', 'true');

        // Update styling from AI-generated to human-confirmed
        inputElement.classList.remove('ai-generated-answer');
        inputElement.classList.add('human-confirmed-answer');

        // Remove AI indicator if it exists
        const aiIndicator = inputElement.parentElement.querySelector(
            '.ai-answer-indicator'
        );

        if (aiIndicator) {
            aiIndicator.remove();
        }

        // Remove low-confidence-badge from the question header (accordion button)
        const questionAccordion = inputElement.closest('.accordion-item');
        if (questionAccordion) {
            const lowConfidenceBadge = questionAccordion.querySelector(
                '.low-confidence-badge'
            );
            if (lowConfidenceBadge) {
                lowConfidenceBadge.remove();
            }

            // Also remove the low-confidence-question class from the accordion item
            questionAccordion.classList.remove('low-confidence-question');
        }

        // Log the change for potential tracking
        console.log(
            'Answer marked as human-confirmed for element:',
            inputElement.id
        );
    }
}

// Utility function to help debug AI answer integration
function debugAIAnswers() {
    const aiAnswers = document.querySelectorAll(
        '[data-is-human-confirmed="false"]'
    );
    const humanAnswers = document.querySelectorAll(
        '[data-is-human-confirmed="true"]'
    );

    console.log('=== AI Answer Integration Debug ===');
    console.log(`Found ${aiAnswers.length} AI-generated answers`);
    console.log(`Found ${humanAnswers.length} human-confirmed answers`);

    // Focus on select lists specifically
    const aiSelectLists = Array.from(aiAnswers).filter(
        (el) => el.tagName === 'SELECT'
    );
    const brokenSelectLists = aiSelectLists.filter(
        (el) => el.value === '' || el.value === null
    );

    console.log(
        `AI Select Lists: ${aiSelectLists.length} total, ${brokenSelectLists.length} broken`
    );

    brokenSelectLists.forEach((select) => {
        console.log('Broken Select List:', {
            id: select.id,
            value: select.value,
            selectedIndex: select.selectedIndex,
            optionCount: select.options.length,
            options: Array.from(select.options).map((opt) => ({
                value: opt.value,
                text: opt.text,
            })),
        });
    });

    aiAnswers.forEach((element) => {
        console.log('AI Answer:', {
            id: element.id,
            tagName: element.tagName,
            value: element.value,
            hasAiClass: element.classList.contains('ai-generated-answer'),
            hasIndicator: !!element.parentElement.querySelector(
                '.ai-answer-indicator'
            ),
        });
    });

    return {
        aiCount: aiAnswers.length,
        humanCount: humanAnswers.length,
        brokenSelectCount: brokenSelectLists.length,
        aiAnswers: Array.from(aiAnswers).map((el) => ({
            id: el.id,
            tagName: el.tagName,
            value: el.value,
        })),
        humanAnswers: Array.from(humanAnswers).map((el) => ({
            id: el.id,
            tagName: el.tagName,
            value: el.value,
        })),
    };
}
function discardChangesScoresSection(formId, sectionId) {
    const secSaveButton = document.getElementById(
        'scoresheet-section-save-' + sectionId
    );
    const secDiscardButton = document.getElementById(
        'scoresheet-section-discard-' + sectionId
    );

    const assessmentAnswersArr = [];
    const inputFieldArr = [];
    const origAnswersArr = [];
    const formData = $(`#${formId}`).serializeArray();

    $.each(formData, function (_, inputData) {
        buildFormData(
            assessmentAnswersArr,
            inputData,
            inputFieldArr,
            origAnswersArr
        );
    });

    //Handle dynamic data to bring back original values
    if (inputFieldArr.length > 0) {
        for (let item of inputFieldArr) {
            let questionId = item.split('-').slice(2).join('-');
            const inputField = document.getElementById(item);
            const originalValue = inputField.getAttribute(
                'data-original-value'
            );
            inputField.value = originalValue;

            if (
                item.includes('answer-number-') ||
                item.includes('answer-text-')
            ) {
                const errorMessage = document.getElementById(
                    'error-message-' + questionId
                );
                errorMessage.textContent = '';
            }
        }
    }

    secSaveButton.disabled = true;
    secDiscardButton.disabled = true;
}

function buildFormData(
    assessmentAnswersArr,
    inputData,
    inputFieldArr,
    origAnswersArr
) {
    const questionTypes = {
        Number: 1,
        Text: 2,
        YesNo: 6,
        SelectList: 12,
        Textarea: 14,
    };
    const n = 2;
    const formAnsObj = {};
    const origAnsObj = {};
    const inputName = inputData.name.split('-');

    if (formAnsObj[inputData.name.split('-')[0]] == '') {
        formAnsObj['answer'] = null;
    }

    if (inputName[0] === 'Answer') {
        let answerValue = inputData.value;
        let inputFieldValue = inputName.slice(0, n).join('-');
        let questionIdValue = inputName.slice(n).join('-');
        const questionTypeValue =
            questionTypes[inputName.slice(1, n).join('-')];

        if (questionTypeValue === 1 && !answerValue) {
            answerValue = 0;
        }

        let tempInputField = `${inputFieldValue.toLowerCase()}-${questionIdValue}`;

        origAnsObj['questionId'] = inputName.slice(n).join('-');
        origAnsObj['questionType'] = questionTypeValue;
        origAnsObj['answer'] = $(`#${tempInputField}`).attr(
            'data-original-value'
        );
        origAnsObj['isValid'] = true;
        origAnsObj['isSame'] = true;

        formAnsObj['questionId'] = inputName.slice(n).join('-');
        formAnsObj['questionType'] = questionTypeValue;
        formAnsObj['answer'] = answerValue;
        formAnsObj['isValid'] = true;
        formAnsObj['isSame'] = true;

        inputFieldArr.push(tempInputField);
        origAnswersArr.push(origAnsObj);
        assessmentAnswersArr.push(formAnsObj);
    }
}

function saveAssessmentScores() {
    try {
        let data = {
            financialAnalysis: parseScoreValueInput('financialAnalysis'),
            economicImpact: parseScoreValueInput('economicImpact'),
            inclusiveGrowth: parseScoreValueInput('inclusiveGrowth'),
            cleanGrowth: parseScoreValueInput('cleanGrowth'),
            assessmentId: $('#AssessmentId').val(),
        };
        unity.grantManager.assessments.assessment
            .updateAssessmentScore(data)
            .done(function () {
                abp.notify.success('Assessment scores has been updated.');
                PubSub.publish('refresh_assessment_scores', null);
                PubSub.publish(
                    'refresh_review_list_without_sidepanel',
                    $('#AssessmentId').val()
                );
            });
    } catch (error) {
        console.log(error);
    }
}

function parseScoreValueInput(name) {
    let control = '#' + name;
    return $(control).val() == ''
        ? 0
        : Math.min($(control).attr('max'), $(control).val());
}

function enableSaveButton(inputText) {
    if (inputText.value.trim() != '') {
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
    let sum =
        parseInt(financialAnalysis) +
        parseInt(inclusiveGrowth) +
        parseInt(cleanGrowth) +
        parseInt(economicImpact);
    $('#subTotal').val(sum);
}

function positiveIntegersOnly(e) {
    if (
        e.keyCode === 9 ||
        e.keyCode === 8 ||
        e.keyCode === 37 ||
        e.keyCode === 39
    ) {
        return true;
    }
    if (e.target?.value?.length >= 2) {
        return false;
    }
    if (
        !(
            (e.keyCode > 95 && e.keyCode < 106) ||
            (e.keyCode > 47 && e.keyCode < 58) ||
            e.keyCode == 8
        )
    ) {
        return false;
    }
}

function compareObj(objA, objB) {
    let res = true;
    Object.keys(objB).forEach((key) => {
        if (!objA.hasOwnProperty(key) || objA[key] !== objB[key]) {
            res = false;
        }
    });
    return res;
}

function handleInputChange(questionId, inputFieldPrefix) {
    const sectionFormId = $(`#${inputFieldPrefix + questionId}`)
        .closest('form')
        .attr('id');
    let sectionId =
        sectionFormId !== null
            ? sectionFormId?.split('-').slice(2).join('-')
            : null;
    const secSaveButton = document.getElementById(
        'scoresheet-section-save-' + sectionId
    );
    const secDiscardButton = document.getElementById(
        'scoresheet-section-discard-' + sectionId
    );

    const assessmentAnswersArr = [];
    const inputFieldArr = [];
    const origAnswersArr = [];
    const formData = $(`#${sectionFormId}`).serializeArray();

    $.each(formData, function (_, inputData) {
        buildFormData(
            assessmentAnswersArr,
            inputData,
            inputFieldArr,
            origAnswersArr
        );
    });

    //Handle values and objects comparison
    for (let x = 0; x < assessmentAnswersArr.length; x++) {
        if (assessmentAnswersArr[x].questionType === 1) {
            let inputNumberField = document.getElementById(
                'answer-number-' + assessmentAnswersArr[x].questionId
            );
            let numberErrorMessage = document.getElementById(
                'error-message-' + assessmentAnswersArr[x].questionId
            );
            assessmentAnswersArr[x].isValid = validateNumericField(
                inputNumberField,
                numberErrorMessage
            );
        } else if (assessmentAnswersArr[x].questionType === 2) {
            let inputTextField = document.getElementById(
                'answer-text-' + assessmentAnswersArr[x].questionId
            );
            let textErrorMessage = document.getElementById(
                'error-message-' + assessmentAnswersArr[x].questionId
            );

            if (inputTextField.required) {
                assessmentAnswersArr[x].isValid = validateTextField(
                    inputTextField,
                    textErrorMessage
                );
            }
        }
        assessmentAnswersArr[x].isSame = compareObj(
            assessmentAnswersArr[x],
            origAnswersArr[x]
        );
    }

    //Handle button events
    let isNotSame = assessmentAnswersArr.some((item) => item.isSame === false);
    let isInValid = assessmentAnswersArr.some((item) => item.isValid === false);

    if (isNotSame && isInValid) {
        secSaveButton.disabled = true;
        secDiscardButton.disabled = false;
    }

    if (isNotSame && !isInValid) {
        secSaveButton.disabled = false;
        secDiscardButton.disabled = false;
    } else {
        secSaveButton.disabled = true;
    }
}

function validateTextField(textInputField, errorMessage) {
    if (
        textInputField.validity.tooShort ||
        textInputField.validity.valueMissing
    ) {
        errorMessage.textContent =
            'The answer is too short. Minimum length is ' +
            textInputField.minLength +
            ' characters.';
        return false;
    } else if (textInputField.validity.tooLong) {
        errorMessage.textContent =
            'The answer is too long. Maximum length is ' +
            textInputField.maxLength +
            ' characters.';
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
        numberInputs.forEach((input) => {
            subtotal += parseFloat(input.value) || 0;
        });

        // Handle Yes/No inputs
        const yesNoInputs = document.querySelectorAll('.answer-yesno-input');
        yesNoInputs.forEach((input) => {
            let value = 0;
            if (input.value === 'Yes') {
                value =
                    parseFloat(input.getAttribute('data-yes-numeric-value')) ||
                    0;
            } else if (input.value === 'No') {
                value =
                    parseFloat(input.getAttribute('data-no-numeric-value')) ||
                    0;
            }
            subtotal += value;
        });

        // Handle select list inputs
        const selectListInputs = document.querySelectorAll(
            '.answer-selectlist-input'
        );
        selectListInputs.forEach((select) => {
            const selectedOption = select.options[select.selectedIndex];
            const numericValue =
                parseFloat(selectedOption.getAttribute('data-numeric-value')) ||
                0;
            subtotal += numericValue;
        });

        // Update the subtotal field
        const subTotalField = document.getElementById('scoresheetSubtotal');
        if (subTotalField) {
            subTotalField.value = subtotal;
        }
    }, 500);
}

function discardChanges(
    questionId,
    inputFieldPrefix,
    saveButtonPrefix,
    discardButtonPrefix
) {
    const inputField = document.getElementById(inputFieldPrefix + questionId);
    const saveButton = document.getElementById(saveButtonPrefix + questionId);
    const discardButton = document.getElementById(
        discardButtonPrefix + questionId
    );

    const originalValue = inputField.getAttribute('data-original-value');
    inputField.value = originalValue;

    saveButton.disabled = true;
    discardButton.disabled = true;

    if (
        inputFieldPrefix == 'answer-number-' ||
        inputFieldPrefix == 'answer-text-'
    ) {
        const errorMessage = document.getElementById(
            'error-message-' + questionId
        );
        errorMessage.textContent = '';
    }
}

function expandAllAccordions(divId) {
    const accordions = document.querySelectorAll(
        '#' + divId + ' .accordion-collapse'
    );
    accordions.forEach((accordion) => {
        accordion.classList.add('show');
        accordion.previousElementSibling
            .querySelector('.accordion-button')
            .classList.remove('collapsed');
    });
}

function collapseAllAccordions(divId) {
    const accordions = document.querySelectorAll(
        '#' + divId + ' .accordion-collapse'
    );
    accordions.forEach((accordion) => {
        accordion.classList.remove('show');
        accordion.previousElementSibling
            .querySelector('.accordion-button')
            .classList.add('collapsed');
    });
}
