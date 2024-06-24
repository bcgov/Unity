$(function () {
    let linkWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/LinkWorksheetModal'
    });

    $(".edit-worksheet-btn").on("click", function (event) {
        //openSectionModal(event.currentTarget.dataset.worksheetId, event.currentTarget.dataset.sectionId, event.currentTarget.dataset.action);
        event.stopPropagation();
        event.preventDefault();
        console.log('boom');        
        return false;
    });

    function openLinkWorksheetModal(worksheetId) {
        linkWorksheetModal.open({
            worksheetId: worksheetId
        });
    }

    function refreshWorksheetInfoWidget(worksheetId) {
        const url = `../Flex/Widget/Worksheet/Refresh?worksheetId=${worksheetId}`;
        fetch(url)
            .then(response => response.text())
            .then(data => {
                debugger;
                document.getElementById('worksheet-info-widget-' + worksheetId).innerHTML = data;
                // showAccordion(worksheetId);
                // PubSub.publish('refresh_worksheet_configuration_page');
            })
            .catch(error => {
                console.error('Error refreshing worksheet-info-widget:', error);
            });
    }

    PubSub.subscribe(
        'refresh_worksheet',
        (msg, data) => {
            refreshWorksheetInfoWidget(data.worksheetId);
        }
    );
});
