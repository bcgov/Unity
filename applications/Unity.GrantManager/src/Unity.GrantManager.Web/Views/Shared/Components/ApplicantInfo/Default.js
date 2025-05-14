/**
 * ApplicantInfoComponent
 * Manages applicant information form interactions for the Unity Grant Manager
 */
class ApplicantInfoComponent extends UnityFormComponent {
    constructor() {
        // Define selectors
        const selectors = {
            form: '#ApplicantInfoForm',
            saveButton: '#saveApplicantInfoBtn',
            unityAppIdField: '#applicantInfoUnityApplicantId',
            spinner: '.cas-spinner',
            applicationIdField: '#ApplicantInfoViewApplicationId',
            formVersionIdField: '#ApplicationFormVersionId',
            worksheetIdField: '#ApplicantInfo_WorksheetId',
            orgNameField: '#ApplicantInfo_OrgName',
            orgNumberField: '#ApplicantInfo_OrgNumber',
            orgStatusField: '#orgBookStatusDropdown',
            orgTypeField: '#orgTypeDropdown',
            indigenousOrgField: '#indigenousOrgInd',
            orgSectorDropdown: '#orgSectorDropdown',
            orgSubSectorDropdown: '#orgSubSectorDropdown',
            orgSectorList: '#orgApplicationSectorList',
            orgBookSelect: '.auto-complete-select',
            applicantIdField: '#ApplicantInfoViewApplicantId'
        };

        super({ selectors });

        this.componentName = 'ApplicantInfo';
        this.previousUnityAppId = '';
    }

    /**
     * @override
     * Initialize form elements and event handlers
     */
    init() {
        super.init();
        this.initializeNumericFields();
        this.initializeUnityApplicantIdHandling();
        this.initializeDropdowns();
        this.initializeOrgBookSelections();
    }

    /**
     * Initialize numeric input fields with masking
     */
    initializeNumericFields() {
        $('.numeric-mask').maskMoney({ precision: 0 });
        $('.numeric-mask').each(function () {
            $(this).maskMoney('mask', this.value);
        });
    }

    /**
     * Initialize Unity Applicant ID field handling
     */
    initializeUnityApplicantIdHandling() {
        const $unityAppId = $(this.selectors.unityAppIdField);
        this.previousUnityAppId = $unityAppId.val().trim();
        $unityAppId.on('input change', () => {
            const current = $unityAppId.val().trim();
            $(this.selectors.saveButton).prop('disabled', current === this.previousUnityAppId);
        });
    }

    /**
     * @override
     * Initialize PubSub event subscriptions
     */
    initializePubSubEvents() {
        this.subscribe('fields_applicantinfo', () => {
            this.enableSaveButton();
        });
    }

    /**
     * Initialize dropdowns for sector and subsector selection
     */
    initializeDropdowns() {
        $(this.selectors.orgSectorDropdown).change(() => {
            const selectedValue = $(this.selectors.orgSectorDropdown).val();
            const sectorList = JSON.parse($(this.selectors.orgSectorList).text());
            const childDropdown = $(this.selectors.orgSubSectorDropdown);

            childDropdown.empty();

            // Add default option
            childDropdown.append($('<option>', {
                value: '',
                text: 'Please choose...'
            }));

            // Add subsectors based on selected sector
            const subSectors = sectorList.find(sector => (sector.sectorName === selectedValue))?.subSectors;
            if (subSectors) {
                $.each(subSectors, function (_, item) {
                    childDropdown.append($('<option>', {
                        value: item.subSectorName,
                        text: item.subSectorName
                    }));
                });
            }
        });
    }

    /**
     * Initialize OrgBook selection handling
     */
    initializeOrgBookSelections() {
        $(this.selectors.orgBookSelect).on('select2:select', (e) => {
            const selectedData = e.params.data;
            const orgBookId = selectedData.id;

            abp.ajax({
                url: '/api/app/org-book/org-book-details-query/' + orgBookId,
                type: 'GET'
            }).done((response) => {
                $(this.selectors.orgNameField).val(response.names[0].text);
                $(this.selectors.orgNumberField).val(orgBookId);

                const entryStatus = this.getAttributeObjectByType("entity_status", response.attributes);
                const orgStatus = entryStatus.value === "HIS" ? "HISTORICAL" : "ACTIVE";
                $(this.selectors.orgStatusField).val(orgStatus);

                const entityType = this.getAttributeObjectByType("entity_type", response.attributes);
                $(this.selectors.orgTypeField).val(entityType.value);

                this.enableSaveButton();
            });
        });
    }

    /**
     * Find attribute object by type from attributes array
     */
    getAttributeObjectByType(type, attributes) {
        return attributes.find(attr => attr.type === type);
    }

    /**
     * @override
     * Build the applicant info object from form data
     */
    buildDataObject(formData) {
        const applicantInfoObj = super.buildDataObject(formData);

        // Handle special fields
        $.each(formData, (_, input) => {
            if (input.name === 'ApplicantId' || input.name === 'SupplierNumber' || input.name === 'OriginalSupplierNumber') {
                applicantInfoObj[input.name] = input.value;
            }
        });

        // Add organization information
        applicantInfoObj.orgName = $(this.selectors.orgNameField).val();
        applicantInfoObj.orgNumber = $(this.selectors.orgNumberField).val();
        applicantInfoObj.orgStatus = $(this.selectors.orgStatusField).val();
        applicantInfoObj.organizationType = $(this.selectors.orgTypeField).val();
        applicantInfoObj.IndigenousOrgInd = $(this.selectors.indigenousOrgField).is(":checked") ? "Yes" : "No";

        return applicantInfoObj;
    }

