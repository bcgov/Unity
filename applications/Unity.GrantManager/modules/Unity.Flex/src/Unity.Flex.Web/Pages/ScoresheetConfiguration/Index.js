$(function () {
    $('#scoresheet_import_upload_btn').click(function () {
        $('#scoresheet_import_upload').trigger('click');
    });
});

function importScoresheetFile(inputId) {
    importFlexFile(inputId, "/api/app/scoresheet/import", "Scoresheet", 'refresh_scoresheet_list');
}


let scoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/ScoresheetModal'
});

let cloneScoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/CloneScoresheetModal'
});

let publishScoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/PublishScoresheetModal'
});

let scoresheetToEditId = null;

scoresheetModal.onResult(function (response) {
    const actionType = $(response.currentTarget).find('#ActionType').val();
    if (actionType.startsWith('Delete')) {
        PubSub.publish('refresh_scoresheet_list', { scoresheetId: null });
    } else {
        PubSub.publish('refresh_scoresheet_list', { scoresheetId: scoresheetToEditId });
    }
    abp.notify.success(
        actionType + ' is successful.', 
        'Scoresheet'
    );
});

cloneScoresheetModal.onResult(function (response) {
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: null });
    abp.notify.success(
        'Scoresheet cloning is successful.',
        'Scoresheet'
    );
});

publishScoresheetModal.onResult(function (response) {
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: scoresheetToEditId });
    abp.notify.success(
        'Scoresheet publishing is successful.',
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

function openCloneScoresheetModal(scoresheetId) {
    scoresheetToEditId = scoresheetId;
    cloneScoresheetModal.open({
        scoresheetId: scoresheetId
    });
}

function openPublishScoresheetModal(scoresheetId) {
    scoresheetToEditId = scoresheetId;
    publishScoresheetModal.open({
        scoresheetId: scoresheetId
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

    const buttonId = 'accordion-button-' + scoresheetId;
    const accordionButton = document.getElementById(buttonId);
    accordionButton.classList.remove('collapsed');
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

