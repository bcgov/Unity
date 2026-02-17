let createPaymentNumberFormatter = createNumberFormatter();

function removeApplicationPaymentRequest(applicationId) {
    let $container = $('#' + applicationId);
    let $parentGroup = $container.closest('.parent-child-group');
    $container.remove();

    $('#' + applicationId).remove();
    let applicationCount = $('#ApplicationCount').val();
    $('#ApplicationCount').val(applicationCount - 1);

    if (applicationCount - 1 == 1) {
        $('.max-error').css('display', 'none');
        $('.payment-divider').css('display', 'none');
    }
    if (!$('div.single-payment').length) {
        $('#no-payment-msg').css('display', 'block');
        $('#payment-modal').find('#btnSubmitPayment').prop('disabled', true);
    } else {
        $('#no-payment-msg').css('display', 'none');
    }

    // Clean up empty group wrappers
    if ($parentGroup.length && $parentGroup.find('.single-payment').length === 0) {
        $parentGroup.remove();
    }

    // Always recalculate the total after removal
    calculateTotalAmount();

    // Re-validate all remaining payment amounts
    validateAllPaymentAmounts();
}

function closePaymentModal() {
    $('#payment-modal').modal('hide');
}

function checkMaxValueRequest(applicationId, input, amountRemaining) {
    // Check if this payment is part of a parent-child group
    if (isPartOfParentChildGroup(applicationId)) {
        // Use parent-child validation
        validateParentChildAmounts(applicationId);
    } else {
        // Use existing remaining amount validation
        let enteredValue = parseFloat(input.value.replace(/,/g, ''));
        let remainingErrorId = '#column_' + applicationId + '_remaining_error';
        if (amountRemaining < enteredValue) {
            $(remainingErrorId).css('display', 'block');
        } else {
            $(remainingErrorId).css('display', 'none');
        }
    }

    // Update the total amount after checking the value
    calculateTotalAmount();
}

function validateAllPaymentAmounts() {
    // Iterate through all payment requests
    $('input[name*=".CorrelationId"]').each(function () {
        let correlationId = $(this).val();
        let index = getIndexByCorrelationId(correlationId);
        let isPartOfGroup =
            $(
                `input[name="ApplicationPaymentRequestForm[${index}].IsPartOfParentChildGroup"]`
            ).val() === 'True';

        if (isPartOfGroup) {
            // Validate parent-child amounts
            validateParentChildAmounts(correlationId);
        } else {
            // Validate standalone payment against remaining amount
            let amountInput = $(
                `input[name="ApplicationPaymentRequestForm[${index}].Amount"]`
            );
            let remainingAmount = parseFloat(
                $(
                    `input[name="ApplicationPaymentRequestForm[${index}].RemainingAmount"]`
                ).val()
            );
            let enteredValue =
                parseFloat(amountInput.val().replace(/,/g, '')) || 0;
            let remainingErrorId = `#column_${correlationId}_remaining_error`;

            if (enteredValue > remainingAmount) {
                $(remainingErrorId).css('display', 'block');
            } else {
                $(remainingErrorId).css('display', 'none');
            }
        }
    });
}

function submitPayments() {
    // Validate all payment amounts before checking for errors
    validateAllPaymentAmounts();

    // check for error class divs
    let validationFailed = $('.payment-error-column:visible').length > 0;

    if (validationFailed) {
        abp.notify.error(
            '',
            'There are payment requests that are in error please remove or fix them before submitting.'
        );
        return false;
    } else {
        $('#paymentform').submit();
    }
}

function calculateTotalAmount() {
    let total = 0;
    $('.amount').each(function () {
        let value = parseFloat($(this).val().replace(/,/g, '')) || 0;
        total += value;
    });

    let totalFormatted = createPaymentNumberFormatter.format(total);
    $('#TotalAmount').val(totalFormatted);
}

function getIndexByCorrelationId(correlationId) {
    // Find the index of the payment request by CorrelationId
    let index = -1;
    $('input[name*=".CorrelationId"]').each(function () {
        if ($(this).val() === correlationId) {
            // Extract the actual index from the name attribute
            // e.g., "ApplicationPaymentRequestForm[2].CorrelationId" -> "2"
            let match = $(this)
                .attr('name')
                .match(/\[(\d+)\]/);
            if (match) {
                index = parseInt(match[1], 10);
            }
            return false; // break
        }
    });
    return index;
}

