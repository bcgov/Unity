$(function () {

    function makeSectionsAndQuestionsSortable() {
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

        let accordionHTML = '';
        let currentSectionItem = null;
        let sectionNumber = 1;
        let questionNumber = 1;
        let parentAccordionId = `accordion-preview`;

        sortedItems.forEach(item => {
            if (item.classList.contains('section-item')) {
                if (currentSectionItem) {
                    accordionHTML += '</div></div></div></div>';
                    sectionNumber++;
                }
                parentAccordionId = `nested-accordion-${hashCode(item.innerText)}`;
                accordionHTML += `
            <div class="accordion-item">
                <h2 class="accordion-header" id="panel-${hashCode(item.innerText)}">
                    <button class="accordion-button preview-btn" type="button" data-bs-toggle="collapse" data-bs-target="#collapse-${hashCode(item.innerText)}" aria-expanded="true" aria-controls="collapse-${hashCode(item.innerText)}">
                        ${sectionNumber}.  ${item.dataset.label}
                    </button>
                </h2>
                <div id="collapse-${hashCode(item.innerText)}" class="accordion-collapse collapse show" aria-labelledby="panel-${hashCode(item.innerText)}">
                    <div class="accordion-body">
                        <div class="accordion" id="${parentAccordionId}">`; // Start a new nested accordion
                currentSectionItem = item;
                questionNumber = 1;
            } else {
                let questionBody = '';
                if (item.dataset.questiontype === "Text") {
                    questionBody = `
                    <p>${item.dataset.questiondesc}</p>
                    <div class="mb-3">
                        <label class="form-label">Answer</label>
                        <input type="text" class="form-control answer-text-input"/>
                    </div>
                    <div class="btn-group">
                        <button type="button" class="btn btn-primary" >SAVE CHANGES</button>
                        <button type="button" class="btn btn-secondary" >DISCARD CHANGES</button>
                    </div>`;
                } else if (item.dataset.questiontype === "YesNo") {
                    questionBody = `
                    <p>${item.dataset.questiondesc}</p>
                    <div class="mb-3">
                        <label class="form-label">Answer</label>
                        <select class="form-control answer-yesno-input">
                            <option value="">Please choose...</option>
                            <option value="Yes">Yes</option>
                            <option value="No">No</option>
                        </select>
                    </div>
                    <div class="btn-group">
                        <button type="button" class="btn btn-primary" >SAVE CHANGES</button>
                        <button type="button" class="btn btn-secondary" >DISCARD CHANGES</button>
                    </div>`;
                } else if (item.dataset.questiontype === "Number") {
                    questionBody = `
                    <p>${item.dataset.questiondesc}</p>
                    <div class="mb-3">
                        <label class="form-label">Answer</label>
                        <input type="number" class="form-control answer-number-input" />
                    </div>
                    <div class="btn-group">
                        <button type="button" class="btn btn-primary" >SAVE CHANGES</button>
                        <button type="button" class="btn btn-secondary" >DISCARD CHANGES</button>
                    </div>`;
                }

                accordionHTML += `
                    <div class="accordion-item">
                        <h2 class="accordion-header" id="nested-panel-${hashCode(item.innerText)}">
                            <button class="accordion-button question-btn collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#nested-collapse-${hashCode(item.innerText)}" aria-expanded="true" aria-controls="nested-collapse-${hashCode(item.innerText)}">
                                ${sectionNumber}.${questionNumber}  ${item.innerText}
                            </button>
                        </h2>
                        <div id="nested-collapse-${hashCode(item.innerText)}" class="accordion-collapse collapse" aria-labelledby="nested-panel-${hashCode(item.innerText)}" data-bs-parent="#${parentAccordionId}">
                            <div class="accordion-body">
                                ${questionBody}
                            </div>
                        </div>
                    </div>`;
                questionNumber++;
            }
        });

        if (currentSectionItem) {
            accordionHTML += '</div></div></div></div>';
        }

        previewDiv.innerHTML = `
            <div class="accordion" id="accordion-preview">
                ${accordionHTML}
            </div>
        `;
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

    
    makeSectionsAndQuestionsSortable();
    attachAccordionToggleListeners();
    updateUnsortedPreview();
    
    PubSub.subscribe(
        'refresh_scoresheet_configuration_page',
        (msg, data) => {
            makeSectionsAndQuestionsSortable();
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




