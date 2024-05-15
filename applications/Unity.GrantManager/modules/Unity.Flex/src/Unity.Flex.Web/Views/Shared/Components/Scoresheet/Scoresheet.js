$(function () {

    function makeSectionsAndQuestionsSortable() {
        document.querySelectorAll('[id^="sections-questions"]').forEach(function (div) {
            _ = new Sortable(div, {
                animation: 150,
                onEnd: updatePreview,
                ghostClass: 'blue-background'
            });
        });
    }

    function updatePreview(event) {
        const sortedItems = Array.from(event.target.children).map(li => li.innerText);
        updatePreviewAccordion(sortedItems);
    }

    function updateUnsortedPreview() {
        const items = document.querySelectorAll('[id^="sections-questions"] .list-group-item');
        const sortedItems = Array.from(items).map(li => li.innerText);
        updatePreviewAccordion(sortedItems);
    }

    updateUnsortedPreview();

    function updatePreviewAccordion(sortedItems) {
        const previewDiv = document.getElementById('preview');

        const accordionHTML = sortedItems.map(item => `
                <div class="accordion-item">
                    <h2 class="accordion-header" id="panel-${hashCode(item)}">
                        <button class="accordion-button preview-btn" type="button" data-bs-toggle="collapse" data-bs-target="#collapse-${hashCode(item)}" aria-expanded="true" aria-controls="collapse-${hashCode(item)}">
                            ${item}
                        </button>
                    </h2>
                    <div id="collapse-${hashCode(item)}" class="accordion-collapse collapse show" aria-labelledby="panel-${hashCode(item)}" data-bs-parent="#preview-accordion">
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

    PubSub.subscribe(
        'make_scoresheet_body_sortable',
        (msg, data) => {
            makeSectionsAndQuestionsSortable();
        }
    );
});

let questionModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/QuestionModal'
});

questionModal.onResult(function () {
    PubSub.publish('refresh_scoresheet_list');
    abp.notify.success(
        'Question is successfully added.',
        'Question'
    );
});

function openQuestionModal(sectionId, questionId, actionType) {
    questionModal.open({
        sectionId: sectionId,
        questionId: questionId,
        actionType: actionType
    });
}

let sectionModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/SectionModal'
});

let selectedScoresheetId = null;

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
