$(document).ready(function () {
    const UIElements = {
        applicationId: $('#DetailsViewApplicationId')[0].value,
        btnSend: $('#btn-send-top'),
        btnSendDropdown: $('#btn-send-dropdown'),
        btnSave: $('#btn-save-top'),
        btnDiscard: $('#btn-send-discard-top'),
        btnSendClose: $('#btn-send-close-top'),
        btnConfirmSend: $('#btn-confirm-send'),
        btnCancelEmail: $('#btn-cancel-email'),
        btnNewEmail: $('#btn-new-email'),
        btnShowBCC: $('#btn-show-bcc'),
        emailForm: $('#EmailForm'),
        inputEmailId: $('#EmailId'),
        inputEmailTo: $($('#EmailTo')[0]),
        inputEmailToField: $('#EmailTo')[0],
        inputEmailCC: $($('#EmailCC')[0]),
        inputEmailBCC: $($('#EmailBCC')[0]),
        inputEmailFrom: $($('#EmailFrom')[0]),
        inputEmailSubject: $($('#EmailSubject')[0]),
        inputEmailBody: $($('#EmailBody')[0]),
        inputOriginalEmailTo: $($('#OriginalDraftEmailTo')[0]),
        inputOriginalEmailCC: $($('#OriginalDraftEmailCC')[0]),
        inputOriginalEmailBCC: $($('#OriginalDraftEmailBCC')[0]),
        inputOriginalEmailFrom: $($('#OriginalDraftEmailFrom')[0]),
        inputOriginalEmailSubject: $($('#OriginalDraftEmailSubject')[0]),
        inputOriginalEmailBody: $($('#OriginalDraftEmailBody')[0]),
        emailSpinner: $('#spinner-modal'),
        confirmationModal: $('#confirmation-modal'),
        alertEmailReadonly: $('#email-alert-readonly'),
        inputSendOnDateTime: $('#SendOnDateTime'),
        delayDateTimeValidation: $('#delay-datetime-validation'),
        sendOnDisplay: $('#send-on-display'),
        btnClearSchedule: $('#btn-clear-schedule'),
        scheduleModalBackdrop: $('#schedule-modal-backdrop'),
        scheduleModal: $('#schedule-send-modal'),
        scheduleModalValidation: $('#schedule-modal-validation'),
        scheduleCalendarGrid: $('#schedule-calendar-grid'),
        calendarMonthYear: $('#calendar-month-year'),
        btnCalendarPrev: $('#btn-calendar-prev'),
        btnCalendarNext: $('#btn-calendar-next'),
        scheduleDateInput: $('#schedule-date-input'),
        scheduleDateValidation: $('#schedule-date-validation'),
        scheduleTimeSelect: $('#schedule-time-select'),
        btnScheduleCancel: $('#btn-schedule-cancel'),
        btnScheduleConfirm: $('#btn-schedule-confirm'),
        btnScheduleModalClose: $('#btn-schedule-modal-close'),
        btnOpenScheduleModal: $('#btn-open-schedule-modal'),
        bccInputRow: $('#bcc-input-row')
    };

    let defaultValues = {
        emailTo: '',
        emailFrom: '',
        emailCC: '',
        emailBCC: ''
    };
    let applicationDetails;
    let mappingConfig;
    let editorInstance;
    let isNewEmailDraft = false;
    let newDraftId = null;
    let selectedEmailData = null; // Store original email data when selected from table
    let emailAttachmentsTable = null;
    let scheduleState = {
        currentMonth: new Date().getMonth(),
        currentYear: new Date().getFullYear(),
        selectedDate: null,
        selectedTime: null
    };


    function bindUIEvents() {
        // Remove any existing event handlers to prevent duplicates
        UIElements.btnNewEmail.off('click');
        UIElements.btnSend.off('click');
        UIElements.btnSave.off('click');
        UIElements.btnDiscard.off('click');
        UIElements.btnSendClose.off('click');
        UIElements.btnConfirmSend.off('click');
        UIElements.btnCancelEmail.off('click');
        $('.btn-send-menu').off('click');
        $('.btn-schedule-send-menu').off('click');

        // Bind button handlers
        UIElements.btnNewEmail.on('click', handleNewEmail);
        UIElements.btnSend.on('click', handleSendEmail);
        UIElements.btnSave.on('click', handleSaveEmail);
        UIElements.btnDiscard.on('click', handleDiscardEmail);
        UIElements.btnSendClose.on('click', handleCloseEmail);

        // Send dropdown menu items
        $('.btn-send-menu').on('click', function (e) {
            e.preventDefault();
            handleSendEmail(e);
        });

        $('.btn-schedule-send-menu').on('click', function (e) {
            e.preventDefault();
            // Reset schedule state and open modal
            scheduleState.currentMonth = new Date().getMonth();
            scheduleState.currentYear = new Date().getFullYear();
            scheduleState.selectedDate = null;
            scheduleState.selectedTime = null;
            openScheduleModal(scheduleState);
        });

        UIElements.btnConfirmSend.on('click', handleConfirmSendEmail);
        UIElements.btnCancelEmail.on('click', handleCancelEmailSend);
        UIElements.inputEmailSubject.on('change', handleKeyUpTrim);
        UIElements.inputEmailFrom.on('change', handleKeyUpTrim);
        UIElements.inputEmailCC.on('change', handleKeyUpTrim);
        UIElements.inputEmailBCC.on('change', handleKeyUpTrim);
        UIElements.inputEmailBody.on('change', handleKeyUpTrim);
        UIElements.inputEmailTo.on('change', validateEmailTo);
        UIElements.inputEmailCC.on('change', validateEmailCC);
        UIElements.inputEmailBCC.on('change', validateEmailBCC);

        // Add real-time validation on input event (as user types) - show errors without toast
        UIElements.inputEmailTo.on('input', function () {
            validateEmailFieldWithOptions(UIElements.inputEmailToField, false, false, false); // showToast=false, onlyShowErrorsIfHasContent=false
        });
        UIElements.inputEmailCC.on('input', function () {
            validateEmailFieldWithOptions(UIElements.inputEmailCC[0], false, false, false);
        });
        UIElements.inputEmailBCC.on('input', function () {
            validateEmailFieldWithOptions(UIElements.inputEmailBCC[0], false, false, false);
        });

        // Add blur event handler to validate and show errors when leaving field
        UIElements.inputEmailTo.on('blur', function () {
            validateEmailFieldWithOptions(UIElements.inputEmailToField, false, false, false); // showToast=false
        });
        UIElements.inputEmailCC.on('blur', function () {
            validateEmailFieldWithOptions(UIElements.inputEmailCC[0], false, false, false);
        });
        UIElements.inputEmailBCC.on('blur', function () {
            validateEmailFieldWithOptions(UIElements.inputEmailBCC[0], false, false, false);
        });

        UIElements.inputEmailTo.on('input', handleDraftChange);
        UIElements.inputEmailCC.on('input', handleDraftChange);
        UIElements.inputEmailBCC.on('input', handleDraftChange);
        UIElements.inputEmailFrom.on('input', handleDraftChange);
        UIElements.inputEmailSubject.on('input', handleDraftChange);
        UIElements.inputEmailBody.on('input', handleDraftChange);

        // BCC button and input handlers
        UIElements.btnShowBCC.on('click', handleShowBCC);
        UIElements.inputEmailBCC.on('input', toggleBCCVisibility);
        UIElements.inputEmailBCC.on('change', toggleBCCVisibility);

        bindDelayModeEvents();

        $('.details-scrollable').on('scroll.emailWidget', function () {
            $('.tox-toolbar__overflow').hide();
        });

        // Initialize BCC visibility on load
        toggleBCCVisibility();
    }

    init();

    function initializeValidator() {
        UIElements.emailForm.validate({
            errorClass: 'field-validation-error',
            validClass: 'field-validation-valid',
            highlight: function (element, errorClass, validClass) {
                $(element).addClass('input-validation-error').removeClass(validClass);
            },
            unhighlight: function (element, errorClass, validClass) {
                $(element).removeClass('input-validation-error').addClass(validClass);
            },
            errorPlacement: function (error, element) {
                // Create or update error span after the element
                let errorSpan = element.siblings('.field-validation-error');
                if (errorSpan.length === 0) {
                    errorSpan = $('<span class="field-validation-error"></span>').insertAfter(element);
                }
                errorSpan.text(error.text());
            }
        });
    }

    function init() {
        bindUIEvents();
        initializeValidator();
        defaultValues.emailTo = UIElements.inputOriginalEmailTo.val();
        defaultValues.emailFrom = UIElements.inputOriginalEmailFrom.val();
        defaultValues.emailCC = UIElements.inputOriginalEmailCC.val() || '';
        defaultValues.emailBCC = UIElements.inputOriginalEmailBCC.val() || '';
        if (globalThis.toastr) { toastr.options.positionClass = 'toast-top-center'; }
        initTemplateDetails();
        $('#templateTextContainer').hide();
        $('#scheduled-delay-section').hide();
        UIElements.btnSave.hide();
        UIElements.btnSend.hide();
        UIElements.btnSendDropdown.hide();
        UIElements.btnDiscard.hide();
        UIElements.btnSendClose.hide();
    }

    async function initTemplateDetails() {
        applicationDetails = await loadApplicationDetails();
        mappingConfig = await getTemplateVariables();
    }

    function disableEmail() {
        UIElements.btnSend.prop('disabled', true);
        UIElements.btnSendDropdown.prop('disabled', true);
        UIElements.btnSave.prop('disabled', true);
        UIElements.btnDiscard.prop('disabled', true);
        UIElements.inputEmailTo.prop('disabled', true);
        UIElements.inputEmailCC.prop('disabled', true);
        UIElements.inputEmailBCC.prop('disabled', true);
        UIElements.inputEmailFrom.prop('disabled', true);
        UIElements.inputEmailSubject.prop('disabled', true);
        UIElements.inputEmailBody.prop('disabled', true);

        // Make TinyMCE read-only if it exists
        const editor = tinymce.get('EmailBody');
        editor?.mode.set('readonly');
    }

    function enableEmail() {
        UIElements.btnSend.prop('disabled', false);
        UIElements.btnSendDropdown.prop('disabled', false);
        UIElements.btnSave.prop('disabled', false);
        UIElements.btnDiscard.prop('disabled', false);
        UIElements.inputEmailTo.prop('disabled', false);
        UIElements.inputEmailCC.prop('disabled', false);
        UIElements.inputEmailBCC.prop('disabled', false);
        UIElements.inputEmailFrom.prop('disabled', false);
        UIElements.inputEmailSubject.prop('disabled', false);
        UIElements.inputEmailBody.prop('disabled', false);

        // Make TinyMCE editable if it exists
        const editor = tinymce.get('EmailBody');
        editor?.mode.set('design');
    }



    function bindDelayModeEvents() {
        // Use global scheduleState
        scheduleState.currentMonth = new Date().getMonth();
        scheduleState.currentYear = new Date().getFullYear();
        scheduleState.selectedDate = null;
        scheduleState.selectedTime = null;

        // Initialize time dropdown with 30-minute intervals
        initializeTimeDropdown();

        // Open modal
        UIElements.btnOpenScheduleModal.on('click', function () {
            openScheduleModal(scheduleState);
        });

        // Close modal
        UIElements.btnScheduleModalClose.on('click', function () {
            closeScheduleModal();
        });
        UIElements.btnScheduleCancel.on('click', function () {
            closeScheduleModal();
        });
        UIElements.scheduleModalBackdrop.on('click', function () {
            closeScheduleModal();
        });

        // Calendar navigation
        UIElements.btnCalendarPrev.on('click', function () {
            scheduleState.currentMonth--;
            if (scheduleState.currentMonth < 0) {
                scheduleState.currentMonth = 11;
                scheduleState.currentYear--;
            }
            renderCalendarGrid(scheduleState);
        });

        UIElements.btnCalendarNext.on('click', function () {
            scheduleState.currentMonth++;
            if (scheduleState.currentMonth > 11) {
                scheduleState.currentMonth = 0;
                scheduleState.currentYear++;
            }
            renderCalendarGrid(scheduleState);
        });

        // Date input two-way binding with input masking for MM/DD/YYYY
        UIElements.scheduleDateInput.on('input', function () {
            let val = UIElements.scheduleDateInput.val().replaceAll(/\D/, '');
            if (val.length > 8) val = val.substring(0, 8);
            if (val.length >= 2) {
                val = val.substring(0, 2) + '/' + val.substring(2);
            }
            if (val.length >= 5) {
                val = val.substring(0, 5) + '/' + val.substring(5);
            }
            UIElements.scheduleDateInput.val(val);

            // Validate immediately when a complete date is entered (MM/DD/YYYY = 10 chars)
            if (val.length === 10) {
                validateScheduleDate();
            } else if (val.length < 10 && val.length > 0) {
                // Clear validation messages while user is still typing an incomplete date
                UIElements.scheduleDateValidation.hide();
            }
        });

        function validateScheduleDate() {
            const dateStr = UIElements.scheduleDateInput.val();
            // Clear any previous validation messages and error toasts
            UIElements.scheduleDateValidation.removeClass('text-success').removeClass('text-danger').text('').hide();

            if (dateStr?.length === 10) {
                const [month, day, year] = dateStr.split('/').map(Number);
                if (isValidDate(month, day, year)) {
                    scheduleState.selectedDate = new Date(year, month - 1, day);
                    scheduleState.currentMonth = month - 1;
                    scheduleState.currentYear = year;
                    renderCalendarGrid(scheduleState);
                    UIElements.scheduleDateValidation.text('✓ Valid date').removeClass('text-danger').addClass('text-success').show();
                } else {
                    const errorMsg = '✗ Invalid date';
                    UIElements.scheduleDateValidation.text(errorMsg).removeClass('text-success').addClass('text-danger').show();
                    showValidationErrorToast([errorMsg]);
                }
            } else if (dateStr) {
                const errorMsg = '✗ Invalid date format (use MM/DD/YYYY)';
                UIElements.scheduleDateValidation.text(errorMsg).removeClass('text-success').addClass('text-danger').show();
                showValidationErrorToast([errorMsg]);
            } else {
                UIElements.scheduleDateValidation.hide();
            }
        }

        UIElements.scheduleDateInput.on('blur', function () {
            validateScheduleDate();
        });

        // Time dropdown
        UIElements.scheduleTimeSelect.on('change', function () {
            scheduleState.selectedTime = UIElements.scheduleTimeSelect.val();
            UIElements.scheduleModalValidation.hide();
        });

        // Confirm button
        UIElements.btnScheduleConfirm.on('click', function () {
            confirmScheduleDateTime(scheduleState);
        });

        // Clear button
        UIElements.btnClearSchedule.on('click', function () {
            clearScheduleValue();
        });


    }

    function initializeTimeDropdown() {
        const times = [];
        for (let hour = 0; hour < 24; hour++) {
            for (let minute = 0; minute < 60; minute += 30) {
                const ampm = hour >= 12 ? 'PM' : 'AM';
                const displayHour = hour % 12 === 0 ? 12 : hour % 12;
                const timeStr = `${displayHour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')} ${ampm}`;
                const timeValue = `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
                times.push({ label: timeStr, value: timeValue });
            }
        }

        UIElements.scheduleTimeSelect.empty().append('<option value="">Select a time</option>');
        times.forEach(t => {
            UIElements.scheduleTimeSelect.append(`<option value="${t.value}">${t.label}</option>`);
        });
    }

    function getMaxScheduleDate() {
        const max = new Date();
        max.setFullYear(max.getFullYear() + 50);
        return max;
    }

    function renderCalendarGrid(state) {
        // Update header
        const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
        UIElements.calendarMonthYear.text(`${monthNames[state.currentMonth]} ${state.currentYear}`);

        // Clear grid
        UIElements.scheduleCalendarGrid.empty();

        // Day headers
        const dayHeaders = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];
        dayHeaders.forEach(day => {
            UIElements.scheduleCalendarGrid.append(`<div class="calendar-day-header">${day}</div>`);
        });

        // Get first day of month and number of days
        const firstDay = new Date(state.currentYear, state.currentMonth, 1).getDay();
        const daysInMonth = new Date(state.currentYear, state.currentMonth + 1, 0).getDate();
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const maxDate = getMaxScheduleDate();
        maxDate.setHours(0, 0, 0, 0);

        // Add empty cells for days before month starts
        for (let i = 0; i < firstDay; i++) {
            UIElements.scheduleCalendarGrid.append('<div></div>');
        }

        // Add day cells
        for (let day = 1; day <= daysInMonth; day++) {
            const cellDate = new Date(state.currentYear, state.currentMonth, day);
            cellDate.setHours(0, 0, 0, 0);
            const isPast = cellDate < today;
            const isFuture = cellDate > maxDate;
            const isToday = cellDate.getTime() === today.getTime();
            const isSelected = state.selectedDate?.getTime() === cellDate.getTime();

            let classes = 'calendar-day';
            if (isPast) classes += ' past';
            if (isFuture) classes += ' past';
            if (isToday) classes += ' today';
            if (isSelected) classes += ' selected';

            const $dayCell = $(`<div class="${classes}">${day}</div>`);

            if (!isPast && !isFuture) {
                $dayCell.on('click', function () {
                    // Clear error immediately when user clicks a date
                    UIElements.scheduleDateValidation.removeClass('text-success').removeClass('text-danger').text('').hide();

                    state.selectedDate = new Date(state.currentYear, state.currentMonth, day);
                    const month = (state.currentMonth + 1).toString().padStart(2, '0');
                    const dayStr = day.toString().padStart(2, '0');
                    UIElements.scheduleDateInput.val(`${month}/${dayStr}/${state.currentYear}`);
                    validateScheduleDate();
                    renderCalendarGrid(state);
                });
            }

            UIElements.scheduleCalendarGrid.append($dayCell);
        }
    }

    function openScheduleModal(state) {
        // Show modal and backdrop first
        UIElements.scheduleModalBackdrop.show();
        UIElements.scheduleModal.addClass('active');

        // Bind Escape key only when modal is open
        $(document).off('keydown.scheduleModal').on('keydown.scheduleModal', function (e) {
            if (e.key === 'Escape') {
                closeScheduleModal();
            }
        });

        // Pre-populate with existing value if set, otherwise default to today
        const existing = UIElements.inputSendOnDateTime.val();
        if (existing) {
            const existingDate = new Date(existing);
            const bcPstDate = new Date(existingDate.toLocaleString('en-US', { timeZone: 'America/Vancouver' }));
            state.selectedDate = bcPstDate;
            state.currentMonth = bcPstDate.getMonth();
            state.currentYear = bcPstDate.getFullYear();
            const month = (bcPstDate.getMonth() + 1).toString().padStart(2, '0');
            const day = bcPstDate.getDate().toString().padStart(2, '0');
            UIElements.scheduleDateInput.val(`${month}/${day}/${bcPstDate.getFullYear()}`);
            const hour = bcPstDate.getHours().toString().padStart(2, '0');
            const minute = bcPstDate.getMinutes().toString().padStart(2, '0');
            UIElements.scheduleTimeSelect.val(`${hour}:${minute}`);
            state.selectedTime = `${hour}:${minute}`;
        } else {
            // Default to today + 1 day at 9:00 AM
            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 1);
            tomorrow.setHours(9, 0, 0, 0);

            state.selectedDate = tomorrow;
            state.currentMonth = tomorrow.getMonth();
            state.currentYear = tomorrow.getFullYear();

            const month = (tomorrow.getMonth() + 1).toString().padStart(2, '0');
            const day = tomorrow.getDate().toString().padStart(2, '0');
            UIElements.scheduleDateInput.val(`${month}/${day}/${tomorrow.getFullYear()}`);
            UIElements.scheduleTimeSelect.val('09:00');
            state.selectedTime = '09:00';
        }

        // Use setTimeout to ensure modal is visible before rendering
        setTimeout(function () {
            renderCalendarGrid(state);
            UIElements.scheduleModalValidation.hide();
            UIElements.scheduleDateValidation.hide();
            UIElements.scheduleDateInput.focus();
        }, 100);
    }

    function closeScheduleModal() {
        UIElements.scheduleModal.removeClass('active');
        UIElements.scheduleModalBackdrop.hide();
        $(document).off('keydown.scheduleModal');
        // Clear modal validation message
        UIElements.scheduleModalValidation.hide();
    }

    function confirmScheduleDateTime(state) {
        if (!state.selectedDate || !state.selectedTime) {
            const errorMsg = 'Please select both a date and time.';
            UIElements.scheduleModalValidation.text(errorMsg).show();
            showValidationErrorToast([errorMsg]);
            return; // Keep modal open on validation error
        }

        // Combine date and time
        const [hours, minutes] = state.selectedTime.split(':').map(Number);
        const bcPstDate = new Date(state.selectedDate);
        bcPstDate.setHours(hours, minutes, 0, 0);

        // Validate future date/time
        const now = new Date();
        if (bcPstDate <= now) {
            const errorMsg = 'Please select a future date and time.';
            UIElements.scheduleModalValidation.text(errorMsg).show();
            showValidationErrorToast([errorMsg]);
            return; // Keep modal open on validation error
        }

        // Validate max date (50 years from today)
        const maxDate = getMaxScheduleDate();
        if (bcPstDate > maxDate) {
            const errorMsg = 'Please select a date within 50 years from today.';
            UIElements.scheduleModalValidation.text(errorMsg).show();
            showValidationErrorToast([errorMsg]);
            return; // Keep modal open on validation error
        }

        // Convert BC PST time to UTC ISO string
        // First, get the offset between BC PST and UTC
        const formatter = new Intl.DateTimeFormat('en-US', {
            timeZone: 'America/Vancouver',
            year: 'numeric', month: '2-digit', day: '2-digit',
            hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
        });

        const bcPstString = formatter.format(bcPstDate);
        const [datePart, timePart] = bcPstString.split(',');
        const [m, d, y] = datePart.trim().split('/');
        const [h, min, s] = timePart.trim().split(':');

        // Create a local date from the user-entered components (not UTC)
        const localDate = new Date(Number.parseInt(y), Number.parseInt(m) - 1, Number.parseInt(d), Number.parseInt(h), Number.parseInt(min), Number.parseInt(s));

        // Convert local time to UTC for storage
        const utcDate = new Date(localDate.getTime() - localDate.getTimezoneOffset() * 60000);
        const utcIso = utcDate.toISOString();

        // Commit to hidden field
        UIElements.inputSendOnDateTime.val(utcIso);

        // Display as local time (no timezone conversion needed since we're storing as UTC)
        const formattedDate = localDate.toLocaleDateString('en-CA'); // YYYY-MM-DD format
        const formattedTime = localDate.toLocaleTimeString('en-US', {
            hour: 'numeric',
            minute: '2-digit',
            hour12: true
        });
        UIElements.sendOnDisplay.text(`${formattedDate}, ${formattedTime}`);
        UIElements.btnClearSchedule.show();
        $('#scheduled-label-container').addClass('show');
        $('#scheduled-delay-section').show();
        UIElements.delayDateTimeValidation.hide();
        closeScheduleModal();
    }



    function updateScheduledDateDisplay() {
        const dateTimeValue = UIElements.inputSendOnDateTime.val();
        if (dateTimeValue) {
            // Parse the ISO string and format as local time without timezone conversion
            const date = new Date(dateTimeValue);
            const formattedDate = date.toLocaleDateString('en-CA'); // YYYY-MM-DD format
            const formattedTime = date.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
            });
            UIElements.sendOnDisplay.text(`${formattedDate}, ${formattedTime}`);
            UIElements.btnClearSchedule.show();
            $('#scheduled-label-container').addClass('show');
            $('#scheduled-delay-section').show();
        } else {
            UIElements.sendOnDisplay.text('');
            UIElements.btnClearSchedule.hide();
            $('#scheduled-label-container').removeClass('show');
            $('#scheduled-delay-section').hide();
        }
    }

    function clearScheduleValue() {
        UIElements.inputSendOnDateTime.val('');
        UIElements.sendOnDisplay.text('');
        UIElements.btnClearSchedule.hide();
        $('#scheduled-label-container').removeClass('show');
        $('#scheduled-delay-section').hide();
        UIElements.delayDateTimeValidation.hide();
        UIElements.scheduleDateInput.val('');
        UIElements.scheduleTimeSelect.val('');
    }

    function closeEmailFormUI() {
        // Close any open dropdowns
        const dropdownIds = ['btn-send-dropdown'];
        dropdownIds.forEach(id => {
            bootstrap.Dropdown.getInstance(document.getElementById(id))?.hide();
        });

        // Clear all validation errors
        UIElements.emailForm.find('.input-validation-error').removeClass('input-validation-error').addClass('field-validation-valid');
        UIElements.emailForm.find('.field-validation-error').html('').removeClass('field-validation-error').addClass('field-validation-valid');

        // Reset jQuery validator state
        const validator = UIElements.emailForm.validate();
        validator?.resetForm();

        // Clear toastr notifications
        toastr?.clear();

        $('#modal-content, #modal-background').removeClass('active');
        UIElements.emailForm.removeClass('active');
        UIElements.btnNewEmail.removeClass('hide');
        UIElements.alertEmailReadonly.removeClass('hide');
        UIElements.emailForm.trigger("reset");
        clearScheduleValue();
        $('#email-attachments-section').hide();
        enableEmail();
        UIElements.btnSave.hide();
        UIElements.btnSend.hide();
        UIElements.btnSendDropdown.hide();
        UIElements.btnDiscard.hide();
        UIElements.btnSendClose.hide();
        UIElements.bccInputRow.removeClass('show');
        UIElements.btnShowBCC.removeClass('hide');

        // Clear the stored selected email data when form is closed
        selectedEmailData = null;

        // Reset draft viewing flag
        isViewingDraft = false;
    }

    function handleCloseEmail() {
        if (isNewEmailDraft && newDraftId) {
            $.ajax({ url: `/api/app/email-notification/${newDraftId}/email`, type: 'DELETE' })
                .catch(e => console.warn('Failed to delete draft on close:', e));
            isNewEmailDraft = false;
            newDraftId = null;
        }
        closeEmailFormUI();
    }

    function handleDiscardEmail() {
        // If it's a new email draft, delete it and reset the form
        if (isNewEmailDraft && newDraftId) {
            $.ajax({
                url: `/api/app/email-notification/${newDraftId}/email`,
                type: 'DELETE'
            })
                .done(() => {
                    isNewEmailDraft = false;
                    newDraftId = null;
                    // Reset all fields to empty
                    UIElements.inputEmailTo.val('');
                    UIElements.inputEmailCC.val('');
                    UIElements.inputEmailBCC.val('');
                    UIElements.inputEmailFrom.val('');
                    UIElements.inputEmailSubject.val('');
                    UIElements.inputEmailBody.val('');
                    // Reset TinyMCE editor
                    if (tinymce.get("EmailBody")) {
                        tinymce.get("EmailBody").setContent('');
                    }
                    // Reset original values
                    UIElements.inputOriginalEmailTo.val('');
                    UIElements.inputOriginalEmailCC.val('');
                    UIElements.inputOriginalEmailBCC.val('');
                    UIElements.inputOriginalEmailFrom.val('');
                    UIElements.inputOriginalEmailSubject.val('');
                    UIElements.inputOriginalEmailBody.val('');
                    // Clear template
                    $('#EmailTemplate').val('').trigger('change');
                    // Reset scheduled send
                    clearScheduleValue();
                    // Reset validation errors
                    resetValidationErrors();
                    // Reset draft change state
                    handleDraftChange();
                    // Reset BCC visibility
                    toggleBCCVisibility();
                    // Show success toast
                    if (globalThis.toastr) {
                        toastr.success('Changes discarded', 'Email Reset');
                    }
                })
                .fail(e => {
                    console.warn('Failed to delete draft on discard:', e);
                    // Still reset the form even if delete fails
                    isNewEmailDraft = false;
                    newDraftId = null;
                    // Reset all fields to empty
                    UIElements.inputEmailTo.val('');
                    UIElements.inputEmailCC.val('');
                    UIElements.inputEmailBCC.val('');
                    UIElements.inputEmailFrom.val('');
                    UIElements.inputEmailSubject.val('');
                    UIElements.inputEmailBody.val('');
                    // Reset TinyMCE editor
                    if (tinymce.get("EmailBody")) {
                        tinymce.get("EmailBody").setContent('');
                    }
                    // Reset original values
                    UIElements.inputOriginalEmailTo.val('');
                    UIElements.inputOriginalEmailCC.val('');
                    UIElements.inputOriginalEmailBCC.val('');
                    UIElements.inputOriginalEmailFrom.val('');
                    UIElements.inputOriginalEmailSubject.val('');
                    UIElements.inputOriginalEmailBody.val('');
                    // Clear template
                    $('#EmailTemplate').val('').trigger('change');
                    // Reset scheduled send
                    clearScheduleValue();
                    // Reset validation errors
                    resetValidationErrors();
                    // Reset draft change state
                    handleDraftChange();
                    // Reset BCC visibility
                    toggleBCCVisibility();
                });
        } else {
            // Reset existing email to stored original data
            if (selectedEmailData) {
                UIElements.inputEmailTo.val(selectedEmailData.toAddress);
                UIElements.inputEmailCC.val(selectedEmailData.cc?.replaceAll(',', '; ') ?? '');
                UIElements.inputEmailBCC.val(selectedEmailData.bcc?.replaceAll(',', '; ') ?? '');
                UIElements.inputEmailFrom.val(selectedEmailData.fromAddress);
                UIElements.inputEmailSubject.val(selectedEmailData.subject);
                UIElements.inputEmailBody.val(refreshTodayDateSpans(selectedEmailData.body));

                // Reset TinyMCE editor
                tinymce.get("EmailBody")?.setContent(refreshTodayDateSpans(selectedEmailData.body));

                // Reset scheduled send date/time to original state
                if (selectedEmailData.sendOnDateTime) {
                    UIElements.inputSendOnDateTime.val(selectedEmailData.sendOnDateTime);
                    updateScheduledDateDisplay();
                } else {
                    clearScheduleValue();
                }
            } else {
                // Fallback to hidden inputs if selectedEmailData is not available
                UIElements.inputEmailTo.val(UIElements.inputOriginalEmailTo.val());
                UIElements.inputEmailCC.val(UIElements.inputOriginalEmailCC.val());
                UIElements.inputEmailBCC.val(UIElements.inputOriginalEmailBCC.val());
                UIElements.inputEmailFrom.val(UIElements.inputOriginalEmailFrom.val());
                UIElements.inputEmailSubject.val(UIElements.inputOriginalEmailSubject.val());
                UIElements.inputEmailBody.val(UIElements.inputOriginalEmailBody.val());

                // Reset TinyMCE editor
                tinymce.get("EmailBody")?.setContent(UIElements.inputOriginalEmailBody.val());

                // Reset scheduled send date/time to original state
                clearScheduleValue();
            }

            // Clear validation errors
            resetValidationErrors();

            // Show success toast
            if (globalThis.toastr) {
                toastr.success('Changes discarded', 'Email Reset');
            }

            // Disable save and discard buttons since no changes are present
            handleDraftChange();
        }
    }

    function handleCancelEmailSend() {
        $('#modal-content, #modal-background').removeClass('active');
    }


    function resetEmailBody() {
        const id = 'EmailBody';

        // 1. Remove any existing editor instance
        if (tinymce.get(id)) {
            tinymce.get(id).remove();
        }

        // 2. Clear the underlying <textarea>
        $('#' + id).val('');           // ← this line prevents the old HTML from coming back
    }

    async function initializeDraft(applicationId) {
        const emailNotificationService = unity?.notifications?.emailNotifications?.emailNotification;

        if (emailNotificationService?.initializeDraft) {
            return await emailNotificationService.initializeDraft(applicationId);
        }

        return await $.ajax({
            url: `/api/app/email-notification/initialize-draft?applicationId=${applicationId}`,
            type: 'POST'
        });
    }

    function handleShowBCC() {
        // Show BCC input row and hide the button
        UIElements.bccInputRow.addClass('show');
        UIElements.btnShowBCC.addClass('hide');
        UIElements.inputEmailBCC.focus();
    }

    function toggleBCCVisibility() {
        const bccValue = UIElements.inputEmailBCC.val().trim();

        if (bccValue) {
            // If BCC has value, show row and hide button
            UIElements.bccInputRow.addClass('show');
            UIElements.btnShowBCC.addClass('hide');
        } else {
            // If BCC is empty, hide row and show button
            UIElements.bccInputRow.removeClass('show');
            UIElements.btnShowBCC.removeClass('hide');
        }
    }

    async function handleNewEmail() {
        isViewingDraft = true; // New emails are drafts
        resetEmailBody();
        $('#templateListContainer').show();
        $('#templateTextContainer').hide();
        $('#EmailTemplate').prop('disabled', false);
        UIElements.inputOriginalEmailTo.val(defaultValues.emailTo);
        UIElements.inputOriginalEmailCC.val(defaultValues.emailCC);
        UIElements.inputOriginalEmailBCC.val(defaultValues.emailBCC);
        UIElements.inputOriginalEmailFrom.val(defaultValues.emailFrom);
        UIElements.inputOriginalEmailSubject.val("");
        UIElements.inputOriginalEmailBody.val("");

        try {
            const draftId = await initializeDraft(UIElements.applicationId);
            UIElements.inputEmailId.val(draftId);
            isNewEmailDraft = true;
            newDraftId = draftId;
        } catch (e) {
            console.warn('Failed to initialize email draft:', e);
            abp.notify.error('Failed to create email draft. Please try again.');
            return;
        }

        tinymce.get("EmailBody")?.remove();

        tinymce.init({
            license_key: 'gpl',
            selector: '#EmailBody',
            plugins: getPlugins(),
            toolbar: getToolbarOptions(),
            resize: true,
            statusbar: true,
            elementpath: false,
            branding: false,
            promotion: false,
            content_css: false,
            skin: false,
            ui_container: '.details-scrollable',
            setup: function (editor) {
                editor.on("input", (e) => {
                    UIElements.inputEmailBody.val(editor.getContent());
                    handleDraftChange();
                });
                editorInstance = editor;
                editorInstance.setContent('');
            }
        });

        $('#email-attachments-section').show();
        $('#email_attachment_upload_btn').show();
        initEmailAttachmentsTable(UIElements.inputEmailId.val(), true);
        handleDraftChange();
        showModalEmail();
        resetValidationErrors();
    }

    function showModalEmail() {
        UIElements.emailForm.addClass('active');
        UIElements.btnNewEmail.addClass('hide');
        UIElements.alertEmailReadonly.addClass('hide');
        UIElements.btnSave.show();
        UIElements.btnSend.show();
        UIElements.btnSendDropdown.show();
        UIElements.btnDiscard.show();
        UIElements.btnSendClose.show();
        toggleBCCVisibility();
    }

    function validateEmailTo(suppressToast = false) {
        const result = validateEmailField(UIElements.inputEmailToField, true, !suppressToast);
        return suppressToast ? result : result.isValid; // For backward compatibility
    }

    function validateEmailCC(suppressToast = false) {
        const result = validateEmailField(UIElements.inputEmailCC[0], false, !suppressToast);
        return suppressToast ? result : result.isValid; // For backward compatibility
    }

    function validateEmailBCC(suppressToast = false) {
        const result = validateEmailField(UIElements.inputEmailBCC[0], false, !suppressToast);
        return suppressToast ? result : result.isValid; // For backward compatibility
    }


    function handleConfirmSendEmail() {
        UIElements.confirmationModal.hide();
        UIElements.emailSpinner.show();
        let templateName = '';
        if (isNewEmailDraft) {
            templateName = $("#EmailTemplate option:selected").text().trim();
            // Check for all placeholder variations
            if (!templateName || templateName === '' || templateName === 'Please select' || templateName === 'Select a template' || templateName.toLowerCase().includes('select')) {
                templateName = "No Template Selected";
            }
        } else {
            templateName = $('#EmailTemplateName').val();
        }

        const rawDateTime = UIElements.inputSendOnDateTime.length ? UIElements.inputSendOnDateTime.val() : '';
        // Hidden field already holds the UTC ISO string set when the modal was confirmed.
        const sendOnDateTime = rawDateTime || null;

        // Ensure body content is synced from editor
        let emailBody = '';
        if (editorInstance) {
            emailBody = editorInstance.getContent();
        } else if (tinymce.get('EmailBody')) {
            emailBody = tinymce.get('EmailBody').getContent();
        } else {
            emailBody = UIElements.inputEmailBody.val();
        }

        performSendEmail(emailBody, templateName, sendOnDateTime);
    }

    function performSendEmail(emailBody, templateName, sendOnDateTime) {
        $.ajax({
            url: '/api/app/email/send',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                emailId: UIElements.inputEmailId[0].value,
                applicationId: UIElements.applicationId,
                emailTo: UIElements.inputEmailTo[0].value,
                emailCC: UIElements.inputEmailCC[0].value,
                emailBCC: UIElements.inputEmailBCC[0].value,
                emailFrom: UIElements.inputEmailFrom[0].value,
                emailBody: emailBody,
                emailSubject: UIElements.inputEmailSubject[0].value,
                currentUserId: decodeURIComponent(abp.currentUser.id),
                emailTemplateName: templateName,
                sendOnDateTime: sendOnDateTime,
            })
        }).done(function () {
            isNewEmailDraft = false; newDraftId = null;
            hideConfirmation();
            handleCloseEmail();
            abp.notify.success('Your email is being sent');
            PubSub.publish('refresh_application_emails');
        }).fail(function () {
            hideConfirmation();
            abp.notify.error('An error ocurred your email could not be sent.');
        });
    }

    function hideConfirmation() {
        UIElements.confirmationModal.hide();
        UIElements.emailSpinner.hide();
        $('#modal-content, #modal-background').removeClass('active');
    }

    function showConfirmation() {
        UIElements.confirmationModal.show();
        $('#modal-content, #modal-background').addClass('active');
    }

    function handleSaveEmail(e) {
        if (validateEmailForm(e)) {
            // Check if email is scheduled
            const sendOnDateTime = UIElements.inputSendOnDateTime.val();
            if (sendOnDateTime) {
                // Email is scheduled, show confirmation
                Swal.fire({
                    title: 'Scheduled Email',
                    text: 'Scheduled Emails can not be saved as Draft',
                    icon: 'info',
                    confirmButtonText: 'OK'
                });
                UIElements.btnSave.prop('disabled', false);
                return;
            }

            let templateName = '';
            // Check the dropdown for the current selection (handles both new and edited drafts)
            const selectedTemplate = $("#EmailTemplate option:selected").text().trim();
            if (selectedTemplate && selectedTemplate !== '' && selectedTemplate !== 'Please select' && selectedTemplate !== 'Select a template' && !selectedTemplate.toLowerCase().includes('select')) {
                templateName = selectedTemplate;
            } else if (isNewEmailDraft) {
                templateName = "No Template Selected";
            } else {
                // For existing drafts without a valid template selected, keep the original
                templateName = $('#EmailTemplateName').val();
            }

            UIElements.btnSave.prop('disabled', true);

            // Ensure body content is synced from editor
            let emailBody = '';
            if (editorInstance) {
                emailBody = editorInstance.getContent();
            } else if (tinymce.get('EmailBody')) {
                emailBody = tinymce.get('EmailBody').getContent();
            } else {
                emailBody = UIElements.inputEmailBody.val();
            }

            $.ajax({
                url: '/api/app/email/save-draft',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    emailId: UIElements.inputEmailId[0].value,
                    applicationId: UIElements.applicationId,
                    emailTo: UIElements.inputEmailTo[0].value,
                    emailCC: UIElements.inputEmailCC[0].value,
                    emailBCC: UIElements.inputEmailBCC[0].value,
                    emailFrom: UIElements.inputEmailFrom[0].value,
                    emailBody: emailBody,
                    emailSubject: UIElements.inputEmailSubject[0].value,
                    currentUserId: decodeURIComponent(abp.currentUser.id),
                    emailTemplateName: templateName,
                    sendOnDateTime: UIElements.inputSendOnDateTime.val() || null,
                })
            }).done(function () {
                isNewEmailDraft = false; newDraftId = null;
                handleCloseEmail();
                abp.notify.success('Your email has been saved.');
                PubSub.publish('refresh_application_emails');
            }).fail(function () {
                UIElements.btnSave.prop('disabled', false);
                abp.notify.error('An error ocurred your email could not be saved.');
            });
        } else {
            // validateEmailForm() already shows the error toast if there are validation errors
            return false;
        }
    }

    function validateEmailField(fieldElement, isRequired = false, showToast = true, onlyShowErrorsIfHasContent = false) {
        // Get the field's value and trim whitespace
        let emailValue = fieldElement.value.trim();

        // Remove trailing commas, semicolons, or spaces
        emailValue = emailValue.replace(/[;,\s]+$/, '');

        // If the field is empty and not required, clear any existing errors
        if (!isRequired && emailValue === '') {
            return clearFieldErrors(fieldElement);
        }

        // Split by comma or semicolon, and trim each email
        let emails = emailValue.split(/[,;]/g).map(email => email.trim());
        let fieldName = fieldElement.name;
        let errorSpan = getOrCreateErrorSpan(fieldElement, fieldName);

        // Validate each email
        const validationResult = validateEmailsInField(emails, emailValue, fieldName, errorSpan, fieldElement, isRequired, showToast);
        return validationResult;
    }

    function clearFieldErrors(fieldElement) {
        let errorSpan = $(`span[data-valmsg-for*='${fieldElement.name}']`)[0];
        if (errorSpan) {
            $(errorSpan).addClass('field-validation-valid').removeClass('field-validation-error');
            $(errorSpan).html('');
        }
        $(fieldElement).removeClass('input-validation-error');
        return { isValid: true, error: null };
    }

    function getOrCreateErrorSpan(fieldElement, fieldName) {
        let errorSpan = $(`span[data-valmsg-for*='${fieldName}']`)[0];

        if (!errorSpan) {
            errorSpan = document.createElement('span');
            errorSpan.className = 'field-validation-valid';
            errorSpan.dataset.valmsigFor = fieldName;
            errorSpan.dataset.valmsigReplace = 'true';
            fieldElement.parentNode.appendChild(errorSpan);
        }
        return errorSpan;
    }

    function validateEmailsInField(emails, emailValue, fieldName, errorSpan, fieldElement, isRequired, showToast) {
        for (let emailStr of emails) {
            // Skip empty emails in optional fields
            if (emailStr === '' && !isRequired) continue;

            // Get error message for this email
            const errorMessage = getEmailValidationError(emailStr, emailValue, fieldName);
            if (errorMessage) {
                updateErrorSpanDisplay(errorSpan, fieldElement, errorMessage, showToast);
                return { isValid: false, error: errorMessage };
            }
        }

        // All emails valid - clear errors
        $(errorSpan).addClass('field-validation-valid').removeClass('field-validation-error');
        $(errorSpan).html('');
        $(fieldElement).removeClass('input-validation-error');
        return { isValid: true, error: null };
    }

    function updateErrorSpanDisplay(errorSpan, fieldElement, errorMessage, showToast) {
        $(errorSpan).addClass('field-validation-error').removeClass('field-validation-valid');
        $(errorSpan).html(errorMessage);
        $(fieldElement).addClass('input-validation-error');
        if (showToast) {
            showValidationErrorToast([errorMessage]);
        }
    }


    function getTinyMceContent() {
        let tinymceContent = '';
        if (editorInstance) {
            tinymceContent = editorInstance.getContent({ format: 'text' }).trim();
        } else {
            tinymceContent = tinymce.get('EmailBody')?.getContent({ format: 'text' }).trim() || UIElements.inputEmailBody.val().trim();
        }
        return tinymceContent;
    }


    function handleEmailFormValidationResult(isValid, allErrors, errorList, fieldName, onlyErrorIsTinyMCE) {
        if (onlyErrorIsTinyMCE) {
            let fieldElement = $('textarea[name="' + fieldName + '"]');
            fieldElement.removeClass('input-validation-error');
            fieldElement.siblings('.field-validation-error').remove();
            if (allErrors.length > 0) {
                showValidationErrorToast(allErrors);
                return false;
            }
            return true;
        }

        if (isValid && allErrors.length === 0) return true;

        // Collect jQuery validator errors
        const formErrors = errorList.map(err => normalizeErrorMessage(err.message));
        formErrors.forEach(err => {
            if (!allErrors.includes(err)) allErrors.push(err);
        });

        if (allErrors.length > 0) {
            showValidationErrorToast(allErrors);
        }
        return false;
    }

    function validateEmailForm(e) {
        // Prevent form submission and stop propagation
        e.stopPropagation();
        e.preventDefault();

        // Collect all errors without showing toasts immediately
        let toResult = validateEmailTo(true); // suppressToast = true
        let ccResult = validateEmailCC(true);
        let bccResult = validateEmailBCC(true);
        let allErrors = collectEmailFieldErrors(toResult, ccResult, bccResult);

        console.log('Email field validation results:', { toResult, ccResult, bccResult });
        console.log('Collected email errors:', allErrors);

        // Sync TinyMCE content to textarea BEFORE running jQuery validation
        if (editorInstance) {
            UIElements.inputEmailBody.val(editorInstance.getContent());
        } else {
            UIElements.inputEmailBody.val(tinymce.get('EmailBody')?.getContent() || UIElements.inputEmailBody.val());
        }

        // Run jQuery validation for other required fields (Subject, Body, From)
        let isValid = UIElements.emailForm.valid();
        let validator = UIElements.emailForm.validate();
        let fieldName = 'EmailBody';
        let errorList = validator.errorList;

        // Get TinyMCE content and check validation state
        let tinymceContent = getTinyMceContent();
        let onlyErrorIsTinyMCE = checkOnlyErrorIsTinyMCE(isValid, errorList, fieldName, tinymceContent);

        // Handle validation result using helper
        return handleEmailFormValidationResult(isValid, allErrors, errorList, fieldName, onlyErrorIsTinyMCE);
    }

    function handleSendEmail(e) {
        // Check if the form is valid
        if (validateEmailForm(e)) {
            showConfirmation(); // Show confirmation if the form is valid
            return true; // Return true to indicate success
        }
        // validateEmailForm() already shows the error toast if there are validation errors
        return false;
    }

    function handleDraftChange() {
        // Only allow draft changes if we're viewing a draft email
        if (!isViewingDraft) {
            console.log('handleDraftChange: Returning early because isViewingDraft is false');
            return;
        }
        console.log('handleDraftChange: Processing for draft email');
        const isDraftChanged = checkDraftChanges();
        UIElements.btnSave.prop('disabled', !isDraftChanged);
        UIElements.btnDiscard.prop('disabled', !isDraftChanged);
    }

    function checkDraftChanges() {
        return UIElements.inputEmailTo.val() !== UIElements.inputOriginalEmailTo.val() ||
            UIElements.inputEmailCC.val() !== UIElements.inputOriginalEmailCC.val() ||
            UIElements.inputEmailBCC.val() !== UIElements.inputOriginalEmailBCC.val() ||
            UIElements.inputEmailFrom.val() !== UIElements.inputOriginalEmailFrom.val() ||
            UIElements.inputEmailSubject.val() !== UIElements.inputOriginalEmailSubject.val() ||
            UIElements.inputEmailBody.val() !== UIElements.inputOriginalEmailBody.val();
    }

    function resetValidationErrors() {
        UIElements.emailForm.find('.field-validation-error').each(function () {
            $(this).removeClass('field-validation-error').addClass('field-validation-valid').html('');
        });
    }


    $('#EmailTemplate').on('change', async function () {
        console.log(this.value);

        try {
            resetValidationErrors();
            let templateDetails = await loadTemplateDetails(this.value);
            const templateData = extractTemplateData(applicationDetails, mappingConfig);
            const template = Handlebars.compile(templateDetails.bodyHTML);

            // Render the compiled template with data
            const renderedHtml = template(templateData);
            $('#EmailFrom').val(templateDetails.sendFrom)
            $('#EmailSubject').val(templateDetails.subject)
            editorInstance.setContent(renderedHtml);

            // Only enable save button if we're viewing a draft
            if (isViewingDraft) {
                UIElements.btnSave.attr('disabled', false);
            }
        } catch (error) {
            console.error("Error loading data:", error);
        }
    });
    function getTemplateVariables() {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: `/api/app/template/template-variables`,
                type: 'GET',
                success: function (response) {
                    resolve(response);
                },
                error: function (xhr, status, error) {
                    reject(error);
                }
            });
        });
    }

    function loadTemplateDetails(templateId) {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: `/api/app/template/${templateId}/template-by-id`,
                type: 'GET',
                success: function (response) {
                    resolve(response);
                },
                error: function (xhr, status, error) {
                    reject(error);
                }
            });
        });
    }

    function loadApplicationDetails() {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: `/api/app/grant-application/${UIElements.applicationId}`,
                type: 'GET',
                success: function (response) {
                    resolve(response);
                },
                error: function (xhr, status, error) {
                    reject(error);
                }
            });
        });
    }


    const currencyFormatter = createCurrencyFormatter();

    function processString(token, inputString) {
        const formatters = {
            currency: ['approved_amount', 'recommended_amount', 'requested_amount'],
            date: ['submission_date', 'approval_date', 'project_start_date', 'project_end_date'],
            lookup: ['category', 'status', 'decline_rationale']
        };

        // Handle currency formatting first
        if (formatters.currency.includes(token)) {
            if (inputString === null || inputString === undefined || inputString === '') {
                return '';
            }

            const numericValue = Number(inputString);
            return Number.isFinite(numericValue) ? currencyFormatter.format(numericValue) : '<ERROR: Invalid Amount>';
        }

        // Return non-string values as-is
        if (typeof inputString !== 'string') {
            return inputString ?? '';
        }

        // Handle date formatting
        if (formatters.date.includes(token)) {
            const dateTime = luxon.DateTime.fromISO(inputString);
            return dateTime.isValid
                ? dateTime.setLocale(abp.localization.currentCulture.name).toUTC().toLocaleString()
                : '';
        }

        // Handle lookup fields to Title Case
        if (formatters.lookup.includes(token)) {
            return toTitleCase(inputString.replaceAll('_', ' '));
        }

        return inputString;
    }

    const buildTodayDateSpan = () => new Handlebars.SafeString(buildTodayDateHtml());

    const buildTodayDateHtml = () => {
        const formatted = new Intl.DateTimeFormat('en-CA', {
            timeZone: 'America/Vancouver',
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        }).format(new Date());
        return `<span data-token="today_date">${formatted}</span>`;
    };

    const refreshTodayDateSpans = (html) => {
        if (!html) return html;
        return html.replaceAll(
            /<span\s[^>]*?data-token="today_date"[^>]*?>[\s\S]*?<\/span>/gi,
            buildTodayDateHtml()
        );
    };

    function extractTemplateData(apiResponse, mappingConfig) {
        const templateData = {};

        mappingConfig.forEach(mapping => {
            const { token, mapTo } = mapping;

            if (!mapTo) {
                templateData[token] = token === 'today_date' ? buildTodayDateSpan() : "";
                return;
            }

            const value = getValueByPath(apiResponse, mapTo);

            let formatValue = processString(token, value);
            templateData[token] = formatValue ?? "";
        });

        return templateData;
    }


    let isViewingDraft = false; // Track if we're viewing a draft email

    PubSub.subscribe('email_selected', (msg, data) => {
        console.log("EMAIL SELECTED EVENT FIRED", data);

        // Determine if this is a draft FIRST, before any field population
        const isDraft = data?.status === 'Draft';
        isViewingDraft = isDraft; // Set flag early to prevent handleDraftChange from enabling buttons
        console.log('isViewingDraft set to:', isViewingDraft);

        // Store the original selected email data for discard functionality
        selectedEmailData = structuredClone(data);

        if (isNewEmailDraft && newDraftId) {
            $.ajax({ url: `/api/app/email-notification/${newDraftId}/email`, type: 'DELETE' })
                .catch(e => console.warn('Failed to delete abandoned draft on email_selected:', e));
            isNewEmailDraft = false;
            newDraftId = null;
        }
        resetValidationErrors();
        console.log("data", data)
        $('#EmailTemplateName').val(data.templateName);
        $('#EmailTemplateName').prop('disabled', true);
        UIElements.inputEmailId.val(data.id);
        UIElements.inputOriginalEmailTo.val(data.toAddress);
        UIElements.inputOriginalEmailCC.val(data.cc?.replaceAll(',', '; ') ?? '');
        UIElements.inputOriginalEmailBCC.val(data.bcc?.replaceAll(',', '; ') ?? '');
        UIElements.inputOriginalEmailFrom.val(data.fromAddress);
        UIElements.inputOriginalEmailSubject.val(data.subject);
        resetEmailBody();
        tinymce.get("EmailBody")?.remove(); // remove existing instance

        tinymce.init({
            license_key: 'gpl',
            selector: '#EmailBody',
            plugins: getPlugins(),
            toolbar: getToolbarOptions(),
            resize: true,
            statusbar: true,
            elementpath: false,
            branding: false,
            promotion: false,
            content_css: false,
            skin: false,
            ui_container: '.details-scrollable',
            setup: function (editor) {
                editor.on("input", (e) => {
                    UIElements.inputEmailBody.val(editor.getContent())
                    handleDraftChange();
                });
                editorInstance = editor;
                editorInstance.setContent(refreshTodayDateSpans(data.body));
            }
        });
        UIElements.inputEmailTo.val(data.toAddress);
        UIElements.inputEmailCC.val(data.cc?.replaceAll(',', '; ') ?? '');
        UIElements.inputEmailBCC.val(data.bcc?.replaceAll(',', '; ') ?? '');
        UIElements.inputEmailFrom.val(data.fromAddress);
        UIElements.inputEmailSubject.val(data.subject);
        UIElements.inputEmailBody.val(refreshTodayDateSpans(data.body));

        // Load scheduled send date/time if available
        if (data.sendOnDateTime) {
            UIElements.inputSendOnDateTime.val(data.sendOnDateTime);
            updateScheduledDateDisplay();
        } else {
            UIElements.inputSendOnDateTime.val('');
            updateScheduledDateDisplay();
        }

        // Set the template dropdown to the current template if available
        // Find the option by its text (template name) since values are GUIDs
        if (data.templateName) {
            const option = $('#EmailTemplate').find('option').filter(function () {
                return $(this).text() === data.templateName;
            });
            if (option.length > 0) {
                // Set without triggering change to avoid overwriting form fields with template defaults
                $('#EmailTemplate').val(option.val());
            }
        }

        // Always show the template dropdown, but disable it for sent emails
        $('#templateListContainer').show();
        $('#EmailTemplate').prop('disabled', !isDraft);

        if (isDraft) {
            enableEmail();
            handleDraftChange();
            $('#email_attachment_upload_btn').show();
        } else {
            disableEmail();
            $('#email_attachment_upload_btn').hide();
            // Don't show clear schedule button for sent emails
            UIElements.btnClearSchedule.hide();

            // Extra safety: ensure buttons stay disabled for sent emails
            setTimeout(() => {
                if (!isViewingDraft) {
                    UIElements.btnSave.prop('disabled', true);
                    UIElements.btnSend.prop('disabled', true);
                    UIElements.btnSendDropdown.prop('disabled', true);
                    UIElements.btnDiscard.prop('disabled', true);
                    console.log('Extra safety check: buttons re-disabled for sent email');
                }
            }, 100);
        }
        $('#email-attachments-section').show();
        initEmailAttachmentsTable(data.id, isDraft);

        console.log("About to call showModalEmail() - emailForm element:", UIElements.emailForm);
        showModalEmail();
        console.log("showModalEmail() called successfully");
        resetValidationErrors();
    });

    PubSub.subscribe(
        'applicant_info_updated',
        (_, ApplicantInfoObj) => {
            if (ApplicantInfoObj + "" !== "undefined"
                && ApplicantInfoObj.ContactEmail + "" != "undefined"
                && ApplicantInfoObj.ContactEmail !== "") {
                UIElements.inputEmailTo[0].value = ApplicantInfoObj.ContactEmail;
            }
        }
    );

    PubSub.subscribe('draft_email_deleted', (msg, data) => {
        if (UIElements.inputEmailId.val() === data.id) {
            isNewEmailDraft = false; newDraftId = null;
            closeEmailFormUI();
        }
    });

    PubSub.subscribe('reload_email_attachments_table', () => {
        reloadEmailAttachmentsTable();
    });

    function initEmailAttachmentsTable(emailLogId, isDraft) {
        if (emailAttachmentsTable) {
            emailAttachmentsTable.destroy();
            emailAttachmentsTable = null;
        }

        emailAttachmentsTable = $('#EmailAttachmentsTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                serverSide: false,
                order: [[2, 'asc']],
                searching: false,
                paging: false,
                select: false,
                info: false,
                scrollX: true,
                scrollY: '200px',
                scrollCollapse: true,
                ajax: abp.libs.datatables.createAjax(
                    unity.notifications.emails.emailLogAttachment.getListByEmailLogId,
                    function () { return emailLogId; },
                    function (result) { return { data: result }; }
                ),
                columnDefs: [
                    {
                        title: '<i class="fl fl-paperclip"></i>',
                        width: '40px',
                        className: 'text-center',
                        orderable: false,
                        render: function () {
                            return '<i class="fl fl-paperclip"></i>';
                        }
                    },
                    {
                        title: 'Document Name',
                        data: 'fileName',
                        className: 'data-table-header text-break',
                        width: '40%'
                    },
                    {
                        title: 'Date',
                        data: 'time',
                        className: 'data-table-header',
                        width: '130px',
                        render: function (data, type) {
                            if (type === 'display' || type === 'filter') {
                                return new Date(data).toDateString();
                            }
                            return data;
                        }
                    },
                    {
                        title: 'Attached by',
                        data: 'attachedBy',
                        className: 'data-table-header',
                        width: '25%'
                    },
                    {
                        title: 'File Size',
                        data: 'fileSize',
                        className: 'data-table-header',
                        width: '90px',
                        render: function (data) {
                            if (!data) return '—';
                            const mb = data * 0.000001;
                            return mb >= 1 ? mb.toFixed(2) + ' MB' : (data / 1024).toFixed(0) + ' KB';
                        }
                    },
                    {
                        title: '',
                        data: 'id',
                        width: '80px',
                        className: 'text-center',
                        orderable: false,
                        render: function (data) {
                            return isDraft ? generateEmailAttachmentButtonContent(data) : '';
                        }
                    }
                ],
                drawCallback: function () {
                    if (isDraft) {
                        checkTotalAttachmentSize();
                    }
                }
            })
        );
    }

    function checkTotalAttachmentSize() {
        const totalMaxFileSize = Number.parseFloat(
            decodeURIComponent($('#TotalEmailAttachmentMaxFileSize').val()) || '25'
        );

        let totalBytes = 0;
        if (emailAttachmentsTable) {
            emailAttachmentsTable.rows().data().each(function (row) {
                totalBytes += (row.fileSize || 0);
            });
        }

        const totalMB = totalBytes * 0.000001;
        const isExceeded = totalMB > totalMaxFileSize;

        if (isExceeded) {
            $('#email-attachment-size-error-message').text(
                'The total size of all attachments (' + totalMB.toFixed(2) + ' MB) exceeds the ' +
                'maximum allowed ' + totalMaxFileSize + ' MB. Please remove one or more attachments before sending.'
            );
            $('#email-attachment-size-error').show();
            $('#btn-send').prop('disabled', true);
        } else {
            $('#email-attachment-size-error').hide();
            $('#btn-send').prop('disabled', false);
        }
    }

    function reloadEmailAttachmentsTable() {
        if (emailAttachmentsTable) {
            emailAttachmentsTable.ajax.reload();
        }
    }

    $('#email_attachment_upload_btn').on('click', function () {
        $('#email_attachment_upload').click();
    });

    $('#email_attachment_upload').on('change', function () {
        uploadEmailFiles('email_attachment_upload');
    });

    function uploadEmailFiles(inputId) {
        const emailLogId = $('#EmailId').val();
        const input = document.getElementById(inputId);
        if (!input?.files?.length) return;

        const disallowedTypes = JSON.parse(decodeURIComponent($('#Extensions').val()));
        const maxFileSize = decodeURIComponent($('#EmailAttachmentMaxFileSize').val());

        let isAllowedTypeError = false;
        let isMaxFileSizeError = false;
        const formData = new FormData();

        for (let file of input.files) {
            const ext = file.name.slice(file.name.lastIndexOf('.') + 1).toLowerCase();
            if (disallowedTypes.includes(ext)) {
                isAllowedTypeError = true;
            }
            if (file.size * 0.000001 > maxFileSize) {
                isMaxFileSizeError = true;
            }
            formData.append('files', file);
        }

        if (isAllowedTypeError) {
            input.value = '';
            return abp.notify.error('Error', 'File type not supported');
        }
        if (isMaxFileSizeError) {
            input.value = '';
            return abp.notify.error(
                'File Too Large',
                'The selected file exceeds the maximum allowed size of ' + maxFileSize + ' MB for email attachments. Please select a smaller file.'
            );
        }

        const totalMaxFileSize = Number.parseFloat(
            decodeURIComponent($('#TotalEmailAttachmentMaxFileSize').val()) || '25'
        );
        let existingTotalBytes = 0;
        if (emailAttachmentsTable) {
            emailAttachmentsTable.rows().data().each(function (row) {
                existingTotalBytes += (row.fileSize || 0);
            });
        }
        let newFilesBytes = 0;
        for (let file of input.files) {
            newFilesBytes += file.size;
        }
        const combinedMB = (existingTotalBytes + newFilesBytes) * 0.000001;
        if (combinedMB > totalMaxFileSize) {
            input.value = '';
            return abp.notify.error(
                'Total Size Exceeded',
                'The total size of all attachments would exceed the maximum allowed ' + totalMaxFileSize +
                ' MB. Please remove existing attachments or select a smaller file.'
            );
        }

        $.ajax({
            url: `/api/app/attachment/email/${emailLogId}/upload`,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            xhr: function () {
                const xhr = new globalThis.XMLHttpRequest();
                xhr.upload.addEventListener('progress', function (e) {
                    if (e.lengthComputable) {
                        const pct = Math.round((e.loaded / e.total) * 100);
                        $('#attachment-upload-progress-bar')
                            .css('width', pct + '%')
                            .attr('value', pct)
                            .text(pct + '%');
                    }
                });
                return xhr;
            },
            beforeSend: function () {
                $('#email_attachment_upload_btn')
                    .html('<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Uploading...')
                    .prop('disabled', true);
                $('#attachment-upload-progress-bar').css('width', '0%').text('0%');
                $('#attachment-upload-progress').show();
            },
            success: function () {
                PubSub.publish('reload_email_attachments_table');
            },
            error: function (xhr) {
                abp.notify.error(xhr.responseText || 'Failed to upload attachment.');
            },
            complete: function () {
                input.value = '';
                $('#email_attachment_upload_btn')
                    .html('<i class="fl fl-plus me-1"></i>Add Attachments')
                    .prop('disabled', false);
                $('#attachment-upload-progress').hide();
            }
        });
    }
});


/**
 * Returns TinyMCE editor toolbar options
 * @returns {string} Toolbar configuration
 */
function getToolbarOptions() {
    return 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist | link image | code preview';
}

/**
 * Returns TinyMCE editor plugins
 * @returns {string} Comma-separated plugin names
 */
function getPlugins() {
    return 'lists link image preview code';
}


/**
 * Displays validation error toast using abp.notify or toastr
 * @param {string[]} errors - Array of error messages to display
 */
function showValidationErrorToast(errors) {
    if (!errors?.length) return;

    // Build error message with line breaks for each error
    let errorMessage = errors.map((err, idx) => (idx === 0 ? '• ' : '') + err).join('<br>• ');

    console.log('Showing validation errors:', errors);

    // Use abp.notify if available, fallback to toastr if available
    if (globalThis.abp?.notify) {
        const errorTitle = 'Validation Error' + (errors.length > 1 ? 's' : '');
        abp.notify.error(errorMessage, errorTitle);
    } else if (globalThis.toastr) {
        toastr.error(errorMessage, 'Validation Error' + (errors.length > 1 ? 's' : ''), {
            timeOut: 0,
            extendedTimeOut: 0,
            closeButton: true,
            escapeHtml: false
        });
    } else {
        // Final fallback: alert
        console.error('Validation Errors:', errors.join('\n'));
        alert('Validation Error:\n\n' + errors.join('\n'));
    }
}

/**
 * Gets email validation error message
 * @param {string} emailStr - Email string to validate
 * @param {string} emailValue - Email value for error context
 * @param {string} fieldName - Field name for error message
 * @returns {string|null} Error message or null if valid
 */
function getEmailValidationError(emailStr, emailValue, fieldName) {
    if (emailStr === '' || !validateEmail(emailStr)) {
        let errorMessage;
        if (emailStr === '') {
            errorMessage = emailValue.length > 0
                ? `An email is required after each comma or semicolon.`
                : `The ${escapeHtml(fieldName)} field is required.`;
        } else {
            errorMessage = `Please enter a valid email in ${escapeHtml(fieldName)}: ${escapeHtml(emailStr)}`;
        }
        return normalizeErrorMessage(errorMessage);
    }
    return null;
}

/**
 * Deletes email attachment with confirmation
 * @param {string} attachmentId - Attachment ID to delete
 */
function deleteEmailAttachment(attachmentId) {
    abp.message.confirm(
        'Are you sure you want to delete this attachment?',
        'Delete Attachment',
        function (confirmed) {
            if (confirmed) {
                unity.notifications.emails.emailLogAttachment
                    .delete(attachmentId)
                    .then(function () {
                        abp.notify.success('Attachment deleted successfully.');
                        PubSub.publish('reload_email_attachments_table');
                    })
                    .catch(function (e) {
                        console.warn('Failed to delete attachment:', e);
                        abp.notify.error('Failed to delete attachment.');
                    });
            }
        }
    );
}


/**
 * Generates HTML for email attachment button
 * @param {string} attachmentId - Attachment ID
 * @returns {string} HTML for attachment button
 */
function generateEmailAttachmentButtonContent(attachmentId) {
    return `
        <div class="dropdown" style="float:right;">
            <button class="btn btn-light dropbtn" type="button">
                <i class="fl fl-attachment-more"></i>
            </button>
            <div class="dropdown-content">
                <button class="btn fullWidth" style="margin:10px" type="button"
                        onclick="deleteEmailAttachment('${attachmentId}')">
                    <i class="fl fl-cancel"></i><span>Delete Attachment</span>
                </button>
            </div>
        </div>`;
}

/**
 * Collects email field validation errors
 * @param {object} toResult - To field validation result
 * @param {object} ccResult - CC field validation result
 * @param {object} bccResult - BCC field validation result
 * @returns {string[]} Array of error messages
 */
function collectEmailFieldErrors(toResult, ccResult, bccResult) {
    let allErrors = [];
    if (!toResult.isValid && toResult.error) allErrors.push(toResult.error);
    if (!ccResult.isValid && ccResult.error) allErrors.push(ccResult.error);
    if (!bccResult.isValid && bccResult.error) allErrors.push(bccResult.error);
    return allErrors;
}

/**
 * Checks if only TinyMCE editor has validation error
 * @param {boolean} isValid - Overall form validity
 * @param {array} errorList - jQuery validator error list
 * @param {string} fieldName - Field name to check
 * @param {string} tinymceContent - TinyMCE editor content
 * @returns {boolean} True if only TinyMCE has error
 */
function checkOnlyErrorIsTinyMCE(isValid, errorList, fieldName, tinymceContent) {
    return !isValid && errorList.length === 1 &&
        errorList[0].element.name === fieldName &&
        tinymceContent.length > 0;
}


function validateEmailFieldWithOptions(fieldElement, isRequired = false, showToast = false, onlyShowErrorsIfHasContent = false) {
    const result = validateEmailField(fieldElement, isRequired, showToast, onlyShowErrorsIfHasContent);
    return result;
}

/**
 * Trims input value on keyup event
 * @param {event} e - Keyup event
 */
function handleKeyUpTrim(e) {
    let trimmedString = e.currentTarget.value.trim();
    e.currentTarget.value = trimmedString;
}


/**
  * Validates email format
  * @param {string} email - Email address to validate
  * @returns {boolean} True if valid email format
  */
function validateEmail(email) {
    // Optimized regex to prevent ReDoS: use atomic-like patterns with specific character classes
    const emailRegex = /^[a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.exec(String(email).toLowerCase()) !== null;
}

/**
 * Escapes HTML special characters to prevent XSS
 * @param {string} str - String to escape
 * @returns {string} HTML-escaped string
 */
function escapeHtml(str) {
    return String(str)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}

/**
 * Normalizes error messages by replacing field names with user-friendly labels
 * @param {string} message - Error message to normalize
 * @returns {string} Normalized error message
 */
function normalizeErrorMessage(message) {
    if (!message) return message;

    // Handle "The {FieldName} field" format
    message = message
        .replaceAll(/The EmailTo field/gi, 'The To field')
        .replaceAll(/The EmailCC field/gi, 'The CC field')
        .replaceAll(/The EmailBCC field/gi, 'The BCC field')
        .replaceAll(/The EmailFrom field/gi, 'The From field')
        .replaceAll(/The EmailSubject field/gi, 'The Subject field')
        .replaceAll(/The EmailBody field/gi, 'The Body field');

    // Also handle just the field names themselves in case they appear elsewhere
    message = message.replaceAll(/(EmailTo|EmailCC|EmailBCC|EmailFrom|EmailSubject|EmailBody)/g, (match) => {
        const replacements = {
            'EmailTo': 'To',
            'EmailCC': 'CC',
            'EmailBCC': 'BCC',
            'EmailFrom': 'From',
            'EmailSubject': 'Subject',
            'EmailBody': 'Body'
        };
        return replacements[match] ?? match;
    });

    return message;
}


/**
 * Validates date values
 * @param {number} month - Month (1-12)
 * @param {number} day - Day (1-31)
 * @param {number} year - Year
 * @returns {boolean} True if valid date
 */
function isValidDate(month, day, year) {
    const date = new Date(year, month - 1, day);
    return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
}

/**
 * Converts string to title case
 * @param {string} str - String to convert
 * @returns {string} Title-cased string
 */
function toTitleCase(str) {
    return str.replaceAll(/\b\w/, function (char) {
        return char.toUpperCase();
    }).replaceAll(/\B\w/, function (char) {
        return char.toLowerCase();
    });
}

/**
 * Gets nested object property by dot-notation path
 * @param {object} obj - Object to traverse
 * @param {string} path - Property path (e.g., "applicant.applicantName")
 * @returns {*} Value at path or undefined
 */
function getValueByPath(obj, path) {
    return path.split('.').reduce((acc, key) => acc?.[key], obj);
}

/**
 * Creates a currency formatter for Canadian dollars
 * @returns {Intl.NumberFormat} Formatter instance
 */
function createCurrencyFormatter() {
    return new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });
}
