$(function () {
    let sectionModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertSectionModal'
    });

    let customFieldModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertCustomFieldModal'
    });

    let editWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertWorksheetModal'
    });

    let publishWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/PublishWorksheetModal'
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

        let publishWorksheetButtons = $(".publish-worksheet-btn");

        if (publishWorksheetButtons) {
            publishWorksheetButtons.on("click", function (event) {
                let worksheetId = event.currentTarget.dataset.worksheetId;
                openPublishWorksheetModal(worksheetId);
            });
        }

        let exportWorksheetButtons = $(".export-worksheet-btn");

        if (exportWorksheetButtons) {
            exportWorksheetButtons.on("click", function (event) {
                let worksheetId = event.currentTarget.dataset.worksheetId;
                let worksheetName = event.currentTarget.dataset.worksheetName;
                let worksheetTitle = event.currentTarget.dataset.worksheetTitle;
                exportWorksheet(worksheetId, worksheetName, worksheetTitle);
            });
        }
    }

    function exportWorksheet(worksheetId, worksheetName, worksheetTitle) {
        fetch(`/api/app/worksheet/export/${worksheetId}`)
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
                a.download = `worksheet_${worksheetTitle}_${worksheetName}.json`;
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
            })
            .catch(error => {
                console.error('There was a problem with the fetch operation:', error);
            });
    }

    function openEditWorksheetModal(worksheetId) {
        editWorksheetModal.open({
            worksheetId: worksheetId,
            actionType: 'Update'
        });
    }

    editWorksheetModal.onResult(function (_, response) {
        if (response.responseText.action === 'Delete') {
            PubSub.publish('refresh_worksheet_list', { worksheetId: response.responseText.worksheetId, action: response.responseText.action });
        }
        else {
            PubSub.publish('refresh_worksheet', { worksheetId: response.responseText.worksheetId, action: response.responseText.action });
        }
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

    sectionModal.onResult(function (_, response) {        
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

    customFieldModal.onResult(function (_, response) {
        PubSub.publish('refresh_worksheet', { worksheetId: response.responseText.worksheetId });
    });

    function openPublishWorksheetModal(worksheetId) {
        publishWorksheetModal.open({
            worksheetId: worksheetId
        });
    }

    publishWorksheetModal.onResult(function (_, response) {
        PubSub.publish('refresh_worksheet', { worksheetId: response.responseText.worksheetId });
    });

    function refreshWorksheetInfoWidget(worksheetId) {
        const url = `../Flex/Widgets/Worksheet/Refresh?worksheetId=${worksheetId}`;
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
                let worksheetPublished = $("#worksheet-published-" + worksheetId);

                titleField?.text(result.title);
                sectionCountField?.text(result.totalSections);
                fieldsCountField?.text(result.totalFields);
                nameField?.text(result.name);
                if (result.published) {
                    worksheetPublished?.removeClass('hidden');
                }
            });
    }

    PubSub.subscribe(
        'worksheet_refreshed',
        (_, data) => {
            bindActionButtons();
            updateWorksheetAccordionButton(data);
        }
    );

    PubSub.subscribe(
        'refresh_worksheet',
        (_, data) => {
            refreshWorksheetInfoWidget(data.worksheetId);
        }
    );

    PubSub.subscribe(
        'worksheet_list_refreshed',
        () => {
            console.log('ws item');
            bindActionButtons();
        }
    );
});



