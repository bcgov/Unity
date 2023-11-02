$(function () {
    $('body').on('click', '#save_assessment_results_btn', function () {
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
