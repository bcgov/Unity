const assessmentScoresWidgetPanelStates = {};

function getAssessmentScoresScrollContainer(wrapper) {
    return (
        wrapper.closest('.details-scrollable') ||
        document.getElementById('detailsTabContent') ||
        document.documentElement
    );
}

function restoreAssessmentScoresScrollPosition(wrapper, scrollTop) {
    if (!Number.isFinite(scrollTop)) {
        return;
    }

    const container = getAssessmentScoresScrollContainer(wrapper);
    const maxScroll = Math.max(
        0,
        container.scrollHeight - container.clientHeight
    );
    container.scrollTop = Math.min(scrollTop, maxScroll);
}

function getAssessmentScoresWidgetStateKey(wrapper) {
    return wrapper.querySelector('#AssessmentId')?.value;
}

function saveAssessmentScoresWidgetState(wrapper) {
    if (!wrapper) {
        return;
    }

    const key = getAssessmentScoresWidgetStateKey(wrapper);
    if (!key) {
        return;
    }

    const scrollContainer = getAssessmentScoresScrollContainer(wrapper);
    const scrollTop = scrollContainer.scrollTop;
    const expandedCollapseIds = Array.from(
        wrapper.querySelectorAll(
            '#assessment-scoresheet .accordion-collapse.show'
        )
    )
        .map((accordion) => accordion.id)
        .filter(Boolean);

    assessmentScoresWidgetPanelStates[key] = {
        expandedCollapseIds,
        scrollTop,
    };
}

function restoreAssessmentScoresWidgetState(wrapper) {
    const key = getAssessmentScoresWidgetStateKey(wrapper);
    const state = assessmentScoresWidgetPanelStates[key];
    if (!state) {
        restoreAssessmentScoresScrollPosition(wrapper, 0);
        return;
    }

    state.expandedCollapseIds.forEach((id) => {
        const accordion = document.getElementById(id);
        if (!accordion || !wrapper.contains(accordion)) {
            return;
        }

        accordion.classList.add('show');
        const accordionButton = accordion.previousElementSibling?.querySelector(
            '.accordion-button'
        );
        accordionButton?.classList.remove('collapsed');
        accordionButton?.setAttribute('aria-expanded', 'true');
    });

    requestAnimationFrame(() =>
        restoreAssessmentScoresScrollPosition(wrapper, state.scrollTop)
    );
}

globalThis.saveAssessmentScoresWidgetState = saveAssessmentScoresWidgetState;

abp.widgets.AssessmentScoresWidget = function ($wrapper) {
    return {
        init: function () {
            // The widget is re-rendered (and init() re-run) whenever the
            // selected application/review changes, without a full page
            // reload. Section ids come from the scoresheet template and can
            // be shared across assessments, so stale dirty/invalid state
            // from a previously-viewed assessment must not carry over.
            resetScoresheetSectionState();
            restoreAssessmentScoresWidgetState($wrapper[0]);
            updateSubtotal();
            refreshBulkScoresheetActionButtons();
            globalThis.syncAIRateLimitButtons?.();
        },
    };
};

// sectionId -> { isDirty: bool, isValid: bool }
const scoresheetSectionState = {};

function resetScoresheetSectionState() {
    Object.keys(scoresheetSectionState).forEach(
        (id) => delete scoresheetSectionState[id]
    );
}

function getDirtySectionIds() {
    return Object.keys(scoresheetSectionState).filter(
        (id) => scoresheetSectionState[id].isDirty
    );
}

function getScoresheetSectionName(sectionId) {
    const schemaInput = document.getElementById('AssessmentScoresheetSchemaJson');
    if (!schemaInput) return sectionId;

    let schema;
    try {
        schema = JSON.parse(schemaInput.value || '{}');
    } catch {
        return sectionId;
    }

    const sections = schema.sections || schema.Sections || [];
    const section = sections.find((s) => String(s.id ?? s.Id) === sectionId);
    return section ? (section.name ?? section.Name) : sectionId;
}

