$(function () {
    let sectionModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertSectionModal'
    });

    bindAddSectionButtons();

    function bindAddSectionButtons() {
        $(".add-worksheet-section-btn").on("click", function (event) {
            openSectionModal(event.currentTarget.dataset.worksheetId, event.currentTarget.dataset.sectionId, event.currentTarget.dataset.action);
        });
    }

    function openSectionModal(worksheetId, sectionId, action) {
        sectionModal.open({
            worksheetId: worksheetId,
            sectionId: sectionId,
            actionType: action
        });
    }

    sectionModal.onResult(function (result, response) {
        PubSub.publish('refresh_worksheet', { worksheetId: response.responseText.worksheetId });
    });

    PubSub.subscribe(
        'worksheet_list_refreshed',
        (msg, data) => {            
            bindAddSectionButtons();
        }
    );
});



