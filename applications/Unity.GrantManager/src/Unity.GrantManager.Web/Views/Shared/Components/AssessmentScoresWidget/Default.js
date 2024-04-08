function saveAssessmentScores() {
    try {        
        let data = {
            "financialAnalysis": parseScoreValueInput("financialAnalysis"),
            "economicImpact": parseScoreValueInput("economicImpact"),
            "inclusiveGrowth": parseScoreValueInput("inclusiveGrowth"),
            "cleanGrowth": parseScoreValueInput("cleanGrowth"),
            "assessmentId": $("#AssessmentId").val(),
        }        
        unity.grantManager.assessments.assessment.updateAssessmentScore(data)
            .done(function () {
                abp.notify.success(
                    'Assessment scores has been updated.'
                );
                PubSub.publish('refresh_assessment_scores', null);
                PubSub.publish('refresh_review_list_without_select', $("#AssessmentId").val());
            });

    }
    catch (error) {
        console.log(error);
    }
};

function parseScoreValueInput(name) {    
    let control = "#" + name;
    return $(control).val() == '' ? 0 : Math.min($(control).attr('max'), $(control).val())
}

function enableSaveButton(inputText) {
    if (inputText.value.trim() != "") {
        $('#saveAssessmentScoresBtn').prop('disabled', false);
    } else {
        $('#saveAssessmentScoresBtn').prop('disabled', true);
    }    
    updateSum();
}

function updateSum() {
    let financialAnalysis = $('#financialAnalysis').val() || 0;
    let inclusiveGrowth = $('#inclusiveGrowth').val() || 0;
    let cleanGrowth = $('#cleanGrowth').val() || 0;
    let economicImpact = $('#economicImpact').val() || 0;
    let sum = parseInt(financialAnalysis) + parseInt(inclusiveGrowth) + parseInt(cleanGrowth) + parseInt(economicImpact);
    $('#subTotal').val(sum);
}

function positiveIntegersOnly(e) {    
    if (e.keyCode === 9
        || e.keyCode === 8
        || e.keyCode === 37
        || e.keyCode === 39) {
        return true;
    }
    if(e.target?.value?.length >= 2 ) {
        return false;
    }
    if (!((e.keyCode > 95 && e.keyCode < 106)
        || (e.keyCode > 47 && e.keyCode < 58)
        || e.keyCode == 8)) {
        return false;
    }
}
