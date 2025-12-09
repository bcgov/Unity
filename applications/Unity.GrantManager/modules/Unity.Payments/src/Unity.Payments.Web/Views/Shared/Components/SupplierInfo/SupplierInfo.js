$(function () {
    let dataTable;
    const l = abp.localization.getResource('Payments');
    let updateModal = new abp.ModalManager(abp.appPath + 'Sites/UpdateModal');

    $(document).on('click', '#btnClearSupplierNo', function (e) {
        e.preventDefault();
        clearSupplierInfo();
    });

    updateModal.onResult(function (data) {
        dataTable.ajax.reload();
        abp.notify.success('Site Updated successfully.', 'Site Updated');
    });

    updateModal.onOpen(function () {
        const UIElements = {
            payGroup: $('#Site_PaymentGroup'),
            bankAccount: $('#Site_BankAccount'),
            bankAccountWarningDiv: $('#bank-account-warning-div'),
        };

        bindUIEvents();
        validateBankeAccount();

        function bindUIEvents() {
            UIElements.payGroup.on('change', validateBankeAccount);
        }

        function validateBankeAccount() {
            if (
                UIElements.payGroup.val() == 1 &&
                UIElements.bankAccount.val() == ''
            ) {
                // EFT
                UIElements.bankAccountWarningDiv.removeClass('hidden');
            } else {
                UIElements.bankAccountWarningDiv.addClass('hidden');
            }
        }
    });

    const UIElements = {
        navOrgInfoTab: $('#nav-organization-info-tab'),
        siteId: $('#SiteId'),
        paymentApplicantId: $('#PaymentInfo_ApplicantId'),
        paymentApplicationId: $('#PaymentInfoViewApplicationId'),
        originalSupplierNumber: $('#OriginalSupplierNumber'),
        supplierNumber: $('#SupplierNumber'),
        supplierName: $('#SupplierName'),
        hasEditSupplier: $('#HasEditSupplierInfo'),
        refreshSitesBtn: $('#btn-refresh-sites'),
        orgName: $('#ApplicantSummary_OrgName'), // Note: Dependent on Applicant Info Tab
        nonRegisteredOrgName: $('#ApplicantSummary_NonRegOrgName'), // Note: Dependent on Applicant Info Tab
        supplierOrgInfoErrorDiv: $('#supplier-error-div'),
    };

    function init() {
        $(document).ready(function () {
            loadSiteInfoTable();
            bindUIEvents();
            validateMatchingSupplierToOrgInfo();
            enableDisableRefreshSitesButton();
        });
    }

    function enableDisableRefreshSitesButton() {
        const supplierNumber = UIElements.supplierNumber.val();
        const originalSupplierNumber =
            UIElements.originalSupplierNumber.val();


        if (originalSupplierNumber != '' && supplierNumber && supplierNumber.trim() !== '' && supplierNumber === originalSupplierNumber) {
            UIElements.refreshSitesBtn.removeAttr('disabled');
        } else {
            UIElements.refreshSitesBtn.attr('disabled', 'disabled');
        }


    }

    init();

    function validateMatchingSupplierToOrgInfo() {
        if (UIElements.paymentApplicantId.length === 0) {
            console.warn(
                'Payment Applicant ID element not found. Skipping validation.'
            );
            UIElements.supplierOrgInfoErrorDiv.toggleClass('hidden', true);
            return;
        }

        const applicantId = UIElements.paymentApplicantId.val();
        let supplierName = ($('#SupplierName').val() || '')
            .toLowerCase()
            .trim();

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
            unity.grantManager.grantApplications.applicationApplicant
                .getSupplierNameMatchesCheck(applicantId, supplierName)
                .then((isMatch) => {
                    $('#supplier-error-div').toggleClass('hidden', isMatch);
                })
                .catch((error) => {
                    console.error(error);
                });
        } else {
            // Only fetch values if elements exist
            const orgName = orgNameExists
                ? (orgNameElem.val() || '').toLowerCase().trim()
                : '';
            const nonRegisteredOrgName = nonRegOrgNameExists
                ? (nonRegOrgNameElem.val() || '').toLowerCase().trim()
                : '';

            // Hides warning if there is a match
            let isMatch =
                (!orgName && !nonRegisteredOrgName) ||
                supplierName === orgName ||
                supplierName === nonRegisteredOrgName;
            $('#supplier-error-div').toggleClass('hidden', isMatch);
        }
    }

    function handleRefreshSitesSuccess(response) {
        if (!response.hasChanges) {
            let message = "The site list has been refreshed, and no changes were detected since the last update.";
            Swal.fire({
                title: 'Action Complete',
                text: message,
                confirmButtonText: 'Ok',
                customClass: {
                    confirmButton: 'btn btn-primary',
                },
            });
            return;
        }

        // Reload the DataTable to properly apply all column render functions
        if (!dataTable) {
            return;
        }

        dataTable.ajax.reload(() => showSiteListUpdateMessage(response));
    }

    function showSiteListUpdateMessage(response) {
        let message = "The site list has been updated. Please re-select your default site";
        const sites = response.sites || [];

        if (sites.length === 0) {
            message = "No sites were found for the supplier";
        } else if (sites.length > 1) {
            $('input[name="default-site"]').prop('checked', false);
        } else if (sites.length === 1) {
            // Auto select the only site as default
            let onlySiteId = sites[0].id;
            $('input[name="default-site"][value="' + onlySiteId + '"]').prop('checked', true);
            message = "The site list has been updated. Only one site was returned and has been defaulted.";
            saveSiteDefault(onlySiteId);
        }

        Swal.fire({
            title: 'Action Complete',
            text: message,
            confirmButtonText: 'Ok',
            customClass: {
                confirmButton: 'btn btn-primary',
            },
        });
    }

    function bindUIEvents() {
        UIElements.navOrgInfoTab.one('click', function () {
            if (dataTable) {
                dataTable.columns.adjust();
            }
        });

        UIElements.supplierNumber.on('change', enableDisableRefreshSitesButton);
        UIElements.supplierNumber.on('keyup', enableDisableRefreshSitesButton);
        UIElements.supplierName.on('change', validateMatchingSupplierToOrgInfo);
        UIElements.orgName.on('change', validateMatchingSupplierToOrgInfo);
        UIElements.nonRegisteredOrgName.on(
            'change',
            validateMatchingSupplierToOrgInfo
        );

        $('#btnClearSupplierNo').show();

        UIElements.refreshSitesBtn.on('click', function () {
            let originalSupplierNumber =
                UIElements.originalSupplierNumber.val();
            let supplierNumber = UIElements.supplierNumber.val();
            // Check if supplier Number matches the original supplier number
            if (originalSupplierNumber == '' || supplierNumber !== originalSupplierNumber) {
                Swal.fire({
                    title: 'Action Complete',
                    text: 'The Supplier # must be saved before refreshing the site list',
                    confirmButtonText: 'Ok',
                    customClass: {
                        confirmButton: 'btn btn-primary',
                    },
                });
                return;
            }

            const applicantId = UIElements.paymentApplicantId.val();
            const applicationId = UIElements.paymentApplicationId.val() || '';
            $.ajax({
                url: `/api/app/supplier/sites-by-supplier-number?supplierNumber=${originalSupplierNumber}&applicantId=${applicantId}&applicationId=${applicationId}`,
                method: 'GET',
                success: handleRefreshSitesSuccess,
                error: function (xhr, status, error) {
                    console.error('Error loading sites:', error);
                    abp.notify.error('Failed to refresh sites');
                },
            });
        });
    }

    function clearSupplierInfo() {
        // Clear supplier fields
        const supplierNumber = $('#SupplierNumber');
        const supplierName = $('#SupplierName');
        const supplierOrgInfoErrorDiv = $('#supplier-error-div');

        // Clear supplier fields
        supplierNumber.value = '';
        supplierNumber.val('');
        supplierNumber.attr('value', '');
        supplierNumber.trigger('change');
        supplierNumber.trigger('keyup');

        supplierName.val('');
        supplierName.trigger('change');
        $('#Status').val('');

        // Clear hidden fields
        $('#SupplierId').val('');
        $('#SupplierCorrelationId').val('');

        // Hide error message
        supplierOrgInfoErrorDiv.addClass('hidden');

        // Clear the data table
        // if (dataTable) {
        //     dataTable.clear().draw();
        // }

        // Enable save button if the function exists
        console.log('Supplier information cleared');
    }

    function loadSiteInfoTable() {
        let dt = $('#SiteInfoTable');

        let inputAction = function () {
            const supplierId = $('#SupplierId').val();
            return String(supplierId);
        };

        let responseCallback = function (result) {
            if (!result || result.length === 0) {
                return {
                    recordsTotal: 0,
                    recordsFiltered: 0,
                    data: [],
                };
            }
            return {
                recordsTotal: result.length,
                recordsFiltered: result.length,
                data: result,
            };
        };

        const listColumns = getColumns();
        const defaultVisibleColumns = [
            'number',
            'paymentGroup',
            'addressLine1',
            'bankAccount',
            'status',
            'id',
            'rowActions',
        ];

        let actionButtons = [
            {
                text: 'Filter',
                className: 'custom-table-btn flex-none btn btn-secondary',
                id: 'btn-toggle-filter',
                action: function (e, dt, node, config) { },
                attr: {
                    id: 'btn-toggle-filter',
                },
            },
        ];

        dataTable = initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 10,
            defaultSortColumn: 0,
            dataEndpoint:
                unity.grantManager.applicants.applicantSupplier
                    .getSitesBySupplierId,
            data: inputAction,
            responseCallback,
            actionButtons,
            colReorder: false,
            serverSideEnabled: false,
            pagingEnabled: false,
            reorderEnabled: false,
            useNullPlaceholder: true,
            languageSetValues: {},
            dataTableName: 'SiteInfoTable',
            externalSearchInputId: 'SiteInfoSearch',
            dynamicButtonContainerId: 'siteDynamicButtonContainerId',
        });

        function getColumns() {
            let columnIndex = 0;

            return [
                getSiteNumber(columnIndex++),
                getPayGroup(columnIndex++),
                getMailingAddress(columnIndex++),
                getBankAccount(columnIndex++),
                getStatus(columnIndex++),
                getSiteDefaultRadio(columnIndex++),
                getEditButtonColumn(columnIndex++),
            ].map((column) => ({
                ...column,
                targets: [column.index],
                orderData: [column.index, 0],
            }));
        }
    }

    function getSiteNumber(columnIndex) {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:SiteNumber'),
            data: 'number',
            name: 'number',
            className: 'data-table-header',
            index: columnIndex,
        };
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
            index: columnIndex,
        };
    }

    function getMailingAddress(columnIndex) {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:MailingAddress'),
            data: 'addressLine1',
            name: 'addressLine1',
            className: 'data-table-header',
            render: function (data, type, full, meta) {
                return (
                    nullToEmpty(full.addressLine1) +
                    ' ' +
                    nullToEmpty(full.addressLine2) +
                    ' ' +
                    nullToEmpty(full.addressLine3) +
                    ' ' +
                    nullToEmpty(full.city) +
                    ' ' +
                    nullToEmpty(full.province) +
                    ' ' +
                    nullToEmpty(full.postalCode)
                );
            },
            index: columnIndex,
        };
    }

    function getBankAccount(columnIndex) {
        return {
            title: 'Bank Account',
            name: 'bankAccount',
            data: 'bankAccount',
            className: 'data-table-header',
            index: columnIndex,
        };
    }

    function getStatus(columnIndex) {
        return {
            title: 'Status',
            name: 'status',
            data: 'status',
            className: 'data-table-header',
            index: columnIndex,
        };
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
                let disabled =
                    UIElements.hasEditSupplier.val() == 'False'
                        ? 'disabled'
                        : '';
                if (full.markDeletedInUse) {
                    return 'Deleted-In Use';
                }

                if (
                    abp.auth.isGranted(
                        'Unity.GrantManager.ApplicationManagement.Payment.Supplier.Update'
                    )
                ) {
                    return `<input type="radio" class="site-radio" name="default-site" value="${data}" onclick="saveSiteDefault('${data}')" ${checked} ${disabled}/>`;
                }

                return `<input type="radio" class="site-radio" name="default-site" ${checked} ${disabled}/>`;
            },
            index: columnIndex,
        };
    }

    function getEditButtonColumn(columnIndex) {
        return {
            title: 'Actions',
            data: 'id',
            name: 'rowActions',
            className: 'data-table-header',
            orderable: false,
            sortable: false,
            index: columnIndex,
            rowAction: {
                items: [
                    {
                        text: 'Edit',
                        action: (data) =>
                            updateModal.open({ id: data.record.id }),
                        visible: () => true,
                        enabled: () =>
                            UIElements.hasEditSupplier.val() === 'True',
                    },
                ],
            },
        };
    }

    PubSub.subscribe('refresh_sites_list', (msg, data) => {
        if (dataTable) {
            dataTable?.ajax.reload();
            dataTable?.columns?.adjust();
        }
    });

    PubSub.subscribe('reload_sites_list', (msg, data) => {
        UIElements.siteId.val(data);
        loadSiteInfoTable();
        validateMatchingSupplierToOrgInfo();
    });

    function nullToEmpty(value) {
        return value == null ? '' : value;
    }
});

function saveSiteDefault(siteId) {
    let applicantId = $('#ApplicantId').val();
    $.ajax({
        url: `/api/app/applicant/${applicantId}/site/${siteId}`,
        type: 'POST',
        data: JSON.stringify({ ApplicantId: applicantId, SiteId: siteId }),
    })
        .then((response) => {
            abp.notify.success(
                'Default site has been successfully saved.',
                'Default Site Saved'
            );
        })
        .catch((error) => {
            console.error(
                'There was a problem with the post operation:',
                error
            );
        });
}

function reinitializeSupplierInfo() {
    // Always show the clear button after widget refresh
    setTimeout(function () {
        $('#btnClearSupplierNo').show();
    }, 100);
}

window.reinitializeSupplierInfo = reinitializeSupplierInfo;