function isPartOfParentChildGroup(correlationId) {
    let index = getIndexByCorrelationId(correlationId);
    if (index === -1) return false;

    let input = $(
        `input[name="ApplicationPaymentRequestForm[${index}].IsPartOfParentChildGroup"]`
    );
    return input.val() === 'True';
}

const cadFormatter = createNumberFormatter(); // wraps Intl.NumberFormat('en-CA', ...)

function formatCurrency(value) {
    const numericValue =
        typeof value === 'number'
            ? value
            : parseFloat(String(value ?? '').replace(/,/g, ''));
    return cadFormatter.format(
        Number.isFinite(numericValue) ? numericValue : 0
    );
}

function validateParentChildAmounts(correlationId) {
    let index = getIndexByCorrelationId(correlationId);
    if (index === -1) return;

    let parentRefNo = $(
        `input[name="ApplicationPaymentRequestForm[${index}].ParentReferenceNo"]`
    ).val();
    let submissionCode = $(
        `input[name="ApplicationPaymentRequestForm[${index}].SubmissionConfirmationCode"]`
    ).val();
    let maximumAllowedInput = $(
        `input[name="ApplicationPaymentRequestForm[${index}].MaximumAllowedAmount"]`
    ).val();
    let parentApprovedAmount = $(
        `input[name="ApplicationPaymentRequestForm[${index}].ParentApprovedAmount"]`
    ).val();
    let maximumAllowed = maximumAllowedInput
        ? parseFloat(maximumAllowedInput)
        : 0;
    let approvedAmount = parentApprovedAmount
        ? parseFloat(parentApprovedAmount)
        : 0;

    // Determine if this is a parent or child
    let isChild = parentRefNo && parentRefNo.trim() !== '';
    let groupKey = isChild ? parentRefNo : submissionCode;

    // Find all payments in this parent-child group
    let groupTotal = 0;

    $('input[name*=".CorrelationId"]').each(function () {
        let itemCorrelationId = $(this).val();
        let itemIndex = getIndexByCorrelationId(itemCorrelationId);

        let itemParentRefNo = $(
            `input[name="ApplicationPaymentRequestForm[${itemIndex}].ParentReferenceNo"]`
        ).val();
        let itemSubmissionCode = $(
            `input[name="ApplicationPaymentRequestForm[${itemIndex}].SubmissionConfirmationCode"]`
        ).val();
        let itemIsPartOfGroup =
            $(
                `input[name="ApplicationPaymentRequestForm[${itemIndex}].IsPartOfParentChildGroup"]`
            ).val() === 'True';

        if (!itemIsPartOfGroup) return true; // Continue to next iteration

        // Check if this item belongs to the current group
        let itemIsChild = itemParentRefNo && itemParentRefNo.trim() !== '';
        let itemGroupKey = itemIsChild ? itemParentRefNo : itemSubmissionCode;

        if (itemGroupKey === groupKey) {
            let amountInput = $(
                `input[name="ApplicationPaymentRequestForm[${itemIndex}].Amount"]`
            );
            let amount = parseFloat(amountInput.val().replace(/,/g, '')) || 0;
            groupTotal += amount;
        }
    });

    // Validate: groupTotal <= maximumAllowed
    let hasError = groupTotal > maximumAllowed;

    // Show/hide error once at the group level
    if (groupKey == null) return;
    let groupWrapper = $(`.parent-child-group[data-group-key="${CSS.escape(groupKey)}"]`);
    let groupErrorDiv = groupWrapper.find('[data-role="group-error"]');
    let groupErrorMessage = groupWrapper.find('[data-role="group-error-message"]');

    if (hasError) {
        let message = `The total payment amount (${formatCurrency(
            groupTotal
        )}) exceeds the remaining balance (${formatCurrency(
            maximumAllowed
        )}) of the approved amount (${formatCurrency(
            approvedAmount
        )}) for the application or its parent application.`;
        groupErrorMessage.text(message);
        groupErrorDiv.css('display', 'block');
        groupWrapper.addClass('has-error');
    } else {
        groupErrorDiv.css('display', 'none');
        groupWrapper.removeClass('has-error');
    }
}
