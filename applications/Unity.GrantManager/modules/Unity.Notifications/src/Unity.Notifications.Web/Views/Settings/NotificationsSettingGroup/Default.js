(function ($) {
    $(function () {
        console.info("Notifications Setting Management UI Loaded");
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

        const editorInstances = {};

        function createCard(data = null) {
            const isPopulated = data !== null;
            const id = data?.id?.toString() || generateTempId();
            const cardId = `collapseDetails-${id}`;
            const formId = `form-${id}`;
            const wrapperId = `cardWrapper-${id}`;
            const editorId = `editor-${id}`;
            const type = data?.type || 'Automatic';
            const lastEdited = data?.lastModificationTime
                ? new Date(data.lastModificationTime).toLocaleDateString('en-CA')
                : data?.creationTime
                    ? new Date(data.creationTime).toLocaleDateString('en-CA')
                    : new Date().toLocaleDateString('en-CA');
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
                            <input type="text" class="form-control form-input" name="templateName" value="${data?.name || ''}" ${disabled}>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Send From</label>
                            <select class="form-select form-input" name="sendFrom" ${disabled}>
                                <option value="">Select sender</option>
                                <option value="noreply@example.com" ${data?.sendFrom === 'noreply@example.com' ? 'selected' : ''}>noreply@example.com</option>
                                <option value="support@example.com" ${data?.sendFrom === 'support@example.com' ? 'selected' : ''}>support@example.com</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Subject</label>
                            <input type="text" class="form-control form-input" name="subject" value="${data?.subject || ''}" ${disabled}>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Body</label>
                            <div id="${editorId}" class="tui-editor-body"></div>
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
            editorInstances[id] = new toastui.Editor.factory({
                el: document.querySelector(`#${editorId}`),
                height: '250px',
                initialEditType: 'wysiwyg',
                previewStyle: 'vertical',
                initialValue: data?.bodyHTML,
                toolbarItems: [
                    ['heading', 'bold', 'italic', 'strike'],
                    ['hr', 'quote', 'link'],
                    ['ul', 'ol', 'task', 'indent', 'outdent']
                ],
                viewer: isPopulated,
                contentEditable: false
            });


            $(`#${formId}`).on("submit", function (e) {
                e.preventDefault();
                const formData = $(this).serializeArray();
                console.log(formData)
                if (id.includes("temp")) {


                    $.ajax({
                        url: `/api/app/template`,
                        method: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify({
                            "name": formData[0].value,
                            "description": "",
                            "subject": formData[2].value,
                            "bodyText": editorInstances[id].getMarkdown(),
                            "bodyHTML": editorInstances[id].getHTML(),
                            "sendFrom": formData[1].value
                        }),
                        success: function (response) {
                            console.log(response)
                            abp.notify.success(
                                'Template saved successfully.',
                            );
                            $("#cardContainer").empty(); 
                            loadCardsFromService();
                        },
                        error: function (xhr, status, error) {
                            abp.notify.error(
                                'Failed to save template.',
                            );
                        }
                    });
                }
                else {
                    $.ajax({
                        url: `/api/app/template/${id}/template`,
                        method: 'PUT',
                        contentType: 'application/json',
                        data: JSON.stringify({
                            "name": formData[0].value,
                            "description": "",
                            "subject": formData[2].value,
                            "bodyText": editorInstances[id].getMarkdown(),
                            "bodyHTML": editorInstances[id].getHTML(),
                            "sendFrom": formData[1].value
                        }),
                        success: function (response) {
                            abp.notify.success(
                                'Template updated successfully.',
                            );
                            $("#cardContainer").empty(); 
                            loadCardsFromService();
                        },
                        error: function (xhr, status, error) {
                            abp.notify.success(
                                'Failed to update the template.',
                            );
                        }
                    });
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
                const editorEl = document.querySelector(`#${editorId}`);
                const currentEditor = editorInstances[id];
                const content = data?.body;
                currentEditor.destroy();
              
                editorInstances[id] = new toastui.Editor({
                    el: editorEl,
                    height: '250px',
                    initialEditType: 'wysiwyg',
                    previewStyle: 'vertical',
                    initialValue: content,
                    toolbarItems: [
                        ['heading', 'bold', 'italic', 'strike'],
                        ['hr', 'quote', 'link'],
                        ['ul', 'ol', 'task', 'indent', 'outdent']
                    ]
                });
                const card = $(`#${wrapperId}`);
                card.find(".form-input").prop('disabled', false);
                card.find(".saveBtn").prop('disabled', false);
                card.find(".discardBtn").removeClass("d-none");
                $(this).addClass("d-none");
            });

            $(`#${wrapperId}`).on("click", ".discardBtn", function () {
                const form = $(`#${formId}`)[0];
                form.reset();

                const editorEl = document.querySelector(`#${editorId}`);
                const currentEditor = editorInstances[id];
               
                const content = data?.bodyHTML;
                currentEditor.destroy();

                editorInstances[id] = new toastui.Editor.factory({
                    el: editorEl,
                    height: '250px',
                    initialEditType: 'wysiwyg',
                    previewStyle: 'vertical',
                    initialValue: content,
                    toolbarItems: [
                        ['heading', 'bold', 'italic', 'strike'],
                        ['hr', 'quote', 'link'],
                        ['ul', 'ol', 'task', 'indent', 'outdent']
                    ],
                    viewer: true,
                    contentEditable: false
                });

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
                    Swal.fire({
                        title: "Delete Template",
                        text: "Are you sure you want to delete this template?",
                        showCancelButton: true,
                        confirmButtonText: "Confirm",
                        customClass: {
                            confirmButton: 'btn btn-primary',
                            cancelButton: 'btn btn-secondary'
                        }
                    }).then((result) => {
                        if (result.isConfirmed) {
                            $.ajax({
                                url: `/api/app/template/${id}/template`,
                                type: 'DELETE',
                                success: function () {
                                    $(`#${wrapperId}`).remove();
                                    abp.notify.success(
                                        'Template deleted successfully.',
                                    );
                                },
                                error: function () {
                                    abp.notify.error(
                                        'Error deleting the template.',
                                    );
                                }
                            });
                        }
                    });
                       
                
                   
                } else {
                    $(`#${wrapperId}`).remove();
                }
            });
        }

        function loadCardsFromService() {
            $.ajax({
                url: `/api/app/template/templates-by-tenent`,
                type: 'GET',
                success: function (response) {
                    response.forEach(item => createCard(item));

                },
                error: function () {
                    abp.notify.error(
                        'unable to load the templates.',
                    );
                }
            });
                
           
        }
        function generateTempId() {
            return `temp-${Math.random().toString(36).substring(2, 10)}`;
        }

        $("#CreateNewTemplate").on("click", function () {
            createCard();
        });
           
        
    });
})(jQuery);