(function () {
    const form = document.getElementById('otherConfigForm');
    const directApproval = form.elements['directApproval'];
    const electoralDistrictAddressType =
        form.elements['electoralDistrictAddressType'];
    const prefix = form.elements['prefix'];
    const suffixType = form.elements['suffixType'];
    const applicationFormId =
        document.getElementById('applicationFormId').value;
    const displayAddressChangeWarning = document.getElementById(
        'change-electoral-address-warning'
    );
    const saveButton = document.getElementById('btn-save-other-config');
    const cancelButton = document.getElementById('btn-cancel-other-config');
    const backButton = document.getElementById('btn-back-other-config');

    // Store last saved values
    let lastSavedValues = {
        directApproval: directApproval.checked,
        electoralDistrictAddressType: electoralDistrictAddressType.value,
        prefix: prefix.value,
        suffixType: suffixType.value
    };

    // Initially disable the save and cancel buttons
    saveButton.disabled = true;
    cancelButton.disabled = true;

    // Function to update Unity ID preview
    function updateUnityIdPreview() {
        const previewDiv = document.getElementById('unityIdPreview');
        const previewValue = document.getElementById('unityIdPreviewValue');
        const prefixValue = prefix.value.trim();
        const suffixTypeValue = suffixType.value;
        
        // Hide preview if no prefix or suffix type is not selected
        if (!prefixValue || !suffixTypeValue) {
            previewDiv.style.display = 'none';
            return;
        }
        
        let sampleId = '';
        
        // Generate sample based on suffix type
        if (suffixTypeValue === '1') { // Sequential Number
            sampleId = prefixValue + '00001';
        } else if (suffixTypeValue === '2') { // Submission Number
            sampleId = prefixValue + '4B2EA7CB';
        }
        
        if (sampleId) {
            previewValue.textContent = sampleId;
            previewDiv.style.display = 'block';
        } else {
            previewDiv.style.display = 'none';
        }
    }

    // Enable save and cancel buttons on any form input change
    form.addEventListener('change', function () {
        saveButton.disabled = false;
        cancelButton.disabled = false;
    });
    
    // Update preview when prefix or suffix type changes
    prefix.addEventListener('input', updateUnityIdPreview);
    suffixType.addEventListener('change', updateUnityIdPreview);
    
    // Initial preview update
    updateUnityIdPreview();

    // Show warning when electoralDistrictAddressType changes
    electoralDistrictAddressType.addEventListener('change', function () {
        displayAddressChangeWarning.style.display = 'block';
    });

    // Hide warning and disable buttons when cancel button is clicked
    cancelButton.addEventListener('click', function () {
        // Restore last saved values
        directApproval.checked = lastSavedValues.directApproval;
        electoralDistrictAddressType.value = lastSavedValues.electoralDistrictAddressType;
        prefix.value = lastSavedValues.prefix;
        suffixType.value = lastSavedValues.suffixType;
        
        // Update preview after restoring values
        updateUnityIdPreview();
        
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
        console.log('Save button clicked');
        console.log(event);
        console.log(
            electoralDistrictAddressType.value,
            prefix.value,
            suffixType.value
        );
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
                electoralDistrictAddressType:
                    electoralDistrictAddressType.value,
                prefix: prefix.value,
                suffixType: suffixType.value === "" ? null : suffixType.value,
            }),
            contentType: 'application/json',
        })
            .done(function () {
                // Update last saved values after successful save
                lastSavedValues = {
                    directApproval: directApproval.checked,
                    electoralDistrictAddressType: electoralDistrictAddressType.value,
                    prefix: prefix.value,
                    suffixType: suffixType.value
                };
                abp.notify.success('Other configuration saved successfully.');
            })
            .fail(function (error) {
                abp.notify.error('Failed to save other configuration.');
            })
            .always(function () {
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
