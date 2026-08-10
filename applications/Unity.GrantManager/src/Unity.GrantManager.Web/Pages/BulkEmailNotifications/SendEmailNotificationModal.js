function removeApplicationEmail(applicationId) {
    $('#' + applicationId).remove();
    let applicationsCount = $('#ApplicationsCount').val();
    $('#ApplicationsCount').val(applicationsCount - 1);
    runValidations();
}

function runValidations() {
    let isValid = true;
    let itemCount = 0;

    $('#bulkEmailNotificationForm input[name="BulkEmailNotifications.Index"]').each(function () {
        itemCount++;
        let index = $(this).val();
        let isValidField = $('#bulkEmailNotificationForm input[name="BulkEmailNotifications[' + index + '].IsValid"]').val();

        if (isValidField.toLowerCase() !== 'true') {
            isValid = false;
        }
    });

    if (itemCount === 0) {
        isValid = false;
    }

    if (!validBatchCount()) {
        isValid = false;
        setMaxCountError(true);
    } else {
        setMaxCountError(false);
    }

    if (isValid) {
        enableBulkEmailSubmit();
    } else {
        disableBulkEmailSubmit();
    }
}

function setMaxCountError(visible) {
    const summary = $('#batch-approval-summary');
    if (visible) {
        summary.css('display', 'block');
    } else {
        summary.css('display', 'none');
    }
}

function validBatchCount() {
    let applicationsCount = $('#ApplicationsCount').val();
    let maxBatchCount = $('#MaxBatchCount').val();
    return applicationsCount <= maxBatchCount;
}

function enableBulkEmailSubmit() {
    $("#sendEmailNotificationModal")
        .find('#btnSubmitBulkEmail').prop("disabled", false);
}

function disableBulkEmailSubmit() {
    $("#sendEmailNotificationModal")
        .find('#btnSubmitBulkEmail').prop("disabled", true);
}

function closeEmailNotifications() {
    $('#sendEmailNotificationModal').modal('hide');
}
