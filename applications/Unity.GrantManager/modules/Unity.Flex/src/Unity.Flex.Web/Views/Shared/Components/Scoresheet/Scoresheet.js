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
    makeSectionsAndQuestionsSortable();
    PubSub.subscribe(
        'make_scoresheet_body_sortable',
        (msg, data) => {
            makeSectionsAndQuestionsSortable();
        }
    );
    

    function updatePreview(event) {
        const previewDiv = document.getElementById('preview');
        const sortedItems = Array.from(event.target.children).map(li => li.innerText);
        previewDiv.innerHTML = '<ul>' + sortedItems.map(item => '<li>' + item + '</li>').join('') + '</ul>';
    }
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

function openScoresheetModal(questionId, actionType) {
    questionModal.open({
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
