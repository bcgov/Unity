(function () {
    const applicationDetailsCache = new Map();
    const attachmentCache = new Map();
    const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    const templateTokenPattern = /\{\{\s*(\w+)\s*\}\}/g;

    let applications = [];
    let templates = [];
    let templateVariables = [];
    let templateVariablesByToken = new Map();
    let applicationStates = new Map();
    let masterState = null;
    let selectedApplicationId = null;
    let currentStep = 1;
    let editor = null;
    let variablesButtonApi = null;
    let sendConfirmed = false;
    let rightPanelValidatedApplications = new Set();

    function parseApplications() {
        try {
            return JSON.parse($('#composeApplicationsJson').val() || '[]').map(function (application) {
                return {
                    applicationId: application.applicationId || application.ApplicationId,
                    referenceNo: application.referenceNo || application.ReferenceNo || '',
                    applicantName: application.applicantName || application.ApplicantName || '',
                    formName: application.formName || application.FormName || '',
                    applicationStatus: application.applicationStatus || application.ApplicationStatus || '',
                    applicantEmail: application.applicantEmail || application.ApplicantEmail || '',
                    approvedAmount: application.approvedAmount ?? application.ApprovedAmount ?? 0,
                    decisionDate: application.decisionDate || application.DecisionDate || null,
                    active: true
                };
            });
        } catch (error) {
            console.error('Failed to read selected applications for Compose & Send Email.', error);
            return [];
        }
    }

    function getTemplate(templateId) {
        return templates.find(function (template) {
            return String(template.id || template.Id) === String(templateId);
        });
    }

    function getTemplateFields(template) {
        return {
            id: template?.id || template?.Id || null,
            name: template?.name || template?.Name || '',
            subject: template?.subject || template?.Subject || '',
            body: template?.body || template?.bodyHTML || template?.Body || template?.BodyHTML || '',
            sendFrom: template?.sendFrom || template?.SendFrom || '',
            recipientIdentifier: template?.recipientIdentifier || template?.RecipientIdentifier || ''
        };
    }

    function readValueByPath(source, path) {
        return path.split('.').reduce(function (value, key) { return value?.[key]; }, source);
    }

    function activeApplications() {
        return applications.filter(function (application) { return application.active; });
    }

    function getEditorBody() {
        return editor ? editor.getContent() : ($('#EmailBody').val() || '');
    }

    function sanitizeEditorHtml(value = '') {
        return typeof sanitizeTinyMceHtml === 'function' ? sanitizeTinyMceHtml(value) : value;
    }

    function setEditorBody(value) {
        const body = sanitizeEditorHtml(value);
        $('#EmailBody').val(body);
        if (editor) {
            editor.setContent(body);
        }
    }

    function readVisibleEditor() {
        const templateId = $('#EmailTemplate').val() || null;
        const template = getTemplate(templateId);
        return {
            emailTo: $('#EmailTo').val() || '',
            emailCC: $('#EmailCC').val() || '',
            emailBCC: $('#EmailBCC').val() || '',
            emailFrom: $('#EmailFrom').val() || '',
            emailSubject: $('#EmailSubject').val() || '',
            emailBody: getEditorBody(),
            templateId: templateId,
            templateName: getTemplateFields(template).name,
            attachmentBytes: getSelectedAttachmentBytes(templateId),
            preparationError: ''
        };
    }

    function writeVisibleEditor(state) {
        $('#EmailTo').val(state.emailTo || '');
        $('#EmailCC').val(state.emailCC || '');
        $('#EmailBCC').val(state.emailBCC || '');
        $('#EmailFrom').val(state.emailFrom || '');
        $('#EmailSubject').val(state.emailSubject || '');
        $('#EmailTemplate').val(state.templateId || '');
        $('#EmailTemplateName').val(state.templateName || '');
        setEditorBody(state.emailBody || '');
        $('#btn-save-top').prop('disabled', true);
    }

    function syncCurrentState() {
        if (currentStep === 1) {
            masterState = { ...masterState, ...readVisibleEditor() };
            return;
        }

        if (!selectedApplicationId || !applicationStates.has(selectedApplicationId)) {
            return;
        }

        const existing = applicationStates.get(selectedApplicationId);
        applicationStates.set(selectedApplicationId, { ...existing, ...readVisibleEditor() });
    }

    function bodyHasContent(body) {
        if (!body) {
            return false;
        }

        if (/<(img|table|hr)\b/i.test(body)) {
            return true;
        }

        // This only checks for visible content; it must not interpret editor input as DOM HTML.
        const text = body
            .replace(/<!--[\s\S]*?-->/g, '')
            .replace(/<[^>]*>/g, '')
            .replace(/&(?:nbsp|#0*160|#x0*a0);/gi, ' ')
            .replace(/[\u200B-\u200D\uFEFF]/g, '')
            .trim();
        return text.length > 0;
    }

    function splitAddresses(value) {
        return (value || '')
            .split(/[;,]/)
            .map(function (address) { return address.trim(); })
            .filter(Boolean);
    }

    function isValidAddressList(value, required) {
        const addresses = splitAddresses(value);
        if (addresses.length === 0) {
            return !required;
        }
        return addresses.every(function (address) { return emailPattern.test(address); });
    }

    function getMaxAttachmentMb() {
        const configured = Number.parseFloat($('#composeMaxAttachmentMb').val());
        return Number.isFinite(configured) && configured > 0 ? configured : 25;
    }

    function validateState(state, includeRecipients) {
        const errors = [];
        if (includeRecipients && !isValidAddressList(state.emailTo, true)) {
            errors.push('A valid To address is required.');
        }
        if (includeRecipients && !isValidAddressList(state.emailCC, false)) {
            errors.push('CC contains an invalid email address.');
        }
        if (includeRecipients && !isValidAddressList(state.emailBCC, false)) {
            errors.push('BCC contains an invalid email address.');
        }
        if (!(state.emailFrom || '').trim()) {
            errors.push('From is required.');
        }
        if (!(state.emailSubject || '').trim()) {
            errors.push('Subject is required.');
        } else if (state.emailSubject.length > 1023) {
            errors.push('Subject cannot exceed 1023 characters.');
        }
        if (!bodyHasContent(state.emailBody)) {
            errors.push('Body is required.');
        }
        if ((state.attachmentBytes || 0) > getMaxAttachmentMb() * 1000000) {
            errors.push(`Template attachments exceed the ${getMaxAttachmentMb()} MB limit.`);
        }
        if (state.preparationError) {
            errors.push(state.preparationError);
        }
        return errors;
    }

    function clearFieldErrors() {
        const $editor = $('#composeEmailEditor');
        $editor.find('.compose-field-error').remove();
        $editor.find('.input-validation-error').removeClass('input-validation-error');
        $editor.find('.field-validation-error')
            .empty()
            .removeClass('field-validation-error')
            .addClass('field-validation-valid');
    }

    function addFieldError(selector, message) {
        const $field = $('#composeEmailEditor').find(selector).first();
        if (!$field.length) {
            return;
        }

        $field.addClass('input-validation-error');
        $('<span class="compose-field-error field-validation-error"></span>')
            .text(message)
            .insertAfter($field);
    }

    function showEditorErrors(errors, includeRecipients) {
        clearFieldErrors();
        errors.forEach(function (error) {
            if (includeRecipients && error.startsWith('A valid To')) addFieldError('#EmailTo', error);
            else if (includeRecipients && error.startsWith('CC')) addFieldError('#EmailCC', error);
            else if (includeRecipients && error.startsWith('BCC')) addFieldError('#EmailBCC', error);
            else if (error.startsWith('From')) addFieldError('#EmailFrom', error);
            else if (error.startsWith('Subject')) addFieldError('#EmailSubject', error);
            else if (error.startsWith('Body')) addFieldError('.tox-tinymce', error);
        });

        const attachmentError = errors.find(function (error) { return error.includes('attachments exceed'); });
        $('#email-attachment-size-error').toggle(Boolean(attachmentError));
        $('#email-attachment-size-error-message').text(attachmentError || '');
    }

    function showStepOneErrors(errors) {
        showEditorErrors(errors, false);
    }

    function showStepTwoErrors(errors) {
        showEditorErrors(errors, true);
    }

    function showStepTwoValidationToast(errors) {
        if (typeof globalThis.showValidationErrorToast === 'function') {
            globalThis.showValidationErrorToast(errors);
            return;
        }

        abp.notify.error(errors.join('; '), errors.length > 1 ? 'Validation Errors' : 'Validation Error');
    }

    function updateRowValidation(applicationId) {
        const state = applicationStates.get(applicationId);
        const errors = state ? validateState(state, true) : ['Email details have not been prepared.'];
        const $validation = $('#compose-row-' + applicationId).find('.compose-row-validation');
        $validation.empty();
        errors.forEach(function (error) {
            const $note = $('<div class="bulk-approval-notes-column"></div>');
            $('<span class="approval-note-prefix approval-note-error">Error</span>').appendTo($note);
            $note.append(document.createTextNode(' '));
            $('<span></span>').text(error).appendTo($note);
            $validation.append($note);
        });
        $('#compose-row-' + applicationId).toggleClass('row-invalid', errors.length > 0);
        return errors;
    }

    function setRowLastModified(applicationId) {
        const now = new Date();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const day = String(now.getDate()).padStart(2, '0');
        $('#compose-last-modified-' + applicationId).val(`${now.getFullYear()}-${month}-${day}`);
    }

    function updateAllValidations() {
        let valid = activeApplications().length > 0;
        activeApplications().forEach(function (application) {
            if (updateRowValidation(application.applicationId).length > 0) {
                valid = false;
            }
        });
        $('#composeSendButton').prop('disabled', !valid);
        return valid;
    }

    function getSelectedAttachmentBytes(templateId) {
        if (!templateId || !attachmentCache.has(String(templateId))) {
            return 0;
        }
        return attachmentCache.get(String(templateId)).reduce(function (total, attachment) {
            return total + Number(attachment.fileSize || attachment.FileSize || 0);
        }, 0);
    }

    function formatFileSize(bytes) {
        const size = Number(bytes || 0);
        if (size < 1000) return size + ' B';
        if (size < 1000000) return (size / 1000).toFixed(1) + ' KB';
        return (size / 1000000).toFixed(2) + ' MB';
    }

    function renderAttachments(attachments) {
        const $section = $('#email-attachments-section');
        const $table = $('#EmailAttachmentsTable');
        $table.empty();

        if (!attachments || attachments.length === 0) {
            $section.hide();
            $('#email-attachment-size-error').hide();
            return;
        }

        const $head = $('<thead><tr><th aria-label="Attachment"></th><th>Document Name</th><th>Date</th><th>File Size</th></tr></thead>');
        const $body = $('<tbody></tbody>');
        attachments.forEach(function (attachment) {
            const fileName = attachment.fileName || attachment.FileName || attachment.displayName || attachment.DisplayName || 'Attachment';
            const time = attachment.time || attachment.Time;
            const fileSize = attachment.fileSize || attachment.FileSize || 0;
            const $row = $('<tr></tr>');
            $('<td><i class="fa-solid fa-paperclip" aria-hidden="true"></i></td>').appendTo($row);
            $('<td class="text-break"></td>').text(fileName).appendTo($row);
            $('<td></td>').text(time ? new Date(time).toLocaleDateString() : '').appendTo($row);
            $('<td></td>').text(formatFileSize(fileSize)).appendTo($row);
            $body.append($row);
        });
        $table.append($head, $body);
        $section.show();

        const totalBytes = attachments.reduce(function (total, attachment) {
            return total + Number(attachment.fileSize || attachment.FileSize || 0);
        }, 0);
        const exceedsLimit = totalBytes > getMaxAttachmentMb() * 1000000;
        $('#email-attachment-size-error').toggle(exceedsLimit);
        $('#email-attachment-size-error-message').text(
            exceedsLimit ? `Template attachments exceed the ${getMaxAttachmentMb()} MB limit.` : ''
        );
    }

    async function loadAttachments(templateId) {
        if (!templateId) {
            renderAttachments([]);
            return [];
        }

        const key = String(templateId);
        if (!attachmentCache.has(key)) {
            const attachmentService = unity?.notifications?.emails?.emailLogAttachment;
            if (!attachmentService?.getListByTemplateId) {
                throw new Error('The template attachment service is unavailable.');
            }
            const attachments = await attachmentService.getListByTemplateId(templateId);
            attachmentCache.set(key, attachments || []);
        }

        const attachments = attachmentCache.get(key);
        renderAttachments(attachments);
        return attachments;
    }

    function updateRecipientSummary(templateId) {
        const fields = getTemplateFields(getTemplate(templateId));
        const count = activeApplications().length;
        if (fields.recipientIdentifier.trim()) {
            $('#composeRecipientSummary').text(
                `Recipients from "${fields.name}" will be resolved for each of the ${count} application(s). ` +
                'An application contact email will be used when the template resolves no recipient. ' +
                'You can review or edit recipients in Step 2.'
            );
            return;
        }

        $('#composeRecipientSummary').text(
            `Each of the ${count} selected application(s) will use its contact email as the initial recipient. ` +
            'You can review or edit recipients in Step 2.'
        );
    }

    async function confirmTemplateReplacement() {
        const result = await Swal.fire({
            title: 'Apply Template?',
            text: 'The template will replace the current From, Subject, Body, recipients, and attachment preview for this email.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Apply Template',
            cancelButtonText: 'Cancel'
        });
        return result.isConfirmed;
    }

    async function getApplicationDetails(applicationId) {
        if (!applicationDetailsCache.has(applicationId)) {
            applicationDetailsCache.set(applicationId, $.ajax({
                url: `/api/app/grant-application/${applicationId}`,
                type: 'GET'
            }));
        }
        return applicationDetailsCache.get(applicationId);
    }

    function getUsedTemplateVariables(...rawValues) {
        const usedVariables = new Map();
        rawValues.forEach(function (rawValue) {
            String(rawValue || '').replace(templateTokenPattern, function (match, token) {
                const mapping = templateVariablesByToken.get(token);
                if (mapping) {
                    usedVariables.set(token, mapping);
                }
                return match;
            });
        });
        return Array.from(usedVariables.values());
    }

    function requiresApplicationDetails(...rawValues) {
        return getUsedTemplateVariables(...rawValues).some(function (mapping) {
            return Boolean((mapping.mapTo || mapping.MapTo || '').trim());
        });
    }

    function processTemplateValue(rawValue, applicationDetails, escapeHtml) {
        if (!rawValue || !templateVariablesByToken.size) {
            return rawValue || '';
        }

        return rawValue.replace(templateTokenPattern, function (match, token) {
            const mapping = templateVariablesByToken.get(token);
            if (!mapping) {
                return match;
            }

            const mapTo = mapping.mapTo || mapping.MapTo;
            let value = '';
            if (token === 'today_date') {
                value = new Intl.DateTimeFormat('en-CA', {
                    year: 'numeric', month: 'long', day: 'numeric'
                }).format(new Date());
            } else if (mapTo && applicationDetails) {
                value = readValueByPath(applicationDetails, mapTo);
            }

            const formattedValue = String(formatTemplateValue(token, value) ?? '');
            return escapeHtml ? Handlebars.escapeExpression(formattedValue) : formattedValue;
        });
    }

    function formatTemplateValue(token, value) {
        if (value === null || value === undefined || value === '') {
            return '';
        }

        if (['approved_amount', 'recommended_amount', 'requested_amount'].includes(token)) {
            const amount = Number(value);
            return Number.isFinite(amount)
                ? new Intl.NumberFormat('en-CA', { style: 'currency', currency: 'CAD' }).format(amount)
                : '';
        }

        if (['submission_date', 'approval_date', 'project_start_date', 'project_end_date'].includes(token)) {
            const date = new Date(value);
            return Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString();
        }

        if (['category', 'status', 'decline_rationale'].includes(token) && typeof value === 'string') {
            return value.replaceAll('_', ' ').toLowerCase().replaceAll(/\b\w/g, function (character) {
                return character.toUpperCase();
            });
        }

        return value;
    }

    async function resolveRecipients(templateId, application) {
        if (templateId) {
            try {
                const response = await $.ajax({
                    url: `/api/form-notifications/templates/${templateId}/resolved-recipients`,
                    type: 'GET',
                    data: { applicationId: application.applicationId }
                });
                const resolved = (response?.emailTo || response?.EmailTo || '').trim();
                if (resolved) {
                    return resolved;
                }
            } catch (error) {
                console.warn('Template recipients could not be resolved; using the application contact.', error);
            }
        }
        return application.applicantEmail || '';
    }

    async function buildApplicationState(application) {
        try {
            const details = requiresApplicationDetails(masterState.emailSubject, masterState.emailBody)
                ? await getApplicationDetails(application.applicationId)
                : null;
            return {
                applicationId: application.applicationId,
                emailTo: await resolveRecipients(masterState.templateId, application),
                emailCC: '',
                emailBCC: '',
                emailFrom: masterState.emailFrom,
                emailSubject: processTemplateValue(masterState.emailSubject, details),
                emailBody: sanitizeEditorHtml(processTemplateValue(masterState.emailBody, details, true)),
                templateId: masterState.templateId,
                templateName: masterState.templateName,
                attachmentBytes: masterState.attachmentBytes,
                preparationError: ''
            };
        } catch (error) {
            console.error('Failed to prepare composed email for application.', application.applicationId, error);
            return {
                applicationId: application.applicationId,
                emailTo: application.applicantEmail || '',
                emailCC: '',
                emailBCC: '',
                emailFrom: masterState.emailFrom,
                emailSubject: masterState.emailSubject,
                emailBody: masterState.emailBody,
                templateId: masterState.templateId,
                templateName: masterState.templateName,
                attachmentBytes: masterState.attachmentBytes,
                preparationError: 'Email variables could not be resolved for this application.'
            };
        }
    }

    async function applyTemplate(templateId) {
        const template = getTemplate(templateId);
        if (!template) {
            abp.notify.error('The selected template could not be loaded.');
            return false;
        }

        if (!await confirmTemplateReplacement()) {
            return false;
        }

        const fields = getTemplateFields(template);
        if (currentStep === 1) {
            await loadAttachments(fields.id);
            $('#EmailFrom').val(fields.sendFrom || masterState.emailFrom);
            $('#EmailSubject').val(fields.subject);
            $('#EmailTemplateName').val(fields.name);
            setEditorBody(fields.body);
            masterState = {
                ...readVisibleEditor(),
                templateId: String(fields.id),
                templateName: fields.name,
                attachmentBytes: getSelectedAttachmentBytes(fields.id)
            };
            updateRecipientSummary(fields.id);
            showStepOneErrors(validateState(masterState, false));
            return true;
        }

        const application = applications.find(function (item) {
            return item.applicationId === selectedApplicationId;
        });
        const details = requiresApplicationDetails(fields.subject, fields.body)
            ? await getApplicationDetails(selectedApplicationId)
            : null;
        const currentState = applicationStates.get(selectedApplicationId);
        const nextState = {
            applicationId: selectedApplicationId,
            emailTo: await resolveRecipients(fields.id, application),
            emailCC: currentState?.emailCC || '',
            emailBCC: currentState?.emailBCC || '',
            emailFrom: fields.sendFrom || $('#EmailFrom').val() || '',
            emailSubject: processTemplateValue(fields.subject, details),
            emailBody: sanitizeEditorHtml(processTemplateValue(fields.body, details, true)),
            templateId: String(fields.id),
            templateName: fields.name,
            attachmentBytes: 0,
            preparationError: ''
        };
        await loadAttachments(fields.id);
        nextState.attachmentBytes = getSelectedAttachmentBytes(fields.id);
        applicationStates.set(selectedApplicationId, nextState);
        writeVisibleEditor(nextState);
        const errors = updateRowValidation(selectedApplicationId);
        if (rightPanelValidatedApplications.has(selectedApplicationId)) {
            showStepTwoErrors(errors);
        }
        updateAllValidations();
        return true;
    }

    async function removeSelectedTemplate() {
        const result = await Swal.fire({
            title: 'Remove Template?',
            text: 'The attachment preview will be removed. The current email content and recipients will remain.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Remove Template',
            cancelButtonText: 'Cancel'
        });
        if (!result.isConfirmed) {
            return false;
        }

        await loadAttachments(null);
        $('#EmailTemplateName').val('');
        if (currentStep === 1) {
            masterState = {
                ...readVisibleEditor(),
                templateId: null,
                templateName: '',
                attachmentBytes: 0
            };
            updateRecipientSummary(null);
            showStepOneErrors(validateState(masterState, false));
            return true;
        }

        const state = {
            ...applicationStates.get(selectedApplicationId),
            ...readVisibleEditor(),
            templateId: null,
            templateName: '',
            attachmentBytes: 0
        };
        applicationStates.set(selectedApplicationId, state);
        const errors = updateRowValidation(selectedApplicationId);
        if (rightPanelValidatedApplications.has(selectedApplicationId)) {
            showStepTwoErrors(errors);
        }
        updateAllValidations();
        $('#btn-save-top').prop('disabled', false);
        return true;
    }

    async function selectApplication(applicationId) {
        if (!applicationStates.has(applicationId)) {
            return;
        }
        syncCurrentState();
        if (selectedApplicationId) {
            updateRowValidation(selectedApplicationId);
        }
        selectedApplicationId = applicationId;
        $('.compose-application-row').removeClass('row-selected');
        $('#compose-row-' + applicationId).addClass('row-selected');
        const state = applicationStates.get(applicationId);
        writeVisibleEditor(state);
        await loadAttachments(state.templateId);
        clearFieldErrors();
        if (rightPanelValidatedApplications.has(applicationId)) {
            showStepTwoErrors(validateState(state, true));
        }
        updateAllValidations();
    }

    async function moveToStepTwo() {
        syncCurrentState();
        const errors = validateState(masterState, false);
        showStepOneErrors(errors);
        if (errors.length > 0) {
            abp.notify.error('Complete the required email fields before continuing.');
            return;
        }

        $('#composeEditorLoading').show();
        $('#composeEmailEditor').hide();
        try {
            applicationStates = new Map();
            rightPanelValidatedApplications = new Set();
            const preparedStates = await Promise.all(activeApplications().map(buildApplicationState));
            preparedStates.forEach(function (state) {
                applicationStates.set(state.applicationId, state);
            });

            currentStep = 2;
            variablesButtonApi?.setEnabled(false);
            $('.compose-last-modified').val('');
            $('#composeWorkflow').removeClass('compose-step-one').addClass('compose-step-two');
            $('#composeStepNumber').text('Step 2 of 2');
            $('#composeStepTitle').text('Review emails');
            $('#composeNextButton').hide();
            $('#composeBackButton, #composeSendButton').show();
            const first = activeApplications()[0];
            if (first) {
                await selectApplication(first.applicationId);
            }
            updateAllValidations();
        } finally {
            $('#composeEditorLoading').hide();
            $('#composeEmailEditor').show();
        }
    }

    async function moveToStepOne() {
        syncCurrentState();
        currentStep = 1;
        rightPanelValidatedApplications = new Set();
        variablesButtonApi?.setEnabled(true);
        selectedApplicationId = null;
        $('.compose-application-row').removeClass('row-selected');
        $('#composeWorkflow').removeClass('compose-step-two').addClass('compose-step-one');
        $('#composeStepNumber').text('Step 1 of 2');
        $('#composeStepTitle').text('Compose email');
        $('#composeBackButton, #composeSendButton').hide();
        $('#composeNextButton').show();
        writeVisibleEditor(masterState);
        await loadAttachments(masterState.templateId);
        updateRecipientSummary(masterState.templateId);
        showStepOneErrors(validateState(masterState, false));
    }

    function removeApplication(applicationId) {
        const application = applications.find(function (item) { return item.applicationId === applicationId; });
        if (!application) return;

        abp.message.confirm(
            `Remove application ${application.referenceNo} from this batch?`,
            'Remove Application',
            async function (confirmed) {
                if (!confirmed) return;
                syncCurrentState();
                application.active = false;
                $('#compose-row-' + applicationId).remove();
                applicationStates.delete(applicationId);
                rightPanelValidatedApplications.delete(applicationId);
                $('#composeApplicationCount').val(activeApplications().length);
                updateRecipientSummary(masterState?.templateId);

                if (selectedApplicationId === applicationId) {
                    selectedApplicationId = null;
                    const next = activeApplications()[0];
                    if (next && currentStep === 2) {
                        await selectApplication(next.applicationId);
                    }
                }
                updateAllValidations();
            }
        );
    }

    function buildRequest() {
        syncCurrentState();
        return {
            emails: activeApplications().map(function (application) {
                const state = applicationStates.get(application.applicationId);
                return {
                    applicationId: application.applicationId,
                    emailTo: state.emailTo,
                    emailCC: state.emailCC || null,
                    emailBCC: state.emailBCC || null,
                    emailFrom: state.emailFrom,
                    emailSubject: state.emailSubject,
                    emailBody: state.emailBody,
                    templateId: state.templateId || null,
                    templateName: state.templateName || null
                };
            })
        };
    }

    function bindEvents() {
        $('#EmailForm').off('submit.composeNoDraft').on('submit.composeNoDraft', function (event) {
            event.preventDefault();
        });

        $('#composeNextButton').off('click.compose').on('click.compose', moveToStepTwo);
        $('#composeBackButton').off('click.compose').on('click.compose', moveToStepOne);
        $('#composeAndSendEmailModal #btn-save-top').off('click').on('click.compose', function (event) {
            event.preventDefault();
            event.stopImmediatePropagation();
            syncCurrentState();
            const errors = updateRowValidation(selectedApplicationId);
            updateAllValidations();
            rightPanelValidatedApplications.add(selectedApplicationId);
            showStepTwoErrors(errors);
            $(this).prop('disabled', errors.length === 0);
            if (errors.length > 0) {
                showStepTwoValidationToast(errors);
            } else {
                setRowLastModified(selectedApplicationId);
                abp.notify.success('Email changes saved for this application.');
            }
        });

        $('#EmailTemplate').off('change.compose').on('change.compose', async function () {
            const previousTemplateId = currentStep === 1
                ? masterState?.templateId
                : applicationStates.get(selectedApplicationId)?.templateId;
            const selectedTemplateId = $(this).val();
            try {
                if (!selectedTemplateId) {
                    if (previousTemplateId && !await removeSelectedTemplate()) {
                        $(this).val(previousTemplateId);
                    }
                    return;
                }
                if (!await applyTemplate(selectedTemplateId)) {
                    $(this).val(previousTemplateId || '');
                }
            } catch (error) {
                console.error('Failed to update the selected template.', error);
                abp.notify.error('The selected template could not be applied.');
                $(this).val(previousTemplateId || '');
            }
        });

        $('#EmailTo, #EmailCC, #EmailBCC, #EmailFrom, #EmailSubject')
            .off('input.compose change.compose')
            .on('input.compose change.compose', function () {
                syncCurrentState();
                if (currentStep === 2 && selectedApplicationId) {
                    const errors = updateRowValidation(selectedApplicationId);
                    if (rightPanelValidatedApplications.has(selectedApplicationId)) {
                        showStepTwoErrors(errors);
                    }
                    updateAllValidations();
                    $('#btn-save-top').prop('disabled', false);
                }
            });

        $('#composeApplicationList')
            .off('click.compose', '.compose-application-row')
            .on('click.compose', '.compose-application-row', function (event) {
                if ($(event.target).closest('.compose-remove-application').length) return;
                selectApplication($(this).data('application-id'));
            })
            .off('keydown.compose', '.compose-application-row')
            .on('keydown.compose', '.compose-application-row', function (event) {
                if ($(event.target).closest('.compose-remove-application').length) return;
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    selectApplication($(this).data('application-id'));
                }
            })
            .off('click.compose', '.compose-remove-application')
            .on('click.compose', '.compose-remove-application', function (event) {
                event.stopPropagation();
                removeApplication($(this).closest('.compose-application-row').data('application-id'));
            });

        $(document)
            .off('click.composeSend', '#composeSendButton')
            .on('click.composeSend', '#composeSendButton', function (event) {
                if (sendConfirmed) {
                    sendConfirmed = false;
                    return;
                }

                event.preventDefault();
                event.stopImmediatePropagation();
                syncCurrentState();
                if (!updateAllValidations()) {
                    abp.notify.error('Resolve all validation errors before sending.');
                    return;
                }

                const count = activeApplications().length;
                $('#ComposeRequestJson').val(JSON.stringify(buildRequest()));
                abp.message.confirm(
                    `Are you sure you want to send this email to ${count} application(s)?`,
                    'Send Email',
                    function (confirmed) {
                        if (confirmed) {
                            sendConfirmed = true;
                            $('#composeSendButton').trigger('click');
                        }
                    }
                );
            });

        bindDivider();
    }

    function bindDivider() {
        const $divider = $('#composePanelDivider');
        $divider.off('mousedown.compose').on('mousedown.compose', function (event) {
            event.preventDefault();
            const $panel = $('#composeApplicationPanel');
            const startX = event.pageX;
            const startWidth = $panel.outerWidth();
            $divider.addClass('dragging');
            $('body').addClass('compose-email-resizing');

            $(document).on('mousemove.composeDivider', function (moveEvent) {
                const layoutWidth = $('.compose-email-layout').width();
                const nextWidth = Math.min(Math.max(startWidth + moveEvent.pageX - startX, 280), layoutWidth - 420);
                $panel.css('flex-basis', nextWidth + 'px');
            }).one('mouseup.composeDivider', function () {
                $(document).off('mousemove.composeDivider');
                $divider.removeClass('dragging');
                $('body').removeClass('compose-email-resizing');
            });
        });

        $divider.off('keydown.compose').on('keydown.compose', function (event) {
            if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
            event.preventDefault();
            const $panel = $('#composeApplicationPanel');
            const delta = event.key === 'ArrowLeft' ? -24 : 24;
            $panel.css('flex-basis', Math.max(280, $panel.outerWidth() + delta) + 'px');
        });
    }

    function getComposeToolbarOptions() {
        const toolbar = typeof getToolbarOptions === 'function'
            ? getToolbarOptions()
            : 'undo redo | styles | bold italic | bullist numlist | link | code preview';
        return toolbar.includes('variablesButton') ? toolbar : `${toolbar} | variablesButton`;
    }

    function buildComposeVariableMenuItems(tinyEditor) {
        return templateVariables
            .map(function (mapping) {
                return {
                    name: mapping.name || mapping.Name || mapping.token || mapping.Token,
                    token: mapping.token || mapping.Token
                };
            })
            .filter(function (mapping) { return Boolean(mapping.token); })
            .map(function (mapping) {
                return {
                    type: 'menuitem',
                    text: mapping.name,
                    onAction: function () {
                        tinyEditor.insertContent(`{{${mapping.token}}}`);
                    }
                };
            });
    }

    async function initializeEditor() {
        tinymce.get('EmailBody')?.remove();
        await tinymce.init({
            license_key: 'gpl',
            selector: '#composeAndSendEmailModal #EmailBody',
            plugins: typeof getPlugins === 'function' ? getPlugins() : 'lists link image preview code',
            toolbar: getComposeToolbarOptions(),
            menubar: 'file edit view insert format tools',
            resize: true,
            statusbar: true,
            elementpath: false,
            branding: false,
            promotion: false,
            content_css: false,
            skin: false,
            ui_container: '#composeAndSendEmailModal',
            setup: function (tinyEditor) {
                editor = tinyEditor;
                tinyEditor.ui.registry.addMenuButton('variablesButton', {
                    text: 'Variables',
                    fetch: function (callback) {
                        callback(buildComposeVariableMenuItems(tinyEditor));
                    },
                    onSetup: function (buttonApi) {
                        variablesButtonApi = buttonApi;
                        buttonApi.setEnabled(currentStep === 1);
                        return function () {
                            if (variablesButtonApi === buttonApi) {
                                variablesButtonApi = null;
                            }
                        };
                    }
                });
                tinyEditor.on('input change undo redo', function () {
                    syncCurrentState();
                    if (currentStep === 2 && selectedApplicationId) {
                        const errors = updateRowValidation(selectedApplicationId);
                        if (rightPanelValidatedApplications.has(selectedApplicationId)) {
                            showStepTwoErrors(errors);
                        }
                        updateAllValidations();
                        $('#btn-save-top').prop('disabled', false);
                    }
                });
            }
        });
        editor = tinymce.get('EmailBody');
    }

    async function loadReferenceData() {
        const results = await Promise.allSettled([
            $.ajax({ url: '/api/form-notifications/templates', type: 'GET' }),
            $.ajax({ url: '/api/app/template/template-variables', type: 'GET' })
        ]);
        templates = results[0].status === 'fulfilled' ? (results[0].value || []) : [];
        templateVariables = results[1].status === 'fulfilled' ? (results[1].value || []) : [];
        templateVariablesByToken = new Map(templateVariables
            .map(function (mapping) {
                return [mapping.token || mapping.Token, mapping];
            })
            .filter(function ([token]) { return Boolean(token); }));
    }

    async function initialize() {
        if (!$('#composeAndSendEmailModal').length || !$('#composeApplicationsJson').length) {
            return;
        }

        applications = parseApplications();
        applicationStates = new Map();
        selectedApplicationId = null;
        currentStep = 1;
        sendConfirmed = false;
        editor = null;
        variablesButtonApi = null;
        rightPanelValidatedApplications = new Set();
        templates = [];
        templateVariables = [];
        templateVariablesByToken = new Map();
        attachmentCache.clear();
        applicationDetailsCache.clear();

        bindEvents();
        await Promise.all([initializeEditor(), loadReferenceData()]);
        if ($.fn.maskMoney) {
            $('#composeApplicationList .unity-currency-input')
                .maskMoney({ thousands: ',', decimal: '.' })
                .maskMoney('mask');
        }
        masterState = readVisibleEditor();
        updateRecipientSummary(null);
        renderAttachments([]);
    }

    window.ComposeAndSendEmail = { initialize: initialize };
})();
