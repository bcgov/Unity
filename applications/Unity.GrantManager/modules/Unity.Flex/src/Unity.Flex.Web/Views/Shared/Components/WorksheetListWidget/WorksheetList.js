$(function () {
    let upsertWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/UpsertWorksheetModal'
    });

    let linkWorksheetModal = new abp.ModalManager({
        viewUrl: 'WorksheetConfiguration/LinkWorksheetModal'
    });

    bindActionButtons();

    function bindActionButtons() {
        let addWorksheet = $("#add_worksheet_btn");

        if (addWorksheet) {
            addWorksheet.on("click", function (_) {
                openWorksheetModal(null, 'Insert');
            });
        }

        let editWorksheetButtons = $(".edit-worksheet-btn");

        if (editWorksheetButtons) {
            editWorksheetButtons.on("click", function (event) {
                let worksheetId = event.currentTarget.dataset.worksheetId;
                openWorksheetModal(worksheetId, 'Update');
            });
        }
    }

    upsertWorksheetModal.onResult(function (result, response) {
        if (response.responseText.action == 'Insert') {
            PubSub.publish('refresh_worksheet_list', { worksheetId: response.responseText.worksheetId, action: response.responseText.action });
        } else {
            PubSub.publish('refresh_worksheet', { worksheetId: response.responseText.worksheetId, action: response.responseText.action });
        }

        abp.notify.success(
            'Operation completed successfully.',
            response.responseText.action + ' Worksheet'
        );
    });

    function openWorksheetModal(worksheetId, actionType) {
        upsertWorksheetModal.open({
            worksheetId: worksheetId,
            actionType: actionType
        });
    }

    function openLinkWorksheetModal(worksheetId) {
        linkWorksheetModal.open({
            worksheetId: worksheetId
        });
    }

    function refreshWorksheetListWidget() {
        const url = `../Flex/Widget/WorksheetList/Refresh`;
        fetch(url)
            .then(response => response.text())
            .then(data => {
                document.getElementById('worksheet-info-widget-list').innerHTML = data;
                PubSub.publish('worksheet_list_refreshed');
            })
            .catch(error => {
                console.error('Error refreshing worksheet-info-list-widget:', error);
            });
    }

    PubSub.subscribe(
        'refresh_worksheet_list',
        (msg, data) => {
            refreshWorksheetListWidget();
        }
    );

    PubSub.subscribe(
        'worksheet_list_refreshed',
        (msg, data) => {
            bindActionButtons();
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
