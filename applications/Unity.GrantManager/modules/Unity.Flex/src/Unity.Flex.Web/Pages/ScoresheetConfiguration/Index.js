let scoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/ScoresheetModal'
});

scoresheetModal.onResult(function () {
    PubSub.publish('refresh_scoresheet_list');
    abp.notify.success(
        'Scoresheet is successfully added.',
        'Scoresheet'
    );
});

function openScoresheetModal(scoresheetId,actionType) {
    scoresheetModal.open({
        scoresheetId: scoresheetId,
        actionType: actionType
    });
}