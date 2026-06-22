// Email Widget Utilities - Reusable functions for email composition and validation

/**
 * Validates email format
 * @param {string} email - Email address to validate
 * @returns {boolean} True if valid email format
 */
globalThis.validateEmail = function(email) {
    // Optimized regex to prevent ReDoS: use atomic-like patterns with specific character classes
    const emailRegex = /^[a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.exec(String(email).toLowerCase()) !== null;
}

/**
 * Escapes HTML special characters to prevent XSS
 * @param {string} str - String to escape
 * @returns {string} HTML-escaped string
 */
globalThis.escapeHtml = function(str) {
    return String(str)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}

/**
 * Normalizes error messages by replacing field names with user-friendly labels
 * @param {string} message - Error message to normalize
 * @returns {string} Normalized error message
 */
globalThis.normalizeErrorMessage = function(message) {
    if (!message) return message;
    
    // Handle "The {FieldName} field" format
    message = message
        .replaceAll(/The EmailTo field/gi, 'The To field')
        .replaceAll(/The EmailCC field/gi, 'The CC field')
        .replaceAll(/The EmailBCC field/gi, 'The BCC field')
        .replaceAll(/The EmailFrom field/gi, 'The From field')
        .replaceAll(/The EmailSubject field/gi, 'The Subject field')
        .replaceAll(/The EmailBody field/gi, 'The Body field');
    
    // Also handle just the field names themselves in case they appear elsewhere
    message = message.replaceAll(/(EmailTo|EmailCC|EmailBCC|EmailFrom|EmailSubject|EmailBody)/g, (match) => {
        const replacements = {
            'EmailTo': 'To',
            'EmailCC': 'CC',
            'EmailBCC': 'BCC',
            'EmailFrom': 'From',
            'EmailSubject': 'Subject',
            'EmailBody': 'Body'
        };
        return replacements[match] ?? match;
    });
    
    return message;
}

/**
 * Displays validation error toast using abp.notify or toastr
 * @param {string[]} errors - Array of error messages to display
 */
globalThis.showValidationErrorToast = function(errors) {
    if (!errors?.length) return;
    
    // Build error message with line breaks for each error
    let errorMessage = errors.map((err, idx) => (idx === 0 ? '• ' : '') + err).join('<br>• ');
    
    console.log('Showing validation errors:', errors);
    
    // Use abp.notify if available, fallback to toastr if available
    if (globalThis.abp?.notify) {
        const errorTitle = 'Validation Error' + (errors.length > 1 ? 's' : '');
        abp.notify.error(errorMessage, errorTitle);
    } else if (globalThis.toastr) {
        toastr.error(errorMessage, 'Validation Error' + (errors.length > 1 ? 's' : ''), {
            timeOut: 0,
            extendedTimeOut: 0,
            closeButton: true,
            escapeHtml: false
        });
    } else {
        // Final fallback: alert
        console.error('Validation Errors:', errors.join('\n'));
        alert('Validation Error:\n\n' + errors.join('\n'));
    }
}

/**
 * Collects email field validation errors
 * @param {object} toResult - To field validation result
 * @param {object} ccResult - CC field validation result
 * @param {object} bccResult - BCC field validation result
 * @returns {string[]} Array of error messages
 */
globalThis.collectEmailFieldErrors = function(toResult, ccResult, bccResult) {
    let allErrors = [];
    if (!toResult.isValid && toResult.error) allErrors.push(toResult.error);
    if (!ccResult.isValid && ccResult.error) allErrors.push(ccResult.error);
    if (!bccResult.isValid && bccResult.error) allErrors.push(bccResult.error);
    return allErrors;
}

/**
 * Checks if only TinyMCE editor has validation error
 * @param {boolean} isValid - Overall form validity
 * @param {array} errorList - jQuery validator error list
 * @param {string} fieldName - Field name to check
 * @param {string} tinymceContent - TinyMCE editor content
 * @returns {boolean} True if only TinyMCE has error
 */
globalThis.checkOnlyErrorIsTinyMCE = function(isValid, errorList, fieldName, tinymceContent) {
    return !isValid && errorList.length === 1 && 
            errorList[0].element.name === fieldName && 
            tinymceContent.length > 0;
}

/**
 * Validates date values
 * @param {number} month - Month (1-12)
 * @param {number} day - Day (1-31)
 * @param {number} year - Year
 * @returns {boolean} True if valid date
 */
globalThis.isValidDate = function(month, day, year) {
    const date = new Date(year, month - 1, day);
    return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
}

/**
 * Converts string to title case
 * @param {string} str - String to convert
 * @returns {string} Title-cased string
 */
globalThis.toTitleCase = function(str) {
    return str.replaceAll(/\b\w/, function (char) {
        return char.toUpperCase();
    }).replaceAll(/\B\w/, function (char) {
        return char.toLowerCase();
    });
}

/**
 * Gets nested object property by dot-notation path
 * @param {object} obj - Object to traverse
 * @param {string} path - Property path (e.g., "applicant.applicantName")
 * @returns {*} Value at path or undefined
 */
globalThis.getValueByPath = function(obj, path) {
    return path.split('.').reduce((acc, key) => acc?.[key], obj);
}

/**
 * Creates a currency formatter for Canadian dollars
 * @returns {Intl.NumberFormat} Formatter instance
 */
globalThis.createCurrencyFormatter = function() {
    return new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });
}

