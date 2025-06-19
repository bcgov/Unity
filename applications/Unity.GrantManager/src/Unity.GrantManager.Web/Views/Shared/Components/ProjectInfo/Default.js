// Called on widget initialization
abp.widgets.ProjectInfo = function ($wrapper) {
    let widgetApi = {
        getFilters: function () {
            return {
                applicationId: $wrapper.find('#ProjectInfo_ApplicationId').val(),
                applicationFormVersionId: $wrapper.find("#ProjectInfo_ApplicationFormVersionId").val()
            };
        },
        init: function (filters) {
            let $widgetForm = $wrapper.find('form');

            // Create a new form instance and store it on the widget API
            this.form = new UnityZoneForm($widgetForm, {
                saveButtonSelector: '#saveProjectInfoBtn'
            });

            this.form.init();

            // Set up additional event handlers here
            this.setupEventHandlers();
        },
        setupEventHandlers: function() {
            const self = this;

            // Save button handler
            self.form.saveButton.on('click', function () {
                let applicationId = document.getElementById('ProjectInfo_ApplicationId').value; 
                let formData = self.form.serializeZoneArray();
                
                let projectInfoObj = {};
                
                // Process all form fields
                $.each(formData, function (_, input) {
                    self.processFormField(projectInfoObj, input);
                });

                const customIncludes = new Set();

                if (typeof Flex === 'function' && Object.keys(projectInfoObj.CustomFields || {}).length > 0) {
                    // Add Worksheet Metadata and filter conditions
                    projectInfoObj.CorrelationId = $("#ProjectInfo_ApplicationFormVersionId").val();
                    projectInfoObj.WorksheetId = $("#ProjectInfo_WorksheetId").val();

                    // Normalize checkboxes to string for custom worksheets
                    $(`#Unity_GrantManager_ApplicationManagement_Project_Worksheet input:checkbox`).each(function () {
                        projectInfoObj.CustomFields[this.name] = (this.checked).toString();
                    });

                    customIncludes
                        .add('CustomFields')
                        .add('CorrelationId')
                        .add('WorksheetId');
                }
                
                // Create filtered object in one functional operation
                let modifiedFieldData = Object.fromEntries(
                    Object.entries(projectInfoObj).filter(([key, _]) => {
                        // Check if it's a directly included widget field
                        if (customIncludes.has(key)) return true;
                        
                        // Check if it's a modified widget field
                        return self.form.modifiedFields.has(`ProjectInfo.${key}`);
                    })
                );

                let projectInfoSubmission = {
                    modifiedFields: Array.from(self.form.modifiedFields).map(field => {
                        const parts = field.split('.');
                        return parts.length > 1 ? parts.slice(1).join('.') : field;
                    }),
                    data: modifiedFieldData
                };
                
                try {
                    unity.grantManager.grantApplications.grantApplication
                        .updatePartialProjectInfo(applicationId, projectInfoSubmission)
                        .done(function () {
                            abp.notify.success('The project info has been updated.');
                            self.form.resetTracking();
                            PubSub.publish('project_info_saved', projectInfoObj);
                            PubSub.publish('refresh_detail_panel_summary');
                        });
                }
                catch (error) {
                    console.log(error);
                    self.form.resetTracking();
                }
            });

            // Date picker initialization
            $('#startDate').on('apply.daterangepicker', function (event, picker) {
                console.log(event, picker);
            });

            // Location dropdowns handling
            $('#economicRegions').change(function () {
                const selectedValue = $(this).val();
                let allEconomicRegions = JSON.parse($('#allEconomicRegionList').text());
                let allRegionalDistricts = JSON.parse($('#allRegionalDistrictList').text());
                let selectedEconomicRegion = allEconomicRegions.find(d => d.economicRegionName == selectedValue);
                let childDropdown = self.initializeDroplist('#regionalDistricts');
                
                if (selectedValue) {
                    let regionalDistricts = allRegionalDistricts.filter(d => 
                        d.economicRegionCode == selectedEconomicRegion.economicRegionCode);
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
                let childDropdown = self.initializeDroplist('#communities');
                if (selectedValue) {
                    let allSubdistricts = JSON.parse($('#allRegionalDistrictList').text());
                    let allCommunities = JSON.parse($('#allCommunitiesList').text());
                    let selectedSubDistrict = allSubdistricts.find(d => d.regionalDistrictName == selectedValue);
                    let communities = allCommunities.filter(d => 
                        d.regionalDistrictCode == selectedSubDistrict.regionalDistrictCode);
                    
                    $.each(communities, function (index, item) {
                        childDropdown.append($('<option>', {
                            value: item.name,
                            text: item.name
                        }));
                    });
                }
            });

            // PubSub subscriptions
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
                    calculatePercentage();
                }
            );
            
            PubSub.subscribe('fields_projectinfo', () => {
                self.enableProjectInfoSaveBtn();
            });

            // PubSub Event Handling should be implemented here
            PubSub.subscribe(
                'application_status_changed',
                (msg, data) => {
                    let projectInfoWidgets = document.querySelectorAll('[data-widget-name="ProjectInfo"]')
                    projectInfoWidgets.forEach(refreshWidget);
                }
            );

            function refreshWidget(widget) {
                const widgetManager = $(widget).data('abp-widget-manager');
                if (!widgetManager) {
                    console.warn('Widget manager not found for ProjectInfo widget');
                    return;
                }

                const refreshResult = widgetManager.refresh();

                // Check if refresh returns a promise
                if (refreshResult && typeof refreshResult.then === 'function') {
                    refreshResult.then(updateGlobalReference);
                } else {
                    // If no promise is returned, update reference directly
                    updateGlobalReference();
                }
            }

            function updateGlobalReference() {
                // After refresh, the widget will automatically call init()
                abp.zones = abp.zones || {};
                abp.zones.projectInfo = $('[data-widget-name="ProjectInfo"]').data('abp-widget-api') || null;
            }

            calculatePercentage();
        },
        // Helper methods
        processFormField: function(projectInfoObj, input) {
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
                
                if (this.isNumberField(input)) {
                    fieldValue = fieldValue === '' ? 0 : Math.min(parseFloat(fieldValue), this.getMaxNumberField(input));
                } else if (fieldValue === '') {
                    fieldValue = null;
                }
                
                projectInfoObj[propertyName] = fieldValue;
            } else {
                projectInfoObj[fieldName] = fieldValue;
            }
        },

        initializeDroplist: function(dropListId) {
            let initializedDropList = $(dropListId);
            initializedDropList.empty();
            initializedDropList.append($('<option>', {
                value: '',
                text: 'Please choose...'
            }));
            
            return initializedDropList;
        },

        enableProjectInfoSaveBtn: function() {
            if (!$("#projectInfoForm").valid() || formHasInvalidCurrencyCustomFields("projectInfoForm")) {
                $('#saveProjectInfoBtn').prop('disabled', true);
                return;
            }
            
            if (abp.auth.isGranted('GrantApplicationManagement.ProjectInfo.Update')) {
                $('#saveProjectInfoBtn').prop('disabled', false);
            }
        },

        isNumberField: function(input) {
            return this.isCurrencyField(input) || this.isPercentageField(input);
        },

        isCurrencyField: function(input) {
            const currencyFields = [
                'ProjectInfo.RequestedAmount',
                'ProjectInfo.TotalProjectBudget',
                'ProjectInfo.ProjectFundingTotal'
            ];
            return currencyFields.includes(input.name);
        },

        isPercentageField: function(input) {
            return input.name == 'ProjectInfo.PercentageTotalProjectBudget';
        },

        getMaxNumberField: function(input) {
            const maxCurrency = 10000000000000000000000000000;
            const maxScore = 2147483647;
            if (this.isCurrencyField(input))
                return maxCurrency;
            else
                return maxScore;
        }


    };

    return widgetApi;
};

$(function () {
    // Initialize widget through ABP's widget system instead of global object
    abp.zones = abp.zones || {};
    abp.zones.projectInfo = $('[data-widget-name="ProjectInfo"]').data('abp-widget-api') || null;
});

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
