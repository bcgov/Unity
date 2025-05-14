/**
 * UnityFormComponent
 * Base class for Unity Grant Manager UI form components with common zone functionality
 */
class UnityFormComponent {
    /**
     * @param {Object} options - Configuration options for the component
     * @param {Object} options.selectors - DOM element selectors
     * @param {string} options.selectors.form - Main form selector
     * @param {string} options.selectors.saveButton - Save button selector
     * @param {string} options.selectors.applicationIdField - Application ID field selector
     * @param {string} options.selectors.formVersionIdField - Form version ID field selector
     * @param {string} options.selectors.worksheetIdField - Worksheet ID field selector
     * @param {string} options.componentName - Name of the component for logging and messages
     * @param {Object} options.fieldMappings - Custom field mappings for serialization
     * @param {string} options.permissionName - Permission required to save the component
     * @param {string} options.apiEndpoint - API endpoint for saving the component data
     * @param {Object} options.fieldTypes - Field type definitions for special handling
     */
    constructor(options) {
        this.selectors = options.selectors || {};
        this.componentName = options.componentName || 'Component';
        this.eventSubscriptions = [];
        this.fieldMappings = options.fieldMappings || {};
        this.permissionName = options.permissionName || '';
        this.apiEndpoint = options.apiEndpoint || '';

        // Default field type configurations
        this.fieldTypes = {
            currency: {
                fields: [],
                maxValue: 10000000000000000000000000000,
                formatter: (value) => value.replace(/,/g, '')
            },
            percentage: {
                fields: [],
                maxValue: 100,
                formatter: (value) => value.replace(/,/g, '')
            },
            number: {
                fields: [],
                maxValue: 2147483647,
                formatter: (value) => value
            },
            date: {
                fields: [],
                validator: (value) => true
            },
            ...options.fieldTypes
        };

        // Component state
        this.isFormDirty = false;
        this.invalidFields = new Set();
        this.originalValues = {};
    }

    /**
     * Initialize the component
     */
    init() {
        this.captureOriginalValues();
        this.initializeSaveButtonHandler();
        this.initializeFieldChangeHandlers();
        this.initializePubSubEvents();
        this.initializeInputs();
        this.initializeDropdowns();
        this.initializeCustomElements();
        this.initializeFormValidation();

        // Disable save button initially
        this.disableSaveButton();
    }

    /**
     * Initialize all input types
     */
    initializeInputs() {
        $('.unity-currency-input').maskMoney();

        $('.numeric-mask').maskMoney({ precision: 0 }).each(function () {
            $(this).maskMoney('mask', this.value);
        });

        $('.percentage-mask').maskMoney().each(function () {
            $(this).maskMoney('mask', this.value);
        });
    }

    /**
     * Capture original form values for change detection
     */
    captureOriginalValues() {
        if (!this.selectors.form) return;

        const form = $(this.selectors.form);
        const inputs = form.find('input, select, textarea');

        this.originalValues = {};
        inputs.each((_, element) => {
            const $el = $(element);
            const name = $el.attr('name');
            if (name) {
                this.originalValues[name] = $el.val();
            }
        });
    }

    /**
     * Initialize the save button handler
     */
    initializeSaveButtonHandler() {
        if (this.selectors.saveButton) {
            $('body').on('click', this.selectors.saveButton, (e) => {
                e.preventDefault();
                this.handleSaveButtonClick();
            });
        }
    }

    /**
     * Initialize handlers for field changes
     */
    initializeFieldChangeHandlers() {
        if (!this.selectors.form) return;

        const form = $(this.selectors.form);

        // Handle standard field change events
        form.on('change input', 'input, select, textarea', (e) => {
            this.handleFieldChange(e);
        });

        // Handle fields with data-enable-save attribute (for backward compatibility)
        form.on('change input', '[data-enable-save="true"]', (e) => {
            this.handleEnableSaveField(e);
        });
    }

    /**
     * Handle field change events
     */
    handleFieldChange(e) {
        const $field = $(e.target);
        const name = $field.attr('name');

        // Skip processing for fields without names
        if (!name) return;

        const newValue = $field.val();

        // Mark form as dirty if value changed
        if (this.originalValues[name] !== newValue) {
            this.isFormDirty = true;
        }

        // Validate the field
        this.validateField($field);

        // Update save button state
        this.updateSaveButtonState();
    }

