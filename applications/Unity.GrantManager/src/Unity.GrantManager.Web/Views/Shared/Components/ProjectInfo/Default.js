// Called on widget initialization
abp.widgets.ProjectInfo = function ($wrapper) {
    let widgetApi = {
        getFilters: function () {
            return {
                applicationId: $wrapper.find('#ProjectInfoViewApplicationId').val()
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
                let applicationId = document.getElementById('ProjectInfoViewApplicationId').value; 
                let formData = self.form.serializeZoneArray();
                
                let projectInfoObj = {};
                
                // Process all form fields
                $.each(formData, function (_, input) {
                    self.processFormField(projectInfoObj, input);
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
                    projectInfoWidgets.forEach(function (entry) {
                        $(entry).data('abp-widget-manager')
                            .refresh()
                            .then(() => {
                                // After refresh, the widget will automatically call init()
                                // Update the global reference if you're using it
                                abp.zones = abp.zones || {};
                                abp.zones.projectInfo = $(entry).data('abp-widget-api')?.form || null;
                            });
                    });
                }
            );

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
    abp.zones.projectInfo = $('[data-widget-name="ProjectInfo"]')
        .data('abp-widget-api')?.form || null;
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
