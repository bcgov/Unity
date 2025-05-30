﻿$(function () {
    let dt = $('#AuditHistoryTable');
    let dataTable;

    const listColumns = getColumns();
    let actionButtons = [...commonTableActionButtons('Payment History')];

    let responseCallback = function (result) {
        if (result + "" == "undefined") {
            return {
                recordsTotal: 0,
                recordsFiltered: 0,
                data: {}
            };
        }
        return {
            recordsTotal: result.length,
            recordsFiltered: result.length,
            data: formatItems(result)
        };
    };

    let inputAction = function () {
        return document.getElementById('paymentId').value
    };

    dataTable = initializeDataTable({
        dt,
        listColumns,
        maxRowsPerPage: 20,
        defaultSortColumn: 0,
        dataEndpoint: unity.grantManager.history.paymentHistory.getPaymentHistoryList,
        data: inputAction,
        responseCallback,
        actionButtons,
        serverSideEnabled: false,
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues: {},
        dataTableName: 'AuditHistoryTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        externalSearchId: 'search-payment-history',
    });

    function getColumns() {
        return [
            getEntityNameColumn(),
            getPropertyNameColumn(),
            getOriginalValueColumn(),
            getNewValueColumn(),
            getChangeTimeColumn(),
            getUserNameColumn()
        ].map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }));
    }

    function getEntityNameColumn() {
        return {
            title: 'Entity Name',
            data: 'entityName',
            name: 'entityName',
            className: 'data-table-header',
            index: 0
        }
    }

    function getPropertyNameColumn() {
        return {
            title: 'Property Name',
            data: 'propertyName',
            name: 'propertyName',
            className: 'data-table-header',
            index: 1
        }
    }

    function getOriginalValueColumn() {
        return {
            title: 'Original Value',
            data: 'originalValue',
            name: 'originalValue',
            className: 'data-table-header',
            index: 2
        }
    }

    function getNewValueColumn() {
        return {
            title: 'New Value',
            data: 'newValue',
            name: 'newValue',
            className: 'data-table-header',
            index: 3
        }
    }

    function getChangeTimeColumn() {
        return {
            title: 'Modified DateTime',
            data: 'changeTime',
            name: 'changeTime',
            className: 'data-table-header',
            index: 4,
            render: function (data) {
                return formatLuxonDate(data);
            }
        }
    }
    
    function getUserNameColumn() {
        return {
            title: 'User Name',
            data: 'userName',
            name: 'userName',
            className: 'data-table-header',
            index: 5
        }
    }

    function formatLuxonDate(data) {
        return data != null ? luxon.DateTime.fromISO(data, {
            locale: abp.localization.currentCulture.name,
        }).toLocaleString({
            day: "numeric",
            year: "numeric",
            month: "numeric",
            hour: "numeric",
            minute: "numeric",
            second: "numeric"
        }) : '';
    }
    let formatItems = function (items) {
        const newData = items.map((item, index) => {
            return {
                ...item,
                rowCount: index
            };
        });
        return newData;
    }

    window.addEventListener('resize', () => {
    });

    PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload(null, false);
            $(".select-all-applications").prop("checked", false);
            PubSub.publish('clear_selected_application');
        }
    );
});
