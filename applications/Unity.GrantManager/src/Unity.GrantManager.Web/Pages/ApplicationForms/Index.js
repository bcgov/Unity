$(function () {
    const l = abp.localization.getResource('GrantManager');
    let createModal = new abp.ModalManager(abp.appPath + 'ApplicationForms/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'ApplicationForms/UpdateModal');
    let tokenModal = new abp.ModalManager({
        viewUrl: '/ApplicationForms/TokenModal',
        modalClass: 'ManageTokens'
    })

    /**
     * Application Forms: List All
     */
    const listColumns = [
        {
            title: l('Actions'),
            orderable: false,
            className: 'notexport text-center',
            data: 'id',
            name: 'rowActions',
            index: 0,
            rowAction: {
                items:
                    [
                        {
                            text: l('Common:Command:Edit'),
                            action: function (data) {
                                updateModal.open({ id: data.record.id })
                            }
                        },
                        {
                            text: "Configuration",
                            action: function (data) {
                                location.href = '/ApplicationForms/Mapping?ApplicationId=' + data.record.id
                            }
                        }
                    ]
            }
        },
        {
            title: l('Common:Name'),
            data: 'applicationFormName',
            name: 'applicationFormName',
            index: 1
        },
        {
            title: l('Common:Description'),
            data: 'applicationFormDescription',
            name: 'applicationFormDescription',
            index: 2
        },
        {
            title: l('ApplicationForms:Category'),
            data: 'category',
            name: 'category',
            index: 3
        },
        {
            title: l('ApplicationForms:ChefsFormId'),
            data: 'chefsApplicationFormGuid',
            name: 'chefsApplicationFormGuid',
            index: 4
        }
    ];

    const defaultVisibleColumns = [
        'rowActions',
        'applicationFormName',
        'applicationFormDescription',
        'category',
        'chefsApplicationFormGuid'
    ];

    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.items.length,
            data: result.items
        };
    };

    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn flex-none';
    let actionButtons = [
        {
            text: '<i class="fl fl-add-to align-middle"></i> <span>' + l('Common:Command:Create') + '</span>',
            titleAttr: l('Common:Command:Create'),
            id: 'CreateApplicationFormButton',
            className: 'btn-light rounded-1',
            action: (e, dt, node, config) => createApplicationFormBtn(e)
        },
        {
            extend: 'collection',
            text: '<i class="fl fl-settings align-middle"></i> <span>' + l('ApplicationForms:APIConfiguration') + '</span>',
            titleAttr: l('ApplicationForms:APIConfiguration'),
            id: 'FormsManageDropdown',
            className: 'btn-light rounded-1',
            buttons: [
                {
                    text: l('ApplicationForms:UpdateAPIToken'),
                    id: 'SetApiTokenBtn',
                    action: (e, dt, node, config) => setApiTokenBtn(e)
                },
                {
                    text: l('ApplicationForms:DownloadAPIConfiguration'),
                    id: 'GetApiTokenBtn',
                    action: (e, dt, node, config) => getApiTokenBtn(e)
                }
            ]
        },
        ...commonTableActionButtons(l('ApplicationForms'))
    ];

    let dataTable = Unity.DataTables.create('#ApplicationFormsTable', {
        listColumns,
        defaultVisibleColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 1,
        dataEndpoint: unity.grantManager.applicationForms.applicationForm.getList,
        data: {},
        responseCallback,
        customButtons: actionButtons,
        serverSideEnabled: false,
        pagingEnabled: true,
        reorderEnabled: false,
        languageSetValues: {},
        useNullPlaceholder: true,
        externalSearchId: 'search-forms',
        exportTitle: 'Application Forms'
    });

    createModal.onResult(function () {
        dataTable.ajax.reload();
    });

    updateModal.onResult(function () {
        dataTable.ajax.reload();
    });

    function createApplicationFormBtn(e) {
        e.preventDefault();
        createModal.open();
    };

    function setApiTokenBtn(e) {
        e.preventDefault();
        tokenModal.open();
    };

    function getApiTokenBtn(e) {
        e.preventDefault();
        let link = document.createElement("a");
        link.setAttribute('download', '');
        link.href = '/api/app/configurationFile/applicationforms';
        document.body.appendChild(link);
        link.click();
        link.remove();
    };
});


