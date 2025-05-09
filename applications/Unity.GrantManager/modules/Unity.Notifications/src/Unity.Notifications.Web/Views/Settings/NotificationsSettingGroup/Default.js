(function ($) {
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
      

        function createCard(data = null) {
            const isPopulated = data !== null;
            const id = data?.id?.toString() || generateTempId();
            const cardId = `collapseDetails-${id}`;
            const formId = `form-${id}`;
            const wrapperId = `cardWrapper-${id}`;
            const editorId = `editor-${id}`;
            const type = data?.type || 'Automatic';
            let lastEdited;
            const dropdownItems = [];
            getTemplateVariables();
            if (data?.lastModificationTime) {
                lastEdited = new Date(data.lastModificationTime).toLocaleDateString('en-CA');
            } else if (data?.creationTime) {
                lastEdited = new Date(data.creationTime).toLocaleDateString('en-CA');
            } else {
                lastEdited = new Date().toLocaleDateString('en-CA');
            }
            const disabled = isPopulated ? 'disabled' : '';

            const cardHtml = `
        <div class="card mb-3 shadow-sm" id="${wrapperId}" data-id="${id}">
            <div class="card-header d-flex justify-content-between align-items-center">
                <strong class="template-title">${data?.name || 'Untitled Template'}</strong>
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
                            ${isPopulated ? `<button type="submit" class="btn btn-primary  saveBtn" disabled>SAVE CHANGES</button>` : `<button type="submit" class="btn btn-primary saveBtn">SAVE CHANGES</button>`}
                            <button type="button" class="btn btn-outline-secondary discardBtn d-none">DISCARD CHANGES</button>
                            ${isPopulated ? `<button type="button" class="btn btn-secondary editBtn">EDIT THIS TEMPLATE</button>` : ''}
                           
                        </div>
                    </form>
                </div>
            </div>
        </div>
    `;

            $("#cardContainer").append(cardHtml);
            $(`#${wrapperId}`).data('original-name', data?.name || '');
            // editorInstances[id] =
            console.log("tinymce", tinymce)
            if (tinymce.get(editorId)) {
                tinymce.get(editorId).remove(); // remove existing instance
            }

            function getToolbarOptions() {
                return 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist | link image | code preview | variablesDropdownButton';
            }

            function getPlugins() {
                return 'lists link image preview code';
            }
           

            function setupEditor(editor, id, editorId, data, isPopulated) {
                editor.ui.registry.addMenuButton('variablesDropdownButton', {
                    text: 'VARIABLES',
                    fetch: function (callback) {
                        const items = dropdownItems.map(item => ({
                            type: 'menuitem',
                            text: item.text,
                            onAction: () => {
                                editor.insertContent(`{{${item.value}}}`);
                            }
                        }));
                        callback(items);
                    }
                });
               
                editor.on('init', function () {
                    editor.mode.set(isPopulated ? 'readonly' : 'design');
                    if (data?.bodyHTML) {
                        editor.setContent(data.bodyHTML);
                    }
                    editorInstances[id] = editor;
                    console.log(`Editor initialized: ${editorId}`);
                });
            }

            function initTinyMCE(editorId, id, data, isPopulated) {                               
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
                        console.log("editor", editor);
                        setupEditor(editor, id, editorId, data, isPopulated);
                    }
                });
            }


            initTinyMCE(editorId, id, data, isPopulated)



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

            $(`#${formId} input[name="templateName"]`).on('input', function () {
                const templateInput = $(this);
                const newTitle = templateInput.val().trim() || 'Untitled Template';
                $(`#${wrapperId} .template-title`).text(newTitle);

                // Check if name is unique
                checkTemplateNameUnique(newTitle, id, function (isUnique) {
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
                });
            });

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

            $(`#${wrapperId}`).on("click", ".editBtn", function () {
                const currentEditor = editorInstances[id];
                currentEditor.destroy();

                initTinyMCE(editorId, id, data, false)

                const card = $(`#${wrapperId}`);
                card.find(".form-input").prop('disabled', false);
                card.find(".saveBtn").prop('disabled', false);
                card.find(".discardBtn").removeClass("d-none");
                $(this).addClass("d-none");
            });

            $(`#${wrapperId}`).on("click", ".discardBtn", function () {
                const form = $(`#${formId}`)[0];
                form.reset();
                const currentEditor = editorInstances[id];
                currentEditor.destroy();

                initTinyMCE(editorId, id, data, true)

                $(`#${wrapperId} .form-input`).prop('disabled', true);
                $(`#${wrapperId} .saveBtn`).prop('disabled', true);
                $(`#${wrapperId} .discardBtn`).addClass("d-none");
                $(`#${wrapperId} .editBtn`).removeClass("d-none");
            });

            $(`#${formId} input[name="templateName"]`).on('input', function () {
                const newTitle = $(this).val().trim() || 'Untitled Template';
                $(`#${wrapperId} .template-title`).text(newTitle);
            });

            $(`#${wrapperId}`).on("click", ".deleteCardBtn", function () {
                if (isPopulated) {
                    showDeleteConfirmation(id, wrapperId);
                } else {
                    $(`#${wrapperId}`).remove();
                }
            });

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

                Swal.fire(swalOptions).then(handleResult.bind(null, id, wrapperId));
            }
            function handleResult(id, wrapperId, result) {
                handleDeleteConfirmation(result, id, wrapperId);
            }

            function handleDeleteConfirmation(result, id, wrapperId) {
                if (!result.isConfirmed) return;
                deleteTemplate(id, wrapperId);
            }
            function handleDeleteSuccess() {
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
                    success: handleDeleteSuccess,
                    error: handleDeleteError
                });
            }
            function checkTemplateNameUnique(name, currentId, callback) {
                $.ajax({
                    url: `/api/app/template/template-by-name?name=${encodeURIComponent(name)}`,
                    type: 'GET',
                    success: function (response) {
                        // Assume response: { isUnique: true/false }
                        // If editing an existing template, allow current name
                        const isSameAsCurrent = !currentId.includes('temp') && name === $(`#${wrapperId}`).data('original-name');
                        let isExist = false;
                        if (response?.id) {
                            isExist = true;
                        }

                        callback(!isExist || isSameAsCurrent);
                    },
                    error: function () {
                        callback(false); // Assume not unique if error
                    }
                });
            }

            function getTemplateVariables() {
                $.ajax({
                    url: `/api/app/template/template-variables`,
                    type: 'GET',
                    success: function (response) {
                        $.map(response, function (item) {
                            dropdownItems.push(  {
                                text: item.name,
                                value: item.token
                            });
                        });
                    },
                    error: function () {
                      
                    }
                });
            }

           
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
})(jQuery);