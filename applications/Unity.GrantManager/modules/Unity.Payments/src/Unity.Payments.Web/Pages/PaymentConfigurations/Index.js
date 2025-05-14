$(function () {
    let createModal = new abp.ModalManager(abp.appPath + 'AccountCoding/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'AccountCoding/UpdateModal');
    let updateThresholdModal = new abp.ModalManager(abp.appPath + 'PaymentThresholds/UpdateModal');
    
    const l = abp.localization.getResource('GrantManager');
    toastr.options.positionClass = 'toast-top-center';
    let dataTable;
    let paymentSettingsDataTable;

    const UIElements = {
        accountCodingDataTable: $('#AccountCodesDataTable'),
        paymentSettingsDataTable: $('#PaymentSettingsDataTable'),
        accountCodingId: $('#AccountCodingId'),
        accountCodingMenu: $('#account-coding-menu-item'),
        paymentSettingMenu: $('#payment-setting-menu-item'),
        accountCodesDiv: $('#account-codes-div'),
        paymentSettingsDiv: $('#payment-settings-div'),
    };

    init();

    function init() {        
        dataTable = initializeAccountCodesDataTable();
        paymentSettingsDataTable = initializePaymentSettingsDataTable();
        bindUIElements();
    }

    function bindUIElements() {
        UIElements.accountCodingMenu.on('click', menuItemClick);
        UIElements.paymentSettingMenu.on('click', menuItemClick);        
    }

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
        dataTable.columns.adjust().draw();
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
       
        let dt = UIElements.paymentSettingsDataTable;
        return initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 25,
            defaultSortColumn: 0,
            dataEndpoint: unity.grantManager.payments.paymentSettings.getL2ApproversThresholds,
            data: {},
            responseCallback,
            actionButtons,
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            dataTableName: 'PaymentSettingsDataTable',
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            useNullPlaceholder: true,
            externalSearchId: 'search-data-table'
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
                    index: index++
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
            },
            ...commonTableActionButtons(l('Intake'))
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
       
        let dt = UIElements.accountCodingDataTable;
        return initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 25,
            defaultSortColumn: 0,
            dataEndpoint: unity.grantManager.payments.accountCoding.getList,
            data: {},
            responseCallback,
            actionButtons,
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            dataTableName: 'AccountCodesDataTable',
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            useNullPlaceholder: true,
            externalSearchId: 'search-data-table'
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
                    return `<input type="radio" name="default-account-code" ${checked}  onclick="handleDefaultAccountCodeRadioClick('${data}')"/>`;
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



});

function handleDefaultAccountCodeRadioClick(id) {
    unity.grantManager.payments.paymentConfiguration.setDefaultAccountCode(id).done(function () {
        toastr.success('Successfully set default account code.');
    }).fail(function () {
        toastr.error('Failed to set default account code.');
    });
};
