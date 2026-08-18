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

    const renewalLinkUri = document.getElementById('renewalLinkUri');
    const renewalLinkTitle = document.getElementById('renewalLinkTitle');
    const renewalLinkPublished = document.getElementById('renewalLinkPublished');
    const applicantMessage = document.getElementById('applicantMessage');
    const relatedLinksContainer = document.getElementById('relatedLinksContainer');
    const addRelatedLinkButton = document.getElementById('btn-add-related-link');

    const MAX_RELATED_LINKS = 8;
    const URL_PATTERN = /^https?:\/\/\S+$/i;
    const EXTERNAL_LINK_TYPE_RENEWAL = 2;
    const EXTERNAL_LINK_TYPE_RELATED = 1;

    const l = abp.localization.getResource('GrantManager');

    function isValidUrl(value) {
        return !!value && URL_PATTERN.test(value.trim());
    }

    function getFieldErrorElement(input) {
        return input.parentElement.querySelector('.field-error');
    }

    function clearFieldError(input) {
        const errorEl = getFieldErrorElement(input);
        if (errorEl) {
            errorEl.textContent = '';
        }
        input.classList.remove('is-invalid');
    }

    function setFieldError(input, message) {
        const errorEl = getFieldErrorElement(input);
        if (errorEl) {
            errorEl.textContent = message;
        }
        input.classList.add('is-invalid');
    }

    function collectRelatedLinkRows() {
        return Array.from(relatedLinksContainer.querySelectorAll('.related-link-row'));
    }

    function collectRelatedLinksSnapshot() {
        return collectRelatedLinkRows().map(function (row) {
            return {
                uri: row.querySelector('.related-link-uri').value,
                title: row.querySelector('.related-link-title').value,
                description: row.querySelector('.related-link-description').value,
                published: row.querySelector('.related-link-published').checked
            };
        });
    }

    function updateAddButtonState() {
        addRelatedLinkButton.disabled = collectRelatedLinkRows().length >= MAX_RELATED_LINKS;
    }

    function createRelatedLinkRow(data) {
        data = data || { uri: '', title: '', description: '', published: false };

        const row = document.createElement('div');
        row.className = 'related-link-row row mt-2';

        const uriCol = document.createElement('div');
        uriCol.className = 'col-12 col-md-4';
        const uriInput = document.createElement('input');
        uriInput.type = 'url';
        uriInput.className = 'form-control related-link-uri';
        uriInput.maxLength = 2048;
        uriInput.placeholder = 'https://...';
        uriInput.value = data.uri;
        const uriError = document.createElement('span');
        uriError.className = 'field-error text-danger small';
        uriCol.appendChild(uriInput);
        uriCol.appendChild(uriError);

        const titleCol = document.createElement('div');
        titleCol.className = 'col-12 col-md-3';
        const titleInput = document.createElement('input');
        titleInput.type = 'text';
        titleInput.className = 'form-control related-link-title';
        titleInput.maxLength = 255;
        titleInput.placeholder = l('ApplicationForms.Configuration:LinkDisplayName');
        titleInput.value = data.title;
        titleCol.appendChild(titleInput);

        const descCol = document.createElement('div');
        descCol.className = 'col-12 col-md-3';
        const descInput = document.createElement('input');
        descInput.type = 'text';
        descInput.className = 'form-control related-link-description';
        descInput.maxLength = 512;
        descInput.placeholder = l('ApplicationForms.Configuration:LinkDescription');
        descInput.value = data.description;
        descCol.appendChild(descInput);

        const toggleCol = document.createElement('div');
        toggleCol.className = 'col-6 col-md-1 d-flex align-items-center';
        const switchWrapper = document.createElement('div');
        switchWrapper.className = 'form-check unt-form-switch form-switch';
        const toggleInput = document.createElement('input');
        toggleInput.type = 'checkbox';
        toggleInput.className = 'form-check-input related-link-published';
        toggleInput.setAttribute('aria-label', l('ApplicationForms.Configuration:ShowOtherLinksInPortal'));
        toggleInput.style.cursor = 'pointer';
        toggleInput.checked = data.published;
        switchWrapper.appendChild(toggleInput);
        toggleCol.appendChild(switchWrapper);

        const removeCol = document.createElement('div');
        removeCol.className = 'col-6 col-md-1 d-flex align-items-center';
        const removeButton = document.createElement('button');
        removeButton.type = 'button';
        removeButton.className = 'btn btn-sm btn-outline-danger btn-remove-related-link';
        removeButton.setAttribute('aria-label', 'Remove Link');
        const removeIcon = document.createElement('i');
        removeIcon.className = 'fl fl-trash';
        removeButton.appendChild(removeIcon);
        removeCol.appendChild(removeButton);

        row.appendChild(uriCol);
        row.appendChild(titleCol);
        row.appendChild(descCol);
        row.appendChild(toggleCol);
        row.appendChild(removeCol);

        return row;
    }

    relatedLinksContainer.addEventListener('click', function (event) {
        const removeButton = event.target.closest('.btn-remove-related-link');
        if (!removeButton || !relatedLinksContainer.contains(removeButton)) {
            return;
        }

        const row = removeButton.closest('.related-link-row');
        if (!row) {
            return;
        }

        row.remove();
        updateAddButtonState();
        saveButton.disabled = false;
        cancelButton.disabled = false;
    });

    function rebuildRelatedLinkRows(links) {
        relatedLinksContainer.innerHTML = '';
        links.forEach(function (link) {
            relatedLinksContainer.appendChild(createRelatedLinkRow(link));
        });
        updateAddButtonState();
    }

    addRelatedLinkButton.addEventListener('click', function () {
        if (collectRelatedLinkRows().length >= MAX_RELATED_LINKS) {
            return;
        }
        relatedLinksContainer.appendChild(createRelatedLinkRow());
        updateAddButtonState();
        saveButton.disabled = false;
        cancelButton.disabled = false;
    });

    updateAddButtonState();

    function validateExternalLinksConfig() {
        let isValid = true;

        clearFieldError(renewalLinkUri);
        const renewalUriValue = renewalLinkUri.value.trim();
        if (renewalLinkPublished.checked && !isValidUrl(renewalUriValue)) {
            setFieldError(renewalLinkUri, l('ApplicationForms.Configuration.Errors:RenewalLinkRequiredForVisibility'));
            isValid = false;
        } else if (renewalUriValue && !isValidUrl(renewalUriValue)) {
            setFieldError(renewalLinkUri, l('ApplicationForms.Configuration.Errors:InvalidUrl'));
            isValid = false;
        }

        const rows = collectRelatedLinkRows();
        if (rows.length > MAX_RELATED_LINKS) {
            abp.notify.error(l('ApplicationForms.Configuration.Errors:MaxRelatedLinksReached'));
            isValid = false;
        }

        rows.forEach(function (row) {
            const uriInput = row.querySelector('.related-link-uri');
            const publishedInput = row.querySelector('.related-link-published');
            clearFieldError(uriInput);
            const value = uriInput.value.trim();
            if (publishedInput.checked && !isValidUrl(value)) {
                setFieldError(uriInput, l('ApplicationForms.Configuration.Errors:OtherLinkRequiredForVisibility'));
                isValid = false;
            } else if (value && !isValidUrl(value)) {
                setFieldError(uriInput, l('ApplicationForms.Configuration.Errors:InvalidUrl'));
                isValid = false;
            }
        });

        return isValid;
    }

    function buildExternalLinksConfigPayload() {
        const renewalUriValue = renewalLinkUri.value.trim();

        return {
            renewalLink: renewalUriValue ? {
                uri: renewalUriValue,
                title: renewalLinkTitle.value,
                published: renewalLinkPublished.checked,
                externalLinkType: EXTERNAL_LINK_TYPE_RENEWAL,
                order: 0
            } : null,
            relatedLinks: collectRelatedLinkRows()
                .map(function (row, index) {
                    return {
                        uri: row.querySelector('.related-link-uri').value.trim(),
                        title: row.querySelector('.related-link-title').value,
                        description: row.querySelector('.related-link-description').value,
                        published: row.querySelector('.related-link-published').checked,
                        externalLinkType: EXTERNAL_LINK_TYPE_RELATED,
                        order: index
                    };
                })
                .filter(function (link) { return link.uri; }),
            applicantMessage: applicantMessage.value
        };
    }

    // Store last saved values
    let lastSavedValues = {
        directApproval: directApproval.checked,
        electoralDistrictAddressType: electoralDistrictAddressType.value,
        prefix: prefix.value,
        suffixType: suffixType.value,
        renewalLinkUri: renewalLinkUri.value,
        renewalLinkTitle: renewalLinkTitle.value,
        renewalLinkPublished: renewalLinkPublished.checked,
        applicantMessage: applicantMessage.value,
        relatedLinks: collectRelatedLinksSnapshot()
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
        renewalLinkUri.value = lastSavedValues.renewalLinkUri;
        renewalLinkTitle.value = lastSavedValues.renewalLinkTitle;
        renewalLinkPublished.checked = lastSavedValues.renewalLinkPublished;
        applicantMessage.value = lastSavedValues.applicantMessage;
        rebuildRelatedLinkRows(lastSavedValues.relatedLinks);
        clearFieldError(renewalLinkUri);

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
        if (isSaving || saveButton.disabled) {
            event.preventDefault();
            return;
        }

        if (!validateExternalLinksConfig()) {
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
            .then(function () {
                // Only save external links config once other-config succeeds,
                // keeping the two saves sequential.
                return abp.ajax({
                    url: `/api/app/application-form/${applicationFormId}/external-links-config`,
                    type: 'PATCH',
                    data: JSON.stringify(buildExternalLinksConfigPayload()),
                    contentType: 'application/json',
                });
            })
            .then(function () {
                // Clear dirty state only after both saves succeed.
                lastSavedValues = {
                    directApproval: directApproval.checked,
                    electoralDistrictAddressType: electoralDistrictAddressType.value,
                    prefix: prefix.value,
                    suffixType: suffixType.value,
                    renewalLinkUri: renewalLinkUri.value,
                    renewalLinkTitle: renewalLinkTitle.value,
                    renewalLinkPublished: renewalLinkPublished.checked,
                    applicantMessage: applicantMessage.value,
                    relatedLinks: collectRelatedLinksSnapshot()
                };
                abp.notify.success('Other configuration saved successfully.');
                resetFormState();
            })
            .catch(function () {
                // Keep the form dirty so the user can retry after a partial failure.
                abp.notify.error('Failed to save configuration.');
                saveButton.disabled = false;
                cancelButton.disabled = false;
            })
            .then(function () {
                isSaving = false;
            });
    });

    function resetFormState() {
        saveButton.disabled = true;
        cancelButton.disabled = true;
        displayAddressChangeWarning.style.display = 'none';
    }
})();
