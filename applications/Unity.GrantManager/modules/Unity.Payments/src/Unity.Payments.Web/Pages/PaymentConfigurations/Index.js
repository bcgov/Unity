$(function () {
    const UIElements = {
        inputMinistryClient: $('input[name="PaymentConfiguration.MinistryClient"]'),
        inputResponsibility: $('input[name="PaymentConfiguration.Responsibility"]'),
        inputServiceLine: $('input[name="PaymentConfiguration.ServiceLine"]'),
        inputStob: $('input[name="PaymentConfiguration.Stob"]'),
        inputProjectNumber: $('input[name="PaymentConfiguration.ProjectNumber"]'),
        readOnlyAccountCoding: $('#account-coding'),
        statusMessage: $('#status-message'),
        inputPaymentIdPrefix: $('#PaymentIdPrefix')
    };

    init();

    function init() {
        bindUIEvents();
        setAccountCodingDisplay();
        displayStatusMessage();
        bindLimitedInputFields(/^[a-zA-Z0-9]+$/, [UIElements.inputPaymentIdPrefix[0].id]);
    }

    let createModal = new abp.ModalManager(abp.appPath + 'PaymentConfigurations/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'PaymentConfigurations/UpdateModal');
    const l = abp.localization.getResource('GrantManager');
    /**
     * List All
     */
    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn flex-none';
    let actionButtons = [
        {
            text: '<i class="fl fl-add-to align-middle"></i> <span>' + l('Common:Command:Create') + '</span>',
            titleAttr: l('Common:Command:Create'),
            id: 'CreateButton',
            className: 'btn-light rounded-1',
            action: (e, dt, node, config) => createIntakeBtn(e)
        },
        ...commonTableActionButtons(l('Intake'))
    ];

    const listColumns = [
        {
            title: 'Ministry Client',
            name: "ministryClient",
            data: "ministryClient",
            index: 0
        },
        {
            title: 'Responsibility',
            name: "responsibility", 
            data: "responsibility",
            index: 1
        },
        {
            title: 'Service Line',
            name: "serviceLine", 
            data: "serviceLine",
            index: 2
        },
        {
            title: 'Stob',
            name: "stob", 
            data: "stob",
            index: 3
        },
        {
            title: 'Project #',
            name: "projectNumber", 
            data: "projectNumber",
            index: 4
        },  
        {
            title: 'Default',
            orderable: false,
            className: 'notexport text-center',
            name: 'rowActions',
            index: 5,
            rowAction: {
                items:
                    [
                        {
                            text: 'Edit',
                            action: (data) => updateModal.open({ id: data.record.id })
                        }
                    ]
            }
        },
        {
            title: '',
            orderable: false,
            className: 'notexport text-center',
            name: 'defaultRadio',
            index: 6,
            rowAction: {
                items:
                    [
                        {
                            text: 'Edit',
                            action: (data) => updateModal.open({ id: data.record.id })
                        }
                    ]
            }
        }
    ];

    const defaultVisibleColumns = [
        'ministryClient',
        'responsibility',
        'serviceLine',
        'stob',
        'projectNumber',
        'rowActions',
        'defaultRadio'
    ];


    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.items.length,
            data: result.items
        };
    };

    let dt = $('#AccountCodesDataTable');

    let dataTable = initializeDataTable({
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
        dataTableName: 'IntakesTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        externalSearchId: 'search-intakes'
    });

    createModal.onResult(function () {
        dataTable.ajax.reload();
    });

    updateModal.onResult(function () {
        dataTable.ajax.reload();
    });

    function createIntakeBtn(e) {
        e.preventDefault();
        createModal.open();
    };


    function displayStatusMessage() {
        let statusMessage = UIElements.statusMessage.val();
        if (statusMessage != "") {
            if (statusMessage.indexOf('error') > 0) {
                abp.notify.error(
                    UIElements.statusMessage.val(),
                    'Error Occurred'
                );
            } else {
                abp.notify.success(
                    UIElements.statusMessage.val(),
                    'Save Successful'
                );
            }
        }
    }

    function bindUIEvents() {
        UIElements.inputMinistryClient.on('keyup', setAccountCodingDisplay);
        UIElements.inputResponsibility.on('keyup', setAccountCodingDisplay);
        UIElements.inputServiceLine.on('keyup', setAccountCodingDisplay);
        UIElements.inputStob.on('keyup', setAccountCodingDisplay);
        UIElements.inputProjectNumber.on('keyup', setAccountCodingDisplay);
    }

    function bindLimitedInputFields(regex, fieldIds) {
        fieldIds.forEach((id) => {
            let inputElement = document.getElementById(id);
            inputElement.addEventListener('input', function (_) {
                let value = inputElement.value;
                let lastChar = value.charAt(value.length - 1);

                if (!regex.test(lastChar)) {
                    inputElement.value = value.slice(0, -1);
                }

                inputElement.value = inputElement.value.toUpperCase();
            });
        });
    }

    function setAccountCodingDisplay() {
        let currentAccount = $(UIElements.inputMinistryClient).val() + "." +
            $(UIElements.inputResponsibility).val() + "." +
            $(UIElements.inputServiceLine).val() + "." +
            $(UIElements.inputStob).val() + "." +
            $(UIElements.inputProjectNumber).val();
        $(UIElements.readOnlyAccountCoding).val(currentAccount);
    }
    $('#resetButton').click(function () {
        $('#paymentConfigForm')[0].reset();
        setAccountCodingDisplay();
    });
});
