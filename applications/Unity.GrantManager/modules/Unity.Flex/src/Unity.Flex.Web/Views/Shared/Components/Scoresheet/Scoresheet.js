$(function () {

    function makeScoresheetsSortable() {
        document.querySelectorAll('[id^="sections-questions"]').forEach(function (div) {

            _ = new Sortable(div, {
                animation: 150,
                onEnd: function (evt) {
                    const sortedScoresheetId = evt.target.dataset.scoresheetid;
                    saveOrder(sortedScoresheetId);
                    updatePreview(evt);
                },
                ghostClass: 'blue-background',
                onMove: function (evt) {
                    const draggedItem = evt.dragged;
                    const targetItem = evt.related;
                    const topItem = evt.from.children[0];
                    const secondItem = evt.from.children[1];

                    const isDraggedSection = draggedItem.classList.contains('section-item');
                    const isTopItem = draggedItem === topItem;
                    const isSecondItemSection = secondItem?.classList.contains('section-item');

                    if (isTopItem && !isSecondItemSection) {
                        return false;
                    }

                    if (isDraggedSection) {
                        return true;
                    }

                    const isTargetTop = targetItem === evt.from.children[0];

                    return !(!isDraggedSection && isTargetTop && !evt.willInsertAfter);
                }
            });
        });


        _ = new Sortable(document.getElementById('scoresheet-accordion'), {
            handle: '.draggable-header',
            animation: 150,
            ghostClass: 'blue-background',
            onEnd: function (evt) {
                let itemEl = evt.item;
                itemEl.style.border = "";
                updateScoresheetOrder();
            },
            onStart: function (evt) {
                let itemEl = evt.item;
                itemEl.style.border = "2px solid lightblue";
            },
        });

    }

    function updateScoresheetOrder() {
        let order = [];
        $("#scoresheet-accordion .accordion-item").each(function (index, element) {
            let scoresheetId = $(element).find(".accordion-header").attr("id").replace("heading-", "");
            order.push(scoresheetId);
        });
        unity.flex.scoresheets.scoresheet.saveScoresheetOrder(order)
            .then(response => {
                abp.notify.success(
                    'Scoresheet ordering is successfully saved.',
                    'Scoresheet'
                );
            });
    }

    function updatePreview(event) {
        const sortedItems = Array.from(event.target.children);
        updatePreviewAccordion(sortedItems);
    }

    function updateUnsortedPreview() {
        const expandedAccordionBodies = document.querySelectorAll('#scoresheet-accordion .accordion-collapse.show');
        const sortedItems = [];
        expandedAccordionBodies.forEach(body => {
            const items = body.querySelectorAll('.list-group-item');
            items.forEach(item => {
                sortedItems.push(item);
            });
        });

        updatePreviewAccordion(sortedItems);
    }

    function attachAccordionToggleListeners() {
        const accordionItems = document.querySelectorAll('#scoresheet-accordion .accordion-button');
        accordionItems.forEach(button => {
            button.addEventListener('click', function () {
                setTimeout(updateUnsortedPreview, 500);
            });
        });
    }

    function updatePreviewAccordion(sortedItems) {
        const previewDiv = document.getElementById('preview');

        if (sortedItems.length === 0) {
            previewDiv.innerHTML = '<p>No sections to display.</p>';
            return;
        }

        const previewBuilders = {
            "Text": buildTextFieldPreview,
            "YesNo": buildYesNoFieldPreview,
            "Number": buildNumberFieldPreview,
            "SelectList": buildSelectListFieldPreview,
            "TextArea": buildTextAreaFieldPreview
        };

        let accordionHTML = '';
        let currentSectionItem = null;
        let sectionNumber = 1;
        let questionNumber = 1;
        let parentAccordionId = `accordion-preview`;

        sortedItems.forEach(item => {
            if (item.classList.contains('section-item')) {
                if (currentSectionItem) {
                    accordionHTML += `</div></div>
                                        <div class="btn-group mx-3 py-2">
                                            <button type="button" class="btn unt-btn-primary btn-primary mx-1 mb-2" disabled
                                                form="section-form-${hashCode(item.innerText) - 1}" 
                                                id="scoresheet-section-save-${hashCode(item.innerText) - 1}" 
                                                onclick="savePreviewSectionChanges('section-form-${hashCode(item.innerText) - 1}', '${hashCode(item.innerText) - 1}')">Save Changes</button>
                                            <button type="button" class="btn unt-btn-link btn-link mx-2 mb-2" disabled
                                                form="section-form-${hashCode(item.innerText) - 1}" 
                                                id="scoresheet-section-discard-${hashCode(item.innerText) - 1}" 
                                                onclick="discardChangesScoresSection('section-form-${hashCode(item.innerText) - 1}', '${hashCode(item.innerText) - 1}')">Discard Changes</button>
                                        </div>
                                    </div>
                                    </form>
                                </div>`;
                    sectionNumber++;
                }
                parentAccordionId = `nested-accordion-${hashCode(item.innerText)}`;
                accordionHTML += `
            <div class="accordion-item unit-accordion-item my-2">
                <form method="post" id="section-form-${hashCode(item.innerText)}" onSubmit="return false;">
                <h2 class="accordion-header" id="panel-${hashCode(item.innerText)}">
                    <button class="accordion-button preview-btn unt-accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#collapse-${hashCode(item.innerText)}" aria-expanded="true" aria-controls="collapse-${hashCode(item.innerText)}">
                        ${sectionNumber}.  ${item.dataset.label}
                    </button>
                </h2>
                <div id="collapse-${hashCode(item.innerText)}" class="accordion-collapse collapse show" aria-labelledby="panel-${hashCode(item.innerText)}">
                    <div class="accordion-body">
                        <div class="accordion" id="${parentAccordionId}">`; // Start a new nested accordion

                currentSectionItem = item;
                questionNumber = 1;
            } else {
                accordionHTML += `
                    <div class="accordion-item">
                        <h2 class="accordion-header" id="nested-panel${hashCode(item.innerText)}">
                            <button class="accordion-button question-btn collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#nested-collapse${hashCode(item.innerText)}" aria-expanded="true" aria-controls="nested-collapse${hashCode(item.innerText)}">
                                ${sectionNumber}.${questionNumber}  ${item.innerText} ${item.dataset.required == 'True' ? '*' : ''}
                            </button>
                        </h2>
                        <div id="nested-collapse${hashCode(item.innerText)}" class="accordion-collapse collapse" aria-labelledby="nested-panel${hashCode(item.innerText)}">
                            <div class="accordion-body">
                                ${buildFieldPreview(previewBuilders[item.dataset.questiontype], item)}
                            </div>
                        </div>
                    </div>`;
                questionNumber++;
            }
        });

        if (currentSectionItem) {
            accordionHTML += `</div></div>
                                     <div class="btn-group mx-3 py-2">
                                        <button type="button" class="btn unt-btn-primary btn-primary mx-1 mb-2" disabled
                                            form="section-form-${hashCode(currentSectionItem.innerText)}" 
                                            id="scoresheet-section-save-${hashCode(currentSectionItem.innerText)}" 
                                            onclick="savePreviewSectionChanges('section-form-${hashCode(currentSectionItem.innerText)}', '${hashCode(currentSectionItem.innerText)}')">Save Changes</button>
                                        <button type="button" class="btn unt-btn-link btn-link mx-2 mb-2" disabled
                                            form="section-form-${hashCode(currentSectionItem.innerText)}" 
                                            id="scoresheet-section-discard-${hashCode(currentSectionItem.innerText)}"
                                            onclick="discardChangesScoresSection('section-form-${hashCode(currentSectionItem.innerText)}', '${hashCode(currentSectionItem.innerText)}')">Discard Changes</button>
                                    </div>
                                </div>
                                </form>
                            </div>`;
        }

        previewDiv.innerHTML = `
            <div class="accordion unt-accordion" id="accordion-preview">
                <div class="d-flex justify-content-end m-3">
                    <button type="button" class="btn unt-btn-outline-primary btn-outline-primary me-2" onclick="expandAllAccordions('accordion-preview')"><i class="unt-icon-sm fa-solid fa-angles-down"></i>Expand All</button>
                    <button type="button" class="btn unt-btn-outline-primary btn-outline-primary" onclick="collapseAllAccordions('accordion-preview')"><i class="unt-icon-sm fa-solid fa-angles-up"></i>Collapse All</button>
                </div>
                <div>
                ${accordionHTML}
                </div>
            </div>
            <div class="p-4" style="margin-top:2px">
                <label class="form-label" for="scoresheetSubtotal">Subtotal</label>
                <input type="number" size="18" value="0" class="form-control" disabled="disabled" name="ScoresheetSubtotal" id="scoresheetSubtotal" min="0" max="2147483647" />
            </div>
        `;

        updateSubtotal();
    }

    function buildFieldPreview(builder, item) {
        return builder ? builder(item) : null;
    }

    function buildTextAreaFieldPreview(item) {
        let req = item.dataset.required ? "required" : null;
        return `
                    <p>${item.dataset.questiondesc}</p>
                    <div class="mb-3">
                        <label for="answer-text-${item.dataset.id}" class="form-label unt-form-label">Answer</label>
                        <textarea rows="${item.dataset.rows}" type="text" ${req} class="form-control answer-text-input" minlength="${item.dataset.minlength}" maxlength="${item.dataset.maxlength}" 
                            id="answer-textarea-${item.dataset.id}" name="Answer-Textarea-${item.dataset.id}" value="" data-original-value="" 
                            oninput="handleInputChange('${item.dataset.id}','answer-textarea-')"></textarea>
                        <span id="error-message-${item.dataset.id}" class="text-danger field-validation-error"></span>
                    </div>`;
    }

    function buildSelectListFieldPreview(item) {
        const options = JSON.parse(item.dataset.definition).options || [];
        let optionsHTML = `<option data-numeric-value="0" value="">Please choose...</option>`;
        optionsHTML += options.map(option => {
            const truncatedValue = option.value.length > 100 ? option.value.substring(0, 100) + " ..." : option.value;
            return `<option data-numeric-value="${option.numeric_value}" value="${option.value}" title="${option.value}">${truncatedValue}</option>`;
        }).join('');

        return `
                    <p>${item.dataset.questiondesc}</p>
                    <div class="mb-3">
                        <label for="answer-selectlist-${item.dataset.id}" class="form-label unt-form-label">Answer</label>
                        <select id="answer-selectlist-${item.dataset.id}"
                                class="form-select form-control answer-selectlist-input"
                                name="Answer-SelectList-${item.dataset.id}"
                                data-original-value=""
                                onchange="handleInputChange('${item.dataset.id}','answer-selectlist-')">
                            ${optionsHTML}
                        </select>
                    </div>`;
    }

    function buildNumberFieldPreview(item) {
        let req = item.dataset.required ? "required" : null;
        return `
                    <p>${item.dataset.questiondesc}</p>
                    <div class="mb-3">
                        <label for="answer-number-${item.dataset.id}" class="form-label unt-form-label">Answer</label>
                        <input type="number" ${req} class="form-control answer-number-input" min="${item.dataset.min}" max="${item.dataset.max}" 
                            id="answer-number-${item.dataset.id}" name="Answer-Number-${item.dataset.id}" data-original-value="" 
                            oninput="handleInputChange('${item.dataset.id}','answer-number-')" />
                        <span id="error-message-${item.dataset.id}" class="text-danger field-validation-error" ></span>
                    </div>`;
    }

    function buildYesNoFieldPreview(item) {
        return `
                    <p>${item.dataset.questiondesc}</p>
                    <div class="mb-3">
                        <label for="answer-yesno-${item.dataset.id}" class="form-label unt-form-label">Answer</label>
                        <select id="answer-yesno-${item.dataset.id}"
                                class="form-select form-control answer-yesno-input"
                                name="Answer-YesNo-${item.dataset.id}"
                                data-original-value=""
                                data-yes-numeric-value="${item.dataset.yesvalue}"
                                data-no-numeric-value="${item.dataset.novalue}"
                                onchange="handleInputChange('${item.dataset.id}','answer-yesno-')">
                            <option value="">Please choose...</option>
                            <option value="Yes">Yes</option>
                            <option value="No">No</option>
                        </select>
                    </div>`;
    }

    function buildTextFieldPreview(item) {
        let req = item.dataset.required ? "required" : null;

        return `
                    <p>${item.dataset.questiondesc}</p>
                    <div class="mb-3">
                        <label for="answer-text-${item.dataset.id}" class="form-label unt-form-label">Answer</label>
                        <input type="text" ${req} class="form-control answer-text-input" minlength="${item.dataset.minlength}" maxlength="${item.dataset.maxlength}" 
                            id="answer-text-${item.dataset.id}" 
                            name="Answer-Text-${item.dataset.id}" value="" data-original-value="" 
                            oninput="handleInputChange('${item.dataset.id}','answer-text-')" />
                        <span id="error-message-${item.dataset.id}" class="text-danger field-validation-error"></span>
                    </div>`;
    }

    function hashCode(str) {
        let hash = 0;
        if (str.length === 0) {
            return hash;
        }
        for (let i = 0; i < str.length; i++) {
            const char = str.charCodeAt(i);
            hash = ((hash << 5) - hash) + char;
            hash |= 0;
        }
        return hash;
    }


    makeScoresheetsSortable();
    attachAccordionToggleListeners();
    updateUnsortedPreview();

    PubSub.subscribe(
        'refresh_scoresheet_configuration_page',
        (msg, data) => {
            makeScoresheetsSortable();
            attachAccordionToggleListeners();
            updateUnsortedPreview();
        }
    );
});

