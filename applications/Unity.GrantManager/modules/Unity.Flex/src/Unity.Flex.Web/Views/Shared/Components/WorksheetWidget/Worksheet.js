$(function () {
    let sectionModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertSectionModal'
    });

    let customFieldModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertCustomFieldModal'
    });

    let linkWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/LinkWorksheetModal'
    });

    let editWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertWorksheetModal'
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

        let addCustomFieldButtons = $(".add-custom-field-btn");

        if (addCustomFieldButtons) {
            addCustomFieldButtons.on("click", function (event) {
                openCustomFieldModal(event.currentTarget.dataset.worksheetId, event.currentTarget.dataset.sectionId, event.currentTarget.dataset.fieldId, event.currentTarget.dataset.action);
            });
        }

        let editCustomFieldButtons = $(".edit-custom-field-btn");

        if (editCustomFieldButtons) {
            editCustomFieldButtons.on("click", function (event) {
                openCustomFieldModal(event.currentTarget.dataset.worksheetId, event.currentTarget.dataset.sectionId, event.currentTarget.dataset.fieldId, event.currentTarget.dataset.action);
            });
        }

        let editWorksheetButtons = $(".edit-worksheet-btn");

        if (editWorksheetButtons) {
            editWorksheetButtons.on("click", function (event) {
                let worksheetId = event.currentTarget.dataset.worksheetId;
                openEditWorksheetModal(worksheetId);
            });
        }

        let linkWorksheetButtons = $(".link-worksheet-btn");

        if (linkWorksheetButtons) {
            linkWorksheetButtons.on("click", function (event) {
                let worksheetId = event.currentTarget.dataset.worksheetId;
                openLinkWorksheetModal(worksheetId);
            });
        }
    }

    function openLinkWorksheetModal(worksheetId) {
        linkWorksheetModal.open({
            worksheetId: worksheetId
        });
    }

    function openEditWorksheetModal(worksheetId) {
        editWorksheetModal.open({
            worksheetId: worksheetId,
            actionType: 'Update'
        });
    }

    editWorksheetModal.onResult(function (result, response) {
        PubSub.publish('refresh_worksheet', { worksheetId: response.responseText.worksheetId, action: response.responseText.action });
        abp.notify.success(
            'Operation completed successfully.',
            response.responseText.action + ' Worksheet'
        );
    });

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

    customFieldModal.onResult(function (result, response) {
        PubSub.publish('refresh_worksheet', { worksheetId: response.responseText.worksheetId });
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
                let nameField = $("#worksheet-name-" + worksheetId);
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

                if (nameField) {
                    nameField.text(result.name);
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

    PubSub.subscribe(
        'worksheet_list_refreshed',
        (msg, data) => {
            bindActionButtons();
        }
    );
});



