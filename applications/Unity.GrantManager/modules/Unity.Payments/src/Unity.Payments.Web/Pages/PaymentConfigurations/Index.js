let accountCodingDataTable;
let paymentSettingsDataTable;

$(function () {
    let createModal = new abp.ModalManager(abp.appPath + 'AccountCoding/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'AccountCoding/UpdateModal');
    let updateThresholdModal = new abp.ModalManager(abp.appPath + 'PaymentThresholds/UpdateModal');
    const formatter = createNumberFormatter();
    
    const l = abp.localization.getResource('GrantManager');
    toastr.options.positionClass = 'toast-top-center';


    const UIElements = {
        accountCodingDT: $('#AccountCodesDataTable'),
        paymentSettingsDT: $('#PaymentSettingsDataTable'),
        accountCodingId: $('#AccountCodingId'),
        accountCodingMenu: $('#account-coding-menu-item'),
        paymentSettingMenu: $('#payment-setting-menu-item'),
        accountCodesDiv: $('#account-codes-div'),
        paymentSettingsDiv: $('#payment-settings-div'),
        paymentPrefixSaveButton: $('#PaymentPrefixSaveButton'),
        paymentPrefixDiscardButton: $('#PaymentPrefixDiscardButton'),
        paymentPrefixInput: $('#payment-id-prefix'),
        originalPaymentPrefix: $('#payment-id-prefix-original')
    };

    init();

    function init() {        
        accountCodingDataTable = initializeAccountCodesDataTable();
        paymentSettingsDataTable = initializePaymentSettingsDataTable();
        bindUIElements();
    }

    function bindUIElements() {
        UIElements.accountCodingMenu.on('click', menuItemClick);
        UIElements.paymentSettingMenu.on('click', menuItemClick);    
        UIElements.paymentPrefixSaveButton.on('click', updatePaymentPrefix);    
        UIElements.paymentPrefixDiscardButton.on('click', discardPaymentPrefix);    
        UIElements.paymentPrefixInput.on('keyup', checkEnableDiscard);    }

    function removeActiveClassFromMenuItems() {
        UIElements.accountCodingMenu.removeClass('active');
        UIElements.paymentSettingMenu.removeClass('active');
    }

    function menuItemClick(e) {
        removeActiveClassFromMenuItems();
        e.target.classList.add('active');  
        UIElements.accountCodesDiv.toggleClass('hide');      
        UIElements.paymentSettingsDiv.toggleClass('hide');      
        paymentSettingsDataTable.columns.adjust().draw();
        accountCodingDataTable.columns.adjust().draw();
        clearFilter();
    }
    
    function bindModalElements() {
        const UIElements = {
            inputMinistryClient: $('input[name="AccountCoding.MinistryClient"]'),
            inputResponsibility: $('input[name="AccountCoding.Responsibility"]'),
            inputServiceLine: $('input[name="AccountCoding.ServiceLine"]'),
            inputStob: $('input[name="AccountCoding.Stob"]'),
            inputProjectNumber: $('input[name="AccountCoding.ProjectNumber"]'),
            inputPaymentThreshold: $('#PaymentThreshold_Threshold'),
            readOnlyAccountCoding: $('#account-coding')
        };

        UIElements.inputMinistryClient.on('keyup', setAccountCodingDisplay);
        UIElements.inputResponsibility.on('keyup', setAccountCodingDisplay);
        UIElements.inputServiceLine.on('keyup', setAccountCodingDisplay);
        UIElements.inputStob.on('keyup', setAccountCodingDisplay);
        UIElements.inputProjectNumber.on('keyup', setAccountCodingDisplay);

        UIElements.inputPaymentThreshold.on('keyup', preventDecimalKeyUp);
        UIElements.inputPaymentThreshold.on('keypress', preventNonCurrencyKeyPress);

        function preventNonCurrencyKeyPress(e) {
            // Prevent alphabetic characters and -
            if (/[a-zA-Z]/.test(e.key) || e.key === ' ' || e.key === '-' || e.keyCode === 45) {
                e.preventDefault();
            }
        }

        function preventDecimalKeyUp(e) {
            const input = e.target;
            const cursorPosition = input.selectionStart;
            const decimalMatch = input.value.match(/\.(\d+)/);
        
            // Limit to two decimal places
            if (decimalMatch && decimalMatch[1].length > 2) {
                input.value = input.value.replace(/\.(\d{2}).*/, '.$1');
                input.setSelectionRange(cursorPosition, cursorPosition); // Restore cursor position
            }
        }

        function setAccountCodingDisplay() {
            let currentAccount = $(UIElements.inputMinistryClient).val() + "." +
                $(UIElements.inputResponsibility).val() + "." +
                $(UIElements.inputServiceLine).val() + "." +
                $(UIElements.inputStob).val() + "." +
                $(UIElements.inputProjectNumber).val();
            
            $(UIElements.readOnlyAccountCoding).val(currentAccount);
        }

        setAccountCodingDisplay();
    }

    function initializePaymentSettingsDataTable() {        
        let actionButtons = [];
        const listColumns = getPaymenSettingsColumns();
    
        const defaultVisibleColumns = [
            'userName',
            'paymentThreshold',
            'description'      
        ];

        let responseCallback = function (result) {
            return {
                recordsTotal: result.length,
                recordsFiltered: result.length,
                data: result
            };
        };
       
        let dt = UIElements.paymentSettingsDT;
        return Unity.DataTables.create('#' + dt.attr('id'), {
            listColumns,
            defaultVisibleColumns,
            maxRowsPerPage: 25,
            defaultSortColumn: 0,
            dataEndpoint: unity.grantManager.payments.paymentSettings.getL2ApproversThresholds,
            data: {},
            responseCallback,
            customButtons: actionButtons,
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            useNullPlaceholder: true,
            disableColumnSelect: true,
            externalSearchId: 'search-data-table',
            exportTitle: 'Payment Settings'
        });

        function getPaymenSettingsColumns() {
            let index = 0;
            return [ 
                {
                    title: 'Id',
                    name: "id",
                    data: "id",
                    visible: false,
                    index: index++
                },
                {
                    title: 'User Id',
                    name: "userId",
                    data: "userId",
                    visible: false,
                    index: index++
                },
                {
                    title: 'Expense Authority',
                    name: "userName",
                    data: "userName",
                    visible: true,
                    index: index++
                },
                {
                    title: 'Approval Threshold',
                    name: "paymentThreshold",
                    className: 'dt-body-right', 
                    data: "threshold",
                    visible: true,
                    index: index++,
                    render: function (data, type, row) {
                        if (data == null || data === '') return '';
                        return formatter.format(data);
                    }                    
                },
                {
                    title: 'Description',
                    name: "description", 
                    data: "description",
                    visible: true,
                    index: index++
                },
                {
                    title: 'Action',
                    orderable: false,
                    sortable: false,
                    data: 'id',
                    className: 'notexport text-center',
                    name: 'rowActions',
                    visible: true,
                    index: index++,
                    rowAction: {
                        items:
                            [
                                {
                                    text: 'Edit',
                                    action: (data) => editThresholdBtn(data.record.id, data.record.userName)                                    
                                }
                            ]
                    }
                }        
            ];
        }
    }

    function initializeAccountCodesDataTable() {
        $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn flex-none';
        let actionButtons = [
            {
                text: '<i class="fl fl-add-to align-middle"></i> <span>' + l('Common:Command:Create') + '</span>',
                titleAttr: l('Common:Command:Create'),
                id: 'CreateButton',
                className: 'btn-light rounded-1',
                action: (e, dt, node, config) => createAccountCodingBtn(e)
            }
        ];
    
        const listColumns = getAccountCodingColumns();
    
        const defaultVisibleColumns = [
            'ministryClient',
            'responsibility',
            'serviceLine',
            'stob',
            'projectNumber',
            'defaultRadio',
            'rowActions',        
        ];

        let responseCallback = function (result) {
            return {
                recordsTotal: result.totalCount,
                recordsFiltered: result.items.length,
                data: result.items
            };
        };
       
        let dt = UIElements.accountCodingDT;
        return Unity.DataTables.create('#' + dt.attr('id'), {
            listColumns,
            defaultVisibleColumns,
            maxRowsPerPage: 25,
            defaultSortColumn: 0,
            dataEndpoint: unity.grantManager.payments.accountCoding.getList,
            data: {},
            responseCallback,
            customButtons: actionButtons,
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            useNullPlaceholder: true,
            disableColumnSelect: true,
            externalSearchId: 'search-data-table',
            exportTitle: 'Account Coding'
        });
    }

    function getAccountCodingColumns() {
        let index = 0;
        return [   
            {
                title: 'Ministry Client',
                name: "ministryClient",
                data: "ministryClient",
                visible: true,
                index: index++
            },
            {
                title: 'Responsibility',
                name: "responsibility", 
                data: "responsibility",
                visible: true,
                index: index++
            },
            {
                title: 'Service Line',
                name: "serviceLine", 
                data: "serviceLine",
                visible: true,
                index: index++
            },
            {
                title: 'Stob',
                name: "stob", 
                data: "stob",
                visible: true,
                index: index++
            },
            {
                title: 'Project #',
                name: "projectNumber", 
                data: "projectNumber",
                visible: true,
                index: index++
            },
            {
                title: 'Default',
                orderable: false,
                visible: true,
                className: 'notexport text-center',
                name: 'defaultRadio',
                index: index++,
                data: 'id',
                render: function (data, type, full, meta) {
                    let checked = UIElements.accountCodingId.val() == data ? 'checked' : '';
                    return `<input type="radio" id="radio-${data}" name="default-account-code" ${checked}  onclick="handleDefaultAccountCodeRadioClick('${data}')"/>`;
                }
            },
            {
                title: 'Action',
                orderable: false,
                sortable: false,
                data: 'id',
                className: 'notexport text-center',
                name: 'rowActions',
                visible: true,
                index: index++,
                rowAction: {
                    items:
                        [
                            {
                                text: 'Edit',
                                action: (data) => editAccountCodingBtn(data.record.id)                                    
                            }
                        ]
                }
            }        
        ];

    }

    createModal.onResult(function () {
        accountCodingDataTable.ajax.reload();
    });

    updateModal.onResult(function () {
        accountCodingDataTable.ajax.reload();
    });

    updateThresholdModal.onResult(function () {
        paymentSettingsDataTable.ajax.reload();
    });    
 
    function editAccountCodingBtn(id) {
        updateModal.open({ id: id });
        updateModal.onOpen(function () {
            bindModalElements();
        });
    };

    function editThresholdBtn(id, userName) {
        updateThresholdModal.open({ id: id, userName: userName });
        updateThresholdModal.onOpen(function () {
            bindModalElements();
        });
    };
    
    function createAccountCodingBtn(e) {
        e.preventDefault();
        createModal.open();
        createModal.onOpen(function () {
            bindModalElements();
        });
    };

    function updatePaymentPrefix() {
        unity.payments.paymentConfigurations.paymentConfiguration.updatePaymentPrefix(UIElements.paymentPrefixInput.val())
        .done(function () {
            toastr.success('Payment prefix updated successfully.');
            $('#payment-id-prefix-original').val(UIElements.paymentPrefixInput.val());
            checkEnableDiscard();
        })
        .fail(function () {
            toastr.error('Failed to update payment prefix.');
        });
    };

    function checkEnableDiscard() {
        const originalPrefix = UIElements.originalPaymentPrefix.val();
        const currentPrefix = UIElements.paymentPrefixInput.val();
        if (currentPrefix !== originalPrefix) {
            UIElements.paymentPrefixDiscardButton.prop('disabled', false);
        } else {
            UIElements.paymentPrefixDiscardButton.prop('disabled', true);
        }
    }

    function discardPaymentPrefix() {
        UIElements.paymentPrefixInput.val(UIElements.originalPaymentPrefix.val());
        toastr.info('Payment prefix changes discarded.');
        checkEnableDiscard();
    };

});

function clearFilter() {
    // Clear the search input (assuming external search input has id 'search-data-table')
    $('#search-data-table').val('');
    $('#search-data-table').trigger("keyup"); // Trigger keyup to clear DataTable's internal search
}

function handleDefaultAccountCodeRadioClick(id) {
    $('#AccountCodingId').val(id); // Update the hidden input with the selected account code ID
    unity.payments.paymentConfigurations.paymentConfiguration.setDefaultAccountCode(id).done(function () {
        toastr.success('Successfully set default account code. Reloading account codes.');
        clearAccountCodesSearchAndReload();    
    }).fail(function () {
        toastr.error('Failed to set default account code.');
    });
};

function clearAccountCodesSearchAndReload() {
    clearFilter();
    // Clear DataTable's internal search and redraw
    accountCodingDataTable.search('').draw();

    // Clear the localStorage key for PaymentSettingsDataTable
    localStorage.removeItem('DataTables_AccountCodesDataTable_/PaymentConfigurations');
    localStorage.removeItem('DataTables_PaymentSettingsDataTable_/PaymentConfigurations');

    // Reload the page
    accountCodingDataTable.ajax.reload();
}
