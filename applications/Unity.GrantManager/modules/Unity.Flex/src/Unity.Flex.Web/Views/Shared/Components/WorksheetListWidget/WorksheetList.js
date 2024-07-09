$(function () {
    let addWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertWorksheetModal'
    });

    let cloneWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/CloneWorksheetModal'
    });

    bindActionButtons();
    makeSectionsAndFieldsSortable();

    function bindActionButtons() {
        let addWorksheetButton = $("#add_worksheet_btn");

        if (addWorksheetButton) {
            addWorksheetButton.on("click", function (_) {
                openAddWorksheetModal(null);
            });
        }

        let cloneWorksheetButtons = $(".clone-worksheet-btn");

        if (cloneWorksheetButtons) {
            cloneWorksheetButtons.on("click", function (event) {
                openCloneWorksheetModal(event.currentTarget.dataset.worksheetId)
            });
        }

        let worksheetSections = $(".accordion-button");

        if (worksheetSections) {
            worksheetSections.on("click", function (_) {
                updatePreview();
            });
        }
    }

    function openAddWorksheetModal(worksheetId) {
        addWorksheetModal.open({
            worksheetId: worksheetId,
            actionType: 'Insert'
        });
    }

    addWorksheetModal.onResult(function (result, response) {
        PubSub.publish('refresh_worksheet_list', { worksheetId: response.responseText.worksheetId, action: response.responseText.action });        
        abp.notify.success(
            'Operation completed successfully.',
            response.responseText.action + ' Worksheet'
        );
    });

    function openCloneWorksheetModal(worksheetId) {
        cloneWorksheetModal.open({
            worksheetId: worksheetId
        });
    }

    cloneWorksheetModal.onResult(function (result, response) {
        PubSub.publish('refresh_worksheet_list');
    });

    function refreshWorksheetListWidget() {
        const url = `../Flex/Widgets/WorksheetList/Refresh`;
        fetch(url)
            .then(response => response.text())
            .then(data => {
                document.getElementById('worksheet-info-widget-list').innerHTML = data;   
                setTimeout(() => {
                    PubSub.publish('worksheet_list_refreshed');
                }, 100);
            })
            .catch(error => {
                console.error('Error refreshing worksheet-info-list-widget:', error);
            });
    }

    function makeSectionsAndFieldsSortable() {
        makeCustomFieldsSortable();
        makeSectionsSortable();
    }

    function makeCustomFieldsSortable() {
        document.querySelectorAll('.custom-fields-wrapper').forEach(function (div) {
            _ = new Sortable(div, {
                animation: 150,
                onEnd: function (evt) {
                    updateCustomFieldsSequence(evt);
                },
                ghostClass: 'blue-background',
                onMove: function (_) {
                    return true;
                }
            });
        });
    }

    function makeSectionsSortable() {
        document.querySelectorAll('.sections-wrapper-outer').forEach(function (div) {
            _ = new Sortable(div, {
                animation: 150,
                onEnd: function (evt) {
                    updateSectionSequence(evt);                    
                },
                ghostClass: 'blue-background',
                onMove: function (_) {
                    return true;
                }
            });
        });
    }

    function updateCustomFieldsSequence(evt) {
        let sectionId = evt.target.dataset.sectionId;
        let oldIndex = evt.oldIndex;
        let newIndex = evt.newIndex;

        unity.flex.worksheets.worksheetSection
            .resequenceCustomFields(sectionId, oldIndex, newIndex, {})
            .done(function () {
                updatePreview();
                abp.notify.success(
                    'Custom fields order updated.'
                );
            });
    }

    function updateSectionSequence(evt) {
        let worksheetId = evt.target.dataset.worksheetId;
        let oldIndex = evt.oldIndex;
        let newIndex = evt.newIndex;

        unity.flex.worksheets.worksheet
            .resequenceSections(worksheetId, oldIndex, newIndex, {})
            .done(function () {
                updatePreview();
                abp.notify.success(
                    'Sections fields order updated.'
                );
            });
    }

    function updatePreview() {
        let worksheets = $('button.accordion-button[aria-expanded=true]');
        const previewPane = $('#preview');

        if (worksheets?.length > 0) {
            let worksheetId = worksheets[0].dataset.worksheetId;
            const url = `../Flex/Widgets/WorksheetInstance/Refresh?`
                + `instanceCorrelationId=00000000-0000-0000-0000-000000000000&`
                + `instanceCorrelationProvider=Preview&`
                + `sheetCorrelationId=00000000-0000-0000-0000-000000000000&`
                + `sheetCorrelationProvider=Preview&`
                + `uiAnchor=Preview&`
                + `worksheetId=${worksheetId}`;
            fetch(url)
                .then(response => response.text())
                .then(data => {
                    previewPane.html(data);
                    $("#preview :input").prop("readonly", true);
                })
                .catch(error => {
                    console.error('Error generating preview:', error);
                });

        } else {
            previewPane?.html('<p>No sections to display.</p>');
        }
    }

    PubSub.subscribe(
        'refresh_worksheet_list',
        () => {            
            refreshWorksheetListWidget();
            makeSectionsAndFieldsSortable();
            updatePreview();            
        }
    );

    PubSub.subscribe(
        'worksheet_list_refreshed',
        () => {
            console.log('ws list');
            bindActionButtons();
            makeSectionsAndFieldsSortable();
            updatePreview();
        }
    );

    PubSub.subscribe(
        'worksheet_refreshed',
        () => {
            bindActionButtons();
            makeSectionsAndFieldsSortable();
            updatePreview();
        }
    );
});
