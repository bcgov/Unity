$(function () {
    const l = abp.localization.getResource('Notifications');
    const usersTableBody = $('#OnlineUsersTable tbody');
    const sendTarget = $('#SendTarget');
    const sendTenantFilterContainer = $('#sendTenantFilterContainer');
    const sendTenantFilter = $('#SendTenantFilter');
    const messageInput = $('#MessageText');
    const messageModeTabs = $('.rt-widget-mode-tab[data-message-mode]');
    const messagingSplitLayout = document.getElementById('messagingSplitLayout');
    const messagingUsersPanel = document.getElementById('messagingUsersPanel');
    const messagingComposePanel = document.getElementById('messagingComposePanel');
    const messagingSplitDivider = document.getElementById('messagingSplitDivider');
    let messageMode = 'individual';
    let availableTenants = [];
    const activityFilters = {
        dateFrom: null,
        dateTo: null,
        searchText: null
    };
    let tenantNamesById = {};

    initializeMessagingSplit();

    initializeActivityDateRange();
    const activityTable = initializeActivityTable();

    $('#activitySearch').on('input', function () {
        activityFilters.searchText = $(this).val() || null;
        activityTable.ajax.reload(null, true);
    });

    messageModeTabs.on('click', function () {
        setMessageMode($(this).data('message-mode'));
    });

    sendTenantFilter.on('change', function () {
        sendTarget.val('');
        renderSendTargetOptions();
    });

    let connection = null;
    let lastKnownUsers = [];
    const STATUS_GREEN_MS = 10 * 60 * 1000;
    const STATUS_ORANGE_MS = 30 * 60 * 1000;

    initializeRealtime().then(() => {
        loadOnlineUsers();
    });

    loadTenants();

    setInterval(() => renderUsers(lastKnownUsers), 30 * 1000);

    $('#SendButton').on('click', async function () {
        const target = sendTarget.val();
        const message = (messageInput.val() || '').trim();

        if (!target) {
            abp.notify.warn(l('RealtimeOps:SelectTarget'));
            return;
        }

        if (!message) {
            return;
        }

        const [kind, id] = splitTarget(target);

        try {
            if (kind === 'user') {
                await abp.ajax({
                    url: '/api/notifications/unity-messaging/message-user',
                    type: 'POST',
                    data: JSON.stringify({ targetUserId: id, message })
                });
            } else if (kind === 'tenant') {
                await abp.ajax({
                    url: '/api/notifications/unity-messaging/message-tenant',
                    type: 'POST',
                    data: JSON.stringify({ targetTenantId: id, message })
                });
            } else {
                return;
            }

            messageInput.val('');
        } catch (error) {
            if (window.abp && abp.notify && typeof abp.notify.error === 'function') {
                abp.notify.error(l('RealtimeOps:MessageSendFailed'));
            }
        }
    });

    usersTableBody.on('click', '.js-select-user', function () {
        setMessageMode('individual', $(this).data('tenant-id'));
        sendTarget.val(`user:${$(this).data('user-id')}`);
    });

    usersTableBody.on('click', '.js-select-tenant', function () {
        setMessageMode('tenant');
        sendTarget.val(`tenant:${$(this).data('tenant-id')}`);
    });

    function setMessageMode(mode, tenantId) {
        messageMode = mode === 'tenant' ? 'tenant' : 'individual';
        messageModeTabs.each(function () {
            const isActive = $(this).data('message-mode') === messageMode;
            $(this).toggleClass('active', isActive);
            $(this).attr('aria-selected', isActive ? 'true' : 'false');
        });

        sendTenantFilterContainer.toggle(messageMode === 'individual');
        if (messageMode === 'individual' && tenantId) {
            sendTenantFilter.val(tenantId);
        }

        renderSendTargetOptions();
    }

    function splitTarget(value) {
        const separatorIndex = value.indexOf(':');
        if (separatorIndex === -1) {
            return [null, null];
        }
        return [value.slice(0, separatorIndex), value.slice(separatorIndex + 1)];
    }

    function initializeMessagingSplit() {
        if (!messagingSplitLayout || !messagingUsersPanel || !messagingComposePanel || !messagingSplitDivider) {
            return;
        }

        const storageKey = 'UnityMessaging_SplitWidth';

        function applyWidth(value) {
            const percentage = Math.min(0.8, Math.max(0.2, value));
            messagingUsersPanel.style.flexBasis = `calc(${percentage * 100}% - 4px)`;
            messagingComposePanel.style.flexBasis = `calc(${(1 - percentage) * 100}% - 4px)`;
            localStorage.setItem(storageKey, String(percentage));
        }

        const savedWidth = Number.parseFloat(localStorage.getItem(storageKey));
        applyWidth(Number.isFinite(savedWidth) ? savedWidth : 0.5);

        let resizing = false;
        messagingSplitDivider.addEventListener('pointerdown', function (event) {
            event.preventDefault();
            resizing = true;
            messagingSplitDivider.setPointerCapture(event.pointerId);
        });

        messagingSplitDivider.addEventListener('pointermove', function (event) {
            if (!resizing) {
                return;
            }

            const bounds = messagingSplitLayout.getBoundingClientRect();
            const percentage = (event.clientX - bounds.left) / bounds.width;
            applyWidth(percentage);
        });

        messagingSplitDivider.addEventListener('pointerup', function (event) {
            resizing = false;
            messagingSplitDivider.releasePointerCapture(event.pointerId);
        });

        messagingSplitDivider.addEventListener('keydown', function (event) {
            if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') {
                return;
            }

            event.preventDefault();
            const current = Number.parseFloat(localStorage.getItem(storageKey)) || 0.5;
            applyWidth(current + (event.key === 'ArrowRight' ? 0.05 : -0.05));
        });
    }

    async function initializeRealtime() {
        if (typeof signalR === 'undefined') {
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/signalr/notifications')
            .withAutomaticReconnect()
            .build();

        connection.on('onlineUsersUpdated', function (users) {
            renderUsers(users || []);
        });

        connection.on('directMessageReceived', function (eventData) {
            activityTable.ajax.reload(null, false);
        });

        try {
            await connection.start();
        } catch {
        }
    }

    function initializeActivityTable() {
        return initializeDataTable({
            dt: $('#UnityMessagingActivityTable'),
            maxRowsPerPage: 20,
            defaultSortColumn: { name: 'creationTime', dir: 'desc' },
            dataEndpoint: unity.notifications.logs.notificationLogsRead.getList,
            data: function () {
                return {
                    dateFrom: activityFilters.dateFrom,
                    dateTo: activityFilters.dateTo,
                    notificationType: 1,
                    searchText: activityFilters.searchText
                };
            },
            listColumns: [
                {
                    title: l('RealtimeOps:Created'),
                    name: 'creationTime',
                    data: 'creationTime',
                    className: 'data-table-header text-nowrap',
                    render: function (data, type) {
                        return DateUtils.formatUtcDateToLocal(data, type, {
                            hour: '2-digit',
                            minute: '2-digit'
                        });
                    }
                },
                {
                    title: l('RealtimeOps:Direction'),
                    name: 'source',
                    data: 'source',
                    className: 'data-table-header text-nowrap',
                    render: function (data) {
                        return data === 'NotificationHub' || data === 'UnityMessagingController'
                            ? l('RealtimeOps:Sent')
                            : l('RealtimeOps:Received');
                    }
                },
                {
                    title: l('NotificationLogs:Message'),
                    name: 'message',
                    data: 'message',
                    className: 'data-table-header',
                    render: function (data) {
                        return $.fn.dataTable.render.text().display(data || '');
                    }
                },
                {
                    title: l('NotificationLogs:Tenant'),
                    name: 'tenantId',
                    data: 'tenantId',
                    className: 'data-table-header text-nowrap',
                    render: function (data) {
                        return $.fn.dataTable.render.text().display(
                            data ? (tenantNamesById[data] || data) : ''
                        );
                    }
                },
                {
                    title: l('NotificationLogs:User'),
                    name: 'userDisplayName',
                    data: 'userDisplayName',
                    className: 'data-table-header text-nowrap'
                }
            ],
            defaultVisibleColumns: ['creationTime', 'source', 'message', 'tenantId', 'userDisplayName'],
            actionButtons: [],
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            dataTableName: 'UnityMessagingActivityTable',
            useNullPlaceholder: true,
            fixedHeaders: true
        });
    }

    function initializeActivityDateRange() {
        const quickDateRange = $('#activityQuickDateRange');
        const customDateInputs = $('#activityCustomDateInputs');
        const fromDateInput = $('#activityFromDate');
        const toDateInput = $('#activityToDate');

        applyQuickDateRange('last6months');

        quickDateRange.on('change', function () {
            const range = $(this).val();
            if (range === 'custom') {
                customDateInputs.show();
                return;
            }

            customDateInputs.hide();
            applyQuickDateRange(range);
            activityTable.ajax.reload(null, true);
        });

        fromDateInput.add(toDateInput).on('change', function () {
            activityFilters.dateFrom = fromDateInput.val() || null;
            activityFilters.dateTo = toDateInput.val() || null;
            quickDateRange.val('custom');
            customDateInputs.show();
            activityTable.ajax.reload(null, true);
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

            activityFilters.dateFrom = from;
            activityFilters.dateTo = to;
            fromDateInput.val(from || '');
            toDateInput.val(to || '');
        }

        function addDays(date, days) {
            const result = new Date(date);
            result.setDate(result.getDate() + days);
            return result;
        }

        function formatDate(date) {
            return date.toISOString().slice(0, 10);
        }
    }

    async function loadTenants() {
        if (!window.unity || !unity.tenantManagement || !unity.tenantManagement.tenant) {
            return;
        }

        try {
            const result = await unity.tenantManagement.tenant.getList({
                maxResultCount: 1000
            });

            const tenants = (result && result.items ? result.items : [])
                .filter(t => !!t && !!t.id)
                .sort((a, b) => (a.name || '').localeCompare(b.name || ''));

            tenantNamesById = {};
            tenants.forEach(t => { tenantNamesById[t.id] = t.name || t.id; });
            availableTenants = tenants;

            renderTenantFilter();
            renderSendTargetOptions();
            renderUsers(lastKnownUsers);
            activityTable.ajax.reload(null, false);
        } catch {
            // Tenant list is a convenience for the dropdown and tenant-name display;
            // user-targeted messaging still works without it.
        }
    }

    function renderTenantFilter() {
        const currentValue = sendTenantFilter.val();

        sendTenantFilter.html(`
            <option value=""></option>
            ${availableTenants
                .map(t => `<option value="${sanitizeHtml(t.id)}">${sanitizeHtml(t.name || t.id)}</option>`)
                .join('')}
        `);

        if (currentValue && availableTenants.some(t => t.id === currentValue)) {
            sendTenantFilter.val(currentValue);
        }
    }

    function renderSendTargetOptions() {
        const currentValue = sendTarget.val();
        const selectedTenantId = sendTenantFilter.val();

        const options = messageMode === 'tenant'
            ? availableTenants
                .map(t => `<option value="tenant:${sanitizeHtml(t.id)}">${sanitizeHtml(t.name || t.id)}</option>`)
                .join('')
            : selectedTenantId
                ? lastKnownUsers
                .filter(u => !!u && !!u.userId && u.tenantId === selectedTenantId)
                .map(u => `<option value="user:${sanitizeHtml(u.userId)}">${sanitizeHtml(u.userName || u.userId)}</option>`)
                .join('')
                : '';

        sendTarget.html(`
            <option value=""></option>
            ${options}
        `);

        if (currentValue && sendTarget.find(`option[value="${currentValue}"]`).length) {
            sendTarget.val(currentValue);
        }
    }

    async function loadOnlineUsers() {
        try {
            const users = await abp.ajax({
                url: '/api/notifications/unity-messaging/online-users',
                type: 'GET'
            });

            renderUsers(users || []);
        } catch {
            abp.notify.error(l('RealtimeOps:LoadUsersFailed'));
        }
    }

    function renderUsers(users) {
        lastKnownUsers = Array.isArray(users) ? users : [];

        if (lastKnownUsers.length === 0) {
            usersTableBody.html(`<tr><td colspan="7">${l('RealtimeOps:NoUsers')}</td></tr>`);
        } else {
            const rows = lastKnownUsers
                .filter(u => !!u && !!u.userId)
                .map(function (u) {
                    const userName = sanitizeHtml(u.userName || '');
                    const userId = sanitizeHtml(u.userId || '');
                    const tenantId = u.tenantId || '';
                    const tenantName = sanitizeHtml(tenantId ? (tenantNamesById[tenantId] || tenantId) : '');
                    const connectionCount = Number.parseInt(u.connectionCount || 0, 10);
                    const status = getActivityStatus(u.lastActivityUtc);

                    return `<tr>
                        <td><span class="rt-status-dot rt-status-${status.className}" title="${sanitizeHtml(status.title)}"></span></td>
                        <td>${userName}</td>
                        <td class="text-monospace">${userId}</td>
                        <td>${tenantName}</td>
                        <td>${connectionCount}</td>
                        <td><button type="button" class="btn btn-sm btn-outline-primary js-select-user" data-user-id="${userId}" data-tenant-id="${sanitizeHtml(tenantId)}">${l('RealtimeOps:Message')}</button></td>
                        <td>${tenantId ? `<button type="button" class="btn btn-sm btn-outline-secondary js-select-tenant" data-tenant-id="${sanitizeHtml(tenantId)}">${l('RealtimeOps:SendToTenant')}</button>` : ''}</td>
                    </tr>`;
                })
                .join('');

            usersTableBody.html(rows);
        }

        renderSendTargetOptions();
    }

    function getActivityStatus(lastActivityUtc) {
        const lastActivity = lastActivityUtc ? new Date(lastActivityUtc) : null;
        const ageMs = lastActivity && !isNaN(lastActivity.getTime())
            ? Date.now() - lastActivity.getTime()
            : Number.POSITIVE_INFINITY;

        if (ageMs <= STATUS_GREEN_MS) {
            return { className: 'green', title: `${l('RealtimeOps:StatusActive')} (${lastActivity.toLocaleTimeString()})` };
        }

        if (ageMs <= STATUS_ORANGE_MS) {
            return { className: 'orange', title: `${l('RealtimeOps:StatusIdle')} (${lastActivity.toLocaleTimeString()})` };
        }

        return {
            className: 'red',
            title: lastActivity ? `${l('RealtimeOps:StatusAway')} (${lastActivity.toLocaleTimeString()})` : l('RealtimeOps:StatusAway')
        };
    }

    function sanitizeHtml(value) {
        return $('<div/>').text(value || '').html();
    }
});
