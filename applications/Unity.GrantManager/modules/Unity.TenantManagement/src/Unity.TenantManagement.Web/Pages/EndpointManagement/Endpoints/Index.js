(function () {
    const l = abp.localization.getResource('GrantManager');
    let createModal = new abp.ModalManager(abp.appPath + 'EndpointManagement/Endpoints/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'EndpointManagement/Endpoints/UpdateModal');

    /**
     * Intakes: List All
     */
    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn flex-none';
    let actionButtons = [
        {
            text: '<i class="fl fl-add-to align-middle"></i> <span>' + l('Common:Command:Create') + '</span>',
            titleAttr: l('Common:Command:Create'),
            id: 'CreateButton',
            className: 'btn-light rounded-1',
            action: (e, dt, node, config) => createBtn(e)
        }
    ];

    const listColumns = [
        {
            title: "Key Name",
            name: "keyName",
            data: "keyName",
            index: 0
        },
        {
            title: "Url",
            name: "url",
            data: "url",
            index: 1
        },
        {
            title: "Description",
            name: "description",
            data: "description",
            index: 2
        },
        {
            title: l('Actions'),
            data: 'id',
            orderable: false,
            className: 'notexport text-center',
            name: 'rowActions',
            index: 3,
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
        'keyName',
        'url',
        'description',
        'rowActions'
    ];

    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.items.length,
            data: result.items
        };
    };

    let dataTable = Unity.DataTables.create('#EndpointsTable', {
        listColumns,
        defaultVisibleColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 0,
        dataEndpoint: unity.grantManager.integrations.endpoints.endpointManagement.getList,
        data: {},
        responseCallback,
        customButtons: actionButtons,
        pagingEnabled: true,
        reorderEnabled: false,
        languageSetValues: {},
        useNullPlaceholder: true,
        externalSearchId: 'search-endpoints',
        exportTitle: 'Endpoints'
    });

    createModal.onResult(function () {
        dataTable.ajax.reload();
    });

    updateModal.onResult(function () {
        dataTable.ajax.reload();
    });

    function createBtn(e) {
        e.preventDefault();
        createModal.open();
    };
})();