/**
 * Returns TinyMCE editor toolbar options
 * @returns {string} Toolbar configuration
 */
globalThis.getToolbarOptions = function() {
    return 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist | link image | code preview';
}

/**
 * Returns TinyMCE editor plugins
 * @returns {string} Comma-separated plugin names
 */
globalThis.getPlugins = function() {
    return 'lists link image preview code';
}

/**
 * Generates HTML for email attachment button
 * @param {string} attachmentId - Attachment ID
 * @returns {string} HTML for attachment button
 */
globalThis.generateEmailAttachmentButtonContent = function(attachmentId) {
    return `
        <div class="dropdown" style="float:right;">
            <button class="btn btn-light dropbtn" type="button">
                <i class="fl fl-attachment-more"></i>
            </button>
            <div class="dropdown-content">
                <button class="btn fullWidth" style="margin:10px" type="button"
                        onclick="deleteEmailAttachment('${attachmentId}')">
                    <i class="fl fl-cancel"></i><span>Delete Attachment</span>
                </button>
            </div>
        </div>`;
}

/**
 * Deletes email attachment with confirmation
 * @param {string} attachmentId - Attachment ID to delete
 */
globalThis.deleteEmailAttachment = function(attachmentId) {
    abp.message.confirm(
        'Are you sure you want to delete this attachment?',
        'Delete Attachment',
        function (confirmed) {
            if (confirmed) {
                unity.notifications.emails.emailLogAttachment
                    .delete(attachmentId)
                    .then(function () {
                        abp.notify.success('Attachment deleted successfully.');
                        PubSub.publish('reload_email_attachments_table');
                    })
                    .catch(function (e) {
                        console.warn('Failed to delete attachment:', e);
                        abp.notify.error('Failed to delete attachment.');
                    });
            }
        }
    );
}

/**
 * Trims input value on keyup event
 * @param {event} e - Keyup event
 */
globalThis.handleKeyUpTrim = function(e) {
    let trimmedString = e.currentTarget.value.trim();
    e.currentTarget.value = trimmedString;
}

/**
 * Gets email validation error message
 * @param {string} emailStr - Email string to validate
 * @param {string} emailValue - Email value for error context
 * @param {string} fieldName - Field name for error message
 * @returns {string|null} Error message or null if valid
 */
globalThis.getEmailValidationError = function(emailStr, emailValue, fieldName) {
    if (emailStr === '' || !validateEmail(emailStr)) {
        let errorMessage;
        if (emailStr === '') {
            errorMessage = emailValue.length > 0
                ? `An email is required after each comma or semicolon.`
                : `The ${escapeHtml(fieldName)} field is required.`;
        } else {
            errorMessage = `Please enter a valid email in ${escapeHtml(fieldName)}: ${escapeHtml(emailStr)}`;
        }
        return normalizeErrorMessage(errorMessage);
    }
    return null;
}
