(function ($) {
    const l = abp.localization.getResource('GrantManager');

    const $manageStatusesMenuItem = $('#manage-statuses-menu-item');
    const $programDetailsMenuItem = $('#program-details-menu-item');
    const $portalStatusPanel = $('#portal-status-div');
    const $programDetailsPanel = $('#program-details-div');

    function setActiveSection(sectionName) {
        const isManageStatuses = sectionName === 'manage-statuses';

        $manageStatusesMenuItem.toggleClass('active', isManageStatuses);
        $portalStatusPanel.toggleClass('active', isManageStatuses).toggleClass('d-none', !isManageStatuses);

        if ($programDetailsMenuItem.length && $programDetailsPanel.length) {
            $programDetailsMenuItem.toggleClass('active', !isManageStatuses);
            $programDetailsPanel.toggleClass('active', !isManageStatuses).toggleClass('d-none', isManageStatuses);
        }
    }

    $manageStatusesMenuItem.on('click', function () {
        setActiveSection('manage-statuses');
    });

    $programDetailsMenuItem.on('click', function () {
        setActiveSection('program-details');
    });

    setActiveSection('manage-statuses');

    const $statusForm = $('#PortalStatusForm');
    const $statusSaveButton = $('#SaveButton');
    const $statusResetButton = $('#ResetButton');
    const originalStatusValues = new Map();

    const portalStatusTable = new DataTable('#PortalStatusTable', {
        paging: false,
        info: false,
        order: [[0, 'asc']],
        columnDefs: [{
            targets: [1, 2],
            orderable: false
        }]
    });

    portalStatusTable.rows().every(function () {
        const $row = $(this.node());
        const id = $row.find('input[type="hidden"]').val();
        const externalStatus = $row.find('input[id$=".ExternalStatus"]').val().trim();
        const notifiedStatus = $row.find('input[id$=".NotifiedStatus"]').val().trim();
        originalStatusValues.set(id, { externalStatus: externalStatus, notifiedStatus: notifiedStatus });
    });

    function hasStatusChanges() {
        let changed = false;
        portalStatusTable.$('tbody tr').each(function () {
            const $row = $(this);
            const id = $row.find('input[type="hidden"]').val();
            const externalStatus = $row.find('input[id$=".ExternalStatus"]').val().trim();
            const notifiedStatus = $row.find('input[id$=".NotifiedStatus"]').val().trim();
            const originalValue = originalStatusValues.get(id);
            if (!originalValue || originalValue.externalStatus !== externalStatus || originalValue.notifiedStatus !== notifiedStatus) {
                changed = true;
                return false;
            }
        });

        return changed;
    }

    function updateStatusButtonStates() {
        const changed = hasStatusChanges();
        $statusSaveButton.prop('disabled', !changed);
        $statusResetButton.prop('disabled', !changed);
    }

    function debounce(func, wait) {
        let timeout;
        return function () {
            clearTimeout(timeout);
            timeout = setTimeout(func, wait);
        };
    }

    const debouncedStatusButtonUpdate = debounce(updateStatusButtonStates, 150);

    $statusForm.on('input', '#PortalStatusTable input[type="text"]', function () {
        debouncedStatusButtonUpdate();
    });

    $statusResetButton.on('click', function (e) {
        e.preventDefault();

        portalStatusTable.$('tbody tr').each(function () {
            const $row = $(this);
            const id = $row.find('input[type="hidden"]').val();
            const originalValue = originalStatusValues.get(id);
            $row.find('input[id$=".ExternalStatus"]').val(originalValue.externalStatus);
            $row.find('input[id$=".NotifiedStatus"]').val(originalValue.notifiedStatus);
        });

        updateStatusButtonStates();
        abp.notify.info(l('ApplicantPortalSettings:ChangesReset'));
    });

    updateStatusButtonStates();

    $statusForm.on('submit', function (e) {
        e.preventDefault();

        const statuses = [];
        let hasValidationError = false;

        portalStatusTable.$('tbody tr').each(function () {
            const $row = $(this);
            const id = $row.find('input[type="hidden"]').val();
            const externalStatus = $row.find('input[id$=".ExternalStatus"]').val().trim();

            if (!externalStatus) {
                abp.notify.warn(l('ApplicantPortalSettings:ValidationRequired'));
                hasValidationError = true;
                return false;
            }

            const notifiedStatus = $row.find('input[id$=".NotifiedStatus"]').val().trim();
            const originalValue = originalStatusValues.get(id);
            if (originalValue.externalStatus !== externalStatus || originalValue.notifiedStatus !== notifiedStatus) {
                statuses.push({
                    id: id,
                    externalStatus: externalStatus,
                    notifiedStatus: notifiedStatus
                });
            }
        });

        if (hasValidationError) {
            return;
        }

        if (statuses.length === 0) {
            abp.notify.info(l('ApplicantPortalSettings:NoChanges'));
            return;
        }

        abp.ui.setBusy($statusForm);
        unity.grantManager.grantApplications.applicationStatus
            .updateExternalStatusLabels({ statuses: statuses })
            .then(function () {
                abp.notify.success(l('ApplicantPortalSettings:SaveSuccess'));
                statuses.forEach(function (status) {
                    originalStatusValues.set(status.id, {
                        externalStatus: status.externalStatus,
                        notifiedStatus: status.notifiedStatus
                    });
                });
                updateStatusButtonStates();
            })
            .catch(function (error) {
                abp.notify.error(error.message || l('ApplicantPortalSettings:SaveError'));
            })
            .always(function () {
                abp.ui.clearBusy($statusForm);
            });
    });

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

    function getProgramDetailsValues() {
        return {
            displayName: $displayNameInput.val().trim(),
            division: $divisionInput.val().trim(),
            branch: $branchInput.val().trim(),
            description: $descriptionInput.val().trim()
        };
    }

    function hasProgramDetailsChanges() {
        const currentValues = getProgramDetailsValues();
        return currentValues.displayName !== originalProgramDetailsValues.displayName
            || currentValues.division !== originalProgramDetailsValues.division
            || currentValues.branch !== originalProgramDetailsValues.branch
            || currentValues.description !== originalProgramDetailsValues.description;
    }

    function updateProgramDetailsButtonStates() {
        const changed = hasProgramDetailsChanges();
        $programDetailsSaveButton.prop('disabled', !changed);
        $programDetailsResetButton.prop('disabled', !changed);
    }

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
        abp.notify.info(l('ApplicantPortalSettings:ChangesReset'));
    });

    updateProgramDetailsButtonStates();

    $programDetailsForm.on('submit', function (e) {
        e.preventDefault();

        if (!hasProgramDetailsChanges()) {
            abp.notify.info(l('ApplicantPortalSettings:NoChanges'));
            return;
        }

        const currentValues = getProgramDetailsValues();

        abp.ui.setBusy($programDetailsForm);
        unity.grantManager.grantApplications.applicationStatus
            .updateApplicantPortalProgramDetails({
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
                abp.notify.success(l('ApplicantPortalSettings:ProgramDetailsSaveSuccess'));
            })
            .catch(function (error) {
                abp.notify.error(error.message || l('ApplicantPortalSettings:ProgramDetailsSaveError'));
            })
            .always(function () {
                abp.ui.clearBusy($programDetailsForm);
            });
    });
})(jQuery);
