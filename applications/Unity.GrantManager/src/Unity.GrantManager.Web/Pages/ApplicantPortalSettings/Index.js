(function ($) {

    const l = abp.localization.getResource('GrantManager');

    const $form = $('#PortalStatusForm');
    const $saveButton = $('#SaveButton');
    const $resetButton = $('#ResetButton');

    // Store original values on page load
    const originalValues = new Map();

    let portalStatusTable = new DataTable("#PortalStatusTable", {
        paging: false,
        ordering: false,
        info: false
    });

    // Capture original values after DataTable initialization
    // Use DataTable's rows().every() to ensure all rows are captured, even if dynamically loaded
    portalStatusTable.rows().every(function () {
        const $row = $(this.node());
        const id = $row.find('input[type="hidden"]').val();
        const externalStatus = $row.find('input[type="text"]').val().trim();
        originalValues.set(id, externalStatus);
    });

    // Check if any values have changed
    function hasChanges() {
        let changed = false;
        portalStatusTable.$('tbody tr').each(function () {
            const $row = $(this);
            const id = $row.find('input[type="hidden"]').val();
            const currentValue = $row.find('input[type="text"]').val().trim();
            if (originalValues.get(id) !== currentValue) {
                changed = true;
                return false;
            }
        });
        return changed;
    }

    // Update button states
    function updateButtonStates() {
        const changed = hasChanges();
        $saveButton.prop('disabled', !changed);
        $resetButton.prop('disabled', !changed);
    }

    // Debounce utility to limit how often updateButtonStates is called
    function debounce(func, wait) {
        let timeout;
        return function () {
            clearTimeout(timeout);
            timeout = setTimeout(func, wait);
        };
    }

    // Debounced version of updateButtonStates
    const debouncedUpdateButtonStates = debounce(updateButtonStates, 150);

    // Listen for input changes, using debounced handler
    $form.on('input', '#PortalStatusTable input[type="text"]', function () {
        debouncedUpdateButtonStates();
    });

    // Reset button handler
    $resetButton.on('click', function (e) {
        e.preventDefault();

        portalStatusTable.$('tbody tr').each(function () {
            const $row = $(this);
            const id = $row.find('input[type="hidden"]').val();
            const originalValue = originalValues.get(id);
            $row.find('input[type="text"]').val(originalValue);
        });

        updateButtonStates();
        abp.notify.info(l('ApplicantPortalSettings:ChangesReset'));
    });

    // Initialize button states
    updateButtonStates();

    $form.on('submit', function (e) {
        e.preventDefault();

        const statuses = [];
        let hasValidationError = false;

        portalStatusTable.$('tbody tr').each(function () {
            const $row = $(this);
            const id = $row.find('input[type="hidden"]').val();
            const externalStatus = $row.find('input[type="text"]').val().trim();

            if (!externalStatus) {
                abp.notify.warn(l('ApplicantPortalSettings:ValidationRequired'));
                hasValidationError = true;
                return false;
            }

            // Only include if value has changed
            if (originalValues.get(id) !== externalStatus) {
                statuses.push({
                    id: id,
                    externalStatus: externalStatus
                });
            }
        });

        if (hasValidationError) {
            return;
        }

        // Check if there are any changes
        if (statuses.length === 0) {
            abp.notify.info(l('ApplicantPortalSettings:NoChanges'));
            return;
        }

        abp.ui.setBusy($form);

        unity.grantManager.grantApplications.applicationStatus
            .updateExternalStatusLabels({ statuses: statuses })
            .then(function () {
                abp.notify.success(l('ApplicantPortalSettings:SaveSuccess'));

                // Update original values after successful save
                statuses.forEach(function(status) {
                    originalValues.set(status.id, status.externalStatus);
                });

                // Update button states after save
                updateButtonStates();
            })
            .catch(function (error) {
                abp.notify.error(error.message || l('ApplicantPortalSettings:SaveError'));
            })
            .always(function () {
                abp.ui.clearBusy($form);
            });
    });

})(jQuery);
