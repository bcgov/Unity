$(function () {

    $('.currency-input').maskMoney();
    //disableForm();

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

});


function enableResultSaveBtn(inputText) {
    if (inputText.value.trim() != "") {
        $('#saveAssessmentResultBtn').prop('disabled', false);
    } else {
        $('#saveAssessmentResultBtn').prop('disabled', true);
    }

}