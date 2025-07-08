$(function () {
    let dataTable;
    const l = abp.localization.getResource('Payments');

    const UIElements = {
        navOrgInfoTab: $('#nav-organization-info-tab'),
        siteId: $("#SiteId"),
        paymentApplicantId: $("#PaymentInfo_ApplicantId"),
        originalSupplierNumber: $("#OriginalSupplierNumber"),
        supplierNumber: $("#SupplierNumber"),
        supplierName: $("#SupplierName"),
        hasEditSupplier: $("#HasEditSupplierInfo"),
        refreshSitesBtn: $("#btn-refresh-sites"),
        orgName: $("#ApplicantSummary_OrgName"), // Note: Dependent on Applicant Info Tab
        nonRegisteredOrgName: $("#ApplicantSummary_NonRegOrgName"), // Note: Dependent on Applicant Info Tab
        supplierOrgInfoErrorDiv: $("#supplier-error-div")
    };

    function init() {
        $(document).ready(function () {
            loadSiteInfoTable();
            bindUIEvents();  
            validateMatchingSupplierToOrgInfo();
        });
    }

    init();

    function validateMatchingSupplierToOrgInfo() {
        if (UIElements.paymentApplicantId.length === 0) {
            console.warn('Payment Applicant ID element not found. Skipping validation.');
            UIElements.supplierOrgInfoErrorDiv.toggleClass('hidden', true);
            return
        }

        const applicantId = UIElements.paymentApplicantId.val();
        let supplierName = ($("#SupplierName").val() || '').toLowerCase().trim();

        if (!supplierName) {
            UIElements.supplierOrgInfoErrorDiv.toggleClass('hidden', true);
            return;
        }

        const orgNameElem = UIElements.orgName;
        const nonRegOrgNameElem = UIElements.nonRegisteredOrgName;
        const orgNameExists = orgNameElem.length > 0;
        const nonRegOrgNameExists = nonRegOrgNameElem.length > 0;

        // If neither element exists, fallback on API check
        if (!orgNameExists && !nonRegOrgNameExists) {
            // NOTE: External module dependency on Unity.GrantManager.GrantApplication.ApplicationApplicantAppService
            unity.grantManager.grantApplications
                .applicationApplicant
                .getSupplierNameMatchesCheck(applicantId, supplierName)
                .then((isMatch) => {
                    abp.notify.success(`Supplier info is now ${isMatch}`);
                    $("#supplier-error-div").toggleClass('hidden', isMatch);
                })
                .catch((error) => {
                    console.error(error);
                });
        } else {
            // Only fetch values if elements exist
            const orgName = orgNameExists ? (orgNameElem.val() || '').toLowerCase().trim() : '';
            const nonRegisteredOrgName = nonRegOrgNameExists ? (nonRegOrgNameElem.val() || '').toLowerCase().trim() : '';

            // Hides warning if there is a match
            let isMatch =
                (!orgName && !nonRegisteredOrgName) ||
                supplierName === orgName ||
                supplierName === nonRegisteredOrgName;
            $("#supplier-error-div").toggleClass('hidden', isMatch);
        }
    }

    function bindUIEvents() {
        UIElements.navOrgInfoTab.one('click', function () { 
            if (dataTable) {
                dataTable.columns.adjust(); 
            }
        });

        UIElements.supplierName.on('change', validateMatchingSupplierToOrgInfo);
        UIElements.orgName.on('change', validateMatchingSupplierToOrgInfo);
        UIElements.nonRegisteredOrgName.on('change', validateMatchingSupplierToOrgInfo);
            
        UIElements.refreshSitesBtn.on('click', function () { 
            let originalSupplierNumber = UIElements.originalSupplierNumber.val();
            // Check if supplier Number matches the original supplier number
            if(originalSupplierNumber == "") {
                Swal.fire({
                    title: "Action Complete",
                    text: "The Supplier # must be saved before refreshing the site list",
                    confirmButtonText: 'Ok',
                    customClass: {
                        confirmButton: 'btn btn-primary'
                    }
                });
                return;
            }

            $.ajax({
                url: `/api/app/supplier/sites-by-supplier-number?supplierNumber=${originalSupplierNumber}`,
                method: 'GET',
                success: function(response) {
                    let dt = $('#SiteInfoTable').DataTable();
                    if (dt) {
                        dt.clear();
                        dt.rows.add(response);
                        dt.draw();
                        dt.columns.adjust();
                        let message = "The site list has been updated. Please re-select your default site";
                        
                        if(response.length == 0) {
                            message = "No sites were found for the supplier";
                        }

                        Swal.fire({
                            title: "Action Complete",
                            text: message,
                            confirmButtonText: 'Ok',
                            customClass: {
                                confirmButton: 'btn btn-primary'
                            }
                        });
                    }
                },
                error: function(xhr, status, error) {
                    console.error('Error loading sites:', error);
                    abp.notify.error('Failed to refresh sites');
                }
            });
        });
    }

    function loadSiteInfoTable() {
        let dt = $('#SiteInfoTable');

        let inputAction = function() {
            const supplierId = $("#SupplierId").val();
            return String(supplierId);
        }

        let responseCallback = function (result) {

            if (!result || result.length === 0) {

                return {
                    recordsTotal: 0,
                    recordsFiltered: 0,
                    data: []
                };
            }
            return {
                recordsTotal: result.length,
                recordsFiltered: result.length,
                data: result
            };
        };
        
        const listColumns = getColumns();
        const defaultVisibleColumns = ['number','paymentGroup','addressLine1','bankAccount','status','id'];

        let actionButtons = [
            {
                text: 'Filter',
                className: 'custom-table-btn flex-none btn btn-secondary',
                id: "btn-toggle-filter",
                action: function (e, dt, node, config) { },
                attr: {
                    id: 'btn-toggle-filter'
                }
            }
        ];

        dataTable = initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 10,
            defaultSortColumn: 0,
            dataEndpoint: unity.grantManager.applicants.applicantSupplier.getSitesBySupplierId,
            data: inputAction,
            responseCallback,
            actionButtons,
            colReorder: false,
            serverSideEnabled: false,
            pagingEnabled: false,
            reorderEnabled: false,
            languageSetValues: {},
            dataTableName: 'SiteInfoTable',
            externalSearchInputId: 'SiteInfoSearch',
            dynamicButtonContainerId: 'siteDynamicButtonContainerId'
        });

        function getColumns() {
            let columnIndex = 0;

            return [
                getSiteNumber(columnIndex++),
                getPayGroup(columnIndex++),
                getMailingAddress(columnIndex++),
                getBankAccount(columnIndex++),
                getStatus(columnIndex++),
                getSiteDefaultRadio(columnIndex++)
            ].map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }));
        }    
    }

    function getSiteNumber(columnIndex) {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:SiteNumber'),
            data: 'number',
            name: 'number',
            className: 'data-table-header',
            index: columnIndex
        }
    }

    function getPayGroup(columnIndex) {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:PayGroup'),
            data: 'paymentGroup',
            className: 'data-table-header',
            render: function (data) {
                switch (data) {
                    case 1:
                        return 'EFT';
                    case 2:
                        return 'Cheque';
                    default:
                        return 'Unknown PaymentGroup';
                }
            },
            index: columnIndex
        }
    }

    function getMailingAddress(columnIndex) {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:MailingAddress'),
            data: 'addressLine1',
            name: 'addressLine1',
            className: 'data-table-header',
            render: function (data, type, full, meta) {
                return nullToEmpty(full.addressLine1) + ' ' + nullToEmpty(full.addressLine2) + " " + nullToEmpty(full.addressLine3) + " " + nullToEmpty(full.city) + " " + nullToEmpty(full.province) + " " + nullToEmpty(full.postalCode);
            },
            index: columnIndex
        }
    }

    function getBankAccount(columnIndex) {
        return {
            title: 'Bank Account',
            name: 'bankAccount',
            data: 'bankAccount',
            className: 'data-table-header',
            index: columnIndex
        }
    }

    function getStatus(columnIndex) {
        return {
            title: 'Status',
            name: 'status',
            data: 'status',
            className: 'data-table-header',
            index: columnIndex
        }
    }

    function getSiteDefaultRadio(columnIndex) {
        return {
            title: 'Default',
            data: 'id',
            name: 'id',
            className: 'data-table-header',
            sortable: false,
            render: function (data, type, full, meta) {
                let checked = UIElements.siteId.val() == data ? 'checked' : '';
                let disabled = UIElements.hasEditSupplier.val() == 'False' ? 'disabled' : '';
                if(full.markDeletedInUse) {
                    return 'Deleted-In Use';
                }
                return `<input type="radio" class="site-radio" name="default-site" onclick="saveSiteDefault('${data}')" ${checked} ${disabled}/>`;
            },
            index: columnIndex
        }
    }

    PubSub.subscribe(
        'refresh_sites_list',
        (msg, data) => {
            if(dataTable) {
                dataTable?.ajax.reload();
                dataTable?.columns?.adjust();
            }

        }
    );

    PubSub.subscribe(
        'reload_sites_list',
        (msg, data) => {
            UIElements.siteId.val(data);
            loadSiteInfoTable();
            validateMatchingSupplierToOrgInfo();
        }
    );

    function nullToEmpty(value) {
        return value == null ? '' : value;
    }
});

function saveSiteDefault(siteId) {
    let applicantId = $("#ApplicantId").val();
    $.ajax({
        url: `/api/app/applicant/${applicantId}/site/${siteId}`,
        type: "POST",
        data: JSON.stringify({ ApplicantId: applicantId, SiteId: siteId }),
    })
    .then(response => {
        abp.notify.success('Default site has been successfully saved.', 'Default Site Saved');
    })
    .catch(error => {
        console.error('There was a problem with the post operation:', error);
    });
}
