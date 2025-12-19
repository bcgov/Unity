$(function () {
    const emptyGuid = '00000000-0000-0000-0000-000000000000';
    const UIElements = {
        applicationId: $('#DetailsViewApplicationId')[0].value,
        btnSend: $('#btn-send'),
        btnSave: $('#btn-save'),
        btnDiscard: $('#btn-send-discard'),
        btnConfirmSend: $('#btn-confirm-send'),
        btnCancelEmail: $('#btn-cancel-email'),
        btnNewEmail: $('#btn-new-email'),
        btnSendClose: $('#btn-send-close'),
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
        alertEmailReadonly: $('#email-alert-readonly')
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
    function bindUIEvents() {
        UIElements.btnNewEmail.on('click', handleNewEmail);
        UIElements.btnSend.on('click', handleSendEmail);
        UIElements.btnSave.on('click', handleSaveEmail);
        UIElements.btnDiscard.on('click', handleDiscardEmail);
        UIElements.btnConfirmSend.on('click', handleConfirmSendEmail);
        UIElements.btnCancelEmail.on('click', handleCancelEmailSend);
        UIElements.btnSendClose.on('click', handleCloseEmail);
        UIElements.inputEmailSubject.on('change', handleKeyUpTrim);
        UIElements.inputEmailFrom.on('change', handleKeyUpTrim);
        UIElements.inputEmailCC.on('change', handleKeyUpTrim);
        UIElements.inputEmailBCC.on('change', handleKeyUpTrim);
        UIElements.inputEmailBody.on('change', handleKeyUpTrim);
        UIElements.inputEmailTo.on('change', validateEmailTo);
        UIElements.inputEmailCC.on('change', validateEmailCC);
        UIElements.inputEmailBCC.on('change', validateEmailBCC);

        UIElements.inputEmailTo.on('input', handleDraftChange);
        UIElements.inputEmailCC.on('input', handleDraftChange);
        UIElements.inputEmailBCC.on('input', handleDraftChange);
        UIElements.inputEmailFrom.on('input', handleDraftChange);
        UIElements.inputEmailSubject.on('input', handleDraftChange);
        UIElements.inputEmailBody.on('input', handleDraftChange);
    }

    init();

    function init() {
        bindUIEvents();
        defaultValues.emailTo = UIElements.inputOriginalEmailTo.val();
        defaultValues.emailFrom = UIElements.inputOriginalEmailFrom.val();
        defaultValues.emailCC = UIElements.inputOriginalEmailCC.val() || '';
        defaultValues.emailBCC = UIElements.inputOriginalEmailBCC.val() || '';
        toastr.options.positionClass = 'toast-top-center';
        initTemplateDetails();
        $('#templateTextContainer').hide();
    }

    async function initTemplateDetails() {
       applicationDetails = await loadApplicationDetails();
       mappingConfig = await getTemplateVariables();
    }

    function disableEmail() {
        UIElements.btnSend.attr('disabled', true);
        UIElements.btnSave.attr('disabled', true);
        UIElements.btnDiscard.attr('disabled', true);
        UIElements.inputEmailTo.attr('disabled', true);
        UIElements.inputEmailCC.attr('disabled', true);
        UIElements.inputEmailBCC.attr('disabled', true);
        UIElements.inputEmailFrom.attr('disabled', true);
        UIElements.inputEmailSubject.attr('disabled', true);
        UIElements.inputEmailBody.attr('disabled', true);
    }

    function enableEmail() {
        UIElements.btnSend.attr('disabled', false);
        UIElements.btnSave.attr('disabled', false);
        UIElements.btnDiscard.attr('disabled', false);
        UIElements.inputEmailTo.attr('disabled', false);
        UIElements.inputEmailCC.attr('disabled', false);
        UIElements.inputEmailBCC.attr('disabled', false);
        UIElements.inputEmailFrom.attr('disabled', false);
        UIElements.inputEmailSubject.attr('disabled', false);
        UIElements.inputEmailBody.attr('disabled', false);
    }

    function handleKeyUpTrim(e) {
        let trimmedString = e.currentTarget.value.trim();
        e.currentTarget.value = trimmedString;
    }

    function handleCloseEmail() {
        $('#modal-content, #modal-background').removeClass('active');
        UIElements.emailForm.removeClass('active');
        UIElements.btnNewEmail.removeClass('hide');
        UIElements.alertEmailReadonly.removeClass('hide');
        UIElements.emailForm.trigger("reset");
        enableEmail();
    }

    function handleDiscardEmail() {
        UIElements.inputEmailTo.val(UIElements.inputOriginalEmailTo.val());
        UIElements.inputEmailCC.val(UIElements.inputOriginalEmailCC.val());
        UIElements.inputEmailBCC.val(UIElements.inputOriginalEmailBCC.val());
        UIElements.inputEmailFrom.val(UIElements.inputOriginalEmailFrom.val());
        UIElements.inputEmailSubject.val(UIElements.inputOriginalEmailSubject.val());
        UIElements.inputEmailBody.val(UIElements.inputOriginalEmailBody.val());
    }

    function handleCancelEmailSend() {
        $('#modal-content, #modal-background').removeClass('active');
    }

    function getToolbarOptions() {
        return 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist | link image | code preview';
    }

    function getPlugins() {
        return 'lists link image preview code';
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
    function handleNewEmail() {
        resetEmailBody();  
        $('#templateListContainer').show();
        $('#templateTextContainer').hide();
        UIElements.inputEmailId.val(emptyGuid);
        // Support discard to empty email template for new emails
        UIElements.inputOriginalEmailTo.val(defaultValues.emailTo);
        UIElements.inputOriginalEmailCC.val(defaultValues.emailCC);
        UIElements.inputOriginalEmailBCC.val(defaultValues.emailBCC);
        UIElements.inputOriginalEmailFrom.val(defaultValues.emailFrom);
        UIElements.inputOriginalEmailSubject.val("");
        UIElements.inputOriginalEmailBody.val("");
        
        if (tinymce.get("EmailBody")) {
            tinymce.get("EmailBody").remove(); // remove existing instance
        }
        tinymce.init({
            license_key: 'gpl',
            selector: `#EmailBody`,
            plugins: getPlugins(),
            toolbar: getToolbarOptions(),
            statusbar: false,
            promotion: false,
            content_css: false,
            skin: false,
            setup: function (editor) {
               
                editor.on("input", (e) => {
                    UIElements.inputEmailBody.val(editor.getContent())
                    handleDraftChange();
                });
                editorInstance = editor;
                editorInstance.setContent('');
            }
        });
        console.log(editorInstance.getContent())
        handleDraftChange();
        showModalEmail();
        resetValidationErrors();
    }

    function showModalEmail() {
        UIElements.emailForm.addClass('active');
        UIElements.btnNewEmail.addClass('hide');
        UIElements.alertEmailReadonly.addClass('hide');
    }

    function validateEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.exec(String(email).toLowerCase()) !== null;
    }

    function validateEmailTo() {
        return validateEmailField(UIElements.inputEmailToField, true); // EmailTo is required
    }

    function validateEmailCC() {
        return validateEmailField(UIElements.inputEmailCC[0], false); // CC is optional
    }

    function validateEmailBCC() {
        return validateEmailField(UIElements.inputEmailBCC[0], false); // BCC is optional
    }

    function handleConfirmSendEmail() {
        UIElements.confirmationModal.hide();
        UIElements.emailSpinner.show();
        let templateName = '';
        if (!UIElements.inputEmailId[0].value || UIElements.inputEmailId[0].value === emptyGuid) {
            // id is either null/undefined/"" OR the empty GUID
            templateName = $("#template option:selected").text();
            if (!templateName || templateName == '' || templateName == 'Please select') {
                // id is either null/undefined/"" OR the empty GUID
                templateName = "No Template Selected"
            }
        }
        else {
            templateName = $('#templateText').val();
        }
        unity.grantManager.emails.email
            .create({
                emailId: UIElements.inputEmailId[0].value,
                applicationId: UIElements.applicationId,
                emailTo: UIElements.inputEmailTo[0].value,
                emailCC: UIElements.inputEmailCC[0].value,
                emailBCC: UIElements.inputEmailBCC[0].value,
                emailFrom: UIElements.inputEmailFrom[0].value,
                emailBody: editorInstance.getContent(),
                emailSubject: UIElements.inputEmailSubject[0].value,
                currentUserId: decodeURIComponent(abp.currentUser.id),
                emailTemplateName: templateName,
            })
            .then(function () {
                hideConfirmation();
                handleCloseEmail();
                abp.notify.success('Your email is being sent');
                PubSub.publish('refresh_application_emails');
            }).catch(function () {
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

            let templateName = ''; 
            if (!UIElements.inputEmailId[0].value || UIElements.inputEmailId[0].value === emptyGuid) {
                // id is either null/undefined/"" OR the empty GUID
                templateName = $("#template option:selected").text();
                if (!templateName || templateName == '' || templateName == 'Please select') {
                    // id is either null/undefined/"" OR the empty GUID
                    templateName = "No Template Selected"
                }
            }
            else {
                templateName = $('#templateText').val();
            }
           
        unity.grantManager.emails.email
            .saveDraft({
                emailId: UIElements.inputEmailId[0].value,
                applicationId: UIElements.applicationId,
                emailTo: UIElements.inputEmailTo[0].value,
                emailCC: UIElements.inputEmailCC[0].value,
                emailBCC: UIElements.inputEmailBCC[0].value,
                emailFrom: UIElements.inputEmailFrom[0].value,
                emailBody: editorInstance.getContent(),
                emailSubject: UIElements.inputEmailSubject[0].value,
                currentUserId: decodeURIComponent(abp.currentUser.id),
                emailTemplateName: templateName,
            })
            .then(function () {
                handleCloseEmail();
                abp.notify.success('Your email has been saved.');
                PubSub.publish('refresh_application_emails');
            }).catch(function () {
                abp.notify.error('An error ocurred your email could not be saved.');
            });
        }
        else {
            return false;
        }
    }

    function validateEmailField(fieldElement, isRequired = false) {
        // Get the field's value and trim whitespace
        let emailValue = fieldElement.value.trim();

        // Remove trailing commas, semicolons, or spaces
        emailValue = emailValue.replace(/[;,\s]+$/, '');

        // If the field is empty and not required, it's valid
        if (!isRequired && emailValue === '') {
            return true;
        }

        // Split by comma or semicolon, and trim each email
        let emails = emailValue.split(/[,;]/g).map(email => email.trim());
        let fieldName = fieldElement.name;
        let errorSpan = $(`span[data-valmsg-for*='${fieldName}']`)[0];

        // Initialize as valid
        let isValid = true;
        let errorMessage = '';

        // Check each email
        for (let emailStr of emails) {
            // Skip empty emails in optional fields
            if (emailStr === '' && !isRequired) {
                continue;
            }

            // Check if the email is empty or invalid
            if (emailStr === '' || !validateEmail(emailStr)) {
                // Handle empty email input
                if (emailStr === '') {
                    errorMessage = emailValue.length > 0
                        ? `An email is required after each comma or semicolon.`
                        : `The ${escapeHtml(fieldName)} field is required.`;
                } else {
                    // Handle invalid email format
                    errorMessage = `Please enter a valid email in ${escapeHtml(fieldName)}: ${escapeHtml(emailStr)}`;
                }

                // Display the error message
                $(errorSpan).addClass('field-validation-error').removeClass('field-validation-valid');
                $(errorSpan).html(errorMessage);

                // Mark the validation as invalid and exit the loop
                isValid = false;
                break;
            }
        }

        // Clear error message if all emails are valid
        if (isValid) {
            $(errorSpan).addClass('field-validation-valid').removeClass('field-validation-error');
            $(errorSpan).html('');
        }

        return isValid;
    }

    function validateEmailForm(e) {
        // Prevent form submission and stop propagation
        e.stopPropagation();
        e.preventDefault();

        // Validate all email fields
        let isToValid = validateEmailTo();
        let isCCValid = validateEmailCC();
        let isBCCValid = validateEmailBCC();

        // If any email field is invalid, return false
        if (!isToValid || !isCCValid || !isBCCValid) {
            return false;
        }

        let isValid = UIElements.emailForm.valid();
        let validator = UIElements.emailForm.validate();
        let fieldName = 'EmailBody';
        let errorList = validator.errorList;
        let tinymceContent = tinymce.get(fieldName).getContent({ format: 'text' }).trim();

        let onlyErrorIsTinyMCE = (
            errorList.length === 1 &&
            errorList[0].element.name === fieldName &&
            tinymceContent.length > 0
        );

        if (!isValid && onlyErrorIsTinyMCE) {
            let fieldElement = $('textarea[name="' + fieldName + '"]');
            fieldElement.removeClass('input-validation-error');
            fieldElement.siblings('.field-validation-error').remove();
            return true;
        } else if (isValid) {
            return true;
        } else {
            return false;
        }
    }

    function handleSendEmail(e) {
        // Check if the form is valid
        if (validateEmailForm(e)) {
            showConfirmation(); // Show confirmation if the form is valid
            return true; // Return true to indicate success
        }
        // If form is not valid, do not show confirmation
        return false; // Return false if validation or other conditions fail
    }

    function handleDraftChange() {
        const isDraftChanged = checkDraftChanges();
        UIElements.btnSave.attr('disabled', !isDraftChanged);
        UIElements.btnDiscard.attr('disabled', !isDraftChanged);
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
        UIElements.emailForm.find('.field-validation-error').each(function() {
            $(this).removeClass('field-validation-error').addClass('field-validation-valid').html('');
        });
    }
   
 
    $('#template').on('change', async function () {
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
            UIElements.btnSave.attr('disabled', false);
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

    function toTitleCase(str) {
        return str.replace(/\b\w/g, function (char) {
            return char.toUpperCase();
        }).replace(/\B\w/g, function (char) {
            return char.toLowerCase();
        });
    }

    function createCurrencyFormatter() {
        return new Intl.NumberFormat('en-CA', {
            style: 'currency',
            currency: 'CAD',
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
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
            return toTitleCase(inputString.replace(/_/g, ' '));
        }

        return inputString;
    }

    function extractTemplateData(apiResponse, mappingConfig) {
        const templateData = {};

        mappingConfig.forEach(mapping => {
            const { token, mapTo } = mapping;

            if (!mapTo) {
                templateData[token] = ""; // handle empty MapTo
                return;
            }

            const value = getValueByPath(apiResponse, mapTo);

            let formatValue = processString(token,value);
            templateData[token] = formatValue !== undefined ? formatValue : "";
        });

        return templateData;
    }

    // Helper to resolve nested properties from a string path (e.g., "applicant.applicantName")
    function getValueByPath(obj, path) {
        return path.split('.').reduce((acc, key) => acc?.[key], obj);
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    PubSub.subscribe('email_selected', (msg, data) => {
        resetValidationErrors();
        console.log("data", data)
        $('#templateListContainer').hide();
        $('#templateTextContainer').show();
        $('#templateText').val(data.templateName);
        $('#templateText').prop('disabled', true);
        UIElements.inputEmailId.val(data.id);
        UIElements.inputOriginalEmailTo.val(data.toAddress);
        UIElements.inputOriginalEmailCC.val(data.cc ? data.cc.replace(/,/g, '; ') : '');
        UIElements.inputOriginalEmailBCC.val(data.bcc ? data.bcc.replace(/,/g, '; ') : '');
        UIElements.inputOriginalEmailFrom.val(data.fromAddress);
        UIElements.inputOriginalEmailSubject.val(data.subject);
         resetEmailBody(); 
        if (tinymce.get("EmailBody")) {
            tinymce.get("EmailBody").remove(); // remove existing instance
        }
        tinymce.init({
            license_key: 'gpl',
            selector: `#EmailBody`,
            plugins: getPlugins(),
            toolbar: getToolbarOptions(),
            statusbar: false,
            promotion: false,
            content_css: false,
            skin: false,
            setup: function (editor) {
                editor.on("input", (e) => {
                    UIElements.inputEmailBody.val(editor.getContent())
                    handleDraftChange();
                });
                editorInstance = editor;
                editorInstance.setContent(data.body);
            }
        });
        UIElements.inputEmailTo.val(data.toAddress);
        UIElements.inputEmailCC.val(data.cc ? data.cc.replace(/,/g, '; ') : '');
        UIElements.inputEmailBCC.val(data.bcc ? data.bcc.replace(/,/g, '; ') : '');
        UIElements.inputEmailFrom.val(data.fromAddress);
        UIElements.inputEmailSubject.val(data.subject);
        UIElements.inputEmailBody.val(data.body);

        if (data && data.status === 'Draft') {
            // Must run after form inputs are assigned
            enableEmail();
            handleDraftChange();
        } else {
            disableEmail();
        }

        showModalEmail();
        resetValidationErrors();
    });

    PubSub.subscribe(
        'applicant_info_updated',
        (_, ApplicantInfoObj) => {
            if(ApplicantInfoObj+"" !== "undefined" 
                && ApplicantInfoObj.ContactEmail+"" != "undefined"
                && ApplicantInfoObj.ContactEmail !== "") {
                UIElements.inputEmailTo[0].value = ApplicantInfoObj.ContactEmail;
            }
        }
    );
});
