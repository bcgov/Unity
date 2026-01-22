$(function () {
    loadCardsFromService();
    const NotificationUiElements = {
        settingForm: $("#NotificationsSettingsForm"),
        saveButton: $("#NotificationsSaveButton"),
        discardButton: $("#NotificationsDiscardButton")
    }

    let initialFormState = NotificationUiElements.settingForm.serialize();

    function checkFormChanges() {
        let currentFormState = NotificationUiElements.settingForm.serialize();
        let isFormChanged = currentFormState !== initialFormState;

        NotificationUiElements.saveButton.prop('disabled', !isFormChanged);
        NotificationUiElements.discardButton.prop('disabled', !isFormChanged);
    }

    NotificationUiElements.settingForm.on('input change', function () {
        checkFormChanges();
    });

    NotificationUiElements.settingForm.on('submit', function (event) {
        event.preventDefault();

        if (!$(this).valid()) {
            return;
        }

        let form = $(this).serializeFormToObject();
        unity.notifications.emailNotifications.emailNotification.updateSettings(form).then(function (result) {
            $(document).trigger("AbpSettingSaved");
            initialFormState = NotificationUiElements.settingForm.serialize();
            checkFormChanges();
        });

    });

    NotificationUiElements.discardButton.on('click', function () {
        NotificationUiElements.settingForm[0].reset();
        initialFormState = NotificationUiElements.settingForm.serialize();
        checkFormChanges();
    });

    checkFormChanges();

    let editorInstances = {};

    // Utility function for debouncing
    function debounce(func, delay) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => func.apply(this, args), delay);
        };
    }

    // Utility functions for template operations
    function extractFormData(formDataArray) {
        return {
            name: formDataArray[0].value,
            sendFrom: formDataArray[1].value,
            subject: formDataArray[2].value
        };
    }

    function buildTemplatePayload(data, editor) {
        return JSON.stringify({
            name: data.name,
            description: "",
            subject: data.subject,
            bodyText: editor.getContent({ format: 'text' }),
            bodyHTML: editor.getContent(),
            sendFrom: data.sendFrom
        });
    }

    function handleSuccess(message) {
        abp.notify.success(message);
        $("#cardContainer").empty();
        loadCardsFromService();
        return true;
    }

    function handleError(message) {
        abp.notify.error(message);
        return false;
    }

    function onSaveTemplateSuccess() {
        handleSuccess('Template saved successfully.');
    }

    function onSaveTemplateError() {
        handleError('Failed to save template.');
    }

    function saveTemplate(payload) {
        $.ajax({
            url: `/api/app/template`,
            method: 'POST',
            contentType: 'application/json',
            data: payload,
            success: onSaveTemplateSuccess,
            error: onSaveTemplateError
        });
    }

    function updateTemplate(id, payload) {
        $.ajax({
            url: `/api/app/template/${id}/template`,
            method: 'PUT',
            contentType: 'application/json',
            data: payload,
            success: onSaveTemplateSuccess,
            error: onSaveTemplateError
        });
    }

    function checkTemplateNameUnique(name, currentId, callback) {
        $.ajax({
            url: `/api/app/template/template-by-name?name=${encodeURIComponent(name)}`,
            type: 'GET',
            success: function (response) {
                const wrapperId = `cardWrapper-${currentId}`;
                const isSameAsCurrent = !currentId.includes('temp') && name === $(`#${wrapperId}`).data('original-name');
                let isExist = false;
                if (response?.id) {
                    isExist = true;
                }
                callback(!isExist || isSameAsCurrent);
            },
            error: function () {
                callback(false);
            }
        });
    }

    function getTemplateVariables(dropdownItems) {
        $.ajax({
            url: `/api/app/template/template-variables`,
            type: 'GET',
            success: function (response) {
                $.map(response, function (item) {
                    dropdownItems.push({
                        text: item.name,
                        value: item.token
                    });
                });
            },
            error: function () {
                // Handle error silently
            }
        });
    }

    function showDeleteConfirmation(id, wrapperId) {
        const swalOptions = {
            title: "Delete Template",
            text: "Are you sure you want to delete this template?",
            showCancelButton: true,
            confirmButtonText: "Confirm",
            customClass: {
                confirmButton: 'btn btn-primary',
                cancelButton: 'btn btn-secondary'
            }
        };

        Swal.fire(swalOptions).then(function(result) {
            handleDeleteConfirmation(result, id, wrapperId);
        });
    }

    function handleDeleteConfirmation(result, id, wrapperId) {
        if (!result.isConfirmed) return;
        deleteTemplate(id, wrapperId);
    }

    function handleDeleteSuccess(wrapperId) {
        return function() {
            $(`#${wrapperId}`).remove();
            abp.notify.success('Template deleted successfully.');
        };
    }

    function handleDeleteError() {
        abp.notify.error('Error deleting the template.');
    }

    function deleteTemplate(id, wrapperId) {
        $.ajax({
            url: `/api/app/template/${id}/template`,
            type: 'DELETE',
            success: handleDeleteSuccess(wrapperId),
            error: handleDeleteError
        });
    }

    // Helper functions moved outside of createCard to reduce complexity
    function getCardLastEditedDate(data) {
        if (data?.lastModificationTime) {
            return new Date(data.lastModificationTime).toLocaleDateString('en-CA');
        } else if (data?.creationTime) {
            return new Date(data.creationTime).toLocaleDateString('en-CA');
        } else {
            return new Date().toLocaleDateString('en-CA');
        }
    }

    function generateCardHtml(cardConfig) {
        const { 
            data, 
            elementIds, 
            displayInfo, 
            isPopulated 
        } = cardConfig;
        
        const disabled = isPopulated ? 'disabled' : '';
        const cardDataId = data?.id?.toString() || elementIds.wrapperId;
        
        return `
        <div class="card mb-3 shadow-sm" id="${elementIds.wrapperId}" data-id="${cardDataId}">
            <div class="card-header d-flex justify-content-between align-items-center">
                <strong class="template-title">${data?.name || 'Untitled Template'}</strong>
                <div class="d-flex align-items-center gap-3">
                    <div class="text-muted small text-end me-3">
                        <div>Type<br /><span class="fw-normal">${displayInfo.type}</span></div>
                    </div>
                    <div class="text-muted small text-end me-3">
                        <div>Last Edited<br /><span class="fw-normal">${displayInfo.lastEdited}</span></div>
                    </div>
                    <button class="btn btn-sm btn-link text-decoration-none" type="button"
                        data-bs-toggle="collapse" data-bs-target="#${elementIds.cardId}" aria-expanded="false"
                        aria-controls="${elementIds.cardId}">
                        <i class="unt-icon-sm fa-solid fa-chevron-down"></i>
                    </button>
                </div>
            </div>
            <div class="collapse" id="${elementIds.cardId}">
                <div class="card-body">
                    <form id="${elementIds.formId}">
                        <div class="mb-3">
                            <label class="form-label">Template Name</label>
                            <input type="text" class="form-control form-input" name="templateName" value="${data?.name || ''}" ${disabled} required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Send From</label>
                            <input type="text" class="form-control form-input" name="sendFrom" value="${data?.sendFrom || ''}" ${disabled} required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Subject</label>
                            <input type="text" class="form-control form-input" name="subject" value="${data?.subject || ''}" ${disabled} required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Body</label>
                            <textarea id="${elementIds.editorId}"></textarea>
                        </div>
                         <div class="mb-3">
                         <p><b>NOTE</b>:<span class="note-text"> Selecting text will let you customize it: replace it with a variable, make it bold, italic, change the alignment, add a link, create a list, etc.</span></p>
                         </div>
                         <div class="mb-3">
                         <hr>
                         <button type="button" class="btn btn-outline-primary deleteCardBtn">X DELETE THIS TEMPLATE</button>
                         <hr>
                         </div>
                        <div class="d-flex gap-2">
                            ${isPopulated ? `<button type="submit" class="btn btn-primary  saveBtn" disabled>SAVE CHANGES</button>` : `<button type="submit" class="btn btn-primary saveBtn">SAVE CHANGES</button>`}
                            <button type="button" class="btn btn-outline-secondary discardBtn d-none">DISCARD CHANGES</button>
                            ${isPopulated ? `<button type="button" class="btn btn-secondary editBtn">EDIT THIS TEMPLATE</button>` : ''}
                        </div>
                    </form>
                </div>
            </div>
        </div>
    `;
    }

    function initializeEditor(editorId, id, data, isPopulated, dropdownItems) {        
        if (tinymce.get(editorId)) {
            tinymce.get(editorId).remove();
        }

        tinymce.init({
            license_key: 'gpl',
            selector: `#${editorId}`,
            plugins: 'lists link image preview code',
            toolbar: 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist | link image | code preview | variablesDropdownButton',
            statusbar: false,
            promotion: false,
            content_css: false,
            skin: false,
            setup: function (editor) {                
                setupEditor(editor, id, editorId, data, isPopulated, dropdownItems);
            }
        });
    }

    function createMenuItems(dropdownItems, editor) {
        return dropdownItems.map(item => ({
            type: 'menuitem',
            text: item.text,
            onAction: () => {
                editor.insertContent(`{{${item.value}}}`);
            }
        }));
    }

    function fetchVariablesMenuItems(dropdownItems, editor) {
        return function (callback) {
            const items = createMenuItems(dropdownItems, editor);
            callback(items);
        };
    }

    function setupEditor(editor, id, editorId, data, isPopulated, dropdownItems) {
        editor.ui.registry.addMenuButton('variablesDropdownButton', {
            text: 'VARIABLES',
            fetch: fetchVariablesMenuItems(dropdownItems, editor)
        });

        editor.on('init', function () {
            editor.mode.set(isPopulated ? 'readonly' : 'design');
            if (data?.bodyHTML) {
                editor.setContent(data.bodyHTML);
            }
            editorInstances[id] = editor;            
        });
    }

    function setupCardEventHandlers(cardData) {
        const { id, formId, cardId, wrapperId, editorId, data, isPopulated, dropdownItems } = cardData;
        
        setupTemplateNameValidation(formId, wrapperId, id);
        setupFormSubmission(formId, id);
        setupCollapseHandlers(cardId, wrapperId);
        setupEditDiscardHandlers(wrapperId, formId, editorId, id, data, dropdownItems);
        setupDeleteHandler(wrapperId, id, isPopulated);
    }

    function setupTemplateNameValidation(formId, wrapperId, id) {
        const debouncedValidation = debounce(function (templateInput, newTitle) {
            checkTemplateNameUnique(newTitle, id, function (isUnique) {
                toggleTemplateNameValidation(templateInput, formId, isUnique);
            });
        }, 250);

        $(`#${formId} input[name="templateName"]`).on('input', function () {
            const templateInput = $(this);
            const newTitle = templateInput.val().trim() || 'Untitled Template';
            $(`#${wrapperId} .template-title`).text(newTitle);

            debouncedValidation(templateInput, newTitle);
        });
    }

    function toggleTemplateNameValidation(templateInput, formId, isUnique) {
        if (!isUnique) {
            templateInput.addClass("is-invalid");
            if (!$(`#${formId} .template-name-feedback`).length) {
                templateInput.after(`<div class="invalid-feedback template-name-feedback">Template name must be unique.</div>`);
            }
            $(`#${formId} .saveBtn`).prop("disabled", true);
        } else {
            templateInput.removeClass("is-invalid");
            $(`#${formId} .template-name-feedback`).remove();
            $(`#${formId} .saveBtn`).prop("disabled", false);
        }
    }

    function setupFormSubmission(formId, id) {
        $(`#${formId}`).on("submit", function (e) {
            e.preventDefault();
            
            const formDataArray = $(this).serializeArray();
            const formData = extractFormData(formDataArray);
            const editor = editorInstances[id];
            const payload = buildTemplatePayload(formData, editor);

            if (id.includes("temp")) {
                saveTemplate(payload);
            } else {
                updateTemplate(id, payload);
            }
        });
    }

    function setupCollapseHandlers(cardId, wrapperId) {
        $(`#${cardId}`).on('show.bs.collapse', function () {
            $(`#${wrapperId} .btn[data-bs-target="#${cardId}"] i`)
                .removeClass('fa-chevron-down')
                .addClass('fa-chevron-up');
        });

        $(`#${cardId}`).on('hide.bs.collapse', function () {
            $(`#${wrapperId} .btn[data-bs-target="#${cardId}"] i`)
                .removeClass('fa-chevron-up')
                .addClass('fa-chevron-down');
        });
    }

    function setupEditDiscardHandlers(wrapperId, formId, editorId, id, data, dropdownItems) {
        $(`#${wrapperId}`).on("click", ".editBtn", function () {
            handleEditClick(wrapperId, editorId, id, data, dropdownItems);
        });

        $(`#${wrapperId}`).on("click", ".discardBtn", function () {
            handleDiscardClick(wrapperId, formId, editorId, id, data, dropdownItems);
        });
    }

    function handleEditClick(wrapperId, editorId, id, data, dropdownItems) {
        const currentEditor = editorInstances[id];
        currentEditor.destroy();

        initializeEditor(editorId, id, data, false, dropdownItems);

        const card = $(`#${wrapperId}`);
        card.find(".form-input").prop('disabled', false);
        card.find(".saveBtn").prop('disabled', false);
        card.find(".discardBtn").removeClass("d-none");
        card.find(".editBtn").addClass("d-none");
    }

    function handleDiscardClick(wrapperId, formId, editorId, id, data, dropdownItems) {
        const form = $(`#${formId}`)[0];
        form.reset();
        const currentEditor = editorInstances[id];
        currentEditor.destroy();

        initializeEditor(editorId, id, data, true, dropdownItems);

        $(`#${wrapperId} .form-input`).prop('disabled', true);
        $(`#${wrapperId} .saveBtn`).prop('disabled', true);
        $(`#${wrapperId} .discardBtn`).addClass("d-none");
        $(`#${wrapperId} .editBtn`).removeClass("d-none");
    }

    function setupDeleteHandler(wrapperId, id, isPopulated) {
        $(`#${wrapperId}`).on("click", ".deleteCardBtn", function () {
            if (isPopulated) {
                showDeleteConfirmation(id, wrapperId);
            } else {
                $(`#${wrapperId}`).remove();
            }
        });
    }

    function createCard(data = null) {
        const isPopulated = data !== null;
        const id = data?.id?.toString() || generateTempId();
        const cardId = `collapseDetails-${id}`;
        const formId = `form-${id}`;
        const wrapperId = `cardWrapper-${id}`;
        const editorId = `editor-${id}`;
        const type = data?.type || 'Automatic';
        const lastEdited = getCardLastEditedDate(data);
        const dropdownItems = [];
        
        getTemplateVariables(dropdownItems);

        const cardConfig = {
            data: data,
            elementIds: {
                cardId: cardId,
                formId: formId,
                wrapperId: wrapperId,
                editorId: editorId
            },
            displayInfo: {
                type: type,
                lastEdited: lastEdited
            },
            isPopulated: isPopulated
        };

        const cardHtml = generateCardHtml(cardConfig);

        $("#cardContainer").append(cardHtml);
        $(`#${wrapperId}`).data('original-name', data?.name || '');

        initializeEditor(editorId, id, data, isPopulated, dropdownItems);

        const cardData = {
            id, formId, cardId, wrapperId, editorId, data, isPopulated, dropdownItems
        };
        setupCardEventHandlers(cardData);
    }

    function loadCardsFromService() {
        $.ajax({
            url: `/api/app/template/templates-by-tenent`,
            type: 'GET',
            success: handleLoadCardsSuccess,
            error: handleLoadCardsError
        });
    }

    function handleLoadCardsSuccess(response) {
        editorInstances = {};
        response.forEach(item => createCard(item));
    }

    function handleLoadCardsError() {
        abp.notify.error('Unable to load the templates.');
    }
    function generateTempId() {
        const array = new Uint32Array(1);
        window.crypto.getRandomValues(array);
        return `temp-${array[0].toString(36)}`;
    }

    $("#CreateNewTemplate").on("click", function () {
        createCard();
    });
});