    /**
     * @override
     * Handle the save button click event
     */
    async handleSaveButtonClick() {
        try {
            const applicationId = $(this.selectors.applicationIdField).val();
            const formData = $(this.selectors.form).serializeArray();
            const formVersionId = $(this.selectors.formVersionIdField).val();
            const worksheetId = $(this.selectors.worksheetIdField).val();

            const applicantInfoObj = this.buildDataObject(formData);

            // Add additional fields
            applicantInfoObj.correlationId = formVersionId;
            applicantInfoObj.worksheetId = worksheetId;

            // Show spinner for supplier number change
            if (this.isSupplierNumberChanged(applicantInfoObj)) {
                $(this.selectors.spinner).show();
            }

            const currentUnityAppId = applicantInfoObj.UnityApplicantId;

            // Determine appropriate action based on Unity Applicant ID
            if (currentUnityAppId !== null) {
                if (this.previousUnityAppId !== currentUnityAppId) {
                    await this.checkUnityApplicantIdExist(currentUnityAppId, applicationId, applicantInfoObj);
                } else {
                    await this.saveData(applicationId, applicantInfoObj);
                }
            } else {
                await this.saveData(applicationId, applicantInfoObj);
            }

            // Update state and UI
            this.previousUnityAppId = currentUnityAppId;
            $(this.selectors.saveButton).prop('disabled', true);
            this.afterSave(applicantInfoObj);
        } catch (error) {
            console.error("Error saving applicant info:", error);
            $(this.selectors.saveButton).prop('disabled', false);
        } finally {
            $(this.selectors.spinner).hide();
        }
    }

    /**
     * Check if supplier number has been changed
     */
    isSupplierNumberChanged(applicantInfoObj) {
        return applicantInfoObj.SupplierNumber != null &&
            applicantInfoObj.SupplierNumber !== "" &&
            applicantInfoObj.SupplierNumber !== applicantInfoObj.OriginalSupplierNumber;
    }

    /**
     * @override
     * Update applicant information via API
     */
    async saveData(appId, info) {
        await unity.grantManager.grantApplications.grantApplication
            .updateProjectApplicantInfo(appId, info);
        abp.notify.success('Applicant Info has been updated.');
        await this.refreshSupplierInfoWidget();
    }

    /**
     * @override
     * Actions to perform after successful save
     */
    afterSave(data) {
        super.afterSave(data);
        PubSub.publish('applicant_info_updated', data);
    }

    /**
     * @override
     * Check if the user has permission to save
     */
    hasPermissionToSave() {
        return abp.auth.isGranted('GrantApplicationManagement.ApplicantInfo.Update');
    }

    /**
     * Generate a new Unity Applicant ID
     */
    async generateUnityApplicantId() {
        try {
            const nextUnityApplicantId = await unity.grantManager.applicants.applicant.getNextUnityApplicantId();
            document.getElementById('applicantInfoUnityApplicantId').value = nextUnityApplicantId;
            $(this.selectors.saveButton).prop('disabled', false);
        } catch (error) {
            console.error("Error generating Unity Applicant ID:", error);
        }
    }

    /**
     * Check if a Unity Applicant ID already exists
     */
    async checkUnityApplicantIdExist(unityAppId, appId, appInfoObj) {
        try {
            const existingApplicant = await unity.grantManager.applicants.applicant.getExistingApplicant(unityAppId);

            if (existingApplicant) {
                Swal.fire({
                    icon: "error",
                    text: "Applicant ID already exists. Please enter a unique ID.",
                    confirmButtonText: 'Ok',
                    customClass: {
                        confirmButton: 'btn btn-primary'
                    }
                });
            } else {
                await this.saveData(appId, appInfoObj);
            }
        } catch (error) {
            console.error("Error checking Unity Applicant ID:", error);
        }
    }

    /**
     * Refresh the supplier info widget
     */
    async refreshSupplierInfoWidget() {
        const applicantId = $(this.selectors.applicantIdField).val();
        const url = `../Payments/Widget/SupplierInfo/Refresh?applicantId=${applicantId}`;

        try {
            const response = await fetch(url);
            const data = await response.text();
            const supplierInfo = document.getElementById('supplier-info-widget');
            const parser = new DOMParser();
            const doc = parser.parseFromString(data, 'text/html');
            const siteIdValue = doc.querySelector('#SiteId')?.value;

            if (supplierInfo) {
                supplierInfo.innerHTML = data;
                if (siteIdValue) {
                    PubSub.publish('reload_sites_list', siteIdValue);
                }
            }
        } catch (error) {
            console.error('Error refreshing supplier-info-widget:', error);
        } finally {
            $(this.selectors.spinner).hide();
        }
    }
}

let applicantInfoInstance;

$(function () {
    applicantInfoInstance = new ApplicantInfoComponent();
    applicantInfoInstance.init();
});

// Global functions that can be called from markup
function generateUnityApplicantIdBtn() {
    applicantInfoInstance.generateUnityApplicantId();
}
