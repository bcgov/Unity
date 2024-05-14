let scoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/ScoresheetModal'
});

scoresheetModal.onResult(function () {
    PubSub.publish('refresh_scoresheet_list');
    abp.notify.success(
        'Scoresheet is successfully added.',
        'Scoresheet'
    );
    refreshScoresheetInfoWidget();
});

function openScoresheetModal(scoresheetId,actionType) {
    scoresheetModal.open({
        scoresheetId: scoresheetId,
        actionType: actionType
    });
}

function refreshScoresheetInfoWidget() {
    const url = `../Flex/Widget/Scoresheet/Refresh`;
    fetch(url)
        .then(response => response.text())
        .then(data => {
            document.getElementById('scoresheet-info-widget').innerHTML = data;
            PubSub.publish('reload_sites_list');
        })
        .catch(error => {
            console.error('Error refreshing supplier-info-widget:', error);
        });
}