    /**
     * Handle field changes specifically for enabling save button
     * This provides backward compatibility with the previous inline handlers
     */
    handleEnableSaveField(e) {
        // Mark form as dirty to enable save button
        this.isFormDirty = true;

        // Validate the field
        this.validateField($(e.target));

        // Update save button state
        this.updateSaveButtonState();

        // Call component-specific handler if defined
        if (typeof this.onFieldChange === 'function') {
            this.onFieldChange(e);
        }
    }

    /**
     * Validate a field and update the invalid fields set
     */
    validateField($field) {
        const name = $field.attr('name');
        const fieldId = $field.attr('id');
        const identifier = name || fieldId;

        if (!identifier) return;

        const isValid = $field[0].validity.valid;

        if (!isValid) {
            this.invalidFields.add(identifier);
            return;
        }

        // Custom validation for special field types
        if ($field.hasClass('unity-currency-input') && !this.isValidCurrency($field.val())) {
            this.invalidFields.add(identifier);
        } else if ($field.hasClass('percentage-mask') && !this.isValidPercentage($field.val())) {
            this.invalidFields.add(identifier);
        } else {
            this.invalidFields.delete(identifier);
        }
    }

    /**
     * Validate all form fields
     */
    validateForm() {
        if (!this.selectors.form) return true;

        const form = $(this.selectors.form)[0];

        // Update invalid fields collection for all inputs
        $(this.selectors.form).find('input, select, textarea').each((_, el) => {
            this.validateField($(el));
        });

        // Check HTML5 validation
        const isFormValid = form.checkValidity();

        // Check custom validations
        const hasInvalidCustomFields = this.hasInvalidCustomFields();

        return isFormValid && !hasInvalidCustomFields && this.invalidFields.size === 0;
    }

    /**
     * Handle the save button click event
     */
    async handleSaveButtonClick() {
        try {
            // Disable the save button to prevent multiple submissions
            this.disableSaveButton();

            // Validate the form before submission
            if (!this.validateForm()) {
                this.enableSaveButtonIfValid();
                return;
            }

            const applicationId = $(this.selectors.applicationIdField).val();
            const formData = $(this.selectors.form).serializeArray();
            const formVersionId = $(this.selectors.formVersionIdField).val();
            const worksheetId = $(this.selectors.worksheetIdField).val();

            // Build the data object with custom field handling
            const dataObject = this.buildDataObject(formData);

            // Add additional standard fields
            dataObject.correlationId = formVersionId;
            dataObject.worksheetId = worksheetId;

            // Call any pre-save hooks
            await this.beforeSave(applicationId, dataObject);

            // Save the data
            await this.saveData(applicationId, dataObject);

            // Update UI and state
            this.disableSaveButton();
            this.isFormDirty = false;
            this.captureOriginalValues();

            // Call post-save hooks
            this.afterSave(dataObject);

            // Show success notification
            this.showSuccessNotification();
        } catch (error) {
            console.error(`Error saving ${this.componentName} data:`, error);
            this.enableSaveButtonIfValid();
            this.showErrorNotification(error);
        }
    }

    /**
     * Pre-save hook for component-specific logic
     * @param {string} applicationId - The application ID
     * @param {Object} data - The data to save
     */
    async beforeSave(applicationId, data) {
        // Optional override in subclass
        return Promise.resolve();
    }

    /**
     * Build data object from form data
     * @param {Array} formData - Serialized form data
     * @returns {Object} Prepared data object for saving
     */
    buildDataObject(formData) {
        const dataObject = {};

        // Process serialized form fields
        for (const input of formData) {
            // Handle Flex custom fields
            if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                Flex.includeCustomFieldObj(dataObject, input);
                continue;
            }

            const value = input.value;
            const isEmpty = value === '';

            // Handle field name patterns like "FieldGroup.FieldName"
            if (input.name.includes('.')) {
                const fieldName = input.name.split(".")[1];

                // Process field by type
                if (this.isCurrencyField(input.name)) {
                    dataObject[fieldName] = this.processCurrencyField(value);
                } else if (this.isPercentageField(input.name)) {
                    dataObject[fieldName] = this.processPercentageField(value);
                } else if (this.isNumberField(input.name)) {
                    dataObject[fieldName] = this.processNumberField(value);
                } else {
                    dataObject[fieldName] = isEmpty ? null : value;
                }
            } else {
                // Fields without dot notation
                dataObject[input.name] = isEmpty ? null : value;
            }
        }

        // Update checkboxes which aren't serialized if unchecked
        $(`${this.selectors.form} input:checkbox`).each(function () {
            dataObject[this.name] = this.checked.toString();
        });

