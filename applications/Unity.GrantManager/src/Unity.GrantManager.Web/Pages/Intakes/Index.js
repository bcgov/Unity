$(function () {
    const l = abp.localization.getResource('GrantManager');
    let createModal = new abp.ModalManager(abp.appPath + 'Intakes/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'Intakes/UpdateModal');

    /**
     * Intakes: List All
     */
    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn flex-none';
    let actionButtons = [
        {
            text: '<i class="fl fl-add-to align-middle"></i> <span>' + l('Common:Command:Create') + '</span>',
            titleAttr: l('Common:Command:Create'),
            id: 'CreateIntakeButton',
            className: 'btn-light rounded-1',
            action: (e, dt, node, config) => createIntakeBtn(e)
        },
        ...commonTableActionButtons(l('Intake'))
    ];

    const listColumns = [
        {
            title: l('Intake'),
            name: "intakeName",
            data: "intakeName",
            index: 0
        },
        {
            title: l('Common:StartDate'),
            name: "startDate",
            data: "startDate",
            index: 1,
            render: (data) => luxon
                .DateTime
                .fromISO(data, {
                    locale: abp.localization.currentCulture.name
                }).toLocaleString(luxon.DateTime.DATE_SHORT)
        },
        {
            title: l('Common:EndDate'),
            name: "endDate",
            data: "endDate",
            index: 2,
            render: (data) => luxon
                .DateTime
                .fromISO(data, {
                    locale: abp.localization.currentCulture.name
                }).toLocaleString(luxon.DateTime.DATE_SHORT)
        },
        {
            title: l("Budget"),
            name: "budget",
            data: "budget",
            index: 3
        },
        {
            title: l('Actions'),
            data: 'id',
            orderable: false,
            className: 'notexport text-center',
            name: 'rowActions',
            index: 4,
            rowAction: {
                items:
                    [
                        {
                            text: l('Common:Command:Edit'),
                            action: (data) => updateModal.open({ id: data.record.id })
                        }
                    ]
            }
        }
    ];

    const defaultVisibleColumns = [
        'intakeName',
        'startDate',
        'endDate',
        'budget',
        'rowActions'
    ];

    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.items.length,
            data: result.items
        };
    };

    let dt = $('#IntakesTable');

    let dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 0,
        dataEndpoint: unity.grantManager.intakes.intake.getList,
        data: {},
        responseCallback,
        actionButtons,
        serverSideEnable: false,
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
});

