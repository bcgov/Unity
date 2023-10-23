function saveAssessmentScores() {
    try {

        let data = {
            "financialAnalysis": $("#financialAnalysis").val() == '' ? 0 : $("#financialAnalysis").val(),
            "economicImpact": $("#economicImpact").val() == '' ? 0 : $("#economicImpact").val(),
            "inclusiveGrowth": $("#inclusiveGrowth").val() == '' ? 0 : $("#inclusiveGrowth").val(),
            "cleanGrowth": $("#cleanGrowth").val() == '' ? 0 : $("#cleanGrowth").val(),
            "assessmentId": $("#AssessmentId").val(),
        }        
        unity.grantManager.assessments.assessment.updateAssessmentScore(data)
            .done(function () {
                abp.notify.success(
                    'Assessment scores has been updated.'
                );
                PubSub.publish('refresh_assessment_scores', null);
                PubSub.publish('refresh_review_list', $("#AssessmentId").val());
            });

    }
    catch (error) {
        console.log(error);
    }
};

function enableSaveButton(inputText) {
    if (inputText.value.trim() != "") {
        $('#saveAssessmentScoresBtn').prop('disabled', false);
    } else {
        $('#saveAssessmentScoresBtn').prop('disabled', true);
    }
    
}