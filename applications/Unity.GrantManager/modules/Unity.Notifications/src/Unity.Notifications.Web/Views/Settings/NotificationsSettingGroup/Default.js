(function ($) {
    $(function () {
        console.info("Notifications Setting Management UI Loaded");

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

        let cardCount = 0;

        function createCard(data = null) {
            const isPopulated = data !== null;
            const id = data?.id || `new-${cardCount}`;
            const cardId = `collapseDetails${cardCount}`;
            const formId = `form${cardCount}`;
            const wrapperId = `cardWrapper${cardCount}`;
            const note = data?.notes || '';
            const type = data?.type || 'Automatic';
            const lastEdited = data?.lastEdited || new Date().toLocaleDateString('en-GB');
            const disabled = isPopulated ? 'disabled' : '';

            const cardHtml = `
                <div class="card mb-3 shadow-sm" id="${wrapperId}" data-id="${id}">
                    <div class="card-header d-flex justify-content-between align-items-center">
                       <strong class="template-title">${data?.templateName || 'Untitled Template'}</strong>
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
                                <i class="bi bi-chevron-down"></i>
                            </button>
                        </div>
                    </div>
                    <div class="collapse" id="${cardId}">
                        <div class="card-body">
                            <form id="${formId}">
                                <div class="mb-3">
                                    <label class="form-label">Template Name</label>
                                    <input type="text" class="form-control form-input" name="templateName" value="${data?.templateName || ''}" ${disabled}>
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
                                    <div id="editor${cardCount}" class="tui-editor-body"></div>
                                </div>
                                <div class="d-flex gap-2">
                                   <button type="submit" class="btn btn-primary saveBtn" disabled>SAVE CHANGES</button>
                                    <button type="button" class="btn btn-outline-secondary discardBtn d-none">DISCARD CHANGES</button>
                                    ${isPopulated ? `<button type="button" class="btn btn-secondary editBtn">Edit</button>` : ''}
                                     <button type="button" class="btn btn-outline-danger deleteCardBtn ms-auto">DELETE THIS TEMPLATE</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            `;

            $("#cardContainer").append(cardHtml);

            $(`#editor${cardCount}`).tuiEditor({
                initialEditType: 'wysiwyg',
                previewStyle: 'vertical',
                height: '300px',
                initialValue: data?.body || ''
            });

           


            // Save handler
            $(`#${formId}`).on("submit", function (e) {
                e.preventDefault();
                const formData = $(this).serializeArray();
                alert(`Saved: ${formData[0].value}`);
                // Here you can call a POST/PUT API to save the data
            });

            $(`#${wrapperId}`).on("click", ".editBtn", function () {
                const card = $(`#${wrapperId}`);
                card.find(".form-input").prop('disabled', false);
                card.find(".saveBtn").prop('disabled', false);
                card.find(".discardBtn").removeClass("d-none");
                $(this).addClass("d-none");
            });

            // Discard button resets fields (optional: reload original values)
            $(`#${wrapperId}`).on("click", ".discardBtn", function () {
                const form = $(`#${formId}`)[0];
                form.reset();
                $(`#${wrapperId} .form-input`).prop('disabled', true);
                $(`#${wrapperId} .saveBtn`).prop('disabled', true);
                $(`#${wrapperId} .discardBtn`).addClass("d-none");
                $(`#${wrapperId} .editBtn`).removeClass("d-none");
            });

            $(`#${formId} input[name="templateName"]`).on('input', function () {
                const newTitle = $(this).val().trim() || 'Untitled Template';
                $(`#${wrapperId} .template-title`).text(newTitle);
            });

            // Delete handler
            $(`#${wrapperId}`).on("click", ".deleteCardBtn", function () {
                if (isPopulated) {
                    const confirmDelete = confirm("Are you sure you want to delete this report?");
                    if (confirmDelete) {
                        $.ajax({
                            url: `/api/reports/${id}`,
                            type: 'DELETE',
                            success: function () {
                                $(`#${wrapperId}`).remove();
                                alert("Deleted successfully.");
                            },
                            error: function () {
                                alert("Error deleting the report.");
                            }
                        });
                    }
                } else {
                    $(`#${wrapperId}`).remove();
                }
            });

            cardCount++;
        }

        function loadCardsFromService() {
            let data = [
                { "id": 1, "notes": "Report A", "type": "Automatic", "lastEdited": "20/02/24" },
                { "id": 2, "notes": "Report B", "type": "Manual", "lastEdited": "21/02/24" }
            ];
                data.forEach(item => createCard(item));
           
        }

        $(document).ready(function () {
            $("#addCardBtn").click(() => createCard());
            loadCardsFromService(); // Load cards initially
        });
    });
})(jQuery);