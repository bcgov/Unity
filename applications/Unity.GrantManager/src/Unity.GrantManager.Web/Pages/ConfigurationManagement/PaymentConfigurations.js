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
        UIElements.paymentPrefixSaveButton.on('click', updatePaymentPrefix);
        UIElements.paymentPrefixDiscardButton.on('click', discardPaymentPrefix);
        UIElements.paymentPrefixInput.on('keyup', checkEnableDiscard);
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
        const listColumns = getPaymentSettingsColumns();

        const defaultVisibleColumns = [
            'userName',
            'paymentThreshold',
            'description'
        ];

        let dt = UIElements.paymentSettingsDT;
        return initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 25,
            defaultSortColumn: {
                name: 'userName',
                dir: 'asc'
            },
            dataEndpoint: unity.grantManager.payments.paymentSettings.getL2ApproversThresholds,
            data: {},
            responseCallback: paymentSettingsResponseCallback,
            actionButtons,
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            dataTableName: 'PaymentSettingsDataTable',
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            useNullPlaceholder: true,
            disableColumnSelect: true,
            externalSearchId: 'search-data-table'
        });

        
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
            'description',
            'defaultRadio',
            'rowActions',
        ];

        let dt = UIElements.accountCodingDT;
        return initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 25,
            defaultSortColumn: {
                name: 'ministryClient',
                dir: 'asc'
            },
            dataEndpoint: unity.grantManager.payments.accountCoding.getList,
            data: {},
            responseCallback: accountCodesResponseCallback,
            actionButtons,
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            dataTableName: 'AccountCodesDataTable',
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            useNullPlaceholder: true,
            disableColumnSelect: true,
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
                title: 'Description',
                name: "description",
                data: "description",
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

    function editThresholdBtn(id, userName) {
        updateThresholdModal.open({ id: id, userName: userName });
        updateThresholdModal.onOpen(function () {
            bindModalElements();
        });
    }

    function editAccountCodingBtn(id) {
        updateModal.open({ id: id });
        updateModal.onOpen(function () {
            bindModalElements();
        });
    }

    function createAccountCodingBtn(e) {
        e.preventDefault();
        createModal.open();
        createModal.onOpen(function () {
            bindModalElements();
        });
    }

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
    }

    function checkEnableDiscard() {
        const originalPrefix = UIElements.originalPaymentPrefix.val();
        const currentPrefix = UIElements.paymentPrefixInput.val();
        UIElements.paymentPrefixDiscardButton.prop('disabled', currentPrefix === originalPrefix);
    }

    function discardPaymentPrefix() {
        UIElements.paymentPrefixInput.val(UIElements.originalPaymentPrefix.val());
        toastr.info('Payment prefix changes discarded.');
        checkEnableDiscard();
    }

    function getPaymentSettingsColumns() {
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
});

function paymentSettingsResponseCallback(result) {
    return {
        recordsTotal: result.length,
        recordsFiltered: result.length,
        data: result
    };
}

function accountCodesResponseCallback(result) {
    return {
        recordsTotal: result.totalCount,
        recordsFiltered: result.items.length,
        data: result.items
    };
}

function clearFilter() {
    $('#search-data-table').val('');
    $('#search-data-table').trigger("keyup");
}

function handleDefaultAccountCodeRadioClick(id) {
    $('#AccountCodingId').val(id);
    unity.payments.paymentConfigurations.paymentConfiguration.setDefaultAccountCode(id).done(function () {
        toastr.success('Successfully set default account code. Reloading account codes.');
        clearAccountCodesSearchAndReload();
    }).fail(function () {
        toastr.error('Failed to set default account code.');
    });
}

function clearAccountCodesSearchAndReload() {
    clearFilter();
    accountCodingDataTable.search('').draw();

    localStorage.removeItem('DataTables_AccountCodesDataTable_/ConfigurationManagement');
    localStorage.removeItem('DataTables_PaymentSettingsDataTable_/ConfigurationManagement');

    accountCodingDataTable.ajax.reload();
}

function preventNonCurrencyKeyPress(e) {
    if (/[a-zA-Z]/.test(e.key) || e.key === ' ' || e.key === '-' || e.keyCode === 45) {
        e.preventDefault();
    }
}

function preventDecimalKeyUp(e) {
    const input = e.target;
    const cursorPosition = input.selectionStart;
    const decimalMatch = input.value.match(/\.(\d+)/);

    if (decimalMatch && decimalMatch[1].length > 2) {
        input.value = input.value.replace(/\.(\d{2}).*/, '.$1');
        input.setSelectionRange(cursorPosition, cursorPosition);
    }
}
