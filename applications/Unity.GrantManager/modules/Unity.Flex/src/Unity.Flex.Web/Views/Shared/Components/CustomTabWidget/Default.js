$(function () {
    $('body').on('click', '.custom-tab-save', function () {
        window.alert('custom save clicked');
    });

    function buildFormData(projectInfoObj, input) {
        // This will not work if the culture is different and uses a different decimal separator
        projectInfoObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');

        if (isNumberField(input)) {
            if (projectInfoObj[input.name.split(".")[1]] == '') {
                projectInfoObj[input.name.split(".")[1]] = 0;
            } else if (projectInfoObj[input.name.split(".")[1]] > getMaxNumberField(input)) {
                projectInfoObj[input.name.split(".")[1]] = getMaxNumberField(input);
            }
        }
        else if (projectInfoObj[input.name.split(".")[1]] == '') {
            projectInfoObj[input.name.split(".")[1]] = null;
        }
    }

    function updateProjectInfo(applicationId, projectInfoObj) {
        try {
            unity.grantManager.grantApplications.grantApplication
                .updateProjectInfo(applicationId, projectInfoObj)
                .done(function () {
                    abp.notify.success(
                        'The project info has been updated.'
                    );
                    $('#saveProjectInfoBtn').prop('disabled', true);
                    PubSub.publish('project_info_saved', projectInfoObj);
                });
        }
        catch (error) {
            console.log(error);
            $('#saveProjectInfoBtn').prop('disabled', false);
        }
    }

    PubSub.subscribe(
        'fields_ProjectInfo',
        () => {
            enableProjectInfoSaveBtn();
        }
    );
});