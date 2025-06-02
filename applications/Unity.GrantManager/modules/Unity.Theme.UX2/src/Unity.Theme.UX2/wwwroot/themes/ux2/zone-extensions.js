(function ($) {
    if (!$) {
        return;
    }

    /**
     * @public
     * Handles zone fieldset serialization with DTO nesting
     * @param {any} includeDisabled
     * @param {any} camelCase
     * @returns
     */
    $.fn.serializeZoneFieldsets = function (camelCase = true, includeDisabledFields = false, nested = false) {
        let $form = $(this);
        // OPTIONS NOTE: Zones to exclude
        // OPTIONS NOTE: Zones to include
        // OPTIONS NOTE: Properties to include


        // Initialize result object
        const resultObject = {};
        // Collection phase: Gather all field data in a single pass
        const data = [];

        // 1. Get standard form fields
        Array.prototype.push.apply(data, $form.serializeArray());

        // 2. Add unchecked checkboxes (serializeArray ignores them)
        $form.find("input[type=checkbox]").each(function () {
            if (!$(this).is(':checked')) {
                data.push({ name: this.name, value: this.checked });
            }
        });

        // TODO-TOGGLE
        // 3. Add disabled fields (also ignored by serializeArray)
        if (includeDisabledFields) {
            $form.find(':disabled[name]').each(function () {
                const value = $(this).is(":checkbox") ?
                    $(this).is(':checked') :
                    $(this).val();

                data.push({ name: this.name, value: value });
            });
        }


        // Convert field names to camelCase if required
        if (camelCase) {
            data.forEach(item => item.name = toCamelCaseInternal(item.name));
        }

        if (nested) {
            // Transformation phase: Convert flat data to nested object structure
            data.forEach(field => {
                const nameParts = field.name.split('.');
                let current = resultObject;

                // Navigate through the object hierarchy
                for (let i = 0; i < nameParts.length - 1; i++) {
                    const part = nameParts[i];
                    if (!current[part]) {
                        current[part] = {};
                    }
                    current = current[part];
                }

                // Set the final property value
                const lastPart = nameParts[nameParts.length - 1];
                if (!current[lastPart] || Object.keys(current[lastPart]).length === 0) {
                    current[lastPart] = field.value;
                }
            });

            return resultObject;
        } else {
            return data;
        }
    };

    /**
     * Converts a string to camelCase
     * @param {string} str
     * @returns
     */
    let toCamelCaseInternal = function (str) {
        let regexs = [
            /(^[A-Z])/, // first char of string
            /((\.)[A-Z])/ // first char after a dot (.)
        ];

        regexs.forEach(
            function (regex) {
                let infLoopAvoider = 0;

                while (regex.test(str)) {
                    str = str
                        .replace(regex, function ($1) { return $1.toLowerCase(); });

                    if (infLoopAvoider++ > 1000) {
                        break;
                    }
                }
            }
        );

        return str;
    }

    /**
     * @public
     * Report fields and feildsets for the initialized component
     */
    $.fn.reportZones = function () {
        let $form = $(this);
        let tableData = [];

        $form.find('fieldset').each(function () {
            const fieldName = $(this).attr('name');

            $(this).find(':input').each(function () {
                tableData.push({
                    'fieldsetName': fieldName,
                    'id': this.id,
                    'name': this.name || '(no name)',
                    'type': this.type,
                    'tag': this.tagName.toLowerCase(),
                    'value': this.value || '(no value)'
                });
            });
        });

        console.table(tableData);
    }
})(jQuery);

class UnityChangeTrackingForm {
    constructor($form, options = {}) {

        this.options = {
            modifiedClass: 'bg-primary-subtle',
            saveButtonSelector: options.saveButtonSelector || '#saveButton', // TODO: Set default
            ...options
        };


        // Expect options.form to be a jQuery object representing the form element
        if (!$form || !($form instanceof $) || !$form.is('form')) {
            throw new Error('UnityChangeTrackingForm requires a jQuery form object in options.form');
        }

        this.form = $form;
        this.modifiedFields = new Set();
        this.originalValues = {};
        this.saveButton = $(this.options.saveButtonSelector);
        console.log('UnityChangeTrackingForm initialized');
    }

    init() {
        this.captureInitialValues();
        this.addChangeHandler();
        this.saveButton.prop('disabled', true);
    }

    captureInitialValues() {
        this.form.find('input, select, textarea').each((_, element) => {
            const $el = $(element);
            const name = $el.attr('name');

            if (name) {
                // Handle different input types
                if ($el.is(':checkbox')) {
                    this.originalValues[name] = $el.prop('checked');
                } else if ($el.is(':radio')) {
                    if ($el.prop('checked')) {
                        this.originalValues[name] = $el.val();
                    }
                } else {
                    this.originalValues[name] = $el.val();
                }
            }
        });
    }

