$(function () {

    function makeSectionsAndQuestionsSortable() {
        document.querySelectorAll('[id^="sections-questions"]').forEach(function (div) {
            _ = new Sortable(div, {
                animation: 150,
                onEnd: updatePreview,
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
        const sortedItems = Array.from(event.target.children).map(li => li.innerText);
        updatePreviewAccordion(sortedItems);
    }

    function updateUnsortedPreview() {
        const expandedAccordionBodies = document.querySelectorAll('#scoresheet-accordion .accordion-collapse.show');
        const sortedItems = [];
        expandedAccordionBodies.forEach(body => {
            const items = body.querySelectorAll('.list-group-item');
            items.forEach(item => {
                sortedItems.push(item.innerText);
            });
        });

        updatePreviewAccordion(sortedItems);
    }
    
    function attachAccordionToggleListeners() {
        const accordionItems = document.querySelectorAll('#scoresheet-accordion .accordion-button');
        accordionItems.forEach(button => {
            button.addEventListener('click', function () {
                setTimeout(updateUnsortedPreview, 500); // Timeout to allow the collapse animation to complete
            });
        });
    }
    
    function updatePreviewAccordion(sortedItems) {
        const previewDiv = document.getElementById('preview');

        if (sortedItems.length === 0) {
            previewDiv.innerHTML = '<p>No sections to display.</p>';
            return;
        }

        const accordionHTML = sortedItems.map(item => `
                <div class="accordion-item">
                    <h2 class="accordion-header" id="panel-${hashCode(item)}">
                        <button class="accordion-button preview-btn" type="button" data-bs-toggle="collapse" data-bs-target="#collapse-${hashCode(item)}" aria-expanded="true" aria-controls="collapse-${hashCode(item)}">
                            ${item}
                        </button>
                    </h2>
                    <div id="collapse-${hashCode(item)}" class="accordion-collapse collapse show" aria-labelledby="panel-${hashCode(item)}" >
                        <div class="accordion-body">
                            <!-- questions go here -->
                        </div>
                    </div>
                </div>
            `).join('');

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
            hash |= 0; // Convert to 32bit integer
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

questionModal.onResult(function () {
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: selectedScoresheetId });
    abp.notify.success(
        'Question is successfully added.',
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



sectionModal.onResult(function () {
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: selectedScoresheetId });
    abp.notify.success(
        'Section is successfully added.',
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
