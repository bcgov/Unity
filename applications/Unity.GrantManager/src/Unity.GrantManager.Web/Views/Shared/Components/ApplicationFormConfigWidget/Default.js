(function () {
    const form = document.getElementById('otherConfigForm');
    const directApproval = form.elements['directApproval'];
    const electoralDistrictAddressType = form.elements['electoralDistrictAddressType'];
    const applicationFormId = document.getElementById('applicationFormId').value;
    const displayAddressChangeWarning = document.getElementById('change-electoral-address-warning');
    const saveButton = document.getElementById('btn-save-other-config');
    const cancelButton = document.getElementById('btn-cancel-other-config');
    const backButton = document.getElementById('btn-back-other-config');

    // Initially disable the save and cancel buttons
    saveButton.disabled = true;
    cancelButton.disabled = true;

    // Enable save and cancel buttons on any form input change
    form.addEventListener('change', function () {
        saveButton.disabled = false;
        cancelButton.disabled = false;
    });

    // Show warning when electoralDistrictAddressType changes
    electoralDistrictAddressType.addEventListener('change', function () {
        displayAddressChangeWarning.style.display = 'block';
    });

    // Hide warning and disable buttons when cancel button is clicked
    cancelButton.addEventListener('click', function () {
        form.reset();
        resetFormState();
    });

    // Handle back button click
    backButton.addEventListener('click', function (e) {
        e.preventDefault();
        location.href = '/ApplicationForms';
    });

    // Debounce flag to prevent duplicate saves
    let isSaving = false;

    saveButton.addEventListener('click', function (event) {
        if (isSaving || saveButton.disabled) {
            event.preventDefault();
            return;
        }
        isSaving = true;
        saveButton.disabled = true; // Disable immediately to prevent double click
        cancelButton.disabled = true;

        abp.ajax({
            url: `/api/app/application-form/${applicationFormId}/other-config`,
            type: 'PATCH',
            data: JSON.stringify({
                isDirectApproval: directApproval.checked,
                electoralDistrictAddressType: electoralDistrictAddressType.value
            }),
            contentType: 'application/json'
        }).done(function () {
            abp.notify.success('Other configuration saved successfully.');
        }).fail(function (error) {
            abp.notify.error('Failed to save other configuration.');
        }).always(function () {
            resetFormState();
            isSaving = false;
        });
    });

    function resetFormState() {        
        saveButton.disabled = true;
        cancelButton.disabled = true;
        displayAddressChangeWarning.style.display = 'none';
    }
})();
