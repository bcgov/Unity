﻿$(function () {
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
    let dt = $('#ApplicationFormsTable');
    const listColumns = [
        {
            title: l('Actions'),
            orderable: false,
            className: 'notexport text-center',
            data: null,
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
                            text: l('ApplicationForms:Mapping'),
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

    let dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 1,
        dataEndpoint: unity.grantManager.applicationForms.applicationForm.getList,
        data: {},
        responseCallback,
        actionButtons,
        pagingEnabled: true,
        reorderEnabled: false,
        languageSetValues: {},
        dataTableName: 'ApplicationFormsTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        externalSearchId: 'search-forms',
    });

    dataTable.on('search.dt', () => handleSearch());
    $('.dataTables_filter input').attr("placeholder", "Search");
    $('.dataTables_filter label')[0].childNodes[0].remove();

    $('#search').on('input', function () {
        dataTable.search($(this).val()).draw();
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

    function handleSearch() {
        let filter = $('.dataTables_filter input').val();
    }
});