function updateSectionHeaderStyle(sectionId, isDirty) {
    const headerButton = document.querySelector(
        '#heading-' + sectionId + ' .accordion-button'
    );
    if (headerButton) {
        headerButton.classList.toggle('section-unsaved', isDirty);
    }
}

// Selector is data-question-id based (rather than a #question-heading-{id}
// id lookup) so this same function works unmodified on the Scoresheet
// configuration preview too, which uses a different DOM id scheme for its
// question headers but stamps the same data-question-id attribute.
function updateQuestionHeaderStyle(questionId, isDirty) {
    const headerButton = document.querySelector(
        '.accordion-button[data-question-id="' + questionId + '"]'
    );
    if (headerButton) {
        headerButton.classList.toggle('question-unsaved', isDirty);
    }
}

function refreshBulkScoresheetActionButtons() {
    const states = Object.values(scoresheetSectionState);
    const anyDirty = states.some((s) => s.isDirty);
    const anyInvalidDirty = states.some((s) => s.isDirty && !s.isValid);

    const saveAllBtn = document.getElementById('scoresheetSaveAllBtn');
    const discardAllBtn = document.getElementById('scoresheetDiscardAllBtn');
    if (discardAllBtn) discardAllBtn.disabled = !anyDirty;
    if (saveAllBtn) saveAllBtn.disabled = !anyDirty || anyInvalidDirty;
}

function saveAllScoresheetSections() {
    const dirtySectionIds = getDirtySectionIds();
    if (dirtySectionIds.length === 0) return;

    const sectionNames = dirtySectionIds.map(getScoresheetSectionName);

    Swal.fire({
        title: 'Are you sure you want to save the changes made to the following section(s)?',
        html:
            '<ul class="text-start">' +
            sectionNames
                .map((n) => `<li>${$('<div>').text(n).html()}</li>`)
                .join('') +
            '</ul>',
        showCancelButton: true,
        confirmButtonText: 'Save Changes',
        cancelButtonText: 'Cancel',
        customClass: {
            confirmButton: 'btn btn-primary',
            cancelButton: 'btn btn-secondary',
        },
    }).then((result) => {
        if (!result.isConfirmed) return;

        const assessmentId = $('#AssessmentId').val();
        document.getElementById('scoresheetSaveAllBtn').disabled = true;
        document.getElementById('scoresheetDiscardAllBtn').disabled = true;

        const combinedAnswers = [];
        const combinedInputFieldArr = [];
        dirtySectionIds.forEach((sectionId) => {
            const answersArr = [];
            const inputFieldArr = [];
            const origAnswersArr = [];
            $.each(
                $(`#section-form-${sectionId}`).serializeArray(),
                function (_, inputData) {
                    buildFormData(
                        answersArr,
                        inputData,
                        inputFieldArr,
                        origAnswersArr
                    );
                }
            );
            combinedAnswers.push(
                ...answersArr.map(({ questionId, questionType, answer }) => ({
                    questionId,
                    questionType,
                    answer,
                }))
            );
            combinedInputFieldArr.push(...inputFieldArr);
        });

        unity.grantManager.assessments.assessment
            .saveScoresheetSectionAnswers({
                AssessmentId: assessmentId,
                AssessmentAnswers: combinedAnswers,
            })
            .done(function () {
                abp.notify.success(
                    'The answers have been saved successfully.',
                    'Save Answers'
                );

                combinedInputFieldArr.forEach((fieldId) => {
                    const el = document.getElementById(fieldId);
                    if (el) {
                        el.dataset.originalValue = el.value;
                        el.dataset.originalIsHumanConfirmed =
                            el.dataset.isHumanConfirmed;
                    }
                    const questionId = fieldId.split('-').slice(2).join('-');
                    updateQuestionHeaderStyle(questionId, false);
                });

                dirtySectionIds.forEach((sectionId) => {
                    scoresheetSectionState[sectionId] = {
                        isDirty: false,
                        isValid: true,
                    };
                    updateSectionHeaderStyle(sectionId, false);
                });

                updateSubtotal();
                PubSub.publish(
                    'refresh_review_list_without_sidepanel',
                    assessmentId
                );
                refreshBulkScoresheetActionButtons();
            })
            .fail(function () {
                refreshBulkScoresheetActionButtons();
            });
    });
}

