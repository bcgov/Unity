function saveAssessmentScores() {
    try {
        let data = {
            "financialAnalysis": $("#financialAnalysis").val(),
            "economicImpact": $("#economicImpact").val(),
            "inclusiveGrowth": $("#inclusiveGrowth").val(),
            "cleanGrowth": $("#cleanGrowth").val(),
            "assessmentId": $("#AssessmentId").val(),
        }        
        unity.grantManager.assessments.assessment.updateAssessmentScore(data)
            .done(function () {
                abp.notify.success(
                    'Assessment scores has been updated.'
                );
                PubSub.publish('refresh_assessment_scores', null);
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