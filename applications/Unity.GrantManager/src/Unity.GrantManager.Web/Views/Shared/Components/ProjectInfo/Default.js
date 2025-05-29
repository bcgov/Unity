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
            this.resetTracking();
        });
    }
}

(function () {
    // Called on widget initialization
    abp.widgets.ProjectInfo = function ($wrapper) {

        let getFilters = function () {
            return {
                applicationId: $wrapper.find('#ProjectInfoViewApplicationId').val()
            };
        }

        let init = function (filters) {
            let $widgetForm = $wrapper.find('form');

            abp.zones = abp.zones || {};
            abp.zones.projectInfo = new UnityZoneForm($widgetForm, {
                saveButtonSelector: '#saveProjectInfoBtn'
            });
        }

        // PubSub Event Handling should be implemented here
        PubSub.subscribe(
            'application_status_changed',
            (msg, data) => {
                let projectInfoWidgets = document.querySelectorAll('[data-widget-name="ProjectInfo"]')
                projectInfoWidgets.forEach(function (entry) {
                    $(entry).data('abp-widget-manager')
                        .refresh();
                });

            }
        );

        return {
            getFilters: getFilters,
            init: init
        };
    };
})();

(function ($) {
    $(function () {
        abp.zones.projectInfo.init();

        // Note: Eventually move this up into UnityZoneForm
        abp.zones.projectInfo.saveButton.on('click', function () {

            let applicationId = document.getElementById('ProjectInfoViewApplicationId').value; 
            let formData = abp.zones.projectInfo.serializeZoneArray();

            let projectInfoObj = {};

            // Process all form fields
            $.each(formData, function (_, input) {
                processFormField(projectInfoObj, input);
            });

            // Add metadata
            projectInfoObj.CorrelationId = $("#ApplicationFormVersionId").val();
            projectInfoObj.WorksheetId = $("#ProjectInfo_WorksheetId").val();

            const customIncludes = new Set(['ApplicantId', 'CorrelationId', 'WorksheetId']);
            // Create filtered object in one functional operation
            let modifiedFieldData = Object.fromEntries(
                Object.entries(projectInfoObj).filter(([key, _]) => {
                    // Check if it's a directly included field
                    if (customIncludes.has(`ProjectInfo.${key}`)) return true;

                    // Check if it's a modified field
                    return abp.zones.projectInfo.modifiedFields.has(`ProjectInfo.${key}`);
                })
            );

            let projectInfoSubmission = {
                modifiedFields: Array.from(abp.zones.projectInfo.modifiedFields).map(field => {
                    const parts = field.split('.');
                    return parts.length > 1 ? parts.slice(1).join('.') : field;
                }),
                data: modifiedFieldData
            };

            try {
                unity.grantManager.grantApplications.grantApplication
                    .updatePartialProjectInfo(applicationId, projectInfoSubmission)
                    .done(function () {
                        abp.notify.success(
                            'The project info has been updated.'
                        );
                        abp.zones.projectInfo.resetTracking();
                        PubSub.publish('project_info_saved', projectInfoObj);
                        PubSub.publish('refresh_detail_panel_summary');
                    });
            }
            catch (error) {
                console.log(error);
                abp.zones.projectInfo.resetTracking();
            }
        });

        function processFormField(projectInfoObj, input) {
            if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                Flex.includeCustomFieldObj(projectInfoObj, input);
                return;
            }

            const fieldName = input.name;
            const inputElement = $(`[name="${fieldName}"]`);
            let fieldValue = input.value;

            if (fieldName.startsWith('ProjectInfo.')) {
                const propertyName = fieldName.split('.')[1];

                if (inputElement.hasClass('unity-currency-input') || inputElement.hasClass('numeric-mask')) {
                    fieldValue = fieldValue.replace(/,/g, '');
                }

                if (isNumberField(input)) {
                    fieldValue = fieldValue === '' ? 0 : Math.min(parseFloat(fieldValue), getMaxNumberField(input));
                } else if (fieldValue === '') {
                    fieldValue = null;
                }

                projectInfoObj[propertyName] = fieldValue;
            } else {
                projectInfoObj[fieldName] = fieldValue;
            }
        }

        function updateProjectInfo(applicationId, projectInfoObj) {
            try {
                unity.grantManager.grantApplications.grantApplication
                    .updateProjectInfo(applicationId, projectInfoObj)
                    .done(function () {
                        abp.notify.success(
                            'The project info has been updated.'
                        );
                        $('#saveProjectInfoBtn').prop('disabled', true);
                        PubSub.publish('project_info_saved', projectInfoObj);
                        PubSub.publish('refresh_detail_panel_summary');
                    });
            }
            catch (error) {
                console.log(error);
                $('#saveProjectInfoBtn').prop('disabled', false);
            }
        }

        function getMaxNumberField(input) {
            const maxCurrency = 10000000000000000000000000000;
            const maxScore = 2147483647;
            if (isCurrencyField(input))
                return maxCurrency;
            else
                return maxScore;
        }

        function isNumberField(input) {
            return isCurrencyField(input) || isPercentageField(input);
        }

        function isCurrencyField(input) {
            const currencyFields =
                ['ProjectInfo.RequestedAmount',
                'ProjectInfo.TotalProjectBudget',
                'ProjectInfo.ProjectFundingTotal'];
            return currencyFields.includes(input.name);
        }

        function isPercentageField(input) {
            return input.name == 'ProjectInfo.PercentageTotalProjectBudget';
        }

        $('#startDate').on('apply.daterangepicker', function (event, picker) {
            console.log(event, picker);
        });


        $('#economicRegions').change(function () {

            const selectedValue = $(this).val();
            let allEconomicRegions = JSON.parse($('#allEconomicRegionList').text());
            let allRegionalDistricts = JSON.parse($('#allRegionalDistrictList').text());
            let selectedEconomicRegion = allEconomicRegions.find(d => d.economicRegionName == selectedValue);
            let childDropdown = initializeDroplist('#regionalDistricts');

            if (selectedValue) {
                let regionalDistricts = allRegionalDistricts.filter(d => d.economicRegionCode == selectedEconomicRegion.economicRegionCode);
                $.each(regionalDistricts, function (index, item) {
                    childDropdown.append($('<option>', {
                        value: item.regionalDistrictName,
                        text: item.regionalDistrictName
                    }));
                });
            }
            $('#regionalDistricts').change();
        });

        $('#regionalDistricts').change(function () {
            const selectedValue = $(this).val();
            let childDropdown = initializeDroplist('#communities');
            if (selectedValue) {
                let allSubdistricts = JSON.parse($('#allRegionalDistrictList').text());
                let allCommunities = JSON.parse($('#allCommunitiesList').text());
                let selectedSubDistrict = allSubdistricts.find(d => d.regionalDistrictName == selectedValue);
                let communities = allCommunities.filter(d => d.regionalDistrictCode == selectedSubDistrict.regionalDistrictCode);

                $.each(communities, function (index, item) {
                    childDropdown.append($('<option>', {
                        value: item.name,
                        text: item.name
                    }));
                });
            }
        });

        function initializeDroplist(dropListId) {
            let initializedDropList = $(dropListId);
            initializedDropList.empty();
            initializedDropList.append($('<option>', {
                value: '',
                text: 'Please choose...'
            }));

            return initializedDropList;
        }

        PubSub.subscribe('application_assessment_results_saved',
            (msg, data) => {
                if (data.RequestedAmount) {
                    $('#RequestedAmountInputPI').prop("value", data.RequestedAmount);
                    $('#RequestedAmountInputPI').maskMoney('mask');
                }
                if (data.TotalProjectBudget) {
                    $('#TotalBudgetInputPI').prop("value", data.TotalProjectBudget);
                    $('#TotalBudgetInputPI').maskMoney('mask');
                }
            }
        );

        PubSub.subscribe(
            'fields_projectinfo',
            () => {
                enableProjectInfoSaveBtn();
            }
        );

    
    });
})(jQuery);


function enableProjectInfoSaveBtn(inputText) {
    if (!$("#projectInfoForm").valid() || formHasInvalidCurrencyCustomFields("projectInfoForm")) {
        $('#saveProjectInfoBtn').prop('disabled', true);
        return;
    }

    if (abp.auth.isGranted('GrantApplicationManagement.ProjectInfo.Update')) {
        $('#saveProjectInfoBtn').prop('disabled', false);
    }
}

function calculatePercentage() {
    const requestedAmount = parseFloat(document.getElementById("RequestedAmountInputPI")?.value.replace(/,/g, ''));
    const totalProjectBudget = parseFloat(document.getElementById("TotalBudgetInputPI")?.value.replace(/,/g, ''));
    if (isNaN(requestedAmount) || isNaN(totalProjectBudget) || totalProjectBudget == 0) {
        document.getElementById("ProjectInfo_PercentageTotalProjectBudget").value = 0;
        return;
    }
    const percentage = ((requestedAmount / totalProjectBudget) * 100.00).toFixed(2);
    $("#ProjectInfo_PercentageTotalProjectBudget").maskMoney('mask', parseFloat(percentage));
   
}
