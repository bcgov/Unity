$(function () {
    let upsertWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertWorksheetModal'
    });

    $("#add_worksheet_btn").on("click", function (event) {
        openWorksheetModal(null, 'Insert');
    });

    bindEditWorksheetButtons();

    function bindEditWorksheetButtons() {
        $("#.edit-worksheet-btn").on("click", function (event) {
            let worksheetId = event.currentTarget.dataset.worksheetId;
            openWorksheetModal(worksheetId, 'Update');
        });
    }

    upsertWorksheetModal.onResult(function (result, response) {
        PubSub.publish('refresh_worksheet_list', { worksheetId: response.worksheetId });

        abp.notify.success(
            'Operation completed successfully.',
            'Add Worksheet'
        );
    });

    function openWorksheetModal(worksheetId, actionType) {
        upsertWorksheetModal.open({
            worksheetId: worksheetId,
            actionType: actionType
        });
    }

    function refreshWorksheetListWidget(worksheetId) {
        const url = `../Flex/Widget/WorksheetList/Refresh`;
        fetch(url)
            .then(response => response.text())
            .then(data => {
                document.getElementById('worksheet-info-widget-list').innerHTML = data;
                // showAccordion(worksheetId);
                PubSub.publish('worksheet_list_refreshed');
            })
            .catch(error => {
                console.error('Error refreshing worksheet-info-list-widget:', error);
            });
    }


    PubSub.subscribe(
        'refresh_worksheet_list',
        (msg, data) => {
            refreshWorksheetListWidget(data.worksheetId);
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
});