function saveAssessmentScores() {
    try {
        let data = {
            "financialAnalysis": $("#financialAnalysis").val(),
            "economicImpact": $("#financialAnalysis").val(),
            "inclusiveGrowth": $("#inclusiveGrowth").val(),
            "cleanGrowth": $("#inclusiveGrowth").val(),
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