abp.modals.ManageTokens = function () {
    let viewToggleState = false;

    function initModal(modalManager, args) {        
        $('#ApplicationForm_ApiToken').get(0).type = 'password';

        $('#GenerateApiTokenBtn').click(function (e) {
            e.preventDefault();
            getAndSetNewToken();
        });

        $('#ViewApiTokenBtn').click(function (e) {
            e.preventDefault();            
            viewToggleState = !viewToggleState;
            if (viewToggleState) {
                $('#ViewApiTokenBtn').addClass('app-forms-btn-toggled');
                $('#ApplicationForm_ApiToken').get(0).type = 'text';
            } else {
                $('#ViewApiTokenBtn').removeClass('app-forms-btn-toggled');
                $('#ApplicationForm_ApiToken').get(0).type = 'password';
            }
        });

        $('#CopyApiTokenBtn').click(function (e) {
            e.preventDefault();
            copyToClipboard($('#ApplicationForm_ApiToken').val());
        });

        $('#ClearApiTokenBtn').click(function (e) {
            e.preventDefault();
            $('#ApplicationForm_ApiToken').val(null);
        });

        function getAndSetNewToken() {
            unity.grantManager.applicationForms.applicationFormToken.generateFormApiToken()
                .done(function (result) {                    
                    $('#ApplicationForm_ApiToken').val(result);                    
                    abp.notify.success(
                        'New token has generated.'
                    );
                });
        }

        function copyToClipboard(value) {
            let textToCopy = value;
            let tempTextarea = $('<textarea>');
            $('body').append(textToCopy);
            tempTextarea.val(textToCopy).select();
            navigator.clipboard.writeText(tempTextarea[0].value);
            tempTextarea.remove();
            abp.notify.success(
                'Copied to clipboard.'
            );
        }
    };

    return {
        initModal: initModal
    };
};

