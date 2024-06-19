let worksheetModal = new abp.ModalManager({
    viewUrl: 'WorksheetConfiguration/UpsertWorksheetModal'
});

let worksheetEditId = null;

worksheetModal.onResult(function (response) {
    //const actionType = $(response.currentTarget).find('#ActionType').val();
    //if (actionType.startsWith('Delete')) {
    //    PubSub.publish('refresh_scoresheet_list', { scoresheetId: null, scorsheetIdsToLoad: getScoresheetIdsToLoad() });
    //} else if (actionType == 'Edit Scoring Sheet On New Version') {
    //    const scoresheetIdsToLoad = getScoresheetIdsToLoad().filter(element => element !== scoresheetToEditId);
    //    PubSub.publish('refresh_scoresheet_list', { scoresheetId: null, scorsheetIdsToLoad: scoresheetIdsToLoad });
    //} else {
    //    PubSub.publish('refresh_scoresheet_list', { scoresheetId: scoresheetToEditId, scorsheetIdsToLoad: getScoresheetIdsToLoad() });
    //}
    abp.notify.success(
        'Operation completed successfully.',
        'Worksheet'
    );
});

function openWorksheetModal(worksheetId, actionType) {
    worksheetEditId = worksheetId;
    worksheetModal.open({
        worksheetId: worksheetId,
        actionType: actionType        
    });
}

PubSub.subscribe(
    'refresh_worksheet_list',
    (msg, data) => {
        refreshWorksheetInfoWidget(data.worksheetId, data.scorsheetIdsToLoad);
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