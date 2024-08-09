$(function () {
    $('#scoresheet_import_upload_btn').click(function () {
        $('#scoresheet_import_upload').trigger('click');
    });
});

function importScoresheetFile(inputId) {
    let input = document.getElementById(inputId);
    let urlStr = "/api/app/scoresheet/import";
    let file = input.files[0]; // Only get the first file
    let formData = new FormData();
    const maxFileSize = decodeURIComponent($("#MaxFileSize").val());

    if (!file) {
        return;
    }

    if ((file.size * 0.000001) > maxFileSize) {
        input.value = null;
        return abp.notify.error(
            'Error',
            'File size exceeds ' + maxFileSize + 'MB'
        );
    }

    formData.append("file", file);

    $.ajax({
        url: urlStr,
        data: formData,
        processData: false,
        contentType: false,
        type: "POST",
        success: function (data) {
            abp.notify.success(
                data.responseText,
                'Scoring Sheet Import Is Successful'
            );
            PubSub.publish('refresh_scoresheet_list', { scoresheetId: null });
            input.value = null;
        },
        error: function (data) {
            abp.notify.error(
                data.responseText,
                'Scoring Sheet Import Not Successful'
            );
            PubSub.publish('refresh_scoresheet_list', { scoresheetId: null });
            input.value = null;
        }
    });
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
        'Scoring sheet cloning is successful.',
        'Scoresheet'
    );
});

publishScoresheetModal.onResult(function (response) {
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: scoresheetToEditId });
    abp.notify.success(
        'Scoring sheet publishing is successful.',
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

