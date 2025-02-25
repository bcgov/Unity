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
        inputEmailFrom: $($('#EmailFrom')[0]),
        inputEmailSubject: $($('#EmailSubject')[0]),
        inputEmailBody: $($('#EmailBody')[0]),
        inputOriginalEmailTo: $($('#OriginalDraftEmailTo')[0]),
        inputOriginalEmailFrom: $($('#OriginalDraftEmailFrom')[0]),
        inputOriginalEmailSubject: $($('#OriginalDraftEmailSubject')[0]),
        inputOriginalEmailBody: $($('#OriginalDraftEmailBody')[0]),
        emailSpinner: $('#spinner-modal'),
        confirmationModal: $('#confirmation-modal'),
        alertEmailReadonly: $('#email-alert-readonly')
    };

    let defaultValues = {
        emailTo: '',
        emailFrom: ''
    };

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
        UIElements.inputEmailBody.on('change', handleKeyUpTrim);
        UIElements.inputEmailTo.on('change', validateEmailTo);

        UIElements.inputEmailTo.on('input', handleDraftChange);
        UIElements.inputEmailFrom.on('input', handleDraftChange);
        UIElements.inputEmailSubject.on('input', handleDraftChange);
        UIElements.inputEmailBody.on('input', handleDraftChange);
    }

    init();

    function init() {
        bindUIEvents();
        defaultValues.emailTo = UIElements.inputOriginalEmailTo.val();
        defaultValues.emailFrom = UIElements.inputOriginalEmailFrom.val();
        toastr.options.positionClass = 'toast-top-center';
    }

    function disableEmail() {
        UIElements.btnSend.attr('disabled', true);
        UIElements.btnSave.attr('disabled', true);
        UIElements.btnDiscard.attr('disabled', true);
        UIElements.inputEmailTo.attr('disabled', true);
        UIElements.inputEmailFrom.attr('disabled', true);
        UIElements.inputEmailSubject.attr('disabled', true);
        UIElements.inputEmailBody.attr('disabled', true);
    }

    function enableEmail() {
        UIElements.btnSend.attr('disabled', false);
        UIElements.btnSave.attr('disabled', false);
        UIElements.btnDiscard.attr('disabled', false);
        UIElements.inputEmailTo.attr('disabled', false);
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
        UIElements.inputEmailFrom.val(UIElements.inputOriginalEmailFrom.val());
        UIElements.inputEmailSubject.val(UIElements.inputOriginalEmailSubject.val());
        UIElements.inputEmailBody.val(UIElements.inputOriginalEmailBody.val());
    }

    function handleCancelEmailSend() {
        $('#modal-content, #modal-background').removeClass('active');
    }

    function handleNewEmail() {
        UIElements.inputEmailId.val(emptyGuid);
        // Support discard to empty email template for new emails
        UIElements.inputOriginalEmailTo.val(defaultValues.emailTo);
        UIElements.inputOriginalEmailFrom.val(defaultValues.emailFrom);
        UIElements.inputOriginalEmailSubject.val("");
        UIElements.inputOriginalEmailBody.val("");

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
        let emailValue = UIElements.inputEmailToField.value.trim(); // Trim leading/trailing whitespace
        // Remove trailing commas, semicolons, or spaces (safe regex)
        emailValue = emailValue.replace(/[;,\s]+$/, ''); // Remove only trailing semicolons, commas, or spaces

        let emails = emailValue.split(/[,;]/g).map(email => email.trim()); // Split by comma or semicolon, and trim each email
        let emailToErrorSpan = $("span[data-valmsg-for*='EmailTo']")[0];

        // Initialize as valid
        let isValid = true;
        let emailToError = '';  // Initialize error message variable

        // Iterate through the list of emails using for...of loop
        for (let emailStr of emails) {
            // Check if the email is empty or invalid
            if (emailStr === '' || !validateEmail(emailStr)) {
                // Handle empty email input
                if (emailStr === '') {
                    emailToError = emailValue.length > 0
                        ? 'An email is required after each comma or semicolon.'
                        : 'The Email To field is required.';
                } else {
                    // Handle invalid email format
                    emailToError = `Please enter a valid Email To: ${emailStr}`;
                }

                // Display the error message
                $(emailToErrorSpan).addClass('field-validation-error').removeClass('field-validation-valid');
                $(emailToErrorSpan).html(emailToError);

                // Mark the validation as invalid and exit the loop
                isValid = false;
                break; // No need to continue checking further emails
            }
        }

        // Clear error message if all emails are valid
        if (isValid) {
            $(emailToErrorSpan).addClass('field-validation-valid').removeClass('field-validation-error');
            $(emailToErrorSpan).html('');  // Clear any existing error message
        }

        return isValid;
    }


    function handleConfirmSendEmail() {
        UIElements.confirmationModal.hide();
        UIElements.emailSpinner.show();
        unity.grantManager.emails.email
            .create({
                emailId: UIElements.inputEmailId[0].value,
                applicationId: UIElements.applicationId,
                emailTo: UIElements.inputEmailTo[0].value,
                emailFrom: UIElements.inputEmailFrom[0].value,
                emailBody: UIElements.inputEmailBody[0].value,
                emailSubject: UIElements.inputEmailSubject[0].value,
                currentUserId: decodeURIComponent(abp.currentUser.id),
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
        unity.grantManager.emails.email
            .saveDraft({
                emailId: UIElements.inputEmailId[0].value,
                applicationId: UIElements.applicationId,
                emailTo: UIElements.inputEmailTo[0].value,
                emailFrom: UIElements.inputEmailFrom[0].value,
                emailBody: UIElements.inputEmailBody[0].value,
                emailSubject: UIElements.inputEmailSubject[0].value,
                currentUserId: decodeURIComponent(abp.currentUser.id),
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

    function validateEmailForm(e) {
        // Prevent form submission and stop propagation
        e.stopPropagation();
        e.preventDefault();

        // Validate the "Email To" field
        if (!validateEmailTo()) {
            return false; // If validation fails, stop further processing
        }

        return UIElements.emailForm.valid();
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
               UIElements.inputEmailFrom.val() !== UIElements.inputOriginalEmailFrom.val() ||
               UIElements.inputEmailSubject.val() !== UIElements.inputOriginalEmailSubject.val() ||
               UIElements.inputEmailBody.val() !== UIElements.inputOriginalEmailBody.val();
    }

    function resetValidationErrors() {
        UIElements.emailForm.find('.field-validation-error').each(function() {
            $(this).removeClass('field-validation-error').addClass('field-validation-valid').html('');
        });
    }

    PubSub.subscribe('email_selected', (msg, data) => {
        
        UIElements.inputEmailId.val(data.id);
        UIElements.inputOriginalEmailTo.val(data.toAddress);
        UIElements.inputOriginalEmailFrom.val(data.fromAddress);
        UIElements.inputOriginalEmailSubject.val(data.subject);
        UIElements.inputOriginalEmailBody.val(data.body);

        UIElements.inputEmailTo.val(data.toAddress);
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
