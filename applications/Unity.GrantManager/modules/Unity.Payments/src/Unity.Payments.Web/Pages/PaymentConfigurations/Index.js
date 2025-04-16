$(function () {

    let createModal = new abp.ModalManager(abp.appPath + 'AccountCoding/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'AccountCoding/UpdateModal');
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
    let index = 0;
    const listColumns = [
        {
            title: 'Ministry Client',
            name: "ministryClient",
            data: "ministryClient",
            index: index
        },
        {
            title: 'Responsibility',
            name: "responsibility", 
            data: "responsibility",
            index: index++
        },
        {
            title: 'Service Line',
            name: "serviceLine", 
            data: "serviceLine",
            index: index++
        },
        {
            title: 'Stob',
            name: "stob", 
            data: "stob",
            index: index++
        },
        {
            title: 'Project #',
            name: "projectNumber", 
            data: "projectNumber",
            index: index++
        },  
        {
            title: 'Action',
            orderable: false,
            className: 'notexport text-center',
            name: 'rowActions',
            index: index++,
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
            title: 'Default',
            orderable: false,
            className: 'notexport text-center',
            name: 'defaultRadio',
            index: index++,
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
        dataTableName: 'AccountCodesDataTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        externalSearchId: 'search-data-table'
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
        createModal.onOpen(function () {

            const UIElements = {
                inputMinistryClient: $('input[name="AccountCoding.MinistryClient"]'),
                inputResponsibility: $('input[name="AccountCoding.Responsibility"]'),
                inputServiceLine: $('input[name="AccountCoding.ServiceLine"]'),
                inputStob: $('input[name="AccountCoding.Stob"]'),
                inputProjectNumber: $('input[name="AccountCoding.ProjectNumber"]'),
                readOnlyAccountCoding: $('#account-coding')
            };

            UIElements.inputMinistryClient.on('keyup', setAccountCodingDisplay);
            UIElements.inputResponsibility.on('keyup', setAccountCodingDisplay);
            UIElements.inputServiceLine.on('keyup', setAccountCodingDisplay);
            UIElements.inputStob.on('keyup', setAccountCodingDisplay);
            UIElements.inputProjectNumber.on('keyup', setAccountCodingDisplay);
            
            function setAccountCodingDisplay() {
                let currentAccount = $(UIElements.inputMinistryClient).val() + "." +
                    $(UIElements.inputResponsibility).val() + "." +
                    $(UIElements.inputServiceLine).val() + "." +
                    $(UIElements.inputStob).val() + "." +
                    $(UIElements.inputProjectNumber).val();
                
                $(UIElements.readOnlyAccountCoding).val(currentAccount);
            }
        });
    };
});
