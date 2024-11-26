$(function () {

    const UIElements = {
        applicationId: $('#DetailsViewApplicationId')[0].value,
        btnSend: $('#btn-send'),
        btnConfirmSend: $('#btn-confirm-send'),
        btnCancelEmail: $('#btn-cancel-email'),
        btnNewEmail: $('#btn-new-email'),
        btnSendClose: $('#btn-send-close'),
        emailForm: $('#EmailForm'),
        inputEmailTo: $($('#EmailTo')[0]),
        inputEmailToField: $('#EmailTo')[0],
        inputEmailFrom: $($('#EmailFrom')[0]),
        inputEmailSubject: $($('#EmailSubject')[0]),
        inputEmailBody: $($('#EmailBody')[0]),
        emailSpinner: $('#spinner-modal'),
        confirmationModal: $('#confirmation-modal')
    };

    function bindUIEvents() {
        UIElements.btnNewEmail.on('click', showModalEmail);
        UIElements.btnSend.on('click', handleSendEmail);
        UIElements.btnConfirmSend.on('click', handleConfirmSendEmail);
        UIElements.btnCancelEmail.on('click', handleCancelEmailSend);
        UIElements.btnSendClose.on('click', handleCloseEmail);
        UIElements.inputEmailSubject.on('change', handleKeyUpTrim);
        UIElements.inputEmailFrom.on('change', handleKeyUpTrim);
        UIElements.inputEmailBody.on('change', handleKeyUpTrim);
        UIElements.inputEmailTo.on('change', validateEmailTo);
    }

    init();

    function init() {
        bindUIEvents();
        toastr.options.positionClass = 'toast-top-center';
    }

    function handleKeyUpTrim(e) {
        let trimmedString = e.currentTarget.value.trim();
        e.currentTarget.value = trimmedString;
    }

    function handleCloseEmail() {
        $('#modal-content, #modal-background').removeClass('active');
        UIElements.emailForm.removeClass('active');
        UIElements.btnNewEmail.removeClass('hide');
        UIElements.emailForm.trigger("reset");
    }

    function handleCancelEmailSend() {
        $('#modal-content, #modal-background').removeClass('active');
    }

    function showModalEmail() {
        UIElements.emailForm.addClass('active');
        UIElements.btnNewEmail.addClass('hide');
    }

    function validateEmail(email) {
        const emailRegex = /^[\w.-]+@[a-z]+\.[a-z]{2,}$/;
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

    function handleSendEmail(e) {
        // Prevent form submission and stop propagation
        e.stopPropagation();
        e.preventDefault();

        // Validate the "Email To" field
        if (!validateEmailTo()) {
            return false; // If validation fails, stop further processing
        }

        // Check if the form is valid
        if (UIElements.emailForm.valid()) {
            showConfirmation(); // Show confirmation if the form is valid
            return true; // Return true to indicate success
        }

        // If form is not valid, do not show confirmation
        return false; // Return false if validation or other conditions fail
    }
});