    addChangeHandler() {
        this.form.find('input, select, textarea').on('change', (e) => {
            const $el = $(e.target);
            const name = $el.attr('name');

            if (name) {
                this.checkFieldModified($el, name);
            }
        });
    }

    checkFieldModified($element, name) {
        let currentValue;

        if ($element.is(':checkbox')) {
            currentValue = $element.prop('checked');
        } else if ($element.is(':radio')) {
            if ($element.prop('checked')) {
                currentValue = $element.val();
            } else {
                return; // Skip radio buttons that aren't checked
            }
        } else {
            currentValue = $element.val();
        }

        const originalValue = this.originalValues[name];

        if (currentValue !== originalValue) {
            this.markAsModified($element, name);
        } else {
            this.markAsUnmodified($element, name);
        }

        this.updateSaveButtonState();
    }

    markAsModified($element, name) {
        this.modifiedFields.add(name);
        $element.addClass(this.options.modifiedClass);
        $element.attr('data-field-modified', 'true');
        $element.removeAttr('data-field-modified');
    }

    markAsUnmodified($element, name) {
        this.modifiedFields.delete(name);
        $element.removeClass(this.options.modifiedClass);
    }

    updateSaveButtonState() {
        // TODO: Include validation check if needed
        this.saveButton.prop('disabled', this.modifiedFields.size === 0);
    }

    /**
     * Reset tracking without changing values
     */
    resetTracking() {
        this.modifiedFields.clear();
        this.form.find('input, select, textarea').each((_, element) => {
            const $el = $(element);
            $el.removeClass(this.options.modifiedClass);
            $el.removeAttr('data-field-modified');
        });
        this.saveButton.prop('disabled', true);
        this.captureInitialValues();
    }

    /**
     * Reset form to original values and clear tracking
     */
    resetForm() {
        // Reset each field to its original value
        this.form.find('input, select, textarea').each((_, element) => {
            const $el = $(element);
            const name = $el.attr('name');

            if (name && this.originalValues.hasOwnProperty(name)) {
                // Handle different input types
                if ($el.is(':checkbox')) {
                    $el.prop('checked', this.originalValues[name]);
                } else if ($el.is(':radio')) {
                    if ($el.val() === this.originalValues[name]) {
                        $el.prop('checked', true);
                    }
                } else {
                    $el.val(this.originalValues[name]);

                    // Re-apply any special formatting (like currency masks)
                    if ($el.hasClass('unity-currency-input') ||
                        $el.hasClass('numeric-mask') ||
                        $el.hasClass('percentage-mask')) {
                        $el.maskMoney('mask', this.originalValues[name]);
                    }
                }

                // Remove modification styling
                $el.removeClass(this.options.modifiedClass);
                $el.removeAttr('data-field-modified');
            }
        });

        // Clear tracking
        this.modifiedFields.clear();
        this.saveButton.prop('disabled', true);

        // Trigger change events for any dependent calculations
        this.form.find('.numeric-mask, .unity-currency-input').trigger('change');
    }

    serializeZoneObject(camelCase = true, includeDisabledFields = false) {
        return this.form.serializeZoneFieldsets(camelCase, includeDisabledFields);
    }

    serializeZoneArray(camelCase = false, includeDisabledFields = false, nested = false) {
        return this.form.serializeZoneFieldsets(camelCase, includeDisabledFields, nested);
    }
}

class UnityZoneForm extends UnityChangeTrackingForm {
    constructor($form, options = {}) {
        super($form, options);
        this.options = {
            ...this.options,
            ...options
        };

        console.log('UnityZoneForm initialized');
    }

    init() {
        super.init();
        this.form.on('reset', () => this.resetTracking());
        this.initializeNumericFields();
        this.addSubmitHandler();
    }

    initializeNumericFields() {
        $('.numeric-mask').maskMoney({ precision: 0 });
        $('.percentage-mask').maskMoney();

        $('.numeric-mask').each(function () {
            $(this).maskMoney('mask', this.value);
        });

        $('.percentage-mask').each(function () {
            $(this).maskMoney('mask', this.value);
        });

        $('.remove-leading-zeros').on('input', function () {
            let inputValue = $(this).val();
            let newValue = inputValue.replace(/^0+(?!$)/, '');
            $(this).val(newValue);
        });

        $('.unity-currency-input').maskMoney();
    }

    addSubmitHandler() {
        this.form.on('submit', (e) => {
            e.preventDefault();
            debugger;
            this.resetTracking();
        });
    }
}