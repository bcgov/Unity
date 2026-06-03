/* ScoresheetConfiguration JS - local copy for ConfigurationManagement */

globalThis.importScoresheetFile = function (inputId) {
    let input = document.getElementById(inputId);
    let file = input.files[0];
    if (!file) return;

    const maxFileSize = decodeURIComponent($("#MaxFileSize").val());
    if ((file.size * 0.000001) > maxFileSize) {
        input.value = null;
        return abp.notify.error('File size exceeds ' + maxFileSize + 'MB', 'Error');
    }

    let formData = new FormData();
    formData.append("file", file);

    $.ajax({
        url: "/api/app/scoresheet/import",
        data: formData,
        processData: false,
        contentType: false,
        type: "POST",
        success: function (data) {
            abp.notify.success(data.responseText, 'Scoresheet Import Is Successful');
            PubSub.publish('refresh_scoresheet_list', { scoresheetId: null });
            input.value = null;
        },
        error: function () {
            abp.notify.error('Import failed.', 'Scoresheet Import Error');
            input.value = null;
        }
    });
};

(function ($) {
    $(function () {
        $('#scoresheet_import_upload_btn').on('click', function () {
            $('#scoresheet_import_upload').trigger('click');
        });
    });
    const scoresheetModal = new abp.ModalManager({
        viewUrl: 'ScoresheetConfiguration/ScoresheetModal'
    });

    const cloneScoresheetModal = new abp.ModalManager({
        viewUrl: 'ScoresheetConfiguration/CloneScoresheetModal'
    });

    const publishScoresheetModal = new abp.ModalManager({
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

    // Exposed globally — called from inline onclick attributes in Scoresheet component HTML
    globalThis.openScoresheetModal = function (scoresheetId, actionType) {
        scoresheetToEditId = scoresheetId;
        scoresheetModal.open({
            scoresheetId: scoresheetId,
            actionType: actionType
        });
    };

    globalThis.openCloneScoresheetModal = function (scoresheetId) {
        scoresheetToEditId = scoresheetId;
        cloneScoresheetModal.open({
            scoresheetId: scoresheetId
        });
    };

    globalThis.openPublishScoresheetModal = function (scoresheetId) {
        scoresheetToEditId = scoresheetId;
        publishScoresheetModal.open({
            scoresheetId: scoresheetId
        });
    };

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

    PubSub.subscribe(
        'refresh_scoresheet_list',
        (msg, data) => {
            refreshScoresheetInfoWidget(data.scoresheetId);
        }
    );
})(jQuery);