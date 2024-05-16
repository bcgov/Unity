let scoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/ScoresheetModal'
});

let scoresheetToEditId = null;
scoresheetModal.onResult(function () {
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: scoresheetToEditId });
    abp.notify.success(
        'Scoresheet is successfully added.',
        'Scoresheet'
    );
});
function openScoresheetModal(scoresheetId, actionType) {
    scoresheetToEditId = scoresheetId;
    scoresheetModal.open({
        scoresheetId: scoresheetId,
        actionType: actionType
    });
}

PubSub.subscribe(
    'refresh_scoresheet_list',
    (msg, data) => {
        refreshScoresheetInfoWidget(data.scoresheetId);
    }
);

function showAccordion(scoresheetId) {
    if (!scoresheetId) {
        return;
    }
    const accordionId = 'collapse-' + scoresheetId;
    const accordion = document.getElementById(accordionId);

    accordion.classList.add('show');
}

function refreshScoresheetInfoWidget(scoresheetId) {
    const url = `../Flex/Widget/Scoresheet/Refresh`;
    fetch(url)
        .then(response => response.text())
        .then(data => {
            document.getElementById('scoresheet-info-widget').innerHTML = data;
            showAccordion(scoresheetId);
            PubSub.publish('refresh_scoresheet_configuration_page');
        })
        .catch(error => {
            console.error('Error refreshing scoresheet-info-widget:', error);
        });
}