$(function () {
    const l = abp.localization.getResource('Notifications');
    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    let dt = $('#NotificationListTable');

    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'submissionReferenceNo',
        'applicantName',
        'sentDateTime',
        'status',
        'fromAddress',
        'toAddress',
        'subject',
        'recipient',
        'emailType'
    ];

    let actionButtons = [
        {
            text: l('Filter'),
            id: 'btn-toggle-filter',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) { },
            attr: { id: 'btn-toggle-filter' }
        },
        {
            extend: 'csvNoPlaceholder',
            text: l('Export'),
            title: l('Menu:Notifications'),
            className: 'custom-table-btn flex-none btn btn-secondary'
        }
    ];

    initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 15,
        defaultSortColumn: { name: 'sentDateTime', dir: 'desc' },
        dataEndpoint: unity.grantManager.notifications.notificationList.getList,
        data: {},
        responseCallback,
        actionButtons,
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues: {},
        dataTableName: 'NotificationListTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        fixedHeaders: true
    });

    function getColumns() {
        let columnIndex = 0;
        const columns = [
            getNotificationIdColumn(columnIndex++),
            getSubmissionIdColumn(columnIndex++),
            getTextColumn(columnIndex++, l('NotificationList:ApplicantName'), 'applicantName'),
            getSentDateColumn(columnIndex++),
            getTextColumn(columnIndex++, l('NotificationList:Status'), 'status', { width: '7rem', className: 'data-table-header text-nowrap' }),
            getTextColumn(columnIndex++, l('NotificationList:From'), 'fromAddress'),
            getTextColumn(columnIndex++, l('NotificationList:To'), 'toAddress'),
            getTextColumn(columnIndex++, l('NotificationList:Subject'), 'subject'),
            getTextColumn(columnIndex++, l('NotificationList:Recipient'), 'recipient', { width: '8rem', className: 'data-table-header text-nowrap' }),
            getTextColumn(columnIndex++, l('NotificationList:EmailType'), 'emailType', { width: '9rem', className: 'data-table-header text-nowrap' })
        ];
        return columns.map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }));
    }

    function getNotificationIdColumn(columnIndex) {
        return {
            title: l('NotificationList:NotificationId'),
            name: 'id',
            data: 'id',
            className: 'data-table-header',
            index: columnIndex,
            visible: false
        };
    }

    function getSubmissionIdColumn(columnIndex) {
        return {
            title: l('NotificationList:SubmissionId'),
            name: 'submissionReferenceNo',
            data: 'submissionReferenceNo',
            className: 'data-table-header text-nowrap',
            width: '8rem',
            index: columnIndex,
            render: function (data, type, row) {
                if (type !== 'display') {
                    return data || '';
                }
                const safe = $.fn.dataTable.render.text().display(data || '');
                const appId = row.applicationId;
                if (data && appId && guidPattern.test(appId)) {
                    return `<a href="/GrantApplications/Details?ApplicationId=${encodeURIComponent(appId)}">${safe}</a>`;
                }
                return safe || null;
            }
        };
    }

    function getSentDateColumn(columnIndex) {
        return {
            title: l('NotificationList:SentDate'),
            name: 'sentDateTime',
            data: 'sentDateTime',
            className: 'data-table-header text-nowrap',
            width: '8rem',
            index: columnIndex,
            render: function (data, type) {
                return DateUtils.formatUtcDateToLocal(data, type);
            }
        };
    }
});

function responseCallback(result) {
    return {
        recordsTotal: result.totalCount,
        recordsFiltered: result.totalCount,
        data: result.items
    };
}

function getTextColumn(columnIndex, title, dataField, options = {}) {
    return {
        title: title,
        name: dataField,
        data: dataField,
        className: options.className ?? 'data-table-header',
        ...(options.width ? { width: options.width } : {}),
        index: columnIndex
    };
}
