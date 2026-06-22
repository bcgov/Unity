// Mapping Utilities - Reusable functions for form mapping and utilities

/**
 * Strips HTML tags from a string using safe DOMParser
 * @param {string} html - HTML string to strip
 * @returns {string} Plain text content
 */
function stripHtml(html) {
    const parser = new DOMParser();
    const doc = parser.parseFromString(String(html), 'text/html');
    return doc.body.textContent || '';
}

/**
 * Validates GUID format
 * @param {string} textString - String to validate as GUID
 * @returns {boolean} True if valid GUID format
 */
function validateGuid(textString) {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-5][0-9a-f]{3}-[089ab][0-9a-f]{3}-[0-9a-f]{12}$/.test(textString);
}

/**
 * Initializes DataTable for Application Forms mapping
 * Destroys existing instance if present
 * @returns {DataTable} Configured DataTable instance
 */
function initializeApplicationFormsTable() {
    const tableSelector = '#ApplicationFormsTable';
    
    // Destroy existing DataTable instance if it exists
    if ($.fn.DataTable.isDataTable(tableSelector)) {
        $(tableSelector).DataTable().destroy();
    }
    
    return new DataTable(tableSelector, {
        info: false,
        ordering: false,
        fixedHeader: false,
        paging: false,
        columnDefs: [
            {
                render: function (data) {
                    return '<div id="' + data + '" class="col map-div non-drag" draggable="false"></div>';
                },
                targets: 3
            }
        ]
    });
}

/**
 * Maps intake field types to icon classes
 * @param {object} intakeField - Field object with Type property
 * @returns {string} Icon class name
 */
function setTypeIcon(intakeField) {
    switch (intakeField.Type) {
        case 'String':
            return 'fl fl-font';
        case 'Phone':
            return 'fl fl-phone';
        case 'Date':
            return 'fl fl-datetime';
        case 'Email':
            return 'fl fl-mail';
        case 'Radio':
            return 'fl fl-radio';
        case 'Checkbox':
            return 'fl fl-checkbox-checked';
        case 'CheckboxGroup':
            return 'fl fl-multi-select';
        case 'SelectList':
            return 'fl fl-list';
        case 'BCAddress':
            return 'fl fl-globe';
        case 'TextArea':
            return 'fl fl-text-area';
        case 'DataGrid':
            return 'fl fl-datagrid';
        default:
            return '';
    }
}

/**
 * Creates HTML span for type indicator text
 * @param {string} text - Indicator text (e.g., "123", "$", "Y/N")
 * @returns {string} HTML span element
 */
function setTypeIndicatorText(text) {
    return `<span class="mapping-indicator-text">${text}</span>`;
}

/**
 * Creates visual indicator for intake field type
 * @param {object} intakeField - Field object with Type property
 * @returns {string} HTML for field type indicator (icon or text)
 */
function setTypeIndicator(intakeField) {
    switch (intakeField.Type) {
        case 'String':
        case 'Phone':
        case 'Date':
        case 'Email':
        case 'Radio':
        case 'Checkbox':
        case 'CheckboxGroup':
        case 'SelectList':
        case 'BCAddress':
        case 'TextArea':
        case 'DataGrid':
            return `<i class="${setTypeIcon(intakeField)}"></i> `;
        case 'Number':
            return setTypeIndicatorText('123');
        case 'Currency':
            return setTypeIndicatorText('$');
        case 'YesNo':
            return setTypeIndicatorText('Y/N');
        default:
            return '';
    }
}

/**
 * Finds the position of element among its siblings
 * @param {Element} el - DOM element
 * @returns {number} Index of element among siblings
 */
function whichChild(el) {
    let i = 0;
    while ((el = el.previousSibling) != null) ++i;
    return i;
}

/**
 * Inserts indentation tabs into output array
 * @param {array} output - Array to push tabs into
 * @param {number} depth - Number of tab levels
 */
function insertTab(output, depth) {
    const TAB = '    ';
    for (let i = 0; i < depth; i++) {
        output.push(TAB);
    }
}

/**
 * Formats JSON with proper indentation and line breaks
 * @param {string} jsonText - JSON string to format
 * @returns {string} Formatted JSON string
 */
function prettyJson(jsonText) {
    if (!jsonText) return jsonText;
    
    const state = { output: [], depth: 0, inQuotes: false, prevChar: '' };

    for (let i = 0; i < jsonText.length; i++) {
        processJsonChar(jsonText.charAt(i), state);
    }

    return state.output.join('');
}

/**
 * Processes a single JSON character for formatting
 * @param {string} char - Character to process
 * @param {object} state - Formatting state (output, depth, inQuotes, prevChar)
 */
function processJsonChar(char, state) {
    if (char === '"' && state.prevChar !== '\\') {
        state.inQuotes = !state.inQuotes;
    }

    if (char === '{') {
        handleOpenBrace(state);
    } else if (char === '}') {
        handleCloseBrace(state);
    } else if (char === ',') {
        handleComma(state);
    } else {
        state.output.push(char);
    }

    state.prevChar = char;
}

/**
 * Handles opening brace in JSON formatting
 * @param {object} state - Formatting state
 */
function handleOpenBrace(state) {
    state.output.push('{');
    if (!state.inQuotes) {
        state.output.push('\n');
        insertTab(state.output, ++state.depth);
    }
}

/**
 * Handles closing brace in JSON formatting
 * @param {object} state - Formatting state
 */
function handleCloseBrace(state) {
    if (!state.inQuotes) {
        state.output.push('\n');
        insertTab(state.output, --state.depth);
    }
    state.output.push('}');
}

/**
 * Handles comma in JSON formatting
 * @param {object} state - Formatting state
 */
function handleComma(state) {
    state.output.push(',');
    if (!state.inQuotes) {
        state.output.push('\n');
        insertTab(state.output, state.depth);
    }
}
