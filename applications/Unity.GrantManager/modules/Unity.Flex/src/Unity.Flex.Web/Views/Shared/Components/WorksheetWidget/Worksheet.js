$(function () {
    let sectionModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertSectionModal'
    });

    let customFieldModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertCustomFieldModal'
    });

    bindActionButtons();

    function bindActionButtons() {
        let addWorksheetSectionButtons = $(".add-worksheet-section-btn");

        if (addWorksheetSectionButtons) {
            addWorksheetSectionButtons.on("click", function (event) {
                openSectionModal(event.currentTarget.dataset.worksheetId, event.currentTarget.dataset.sectionId, event.currentTarget.dataset.action);
            });
        }

        let editWorksheetSectionButtons = $(".edit-section-btn");

        if (editWorksheetSectionButtons) {
            editWorksheetSectionButtons.on("click", function (event) {
                openSectionModal(event.currentTarget.dataset.worksheetId, event.currentTarget.dataset.sectionId, event.currentTarget.dataset.action);
            });
        }

        let addCustomFieldButtons = $(".add-custom-field-btn")

        if (addCustomFieldButtons) {
            addCustomFieldButtons.on("click", function (event) {
                openCustomFieldModal(event.currentTarget.dataset.worksheetId, event.currentTarget.dataset.sectionId, event.currentTarget.dataset.fieldId, event.currentTarget.dataset.action);
            });
        }
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

    function openCustomFieldModal(worksheetId, sectionId, fieldId, action) {
        customFieldModal.open({
            worksheetId: worksheetId,
            sectionId: sectionId,
            fieldId: fieldId,
            actionType: action
        });
    }

    sectionModal.onResult(function (result, response) {
        console.log(response);
    });

    function refreshWorksheetInfoWidget(worksheetId) {
        const url = `../Flex/Widget/Worksheet/Refresh?worksheetId=${worksheetId}`;
        fetch(url)
            .then(response => response.text())
            .then(data => {
                document.getElementById('worksheet-info-widget-' + worksheetId).innerHTML = data;
                PubSub.publish('worksheet_refreshed', worksheetId);
            })
            .catch(error => {
                console.error('Error refreshing worksheet-info-widget:', error);
            });
    }

    function updateWorksheetAccordionButton(worksheetId) {
        // Get basic refreshed header level details of the worksheet        
        unity.flex.worksheets.worksheetList.get(worksheetId)
            .done(function (result) {
                let titleField = $("#worksheet-title-" + worksheetId);
                let sectionCountField = $("#worksheet-total-sections-" + worksheetId);
                let fieldsCountField = $("#worksheet-total-fields-" + worksheetId);

                if (titleField) {
                    titleField.text(result.title);
                }

                if (sectionCountField) {
                    sectionCountField.text(result.totalSections);
                }

                if (fieldsCountField) {
                    fieldsCountField.text(result.totalFields);
                }
            });
    }

    PubSub.subscribe(
        'worksheet_refreshed',
        (msg, data) => {
            bindActionButtons();
            updateWorksheetAccordionButton(data);
        }
    );

    PubSub.subscribe(
        'refresh_worksheet',
        (msg, data) => {
            refreshWorksheetInfoWidget(data.worksheetId);
        }
    );
});



