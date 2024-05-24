let scoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/ScoresheetModal'
});

let scoresheetToEditId = null;

scoresheetModal.onResult(function (response) {
    const actionType = $(response.currentTarget).find('#ActionType').val();
    if (actionType.startsWith('Delete')) {
        PubSub.publish('refresh_scoresheet_list', { scoresheetId: null, scorsheetIdsToLoad: [] });
    } else {
        PubSub.publish('refresh_scoresheet_list', { scoresheetId: scoresheetToEditId, scorsheetIdsToLoad: getScoresheetIdsToLoad() });
    }
    abp.notify.success(
        actionType + ' is successful.', 
        'Scoresheet'
    );
});
function openScoresheetModal(scoresheetId, actionType, groupId) {
    scoresheetToEditId = scoresheetId;
    scoresheetModal.open({
        scoresheetId: scoresheetId,
        actionType: actionType,
        groupId: groupId
    });
}

PubSub.subscribe(
    'refresh_scoresheet_list',
    (msg, data) => {
        refreshScoresheetInfoWidget(data.scoresheetId, data.scorsheetIdsToLoad);
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

function refreshScoresheetInfoWidget(scoresheetId, scorsheetIdsToLoad) {
    const url = `../Flex/Widget/Scoresheet/Refresh?scoresheetIdsToLoad=${scorsheetIdsToLoad.join(',')}`;
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