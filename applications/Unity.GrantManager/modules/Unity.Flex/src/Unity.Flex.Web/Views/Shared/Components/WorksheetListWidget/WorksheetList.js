$(function () {
    let addWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertWorksheetModal'
    });

    let cloneWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/CloneWorksheetModal'
    });

    bindActionButtons();

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
        const url = `../Flex/Widget/WorksheetList/Refresh`;
        fetch(url)
            .then(response => response.text())
            .then(data => {
                document.getElementById('worksheet-info-widget-list').innerHTML = data;
                PubSub.publish('worksheet_list_refreshed');
            })
            .catch(error => {
                console.error('Error refreshing worksheet-info-list-widget:', error);
            });
    }

    PubSub.subscribe(
        'refresh_worksheet_list',
        () => {
            refreshWorksheetListWidget();
        }
    );

    PubSub.subscribe(
        'worksheet_list_refreshed',
        () => {
            bindActionButtons();            
        }
    );

    PubSub.subscribe(
        'worksheet_refreshed',
        () => {
            bindActionButtons();            
        }
    );
});