let selectedScoresheetId = null;

let questionModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/QuestionModal'
});

questionModal.onResult(function (response) {
    const actionType = $(response.currentTarget).find('#ActionType').val();
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: selectedScoresheetId });

    abp.notify.success(
        actionType + ' is successful.',
        'Question'
    );
});

function openQuestionModal(scoresheetId, sectionId, questionId, actionType) {
    selectedScoresheetId = scoresheetId;
    questionModal.open({
        scoresheetId: scoresheetId,
        sectionId: sectionId,
        questionId: questionId,
        actionType: actionType
    });
}

let sectionModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/SectionModal'
});

sectionModal.onResult(function (response) {
    const actionType = $(response.currentTarget).find('#ActionType').val();
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: selectedScoresheetId });

    abp.notify.success(
        actionType + ' is successful.',
        'Scoresheet Section'
    );
});

function openSectionModal(scoresheetId, sectionId, actionType) {
    selectedScoresheetId = scoresheetId;
    sectionModal.open({
        scoresheetId: scoresheetId,
        sectionId: sectionId,
        actionType: actionType
    });
}

function saveOrder(sortedScoresheetId) {

    const sortedScoresheet = document.querySelector(`[id^="sections-questions"][data-scoresheetid="${sortedScoresheetId}"]`);
    const orderData = [];

    if (sortedScoresheet) {
        const items = sortedScoresheet.children;
        Array.from(items).forEach((item, index) => {
            orderData.push({
                type: item.dataset.type,
                id: item.dataset.id,
                scoresheetid: item.dataset.scoresheetid,
                order: index,
                label: item.innerText.trim()
            });
        });

        unity.flex.scoresheets.scoresheet.saveOrder(orderData)
            .then(response => {
                abp.notify.success(
                    'Sections and Questions ordering is successfully saved.',
                    'Scoresheet Section and Question'
                );
            });
    }
}

