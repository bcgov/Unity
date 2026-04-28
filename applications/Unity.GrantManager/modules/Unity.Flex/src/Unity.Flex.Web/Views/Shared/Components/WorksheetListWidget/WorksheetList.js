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
        let addWorksheetButton = $(".worksheet-add-btn");

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

function makeSectionsAndFieldsSortable() {
    makeCustomFieldsSortable();
    makeSectionsSortable();
}

let customFieldSortables = [];
let sectionSortables = [];

function makeCustomFieldsSortable() {
    customFieldSortables.forEach(s => s.destroy());
    customFieldSortables = [];
    document.querySelectorAll('.custom-fields-wrapper').forEach(function (div) {
        const wrapper = div.closest('.sections-wrapper-outer');
        const isArchived = wrapper?.dataset.isArchived === 'true';
        const worksheetId = wrapper?.dataset.worksheetId;
        customFieldSortables.push(new Sortable(div, {
            group: `custom-fields-${worksheetId}`,
            animation: 150,
            disabled: isArchived,
            onEnd: function (evt) {
                updateCustomFieldsSequence(evt);
            },
            ghostClass: 'blue-background',
            onMove: function () {
                return true;
            }
        }));
    });
}

function makeSectionsSortable() {
    sectionSortables.forEach(s => s.destroy());
    sectionSortables = [];
    document.querySelectorAll('.sections-wrapper-outer').forEach(function (div) {
        const isArchived = div.dataset.isArchived === 'true';
        sectionSortables.push(new Sortable(div, {
            animation: 150,
            disabled: isArchived,
            onEnd: function (evt) {
                updateSectionSequence(evt);
            },
            ghostClass: 'blue-background',
            onMove: function () {
                return true;
            }
        }));
    });
}

function updateCustomFieldsSequence(evt) {
    if (evt.from === evt.to) {
        // Reorder within the same section
        const sectionId = evt.from.dataset.sectionId;
        const oldIndex = evt.oldIndex;
        const newIndex = evt.newIndex;

        unity.flex.worksheets.worksheetSection
            .resequenceCustomFields(sectionId, oldIndex, newIndex, {})
            .done(function () {
                updatePreview();
                abp.notify.success('Custom fields order updated.');
            });
    } else {
        // Move to a different section
        const fieldId = evt.item.dataset.id;
        const targetSectionId = evt.to.dataset.sectionId;
        const newIndex = evt.newIndex;

        unity.flex.worksheets.customField
            .moveToSection(fieldId, targetSectionId, newIndex, {})
            .done(function () {
                updatePreview();
                abp.notify.success('Field moved to new section.');
            })
            .fail(function () {
                // Revert the DOM move on failure
                evt.from.insertBefore(evt.item, evt.from.children[evt.oldIndex] || null);
                abp.notify.error('Failed to move field.');
            });
    }
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
                $('#preview :input').prop('readonly', true);
                PubSub.publish('worksheet_preview_datagrid_refresh');
            })
            .catch(error => {
                console.error('Error generating preview:', error);
            });
    } else {
        previewPane?.html('<p>No sections to display.</p>');
    }

    $('.preview-scrollable').first().scrollTop(0);
}
