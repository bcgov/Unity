let scoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/ScoresheetModal'
});

let cloneScoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/CloneScoresheetModal'
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
function openScoresheetModal(scoresheetId, actionType, groupId) {
    scoresheetToEditId = scoresheetId;
    scoresheetModal.open({
        scoresheetId: scoresheetId,
        actionType: actionType,
        groupId: groupId
    });
}

function openCloneScoresheetModal(scoresheetId, groupId) {
    scoresheetToEditId = scoresheetId;
    cloneScoresheetModal.open({
        scoresheetId: scoresheetId,
        groupId: groupId
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

async function askToCreateNewVersion() {
    const result = await Swal.fire({
        title: "Confirm changes made to scoring sheet",
        text: "Do you want to save your changes on the current version or create a new score sheet version?",
        showCancelButton: true,
        confirmButtonText: 'Save changes to the current version',
        cancelButtonText: 'Create a new version',
        customClass: {
            confirmButton: 'btn btn-primary',
            cancelButton: 'btn btn-secondary'
        }
    });
    
    
    if (result.isConfirmed) {
        return " On Current Version";
    } else if (result.dismiss === Swal.DismissReason.cancel) {
        await Swal.fire({
            title: "Note",
            text: "Note that to apply the new version of the scoresheet in the assessment process, you need to link the corresponding form to the updated version.",
            confirmButtonText: 'Ok',
            customClass: {
                confirmButton: 'btn btn-primary'
            }
        });
        return " On New Version";
    }
    
}