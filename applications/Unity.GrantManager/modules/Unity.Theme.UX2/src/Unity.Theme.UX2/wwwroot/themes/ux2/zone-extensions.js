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