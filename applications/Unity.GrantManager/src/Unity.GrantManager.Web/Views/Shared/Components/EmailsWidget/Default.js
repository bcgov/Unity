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
        UIElements.btnSend.on('click', handlSendEmail);
        UIElements.btnConfirmSend.on('click', handlConfirmSendEmail);
        UIElements.btnCancelEmail.on('click', handlCancelEmailSend);
        UIElements.btnSendClose.on('click', handlCloseEmail);
        UIElements.inputEmailSubject.on('change', handlKeyUpTrim);
        UIElements.inputEmailFrom.on('change', handlKeyUpTrim);
        UIElements.inputEmailBody.on('change', handlKeyUpTrim);
        UIElements.inputEmailTo.on('change', validateEmailTo);
    }

    init();

    function init() {
        bindUIEvents();
        toastr.options.positionClass = 'toast-top-center';
    }

    function handlKeyUpTrim(e) {
        let trimmedString = e.currentTarget.value.trim();
        e.currentTarget.value = trimmedString;
    }

    function handlCloseEmail() {
        $('#modal-content, #modal-background').removeClass('active');
        UIElements.emailForm.removeClass('active');
        UIElements.btnNewEmail.removeClass('hide');
        UIElements.emailForm.trigger("reset");
    }

    function handlCancelEmailSend() {
        $('#modal-content, #modal-background').removeClass('active');
    }

    function showModalEmail() {
        UIElements.emailForm.addClass('active');
        UIElements.btnNewEmail.addClass('hide');
    }

    function validateEmail(email)
    {
        return String(email)
          .toLowerCase()
          .match(
            /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|.(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/
          );
      };

    function validateEmailTo() {
        // Split on both semi colon and comma
        let emailValue = UIElements.inputEmailToField.value.trim().trimEnd(',').trimEnd(';');
        let emails = emailValue.split(/;|,/g);
        let valid = true;
        let emailToErrorSpan = $("span[data-valmsg-for*='EmailTo']")[0];

        for (let i = 0; i < emails.length; i++) {
             let emailStr = emails[i].trim();
             if( emailStr == '' || ! validateEmail(emailStr)){
                let emailToError = 'Please enter a valid Email To : '+ emailStr;
                if(emailStr == '') {
                     if(emailValue.length > 0) {
                        emailToError = 'An email is required after each comma or semicolon.';
                     } else {
                        emailToError = 'The Email To field is required.';
                     }
                     
                }

                $(emailToErrorSpan).addClass('field-validation-error').removeClass('field-validation-valid');
                $(emailToErrorSpan).html(emailToError);
                return false;
             }
        }
        $(emailToErrorSpan).addClass('field-validation-valid').removeClass('field-validation-error');
        $(emailToErrorSpan).html('');
        return valid;
    }

    function handlConfirmSendEmail() {
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
                handlCloseEmail();
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

    function handlSendEmail(e) {
        UIElements.emailForm.valid();
        e.stopPropagation();
        e.preventDefault();

        if(!validateEmailTo()) {
            return false;
        }
        
        if(UIElements.emailForm.valid()) {
            showConfirmation();
            return false;
        }

        return false;
    }
});