function exportScoresheet(scoresheetId, scoresheetName, scoresheetTitle) {
    fetch(`/api/app/scoresheet/export/${scoresheetId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.blob();
        })
        .then(blob => {
            let url = window.URL.createObjectURL(blob);
            let a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = `scoresheet_${scoresheetTitle}_${scoresheetName}.json`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
        })
        .catch(error => {
            console.error('There was a problem with the fetch operation:', error);
        });
}

function savePreviewChanges(questionId, inputFieldPrefix, saveButtonPrefix, discardButtonPrefix) {
    const inputField = document.getElementById(inputFieldPrefix + questionId);
    const saveButton = document.getElementById(saveButtonPrefix + questionId);
    const discardButton = document.getElementById(discardButtonPrefix + questionId);

    inputField.setAttribute('data-original-value', inputField.value);
    saveButton.disabled = true;
    discardButton.disabled = true;

    updateSubtotal();
}

function savePreviewSectionChanges(formId, sectionId) {
    const secSaveButton = document.getElementById('scoresheet-section-save-' + sectionId);
    const secDiscardButton = document.getElementById('scoresheet-section-discard-' + sectionId);

    const assessmentAnswersArr = [];
    const inputFieldArr = [];
    const origAnswersArr = [];
    const formData = $(`#${formId}`).serializeArray();

    //Handle form object data
    $.each(formData, function (_, inputData) {
        buildFormData(assessmentAnswersArr, inputData, inputFieldArr, origAnswersArr);
    });

    secSaveButton.disabled = true;
    secDiscardButton.disabled = true;

    updateSubtotal();
}


