function removeApplicationRow(applicationId) {
    $('#' + applicationId).remove();
    let applicationsCount = $('#ApplicationsCount').val();
    $('#ApplicationsCount').val(applicationsCount - 1);
    runValidations();
}

function setMaxCountError(visible) {
    const summary = $('#batch-action-summary');
    if (visible) {
        summary.css('display', 'block');
    } else {
        summary.css('display', 'none');
    }
}

function enableBulkPublishSubmit() {
    $("#bulkPublishApplicationsModal")
        .find('#btnSubmitBatchPublish').prop("disabled", false);
}

function disableBulkPublishSubmit() {
    $("#bulkPublishApplicationsModal")
        .find('#btnSubmitBatchPublish').prop("disabled", true);
}

function closePublish() {
    $('#bulkPublishApplicationsModal').modal('hide');
}

function runValidations() {
    let isValid = true;
    let itemCount = 0;

    $('#bulkPublishForm input[name="BulkApplications.Index"]').each(function () {
        itemCount++;
    });

    isValid = validBatchCount();
    $('#batch-action-summary').toggleClass('d-none', isValid);

    if (isValid) {
        enableBulkPublishSubmit();
    } else {
        disableBulkPublishSubmit();
    }
}

function validBatchCount() {
    let applicationsCount = $('#ApplicationsCount').val();
    let maxBatchCount = $('#MaxBatchCount').val();
    let validationMaxValid = true;
    let validationMinValid = true;

    if (maxBatchCount <= applicationsCount) {
        validationMaxValid = false;
    } else if (applicationsCount === 0) {
        validationMinValid = false;
    }

    $('#maxCountWarning').toggleClass('d-none', validationMaxValid);
    $('#minCountWarning').toggleClass('d-none', validationMinValid);

    return validationMaxValid && validationMinValid;
}

$(function () {
    runValidations();
});