function markAsHumanConfirmed(inputElement) {
    console.log('markAsHumanConfirmed inputElement', inputElement);
    // Check if this was an AI-generated answer
    const isHumanConfirmed =
        inputElement.dataset.isHumanConfirmed === 'true';

    if (!isHumanConfirmed) {
        // Mark as human confirmed
        inputElement.dataset.isHumanConfirmed = 'true';

        // Update styling from AI-generated to human-confirmed
        inputElement.classList.remove('ai-generated-answer');
        inputElement.classList.add('human-confirmed-answer');

        // Hide (not remove) the AI indicator and citation so they can be
        // restored later if the user discards an unsaved edit.
        const aiIndicator = inputElement.parentElement.querySelector(
            '.ai-answer-indicator'
        );
        if (aiIndicator) {
            aiIndicator.classList.add('d-none');
        }
        const aiCitation = inputElement.parentElement.querySelector(
            '.ai-citation'
        );
        if (aiCitation) {
            aiCitation.classList.add('d-none');
        }

        // Hide the low-confidence badge from the question header (accordion button)
        const questionAccordion = inputElement.closest('.accordion-item');
        if (questionAccordion) {
            const lowConfidenceBadge = questionAccordion.querySelector(
                '.low-confidence-badge'
            );
            if (lowConfidenceBadge) {
                lowConfidenceBadge.classList.add('d-none');
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

function restoreAiIndicators(inputElement) {
    inputElement.dataset.isHumanConfirmed = 'false';
    inputElement.classList.remove('human-confirmed-answer');
    inputElement.classList.add('ai-generated-answer');

    const aiIndicator = inputElement.parentElement.querySelector(
        '.ai-answer-indicator'
    );
    if (aiIndicator) {
        aiIndicator.classList.remove('d-none');
    }
    const aiCitation = inputElement.parentElement.querySelector(
        '.ai-citation'
    );
    if (aiCitation) {
        aiCitation.classList.remove('d-none');
    }

    const questionAccordion = inputElement.closest('.accordion-item');
    if (questionAccordion) {
        const lowConfidenceBadge = questionAccordion.querySelector(
            '.low-confidence-badge'
        );
        if (lowConfidenceBadge) {
            // Its continued presence in the DOM (hidden, not removed) is itself
            // the signal that this question was originally low-confidence.
            lowConfidenceBadge.classList.remove('d-none');
            questionAccordion.classList.add('low-confidence-question');
        }
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

function discardAllScoresheetSections() {
    const dirtySectionIds = getDirtySectionIds();
    if (dirtySectionIds.length === 0) return;

    const sectionNames = dirtySectionIds.map(getScoresheetSectionName);

    Swal.fire({
        title: 'You have unsaved changes in the following section(s):',
        html:
            '<ul class="text-start">' +
            sectionNames
                .map((n) => `<li>${$('<div>').text(n).html()}</li>`)
                .join('') +
            '</ul><p class="mt-3">Discarding changes will permanently remove all unsaved updates. This action cannot be undone.</p>',
        showCancelButton: true,
        confirmButtonText: 'Discard Changes',
        cancelButtonText: 'Cancel',
        customClass: {
            confirmButton: 'btn btn-danger',
            cancelButton: 'btn btn-secondary',
        },
    }).then((result) => {
        if (!result.isConfirmed) return;

        dirtySectionIds.forEach((sectionId) => {
            const answersArr = [];
            const inputFieldArr = [];
            const origAnswersArr = [];
            $.each(
                $(`#section-form-${sectionId}`).serializeArray(),
                function (_, inputData) {
                    buildFormData(
                        answersArr,
                        inputData,
                        inputFieldArr,
                        origAnswersArr
                    );
                }
            );

            inputFieldArr.forEach((fieldId) => {
                const questionId = fieldId.split('-').slice(2).join('-');
                const el = document.getElementById(fieldId);
                el.value = el.dataset.originalValue;

                const errorMessage = document.getElementById(
                    'error-message-' + questionId
                );
                if (errorMessage) {
                    errorMessage.textContent = '';
                }

                // Only restore AI styling if the checkpoint itself was never
                // human-confirmed - i.e. this edit was never saved.
                if (
                    el.dataset.originalIsHumanConfirmed === 'false' &&
                    el.dataset.isHumanConfirmed === 'true'
                ) {
                    restoreAiIndicators(el);
                }

                updateQuestionHeaderStyle(questionId, false);
            });

            scoresheetSectionState[sectionId] = {
                isDirty: false,
                isValid: true,
            };
            updateSectionHeaderStyle(sectionId, false);
        });

        refreshBulkScoresheetActionButtons();
    });
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
        Number.parseInt(financialAnalysis) +
        Number.parseInt(inclusiveGrowth) +
        Number.parseInt(cleanGrowth) +
        Number.parseInt(economicImpact);
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

function validateRequiredSelectField(selectField, errorMessage) {
    if (selectField.validity.valueMissing) {
        errorMessage.textContent = 'This field is required.';
        return false;
    }
    errorMessage.textContent = '';
    return true;
}

function handleInputChange(questionId, inputFieldPrefix) {
    const sectionFormId = $(`#${inputFieldPrefix + questionId}`)
        .closest('form')
        .attr('id');
    let sectionId =
        sectionFormId !== null
            ? sectionFormId?.split('-').slice(2).join('-')
            : null;

    if (!sectionId) {
        return;
    }

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
        const qId = assessmentAnswersArr[x].questionId;
        const errorMessage = document.getElementById(
            'error-message-' + qId
        );

        if (assessmentAnswersArr[x].questionType === 1) {
            let inputNumberField = document.getElementById(
                'answer-number-' + qId
            );
            assessmentAnswersArr[x].isValid = validateNumericField(
                inputNumberField,
                errorMessage
            );
        } else if (assessmentAnswersArr[x].questionType === 2) {
            let inputTextField = document.getElementById(
                'answer-text-' + qId
            );

            if (inputTextField.required) {
                assessmentAnswersArr[x].isValid = validateTextField(
                    inputTextField,
                    errorMessage
                );
            }
        } else if (assessmentAnswersArr[x].questionType === 14) {
            let inputTextAreaField = document.getElementById(
                'answer-textarea-' + qId
            );

            if (inputTextAreaField.required) {
                assessmentAnswersArr[x].isValid = validateTextField(
                    inputTextAreaField,
                    errorMessage
                );
            }
        } else if (assessmentAnswersArr[x].questionType === 6) {
            let inputYesNoField = document.getElementById(
                'answer-yesno-' + qId
            );

            if (inputYesNoField.required) {
                assessmentAnswersArr[x].isValid = validateRequiredSelectField(
                    inputYesNoField,
                    errorMessage
                );
            }
        } else if (assessmentAnswersArr[x].questionType === 12) {
            let inputSelectListField = document.getElementById(
                'answer-selectlist-' + qId
            );

            if (inputSelectListField.required) {
                assessmentAnswersArr[x].isValid = validateRequiredSelectField(
                    inputSelectListField,
                    errorMessage
                );
            }
        }
        assessmentAnswersArr[x].isSame = compareObj(
            assessmentAnswersArr[x],
            origAnswersArr[x]
        );
        updateQuestionHeaderStyle(qId, !assessmentAnswersArr[x].isSame);
    }

    //Handle section dirty/valid state
    let isNotSame = assessmentAnswersArr.some((item) => item.isSame === false);
    let isInValid = assessmentAnswersArr.some((item) => item.isValid === false);

    scoresheetSectionState[sectionId] = {
        isDirty: isNotSame,
        isValid: !isInValid,
    };
    updateSectionHeaderStyle(sectionId, isNotSame);
    refreshBulkScoresheetActionButtons();

    // Optional extension point: the Scoresheet configuration preview
    // (Scoresheet.js) shares this function via the same script bundle and
    // registers this hook to drive its own (separately-tracked) bulk
    // Save All/Discard All state. No-op on the real AssessmentScoresWidget.
    globalThis.onScoresheetSectionValidated?.(sectionId, isNotSame, isInValid);
}

function validateTextField(textInputField, errorMessage) {
    if (
        textInputField.validity.valueMissing
    ) {
        errorMessage.textContent = 'This field is required.';
        return false;
    } else if (textInputField.validity.tooShort) {
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
    if (numericInputField.validity.valueMissing) {
        errorMessage.textContent = 'This field is required.';
        return false;
    } else if (numericInputField.validity.rangeOverflow) {
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
            subtotal += Number.parseFloat(input.value) || 0;
        });

        // Handle Yes/No inputs
        const yesNoInputs = document.querySelectorAll('.answer-yesno-input');
        yesNoInputs.forEach((input) => {
            let value = 0;
            if (input.value === 'Yes') {
                value =
                    Number.parseFloat(input.dataset.yesNumericValue) ||
                    0;
            } else if (input.value === 'No') {
                value =
                    Number.parseFloat(input.dataset.noNumericValue) ||
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
                Number.parseFloat(selectedOption.dataset.numericValue) ||
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

    const originalValue = inputField.dataset.originalValue;
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

function queueApplicationScoring(triggerButton = null) {
    const applicationId = $('#DetailsViewApplicationId').val();
    const $button = triggerButton ? $(triggerButton) : $('#regenerateAiScoresheetBtn');
    const existingHtml = $button.html();

    if (!applicationId || $button.prop('disabled')) {
        return;
    }

    globalThis.AIGenerationButtonState?.setGenerating($button);

    const monitorScoring = () => globalThis.AIGenerationButtonState.monitor({
        $button,
        originalHtml: existingHtml,
        getStatus: () => globalThis.AIGenerationApi.getStatus(applicationId, 'application-scoring'),
        onComplete: () => {
            PubSub.publish('refresh_assessment_scores', null);
        },
        onFailed: (request) => abp.message.error(request?.failureReason || 'AI scoring failed.')
    });

    globalThis.AIGenerationApi.queueApplicationScoring(applicationId)
        .done(function (generationStatus) {
            const request = generationStatus?.generationRequest;
            const status = String(request?.status ?? '').trim();

            if (status === 'Completed') {
                globalThis.AIGenerationButtonState?.restoreForCooldownCheck($button, existingHtml);
                globalThis.AIGenerationButtonState?.applyStatusState(generationStatus);
                PubSub.publish('refresh_assessment_scores', null);
                return;
            }
            monitorScoring();
        })
        .fail(function () {
            abp.message.error(
                'Failed to queue AI scoring. Please try again.'
            );
            globalThis.AIGenerationButtonState?.restore($button);
            $button.html(existingHtml).prop('disabled', false);
            globalThis.syncAIRateLimitButtons?.();
        });
}

$(function () {
    // Static buttons
    $(document).on('click', '#regenerateAiScoresheetBtn', function () {
        queueApplicationScoring();
    });
    $(document).on('click', '#btn-expand-all', function () {
        expandAllAccordions('assessment-scoresheet');
    });
    $(document).on('click', '#btn-collapse-all', function () {
        collapseAllAccordions('assessment-scoresheet');
    });
    $(document).on('click', '#saveAssessmentScoresBtn', function () {
        saveAssessmentScores();
    });

    // Save All / Discard All (assessment-wide)
    $(document).on('click', '#scoresheetSaveAllBtn', function () {
        saveAllScoresheetSections();
    });
    $(document).on('click', '#scoresheetDiscardAllBtn', function () {
        discardAllScoresheetSections();
    });
});
