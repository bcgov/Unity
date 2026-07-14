(function ($) {
    const l = abp.localization.getResource('GrantManager');

    function debounce(func, wait) {
        let timeout;
        return function () {
            clearTimeout(timeout);
            timeout = setTimeout(func, wait);
        };
    }

    const $programDetailsForm = $('#ProgramDetailsForm');
    if (!$programDetailsForm.length) {
        return;
    }

    UnityCharacterCounter.init('#ProgramDetailsForm');

    const $programDetailsSaveButton = $('#ProgramDetailsSaveButton');
    const $programDetailsResetButton = $('#ProgramDetailsResetButton');
    const $displayNameInput = $('#ProgramDetailsDisplayName');
    const $divisionInput = $('#ProgramDetailsDivision');
    const $branchInput = $('#ProgramDetailsBranch');
    const $descriptionInput = $('#ProgramDetailsDescription');

    const originalProgramDetailsValues = {
        displayName: $displayNameInput.val().trim(),
        division: $divisionInput.val().trim(),
        branch: $branchInput.val().trim(),
        description: $descriptionInput.val().trim()
    };

    const getProgramDetailsValues = () => {
        return {
            displayName: $displayNameInput.val().trim(),
            division: $divisionInput.val().trim(),
            branch: $branchInput.val().trim(),
            description: $descriptionInput.val().trim()
        };
    };

    const hasProgramDetailsChanges = () => {
        const currentValues = getProgramDetailsValues();
        return currentValues.displayName !== originalProgramDetailsValues.displayName
            || currentValues.division !== originalProgramDetailsValues.division
            || currentValues.branch !== originalProgramDetailsValues.branch
            || currentValues.description !== originalProgramDetailsValues.description;
    };

    const updateProgramDetailsButtonStates = () => {
        const changed = hasProgramDetailsChanges();
        $programDetailsSaveButton.prop('disabled', !changed);
        $programDetailsResetButton.prop('disabled', !changed);
    };

    const debouncedProgramDetailsButtonUpdate = debounce(updateProgramDetailsButtonStates, 150);

    $programDetailsForm.on('input', 'input, textarea', function () {
        debouncedProgramDetailsButtonUpdate();
    });

    $programDetailsResetButton.on('click', function (e) {
        e.preventDefault();
        $displayNameInput.val(originalProgramDetailsValues.displayName);
        $divisionInput.val(originalProgramDetailsValues.division);
        $branchInput.val(originalProgramDetailsValues.branch);
        $descriptionInput.val(originalProgramDetailsValues.description);

        // Trigger input events to update character counters
        $displayNameInput.trigger('input');
        $divisionInput.trigger('input');
        $branchInput.trigger('input');
        $descriptionInput.trigger('input');

        updateProgramDetailsButtonStates();
        abp.notify.info(l('ProgramDetails:ChangesReset'));
    });

    updateProgramDetailsButtonStates();

    $programDetailsForm.on('submit', function (e) {
        e.preventDefault();

        if (!hasProgramDetailsChanges()) {
            abp.notify.info(l('ProgramDetails:NoChanges'));
            return;
        }

        const currentValues = getProgramDetailsValues();

        abp.ui.setBusy($programDetailsForm);
        unity.grantManager.settingManagement.programDetails
            .updateProgramDetails({
                displayName: currentValues.displayName,
                division: currentValues.division,
                branch: currentValues.branch,
                description: currentValues.description
            })
            .then(function () {
                originalProgramDetailsValues.displayName = currentValues.displayName;
                originalProgramDetailsValues.division = currentValues.division;
                originalProgramDetailsValues.branch = currentValues.branch;
                originalProgramDetailsValues.description = currentValues.description;
                updateProgramDetailsButtonStates();
                abp.notify.success(l('ProgramDetails:SaveSuccess'));
            })
            .catch(function (error) {
                abp.notify.error(error.message || l('ProgramDetails:SaveError'));
            })
            .always(function () {
                abp.ui.clearBusy($programDetailsForm);
            });
    });
})(jQuery);
