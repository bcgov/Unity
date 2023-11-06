$(function () {

    $('.currency-input').maskMoney();
    disableForm();

    $('body').on('click', '#saveAssessmentResultBtn', function () {
        let applicationId = document.getElementById('AssessmentResultViewApplicationId').value;
        let formData = $("#assessmentResultForm").serializeArray();
        let assessmentResultObj = {};
        $.each(formData, function (key, input) {
            if ((input.name == "AssessmentResults.ProjectSummary") || (input.name == "AssessmentResults.Notes")) {
                assessmentResultObj[input.name.split(".")[1]] = input.value;
            } else {
                assessmentResultObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');
            }
        });
        try {
            unity.grantManager.grantApplications.grantApplication
                .update(applicationId, assessmentResultObj)
                .done(function () {
                    abp.notify.success(
                        'The application has been updated.'
                    );
                    $('#saveAssessmentResultBtn').prop('disabled', true);
                });
        }
        catch (error) {
            console.log(error);
            $('#saveAssessmentResultBtn').prop('disabled', false);
        }
    });

    PubSub.subscribe(
        'application_status_changed',
        (msg, data) => {
            console.log(msg, data);
            if (['Approve', 'Deny', 'Close', 'WithDraw'].includes(data)) {
                disableForm(true);
            }
        }
    );
});

function disableForm(doDisable = false) {
    let isEditGranted = (document.getElementById('isEditGranted').value == 'True');
    let isEditApprovedAmount = (document.getElementById('isEditApprovedAmount').value == 'True');
    if (!isEditGranted || doDisable) {
        $('.assessment-result-form .form-control').prop('disabled', true);
    }
    if (isEditApprovedAmount) {
        $('#approvedAmountInput').prop('disabled', false);
    }
}


function enableResultSaveBtn(inputText) {
    if (inputText.value.trim() != "") {
        $('#saveAssessmentResultBtn').prop('disabled', false);
    } else {
        $('#saveAssessmentResultBtn').prop('disabled', true);
    }

}