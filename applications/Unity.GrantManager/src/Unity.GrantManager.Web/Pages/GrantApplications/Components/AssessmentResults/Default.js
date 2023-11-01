$(function () {
    $('body').on('click', '#saveAssessmentResultBtn', function () {
        let applicationId = document.getElementById('AssessmentResultViewApplicationId').value;
        let formData = $("#assessment_result_form").serializeArray()
        let assessmentResultObj = {};
        $.each(formData, function (key, input) {
            assessmentResultObj[input.name.split(".")[1]] = input.value;
        });
        try {
            unity.grantManager.grantApplications.grantApplication
                .update(applicationId, assessmentResultObj)
                .done(function () {
                    abp.notify.success(
                        'The application has been updated.'
                    );
                });
        }
        catch (error) {
            console.log(error);
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