$(function () {
    const l = abp.localization.getResource('Notifications');
    const notificationsService = unity.notifications.logs.notificationLogsRead;

    const filters = {
        dateFrom: null,
        dateTo: null,
        notificationType: null,
        severity: null,
        channel: null,
        tenantId: null,
        userId: null,
        searchText: null
    };

    initializeTypeFilter();
    initializeDateRange();

    const dt = $('#NotificationLogsTable');
    const dataTable = initializeDataTable({
        dt,
        maxRowsPerPage: 20,
        defaultSortColumn: { name: 'creationTime', dir: 'desc' },
        dataEndpoint: notificationsService.getList,
        data: function () {
            return {
                dateFrom: filters.dateFrom,
                dateTo: filters.dateTo,
                notificationType: filters.notificationType,
                severity: filters.severity,
                channel: filters.channel,
                tenantId: filters.tenantId,
                userId: filters.userId,
                searchText: filters.searchText
            };
        },
        listColumns: getColumns(),
        defaultVisibleColumns: [
            'creationTime',
            'notificationType',
            'severity',
            'channel',
            'title',
            'message',
            'tenantId',
            'userId',
            'source',
            'correlationId',
            'isDeliveredRealtime'
        ],
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues: {},
        dataTableName: 'NotificationLogsTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        fixedHeaders: true
    });

    $('#search').on('input', function () {
        filters.searchText = $(this).val() || null;
        dataTable.ajax.reload(null, true);
    });

    $('#typeFilter').on('change', function () {
        filters.notificationType = normalizeEnumFilter($(this).val());
        dataTable.ajax.reload(null, true);
    });

    $('#severityFilter').on('change', function () {
        filters.severity = normalizeEnumFilter($(this).val());
        dataTable.ajax.reload(null, true);
    });

    $('#channelFilter').on('change', function () {
        filters.channel = normalizeEnumFilter($(this).val());
        dataTable.ajax.reload(null, true);
    });

    $('#tenantFilter').on('input', function () {
        filters.tenantId = normalizeGuidFilter($(this).val());
        dataTable.ajax.reload(null, true);
    });

    $('#userFilter').on('input', function () {
        filters.userId = normalizeGuidFilter($(this).val());
        dataTable.ajax.reload(null, true);
    });

    $('#NotificationLogsTable').on('click', '.js-view-details', async function () {
        const id = $(this).data('id');

        try {
            const detail = await notificationsService.get(id);
            const payload = detail.payloadJson ? `\n\nPayload:\n${detail.payloadJson}` : '';

            await abp.message.info(
                `${detail.message || ''}${payload}`,
                `${detail.notificationType} | ${detail.severity}`
            );
        } catch (error) {
            console.error(error);
            abp.notify.error(l('NotificationLogs:DetailsLoadFailed'));
        }
    });

    connectRealtime(dataTable);

    function initializeTypeFilter() {
        const typeFilter = $('#typeFilter');
        const values = [
            { value: 0, text: 'SignalRSystemNotification' },
            { value: 1, text: 'SignalRDirectMessage' },
            { value: 2, text: 'SignalRGroupNotification' },
            { value: 3, text: 'DbException' },
            { value: 4, text: 'AbpHandledException' },
            { value: 5, text: 'MiddlewareUnhandledException' },
            { value: 6, text: 'PrometheusErrorCounterEvent' },
            { value: 7, text: 'PrometheusExceptionCounterEvent' },
            { value: 8, text: 'LegacyBridgeEvent' },
            { value: 9, text: 'UnityException' }
        ];

        values.forEach(function (item) {
            typeFilter.append(`<option value="${item.value}">${item.text}</option>`);
        });
    }

    function initializeDateRange() {
        const quickDateRange = $('#quickDateRange');
        const customDateInputs = $('#customDateInputs');
        const fromDateInput = $('#fromDate');
        const toDateInput = $('#toDate');

        applyQuickDateRange('last6months');

        quickDateRange.on('change', function () {
            const range = $(this).val();
            if (range === 'custom') {
                customDateInputs.show();
                return;
            }

            customDateInputs.hide();
            applyQuickDateRange(range);
            dataTable.ajax.reload(null, true);
        });

        fromDateInput.on('change', function () {
            filters.dateFrom = $(this).val() || null;
            quickDateRange.val('custom');
            customDateInputs.show();
            dataTable.ajax.reload(null, true);
        });

        toDateInput.on('change', function () {
            filters.dateTo = $(this).val() || null;
            quickDateRange.val('custom');
            customDateInputs.show();
            dataTable.ajax.reload(null, true);
        });

        function applyQuickDateRange(range) {
            const today = new Date();
            const to = formatDate(today);
            let from = null;

            switch (range) {
                case 'today':
                    from = to;
                    break;
                case 'last7days':
                    from = formatDate(addDays(today, -7));
                    break;
                case 'last30days':
                    from = formatDate(addDays(today, -30));
                    break;
                case 'last3months':
                    from = formatDate(addDays(today, -90));
                    break;
                case 'last6months':
                    from = formatDate(addDays(today, -180));
                    break;
                case 'alltime':
                    break;
            }

            filters.dateFrom = from;
            filters.dateTo = to;

            fromDateInput.val(from || '');
            toDateInput.val(to || '');
        }
    }

    function getColumns() {
        return [
            {
                title: l('NotificationLogs:Created'),
                name: 'creationTime',
                data: 'creationTime',
                className: 'data-table-header text-nowrap',
                render: function (data, type) {
                    return DateUtils.formatUtcDateToLocal(data, type);
                }
            },
            {
                title: l('NotificationLogs:Type'),
                name: 'notificationType',
                data: 'notificationType',
                className: 'data-table-header text-nowrap'
            },
            {
                title: l('NotificationLogs:Severity'),
                name: 'severity',
                data: 'severity',
                className: 'data-table-header text-nowrap'
            },
            {
                title: l('NotificationLogs:Channel'),
                name: 'channel',
                data: 'channel',
                className: 'data-table-header text-nowrap'
            },
            {
                title: l('NotificationLogs:Title'),
                name: 'title',
                data: 'title',
                className: 'data-table-header'
            },
            {
                title: l('NotificationLogs:Message'),
                name: 'message',
                data: 'message',
                className: 'data-table-header',
                render: function (data) {
                    const safe = $.fn.dataTable.render.text().display(data || '');
                    return safe.length > 160 ? `${safe.substring(0, 160)}...` : safe;
                }
            },
            {
                title: l('NotificationLogs:Tenant'),
                name: 'tenantId',
                data: 'tenantId',
                className: 'data-table-header text-nowrap'
            },
            {
                title: l('NotificationLogs:User'),
                name: 'userId',
                data: 'userId',
                className: 'data-table-header text-nowrap'
            },
            {
                title: l('NotificationLogs:Source'),
                name: 'source',
                data: 'source',
                className: 'data-table-header text-nowrap'
            },
            {
                title: l('NotificationLogs:CorrelationId'),
                name: 'correlationId',
                data: 'correlationId',
                className: 'data-table-header text-nowrap'
            },
            {
                title: l('NotificationLogs:RealtimeDelivered'),
                name: 'isDeliveredRealtime',
                data: 'isDeliveredRealtime',
                className: 'data-table-header text-nowrap',
                render: function (data) {
                    return data ? l('NotificationLogs:Yes') : l('NotificationLogs:No');
                }
            },
            {
                title: l('NotificationLogs:Actions'),
                name: 'id',
                data: 'id',
                className: 'data-table-header text-nowrap',
                render: function (data, type) {
                    if (type !== 'display') {
                        return data;
                    }

                    const safeId = $.fn.dataTable.render.text().display(data || '');
                    return `<button type="button" class="btn btn-secondary btn-sm js-view-details" data-id="${safeId}">${l('NotificationLogs:ViewDetails')}</button>`;
                }
            }
        ];
    }

});

async function connectRealtime(table) {
    if (typeof signalR === 'undefined') {
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/signalr/notifications')
        .withAutomaticReconnect()
        .build();

    connection.on('notificationLogCreated', function () {
        table.ajax.reload(null, false);
    });

    try {
        await connection.start();
    } catch {
        // Keep the page usable when realtime connection is temporarily unavailable.
    }
}

function normalizeEnumFilter(value) {
    if (value === null || value === undefined || value === '') {
        return null;
    }

    return Number.parseInt(value, 10);
}

function normalizeGuidFilter(value) {
    const input = (value || '').trim();

    if (!input) {
        return null;
    }

    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    return guidPattern.test(input) ? input : null;
}

function addDays(date, days) {
    const copy = new Date(date);
    copy.setDate(copy.getDate() + days);
    return copy;
}

function formatDate(date) {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
}