        // Make sure all the custom fields are set
        if (typeof Flex === 'function') {
            Flex?.setCustomFields(dataObject);
        }

        return dataObject;
    }

    /**
     * Process a currency field value
     */
    processCurrencyField(value) {
        if (!value) return 0;

        const numericValue = value.replace(/,/g, '');
        const parsedValue = parseFloat(numericValue);
        const maxValue = this.fieldTypes.currency.maxValue;

        return isNaN(parsedValue) ? 0 : Math.min(parsedValue, maxValue);
    }

    /**
     * Process a percentage field value
     */
    processPercentageField(value) {
        if (!value) return 0;

        const numericValue = value.replace(/,/g, '');
        const parsedValue = parseFloat(numericValue);
        const maxValue = this.fieldTypes.percentage.maxValue;

        return isNaN(parsedValue) ? 0 : Math.min(parsedValue, maxValue);
    }

    /**
     * Process a number field value
     */
    processNumberField(value) {
        if (!value) return 0;

        const parsedValue = parseInt(value, 10);
        const maxValue = this.fieldTypes.number.maxValue;

        return isNaN(parsedValue) ? 0 : Math.min(parsedValue, maxValue);
    }

    /**
     * Check if a field is a currency field
     */
    isCurrencyField(fieldName) {
        return this.fieldTypes.currency.fields.includes(fieldName);
    }

    /**
     * Check if a field is a percentage field
     */
    isPercentageField(fieldName) {
        return this.fieldTypes.percentage.fields.includes(fieldName);
    }

    /**
     * Check if a field is a number field
     */
    isNumberField(fieldName) {
        return this.isCurrencyField(fieldName) ||
            this.isPercentageField(fieldName) ||
            this.fieldTypes.number.fields.includes(fieldName);
    }

    /**
     * Validate a currency value
     */
    isValidCurrency(value) {
        if (!value) return true;
        return /^(?:\d{1,3}(?:,\d{3})*|\d+)(?:\.\d{1,2})?$/.test(value);
    }

    /**
     * Validate a percentage value
     */
    isValidPercentage(value) {
        if (!value) return true;
        const numericValue = value.replace(/,/g, '');
        const parsedValue = parseFloat(numericValue);
        return !isNaN(parsedValue) && parsedValue >= 0 && parsedValue <= 100;
    }

    /**
     * Save data to the server
     * @param {string} applicationId - The application ID
     * @param {Object} data - The data to save
     */
    async saveData(applicationId, data) {
        if (!this.apiEndpoint) {
            throw new Error('apiEndpoint not defined in component');
        }

        return new Promise((resolve, reject) => {
            const apiMethod = unity.grantManager.grantApplications.grantApplication[this.apiEndpoint];

            if (!apiMethod) {
                reject(new Error(`API endpoint "${this.apiEndpoint}" not found`));
                return;
            }

            apiMethod(applicationId, data)
                .done(() => resolve())
                .fail((error) => reject(error));
        });
    }

    /**
     * Actions to perform after successful save
     * @param {Object} data - The saved data
     */
    afterSave(data) {
        // Publish common refresh event
        PubSub.publish("refresh_detail_panel_summary");

        // Publish component-specific event
        PubSub.publish(`${this.componentName.toLowerCase()}_saved`, data);
    }

    /**
     * Show success notification
     */
    showSuccessNotification() {
        abp.notify.success(`The ${this.componentName} has been updated.`);
    }

    /**
     * Show error notification
     */
    showErrorNotification(error) {
        console.error(error);
        abp.notify.error(`Error saving ${this.componentName}.`);
    }

    /**
     * Initialize PubSub event subscriptions
     */
    initializePubSubEvents() {
        this.subscribe(`fields_${this.componentName.toLowerCase()}`, () => {
            this.enableSaveButtonIfValid();
        });
    }

    /**
     * Subscribe to a PubSub event and track it for cleanup
     * @param {string} event - Event name
     * @param {Function} callback - Event callback
     */
    subscribe(event, callback) {
        const token = PubSub.subscribe(event, callback);
        this.eventSubscriptions.push(token);
        return token;
    }

    /**
     * Initialize dropdowns and their dependencies
     */
    initializeDropdowns() {
        // Override in subclass for component-specific dropdown initialization
    }

    /**
     * Initialize form validation
     */
    initializeFormValidation() {
        if (!this.selectors.form) return;

        // Validate fields on load
        $(this.selectors.form).find('input, select, textarea').each((_, element) => {
            this.validateField($(element));
        });
    }

    /**
     * Initialize custom elements specific to the component
     */
    initializeCustomElements() {
        // Override in subclass for component-specific initialization
    }

    /**
     * Update save button state based on form validity and changes
     */
    updateSaveButtonState() {
        if (this.isFormDirty && this.invalidFields.size === 0 && this.hasPermissionToSave()) {
            this.enableSaveButton();
        } else {
            this.disableSaveButton();
        }
    }

    /**
     * Enable the save button
     */
    enableSaveButton() {
        $(this.selectors.saveButton).prop('disabled', false);
    }

    /**
     * Enable save button only if form is valid
     */
    enableSaveButtonIfValid() {
        if (this.validateForm() && this.hasPermissionToSave()) {
            this.enableSaveButton();
        } else {
            this.disableSaveButton();
        }
    }

    /**
     * Disable the save button
     */
    disableSaveButton() {
        $(this.selectors.saveButton).prop('disabled', true);
    }

    /**
     * Check if the user has permission to save
     * @returns {boolean} True if the user has permission
     */
    hasPermissionToSave() {
        return !this.permissionName || abp.auth.isGranted(this.permissionName);
    }

    /**
     * Check if the form has invalid custom fields
     * @returns {boolean} True if there are invalid custom fields
     */
    hasInvalidCustomFields() {
        let invalidFieldsFound = false;
        $(`${this.selectors.form} input[id^='custom']:visible`).each((i, el) => {
            const $field = $(el);
            if ($field.hasClass('custom-currency-input')) {
                if (!isValidCurrencyCustomField($field)) {
                    invalidFieldsFound = true;
                }
            } else {
                const fieldValidity = el.validity.valid;
                if (!fieldValidity) {
                    invalidFieldsFound = true;
                }
            }
        });
        return invalidFieldsFound;
    }

    /**
     * Format a dropdown list from a data array
     * @param {Array} dataArray - Array of data objects
     * @param {string} valueField - Field to use as option value
     * @param {string} textField - Field to use as option text
     * @returns {Array} Array of {value, text} objects for dropdowns
     */
    formatDropdownOptions(dataArray, valueField, textField) {
        return dataArray.map(item => ({
            value: item[valueField],
            text: item[textField]
        }));
    }

    /**
     * Initialize a dropdown with options
     * @param {string} selector - Dropdown selector
     * @param {Array} options - Options array with value and text properties
     * @param {string} defaultText - Default option text
     */
    populateDropdown(selector, options, defaultText = 'Please choose...') {
        const dropdown = $(selector);
        dropdown.empty();

        // Add default option
        dropdown.append($('<option>', {
            value: '',
            text: defaultText
        }));

        // Add options
        options.forEach(option => {
            dropdown.append($('<option>', {
                value: option.value,
                text: option.text
            }));
        });
    }

    /**
     * Handle dependent dropdowns (parent affects child options)
     * @param {string} parentSelector - Parent dropdown selector
     * @param {string} childSelector - Child dropdown selector
     * @param {Function} getChildOptions - Function that returns child options based on parent value
     */
    setupDependentDropdowns(parentSelector, childSelector, getChildOptions) {
        $(parentSelector).on('change', () => {
            const parentValue = $(parentSelector).val();
            const childOptions = getChildOptions(parentValue);
            this.populateDropdown(childSelector, childOptions);
        });
    }

    /**
     * Clean up resources when the component is destroyed
     */
    destroy() {
        // Unsubscribe from all PubSub events
        this.eventSubscriptions.forEach(token => PubSub.unsubscribe(token));

        // Remove event handlers
        if (this.selectors.saveButton) {
            $('body').off('click', this.selectors.saveButton);
        }

        if (this.selectors.form) {
            $(this.selectors.form).off('change input', 'input, select, textarea');
        }
    }
}

/**
 * Factory function to create component instances
 * @param {string} componentType - Type of component to create
 * @param {Object} options - Component configuration options
 * @returns {UnityFormComponent} Component instance
 */
function createComponent(componentType, options = {}) {
    // Import component classes dynamically if needed
    const componentMap = {
        'ProjectInfo': ProjectInfoComponent,
        'ApplicantInfo': ApplicantInfoComponent,
        'AssessmentResults': AssessmentResultsComponent,
        'FundingAgreementInfo': FundingAgreementInfoComponent
    };

    const ComponentClass = componentMap[componentType];
    if (!ComponentClass) {
        throw new Error(`Unknown component type: ${componentType}`);
    }

    return new ComponentClass(options);
}
