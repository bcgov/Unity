(function ($) {
    $(function () {
        loadCardsFromService();
        const NotificationUiElements = {
            settingForm: $('#NotificationsSettingsForm'),
            saveButton: $('#NotificationsSaveButton'),
            discardButton: $('#NotificationsDiscardButton'),
        };

        let initialFormState = NotificationUiElements.settingForm.serialize();
        let editorInstances = {};

        function checkFormChanges() {
            let currentFormState =
                NotificationUiElements.settingForm.serialize();
            let isFormChanged = currentFormState !== initialFormState;

            NotificationUiElements.saveButton.prop('disabled', !isFormChanged);
            NotificationUiElements.discardButton.prop(
                'disabled',
                !isFormChanged
            );
        }

        function handleFormSubmit(event) {
            event.preventDefault();

            if (!$(this).valid()) {
                return;
            }

            let form = $(this).serializeFormToObject();
            unity.notifications.emailNotifications.emailNotification
                .updateSettings(form)
                .then(function (result) {
                    $(document).trigger('AbpSettingSaved');
                    initialFormState =
                        NotificationUiElements.settingForm.serialize();
                    checkFormChanges();
                });
        }

        function handleDiscardClick() {
            NotificationUiElements.settingForm[0].reset();
            initialFormState = NotificationUiElements.settingForm.serialize();
            checkFormChanges();
        }

        // Event bindings for main form
        NotificationUiElements.settingForm.on('input change', checkFormChanges);
        NotificationUiElements.settingForm.on('submit', handleFormSubmit);
        NotificationUiElements.discardButton.on('click', handleDiscardClick);
        checkFormChanges();

        // Helper functions
        function getToolbarOptions() {
            return 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist | link image | code preview | variablesDropdownButton';
        }

        function getPlugins() {
            return 'lists link image preview code';
        }

        function extractFormData(formDataArray) {
            return {
                name: formDataArray[0].value,
                sendFrom: formDataArray[1].value,
                subject: formDataArray[2].value,
            };
        }

        function buildTemplatePayload(data, editor) {
            return JSON.stringify({
                name: data.name,
                description: '',
                subject: data.subject,
                bodyText: editor.getContent({ format: 'text' }),
                bodyHTML: editor.getContent(),
                sendFrom: data.sendFrom,
            });
        }

        function handleSuccess(message) {
            abp.notify.success(message);
            $('#cardContainer').empty();
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
                error: onSaveTemplateError,
            });
        }

        function updateTemplate(id, payload) {
            $.ajax({
                url: `/api/app/template/${id}/template`,
                method: 'PUT',
                contentType: 'application/json',
                data: payload,
                success: onSaveTemplateSuccess,
                error: onSaveTemplateError,
            });
        }

        function processTemplateNameCheck(
            response,
            name,
            currentId,
            wrapperId,
            callback
        ) {
            const isSameAsCurrent =
                !currentId.includes('temp') &&
                name === $(`#${wrapperId}`).data('original-name');
            let isExist = false;
            if (response?.id) {
                isExist = true;
            }
            callback(!isExist || isSameAsCurrent);
        }

        function checkTemplateNameUnique(name, currentId, wrapperId, callback) {
            $.ajax({
                url: `/api/app/template/template-by-name?name=${encodeURIComponent(
                    name
                )}`,
                type: 'GET',
                success: function (response) {
                    processTemplateNameCheck(
                        response,
                        name,
                        currentId,
                        wrapperId,
                        callback
                    );
                },
                error: function () {
                    callback(false);
                },
            });
        }

        function processTemplateVariables(response, dropdownItems) {
            $.map(response, function (item) {
                dropdownItems.push({
                    text: item.name,
                    value: item.token,
                });
            });
        }

        function getTemplateVariables() {
            const dropdownItems = [];
            $.ajax({
                url: `/api/app/template/template-variables`,
                type: 'GET',
                success: function (response) {
                    processTemplateVariables(response, dropdownItems);
                },
                error: function () {
                    // Handle error silently
                },
            });
            return dropdownItems;
        }

        function createVariableMenuItem(item, editor) {
            return {
                type: 'menuitem',
                text: item.text,
                onAction: () => {
                    editor.insertContent(`{{${item.value}}}`);
                },
            };
        }

        function setupEditorVariablesDropdown(editor, dropdownItems) {
            editor.ui.registry.addMenuButton('variablesDropdownButton', {
                text: 'VARIABLES',
                fetch: function (callback) {
                    const items = dropdownItems.map((item) =>
                        createVariableMenuItem(item, editor)
                    );
                    callback(items);
                },
            });
        }

        function onEditorInit(editor, id, editorId, data, isPopulated) {
            editor.mode.set(isPopulated ? 'readonly' : 'design');
            if (data?.bodyHTML) {
                editor.setContent(data.bodyHTML);
            }
            editorInstances[id] = editor;
            console.log(`Editor initialized: ${editorId}`);
        }

        function setupEditor(
            editor,
            id,
            editorId,
            data,
            isPopulated,
            dropdownItems
        ) {
            setupEditorVariablesDropdown(editor, dropdownItems);
            editor.on('init', function () {
                onEditorInit(editor, id, editorId, data, isPopulated);
            });
        }

        function setupTinyMCEEditor(
            editor,
            id,
            editorId,
            data,
            isPopulated,
            dropdownItems
        ) {
            console.log('editor', editor);
            setupEditor(editor, id, editorId, data, isPopulated, dropdownItems);
        }

        function initTinyMCE(editorId, id, data, isPopulated, dropdownItems) {
            tinymce.init({
                license_key: 'gpl',
                selector: `#${editorId}`,
                plugins: getPlugins(),
                toolbar: getToolbarOptions(),
                statusbar: false,
                promotion: false,
                content_css: false,
                skin: false,
                setup: function (editor) {
                    setupTinyMCEEditor(
                        editor,
                        id,
                        editorId,
                        data,
                        isPopulated,
                        dropdownItems
                    );
                },
            });
        }

        function updateTemplateNameValidation(templateInput, formId, isUnique) {
            if (!isUnique) {
                templateInput.addClass('is-invalid');
                if (!$(`#${formId} .template-name-feedback`).length) {
                    templateInput.after(
                        `<div class="invalid-feedback template-name-feedback">Template name must be unique.</div>`
                    );
                }
                $(`#${formId} .saveBtn`).prop('disabled', true);
            } else {
                templateInput.removeClass('is-invalid');
                $(`#${formId} .template-name-feedback`).remove();
                $(`#${formId} .saveBtn`).prop('disabled', false);
            }
        }

        function processNameValidationResult(templateInput, formId, isUnique) {
            updateTemplateNameValidation(templateInput, formId, isUnique);
        }

        function handleTemplateNameValidation(
            templateInput,
            newTitle,
            id,
            formId,
            wrapperId
        ) {
            checkTemplateNameUnique(
                newTitle,
                id,
                wrapperId,
                function (isUnique) {
                    processNameValidationResult(
                        templateInput,
                        formId,
                        isUnique
                    );
                }
            );
        }

        function handleFormSubmitEvent(e, formId, id) {
            e.preventDefault();
            const formDataArray = $(e.target).serializeArray();
            const formData = extractFormData(formDataArray);
            const editor = editorInstances[id];
            const payload = buildTemplatePayload(formData, editor);

            if (id.includes('temp')) {
                saveTemplate(payload);
            } else {
                updateTemplate(id, payload);
            }
        }

        function setupFormSubmitHandler(formId, id) {
            $(`#${formId}`).on('submit', function (e) {
                handleFormSubmitEvent(e, formId, id);
            });
        }

        function updateChevronIcon(wrapperId, cardId, direction) {
            const iconSelector = `#${wrapperId} .btn[data-bs-target="#${cardId}"] i`;
            if (direction === 'up') {
                $(iconSelector)
                    .removeClass('fa-chevron-down')
                    .addClass('fa-chevron-up');
            } else {
                $(iconSelector)
                    .removeClass('fa-chevron-up')
                    .addClass('fa-chevron-down');
            }
        }

        function setupCollapseHandlers(cardId, wrapperId) {
            $(`#${cardId}`).on('show.bs.collapse', function () {
                updateChevronIcon(wrapperId, cardId, 'up');
            });

            $(`#${cardId}`).on('hide.bs.collapse', function () {
                updateChevronIcon(wrapperId, cardId, 'down');
            });
        }

        function enableCardEditMode(wrapperId, editButton) {
            const card = $(`#${wrapperId}`);
            card.find('.form-input').prop('disabled', false);
            card.find('.saveBtn').prop('disabled', false);
            card.find('.discardBtn').removeClass('d-none');
            editButton.addClass('d-none');
        }

        function handleEditButtonClick(
            wrapperId,
            editorId,
            id,
            data,
            dropdownItems
        ) {
            const currentEditor = editorInstances[id];
            currentEditor.destroy();
            initTinyMCE(editorId, id, data, false, dropdownItems);
            enableCardEditMode(wrapperId, $(this));
        }

        function handleEditButton(
            wrapperId,
            editorId,
            id,
            data,
            dropdownItems
        ) {
            $(`#${wrapperId}`).on('click', '.editBtn', function () {
                handleEditButtonClick.call(
                    this,
                    wrapperId,
                    editorId,
                    id,
                    data,
                    dropdownItems
                );
            });
        }

        function resetCardToReadonlyMode(wrapperId) {
            $(`#${wrapperId} .form-input`).prop('disabled', true);
            $(`#${wrapperId} .saveBtn`).prop('disabled', true);
            $(`#${wrapperId} .discardBtn`).addClass('d-none');
            $(`#${wrapperId} .editBtn`).removeClass('d-none');
        }

        function handleDiscardButtonClick(
            wrapperId,
            formId,
            editorId,
            id,
            data,
            dropdownItems
        ) {
            const form = $(`#${formId}`)[0];
            form.reset();
            const currentEditor = editorInstances[id];
            currentEditor.destroy();
            initTinyMCE(editorId, id, data, true, dropdownItems);
            resetCardToReadonlyMode(wrapperId);
        }

        function handleDiscardButton(
            wrapperId,
            formId,
            editorId,
            id,
            data,
            dropdownItems
        ) {
            $(`#${wrapperId}`).on('click', '.discardBtn', function () {
                handleDiscardButtonClick(
                    wrapperId,
                    formId,
                    editorId,
                    id,
                    data,
                    dropdownItems
                );
            });
        }

        function processDeleteConfirmation(result, id, wrapperId) {
            if (result.isConfirmed) {
                deleteTemplate(id, wrapperId);
            }
        }

        function showDeleteConfirmation(id, wrapperId) {
            const swalOptions = {
                title: 'Delete Template',
                text: 'Are you sure you want to delete this template?',
                showCancelButton: true,
                confirmButtonText: 'Confirm',
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-secondary',
                },
            };

            Swal.fire(swalOptions).then(function (result) {
                processDeleteConfirmation(result, id, wrapperId);
            });
        }

        function handleDeleteSuccess(wrapperId) {
            $(`#${wrapperId}`).remove();
            abp.notify.success('Template deleted successfully.');
        }

        function handleDeleteError() {
            abp.notify.error('Error deleting the template.');
        }

        function deleteTemplate(id, wrapperId) {
            $.ajax({
                url: `/api/app/template/${id}/template`,
                type: 'DELETE',
                success: function () {
                    handleDeleteSuccess(wrapperId);
                },
                error: handleDeleteError,
            });
        }

        function handleDeleteButtonClick(id, wrapperId, isPopulated) {
            if (isPopulated) {
                showDeleteConfirmation(id, wrapperId);
            } else {
                $(`#${wrapperId}`).remove();
            }
        }

        function setupDeleteHandler(wrapperId, id, isPopulated) {
            $(`#${wrapperId}`).on('click', '.deleteCardBtn', function () {
                handleDeleteButtonClick(id, wrapperId, isPopulated);
            });
        }

        function calculateLastEdited(data) {
            if (data?.lastModificationTime) {
                return new Date(data.lastModificationTime).toLocaleDateString(
                    'en-CA'
                );
            } else if (data?.creationTime) {
                return new Date(data.creationTime).toLocaleDateString('en-CA');
            } else {
                return new Date().toLocaleDateString('en-CA');
            }
        }

        function generateCardHtml(
            wrapperId,
            id,
            cardId,
            formId,
            editorId,
            data,
            isPopulated,
            type,
            lastEdited,
            disabled
        ) {
            return `
        <div class="card mb-3 shadow-sm" id="${wrapperId}" data-id="${id}">
            <div class="card-header d-flex justify-content-between align-items-center">
                <strong class="template-title">${
                    data?.name || 'Untitled Template'
                }</strong>
                <div class="d-flex align-items-center gap-3">
                    <div class="text-muted small text-end me-3">
                        <div>Type<br /><span class="fw-normal">${type}</span></div>
                    </div>
                    <div class="text-muted small text-end me-3">
                        <div>Last Edited<br /><span class="fw-normal">${lastEdited}</span></div>
                    </div>
                    <button class="btn btn-sm btn-link text-decoration-none" type="button"
                        data-bs-toggle="collapse" data-bs-target="#${cardId}" aria-expanded="false"
                        aria-controls="${cardId}">
                        <i class="unt-icon-sm fa-solid fa-chevron-down"></i>
                    </button>
                </div>
            </div>
            <div class="collapse" id="${cardId}">
                <div class="card-body">
                    <form id="${formId}">
                        <div class="mb-3">
                            <label class="form-label">Template Name</label>
                            <input type="text" class="form-control form-input" name="templateName" value="${
                                data?.name || ''
                            }" ${disabled} required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Send From</label>
                            <input type="text" class="form-control form-input" name="sendFrom" value="${
                                data?.sendFrom || ''
                            }" ${disabled} required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Subject</label>
                            <input type="text" class="form-control form-input" name="subject" value="${
                                data?.subject || ''
                            }" ${disabled} required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Body</label>
                            <textarea id="${editorId}"></textarea>
                        </div>
                         <div class="mb-3">
                         <p><b>NOTE</b>:<span class="note-text"> Selecting text will let your customize it: replace it with a variable, make it bold, italic, change the alignment, add a link, create a list, etc.</span></p>
                         </div>
                         <div class="mb-3">
                         <hr>
                         <button type="button" class="btn btn-outline-primary deleteCardBtn">X DELETE THIS TEMPLATE</button>
                         <hr>
                         </div>
                        <div class="d-flex gap-2">
                            ${
                                isPopulated
                                    ? `<button type="submit" class="btn btn-primary  saveBtn" disabled>SAVE CHANGES</button>`
                                    : `<button type="submit" class="btn btn-primary saveBtn">SAVE CHANGES</button>`
                            }
                            <button type="button" class="btn btn-outline-secondary discardBtn d-none">DISCARD CHANGES</button>
                            ${
                                isPopulated
                                    ? `<button type="button" class="btn btn-secondary editBtn">EDIT THIS TEMPLATE</button>`
                                    : ''
                            }
                        </div>
                    </form>
                </div>
            </div>
        </div>`;
        }

        function setupTemplateNameHandler(formId, wrapperId, id) {
            $(`#${formId} input[name="templateName"]`).on('input', function () {
                const templateInput = $(this);
                const newTitle =
                    templateInput.val().trim() || 'Untitled Template';
                $(`#${wrapperId} .template-title`).text(newTitle);
                handleTemplateNameValidation(
                    templateInput,
                    newTitle,
                    id,
                    formId,
                    wrapperId
                );
            });
        }

        function setupCardEventHandlers(
            cardId,
            formId,
            wrapperId,
            editorId,
            id,
            data,
            isPopulated,
            dropdownItems
        ) {
            setupFormSubmitHandler(formId, id);
            setupCollapseHandlers(cardId, wrapperId);
            handleEditButton(wrapperId, editorId, id, data, dropdownItems);
            handleDiscardButton(
                wrapperId,
                formId,
                editorId,
                id,
                data,
                dropdownItems
            );
            setupDeleteHandler(wrapperId, id, isPopulated);
            setupTemplateNameHandler(formId, wrapperId, id);
        }

        function initializeEditor(
            editorId,
            id,
            data,
            isPopulated,
            dropdownItems
        ) {
            console.log('tinymce', tinymce);
            if (tinymce.get(editorId)) {
                tinymce.get(editorId).remove();
            }
            initTinyMCE(editorId, id, data, isPopulated, dropdownItems);
        }

        function createCard(data = null) {
            const isPopulated = data !== null;
            const id = data?.id?.toString() || generateTempId();
            const cardId = `collapseDetails-${id}`;
            const formId = `form-${id}`;
            const wrapperId = `cardWrapper-${id}`;
            const editorId = `editor-${id}`;
            const type = data?.type || 'Automatic';
            const lastEdited = calculateLastEdited(data);
            const disabled = isPopulated ? 'disabled' : '';
            const dropdownItems = getTemplateVariables();

            const cardHtml = generateCardHtml(
                wrapperId,
                id,
                cardId,
                formId,
                editorId,
                data,
                isPopulated,
                type,
                lastEdited,
                disabled
            );

            $('#cardContainer').append(cardHtml);
            $(`#${wrapperId}`).data('original-name', data?.name || '');

            initializeEditor(editorId, id, data, isPopulated, dropdownItems);
            setupCardEventHandlers(
                cardId,
                formId,
                wrapperId,
                editorId,
                id,
                data,
                isPopulated,
                dropdownItems
            );
        }

        function loadCardsFromService() {
            $.ajax({
                url: `/api/app/template/templates-by-tenent`,
                type: 'GET',
                success: handleLoadCardsSuccess,
                error: handleLoadCardsError,
            });
        }

        function handleLoadCardsSuccess(response) {
            editorInstances = {};
            response.forEach((item) => createCard(item));
        }

        function handleLoadCardsError() {
            abp.notify.error('Unable to load the templates.');
        }

        function generateTempId() {
            const array = new Uint32Array(1);
            window.crypto.getRandomValues(array);
            return `temp-${array[0].toString(36)}`;
        }

        $('#CreateNewTemplate').on('click', function () {
            createCard();
        });
    });
})(jQuery);
