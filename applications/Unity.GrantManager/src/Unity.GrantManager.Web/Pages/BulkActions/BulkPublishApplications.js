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

function closePublish() {
    $('#bulkPublishApplicationsModal').modal('hide');
}

function runValidations() {
    const isValid = validBatchCount();
    $('#batch-action-summary').toggleClass('d-none', isValid);
    $("#bulkPublishApplicationsModal").find('#btnSubmitBatchPublish').prop("disabled", !isValid);
}

function validBatchCount() {
    const applicationsCount = Number.parseInt(String($('#ApplicationsCount').val() ?? ''), 10);
    const maxBatchCount = Number.parseInt(String($('#MaxBatchCount').val() ?? ''), 10);
    let validationMaxValid = true;
    let validationMinValid = true;

    const hasInvalidNumber = Number.isNaN(applicationsCount) || Number.isNaN(maxBatchCount);
    if (hasInvalidNumber) {
        return false;
    }

    if (applicationsCount > maxBatchCount) {
        validationMaxValid = false;
    } else if (applicationsCount === 0) {
        validationMinValid = false;
    }

    $('#maxCountWarning').toggleClass('d-none', validationMaxValid);
    $('#minCountWarning').toggleClass('d-none', validationMinValid);
    $('.batch-action-card').toggleClass('d-none', !validationMinValid);

    return validationMaxValid && validationMinValid;
}

$(function () {
    class BulkPublishModal {
        initModal(publicApi, args) {
            runValidations();
        }
    }

    abp.modals.BulkPublishModal = BulkPublishModal;
});