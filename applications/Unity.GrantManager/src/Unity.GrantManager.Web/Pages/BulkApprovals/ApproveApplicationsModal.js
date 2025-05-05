function removeApplicationApproval(applicationId) {
    $('#' + applicationId).remove();
    let applicationsCount = $('#ApplicationsCount').val();
    $('#ApplicationsCount').val(applicationsCount - 1);
    runValidations();
}

function approvedAmountUpdated(event) {
    const input = event.target;
    const value = parseFloat(input.value.replace(/,/g, ''));

    setNote(event.target, '_APPROVED_AMOUNT_DEFAULTED', false);

    if (isNaN(value) || value <= 0) {
        setNote(event.target, '_INVALID_APPROVED_AMOUNT', true);
    } else {
        setNote(event.target, '_INVALID_APPROVED_AMOUNT', false);
    }
    runValidations();
}

function decisionDateUpdated(event) {
    setNote(event.target, '_DECISION_DATE_DEFAULTED', false)
    runValidations();
}

function setNote(target, note, visible) {
    const input = target;
    const containerId = input.closest('.batch-approval-container').id;
    const noteField = $('#' + containerId + note);

    if (noteField.length) {
        if (visible) {
            noteField.css('display', 'block');
        } else {
            noteField.css('display', 'none');
        }
    }
}

function runValidations() {
    let isValid = true;
    let itemCount = 0;

    $('#bulkApprovalForm input[name="BulkApplicationApprovals.Index"]').each(function () {
        itemCount++;
        let index = $(this).val();
        let approvedAmount = parseFloat($('#bulkApprovalForm input[name="BulkApplicationApprovals[' + index + '].ApprovedAmount"]').val().replace(/,/g, ''));
        let decisionDate = new Date($('#bulkApprovalForm input[name="BulkApplicationApprovals[' + index + '].DecisionDate"]').val());
        let isValidField = $('#bulkApprovalForm input[name="BulkApplicationApprovals[' + index + '].IsValid"]').val();

        if (isValidField.toLowerCase() !== 'true' || isNaN(approvedAmount) || approvedAmount <= 0 || isNaN(decisionDate.getTime()) || decisionDate > new Date()) {
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
        enableBulkApprovalSubmit();
    } else {
        disableBulkApprovalSubmit();
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

function enableBulkApprovalSubmit() {
    $("#approveApplicationsModal")
        .find('#btnSubmitBatchApproval').prop("disabled", false);
}

function disableBulkApprovalSubmit() {
    $("#approveApplicationsModal")
        .find('#btnSubmitBatchApproval').prop("disabled", true);
}

function closeApprovals() {
    $('#approveApplicationsModal').modal('hide');
}

