(function () {
    if (globalThis.__unityRealtimeWidgetInitialized) {
        return;
    }

    globalThis.__unityRealtimeWidgetInitialized = true;

    whenReady(init);

    function whenReady(callback) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', callback);
        } else {
            callback();
        }
    }

    function init() {
        if (/\/(account\/login|login|splash)(?:\/|$)/i.test(window.location.pathname)) {
            return;
        }

        if (window.location.pathname === '/') {
            return;
        }

        if (abp.features && typeof abp.features.isEnabled === 'function'
            && !abp.features.isEnabled('Unity.Notifications.DirectMessaging')) {
            return;
        }

        if (typeof signalR === 'undefined') {
            return;
        }

        if (!window.abp?.currentUser?.isAuthenticated) {
            return;
        }

        const l = window.abp?.localization?.getResource?.('Notifications')
            || function (key) { return key; };

        const myUserId = abp.currentUser.id || null;

        let unreadCount = 0;
        let panelOpen = false;
        let peers = [];
        let activeMode = 'individual';
        let currentTenant = null;
        const histories = {};
        const MAX_HISTORY_ITEMS = 100;
        const STATUS_GREEN_MS = 10 * 60 * 1000;
        const STATUS_ORANGE_MS = 30 * 60 * 1000;
        const widget = buildWidget();

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/signalr/notifications')
            .withAutomaticReconnect()
            .build();

        connection.on('directMessageReceived', function (eventData) {
            const scope = eventData?.scope || 'user';
            const sender = eventData?.senderName || eventData?.senderId || 'unknown';
            const senderId = eventData?.senderId;
            const message = eventData?.message || '';
            const targetId = scope === 'tenant'
                ? eventData?.tenantId || currentTenant?.id
                : senderId;

            if (!targetId) {
                return;
            }

            const mode = scope === 'tenant' ? 'tenant' : 'individual';
            addMessage(mode, targetId, sender, senderId, message, eventData?.timestamp);

            if (scope === 'user') {
                activeMode = 'individual';
                ensurePeerOption(senderId, sender);
            } else {
                activeMode = 'tenant';
            }

            widget.modeTabs.forEach(tab => tab.classList.toggle('active', tab.dataset.mode === activeMode));
            renderTargetOptions();
            widget.target.value = targetId;
            renderConversation();

            if (eventData.source === 'UnityMessagingController' && senderId && senderId !== myUserId) {
                showIncomingToast(sender, message);
            }

        });

        connection.on('tenantPresenceUpdated', function (result) {
            applyPeers(result);
        });

        const HEARTBEAT_INTERVAL_MS = 5 * 60 * 1000;
        const ACTIVITY_HEARTBEAT_THROTTLE_MS = 60 * 1000;
        const ACTIVITY_EVENTS = ['click', 'keydown', 'mousemove', 'scroll'];
        let lastHeartbeatSentAt = 0;

        connection.start().then(function () {
            sendHeartbeat(true);
            refreshPeers();
            refreshTenant();
            refreshUnreadMessages();
            setInterval(function () { sendHeartbeat(true); }, HEARTBEAT_INTERVAL_MS);

            ACTIVITY_EVENTS.forEach(function (eventName) {
                document.addEventListener(eventName, function () { sendHeartbeat(false); }, { passive: true });
            });
        }).catch(function () {
            // Keep normal page flow even when realtime connection is unavailable.
        });

        function refreshUnreadMessages() {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            connection.invoke('GetUnreadMessagesAsync').then(function (messages) {
                (Array.isArray(messages) ? messages : []).forEach(function (eventData) {
                    const scope = eventData.scope === 'tenant' ? 'tenant' : 'user';
                    const senderId = eventData.senderId || null;
                    const targetId = scope === 'tenant' ? eventData.targetId : senderId;

                    if (!targetId) {
                        return;
                    }

                    addMessage(
                        scope === 'tenant' ? 'tenant' : 'individual',
                        targetId,
                        eventData.senderName || senderId || 'unknown',
                        senderId,
                        eventData.message || '',
                        eventData.timestamp
                    );

                    if (scope === 'user') {
                        ensurePeerOption(senderId, eventData.senderName || senderId);
                    }

                    if (eventData.source === 'UnityMessagingController' && senderId && senderId !== myUserId) {
                        showIncomingToast(eventData.senderName || senderId, eventData.message || '');
                    }
                });
            }).catch(function () {
                // Unread history is a convenience; realtime delivery remains available.
            });
        }

        function sendHeartbeat(force) {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            const now = Date.now();
            if (!force && now - lastHeartbeatSentAt < ACTIVITY_HEARTBEAT_THROTTLE_MS) {
                return;
            }

            lastHeartbeatSentAt = now;
            connection.invoke('HeartbeatAsync').catch(function () {
                // Ignore heartbeat failures; presence will simply age out.
            });
        }

        function refreshPeers() {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            connection.invoke('GetTenantUsersAsync').then(function (result) {
                applyPeers(result);
            }).catch(function () {
                // Peer list is a convenience for the compose "To" selector.
            });
        }

        function applyPeers(result) {
            peers = (Array.isArray(result) ? result : []).filter(function (p) {
                return p?.userId && p.userId !== myUserId;
            });
            renderTargetOptions();
        }

        function refreshTenant() {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            connection.invoke('GetCurrentTenantAsync').then(function (result) {
                currentTenant = result || null;
                renderTargetOptions();
            }).catch(function () {
                currentTenant = null;
            });
        }

        function renderTargetOptions() {
            const currentValue = widget.target.value;

            if (activeMode === 'tenant') {
                widget.targetControl.style.display = 'none';
                widget.target.innerHTML = currentTenant
                    ? `<option value="${escapeAttribute(currentTenant.id)}">${escapeHtml(currentTenant.name || currentTenant.id)}</option>`
                    : `<option value="">${l('RealtimeWidget:NoTenant')}</option>`;
                widget.target.disabled = !currentTenant;
                if (currentTenant) {
                    widget.target.value = currentTenant.id;
                }
                updateTargetStatusDot();
                renderConversation();
                return;
            }

            widget.targetControl.style.display = '';

            if (peers.length === 0) {
                widget.target.innerHTML = `<option value="">${l('RealtimeWidget:NoOnlineUsers')}</option>`;
                widget.target.disabled = true;
                updateTargetStatusDot();
                return;
            }

            widget.target.disabled = false;
            widget.target.innerHTML = `<option value="">${l('RealtimeWidget:To')}</option>` + peers
                .map(function (p) {
                    const suffix = p.isOnline ? '' : ` (${l('RealtimeWidget:StatusOffline')})`;
                    return `<option value="${escapeAttribute(p.userId)}">${escapeHtml(p.userName || p.userId)}${suffix}</option>`;
                })
                .join('');

            if (currentValue && peers.some(function (p) { return p.userId === currentValue; })) {
                widget.target.value = currentValue;
            }

            updateTargetStatusDot();
            renderConversation();
        }

        function getPeerStatus(peer) {
            if (!peer?.isOnline) {
                return { className: 'offline', label: l('RealtimeWidget:StatusOffline') };
            }

            const lastActivity = peer.lastActivityUtc ? new Date(peer.lastActivityUtc) : null;
            const ageMs = lastActivity && !Number.isNaN(lastActivity.getTime())
                ? Date.now() - lastActivity.getTime()
                : Number.POSITIVE_INFINITY;

            if (ageMs <= STATUS_GREEN_MS) {
                return { className: 'green', label: l('RealtimeOps:StatusActive') };
            }

            if (ageMs <= STATUS_ORANGE_MS) {
                return { className: 'orange', label: l('RealtimeOps:StatusIdle') };
            }

            return { className: 'red', label: l('RealtimeOps:StatusAway') };
        }

        function updateTargetStatusDot() {
            if (activeMode === 'tenant') {
                widget.targetStatus.className = 'rt-status-dot rt-widget-target-status rt-status-hidden';
                return;
            }

            const peer = peers.find(function (p) { return p.userId === widget.target.value; });
            const status = getPeerStatus(peer);
            widget.targetStatus.className = `rt-status-dot rt-widget-target-status rt-status-${status.className}`;
            widget.targetStatus.title = status.label;
        }

        widget.composeSend.addEventListener('click', sendComposeMessage);
        widget.target.addEventListener('change', function () {
            updateTargetStatusDot();
            renderConversation();
        });
        widget.modeTabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                activeMode = tab.dataset.mode;
                widget.modeTabs.forEach(item => item.classList.toggle('active', item === tab));
                renderTargetOptions();
            });
        });
        widget.composeInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendComposeMessage();
            }
        });

        function sendComposeMessage() {
            const targetId = widget.target.value;
            const message = widget.composeInput.value.trim();

            if (!targetId || !message) {
                return;
            }

            if (message.length > 4000) {
                return;
            }

            if (connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            const sendTask = activeMode === 'tenant'
                ? connection.invoke('SendTenantMessageAsync', message)
                : connection.invoke('SendPeerMessageAsync', targetId, message);

            sendTask.then(function () {
                if (activeMode !== 'tenant') {
                    addMessage(activeMode, targetId, l('RealtimeWidget:You'), null, message, new Date().toISOString());
                }
                widget.composeInput.value = '';
            }).catch(function () {
                // Recipient is invalid or outside the tenant; leave the draft in place.
            });
        }

        function addMessage(mode, targetId, sender, senderId, message, timestamp) {
            const key = `${mode}:${targetId}`;
            histories[key] = histories[key] || [];
            histories[key].push({ sender, senderId, message, timestamp, mode });
            histories[key] = histories[key].slice(-MAX_HISTORY_ITEMS);

            if (!panelOpen) {
                unreadCount += 1;
                updateBadge();
                widget.bubble.classList.remove('rt-widget-bounce');
                widget.bubble.getBoundingClientRect();
                widget.bubble.classList.add('rt-widget-bounce');
            }

            renderConversation();
        }

        function ensurePeerOption(userId, userName) {
            if (!userId) {
                return;
            }

            const existing = peers.find(peer => peer.userId === userId);

            if (existing) {
                existing.isOnline = true;
                existing.lastActivityUtc = new Date().toISOString();
            } else {
                peers.push({ userId, userName, isOnline: true, connectionCount: 1, lastActivityUtc: new Date().toISOString() });
            }
        }

        function renderConversation() {
            const targetId = widget.target.value;
            const items = targetId ? histories[`${activeMode}:${targetId}`] || [] : [];
            widget.list.innerHTML = '';

            if (items.length === 0) {
                const empty = document.createElement('div');
                empty.className = 'rt-widget-empty';
                empty.textContent = l('RealtimeWidget:Empty');
                widget.list.appendChild(empty);
                return;
            }

            items.forEach(function (entry) {
                const item = document.createElement('div');
                item.className = 'rt-widget-message' + (entry.mode === 'tenant' ? ' rt-widget-message-alert' : '');

                const meta = document.createElement('div');
                meta.className = 'rt-widget-message-meta';
                const senderEl = document.createElement('span');
                senderEl.className = 'rt-widget-message-sender';
                senderEl.textContent = entry.mode === 'tenant'
                    ? `${entry.sender} (${l('RealtimeWidget:TenantBroadcast')})`
                    : entry.sender;
                const timeEl = document.createElement('span');
                timeEl.className = 'rt-widget-message-time';
                timeEl.textContent = formatTime(entry.timestamp);
                meta.appendChild(senderEl);
                meta.appendChild(timeEl);

                const textEl = document.createElement('div');
                textEl.className = 'rt-widget-message-text';
                textEl.textContent = entry.message;
                item.appendChild(meta);
                item.appendChild(textEl);
                widget.list.appendChild(item);
            });

            widget.list.scrollTop = widget.list.scrollHeight;
        }

        function formatTime(value) {
            const date = value ? new Date(value) : new Date();
            return Number.isNaN(date.getTime()) ? '' : date.toLocaleTimeString();
        }

        function updateBadge() {
            if (unreadCount > 0) {
                widget.badge.textContent = unreadCount > 9 ? '9+' : String(unreadCount);
                widget.badge.classList.remove('rt-widget-hidden');
            } else {
                widget.badge.classList.add('rt-widget-hidden');
            }
        }

        function showIncomingToast(sender, message) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'info',
                    titleText: String(sender || ''),
                    text: message,
                    showConfirmButton: false,
                    timer: 5000,
                    timerProgressBar: true
                });
                return;
            }

            if (window.abp?.notify?.info && typeof window.abp.notify.info === 'function') {
                abp.notify.info(`${sender}: ${message}`);
            }
        }

        function togglePanel() {
            panelOpen = !panelOpen;
            widget.panel.classList.toggle('rt-widget-panel-open', panelOpen);
            widget.bubble.classList.toggle('rt-widget-hidden', panelOpen);

            if (panelOpen) {
                unreadCount = 0;
                updateBadge();
                connection.invoke('MarkMessagesReadAsync').catch(function () {
                    // Ignore read-state failures; messages remain available in the activity log.
                });
                refreshPeers();
                refreshTenant();
            }
        }

        function escapeHtml(value) {
            const div = document.createElement('div');
            div.textContent = value || '';
            return div.innerHTML;
        }

        function escapeAttribute(value) {
            return escapeHtml(value).replaceAll('"', '&quot;');
        }

        function buildWidget() {
            const container = document.createElement('div');
            container.className = 'rt-widget-container';

            const panel = document.createElement('div');
            panel.className = 'rt-widget-panel';

            const header = document.createElement('div');
            header.className = 'rt-widget-panel-header';

            const titleEl = document.createElement('span');
            titleEl.textContent = l('RealtimeWidget:Title');

            const closeBtn = document.createElement('button');
            closeBtn.type = 'button';
            closeBtn.className = 'rt-widget-close';
            closeBtn.setAttribute('aria-label', 'Close');
            closeBtn.innerHTML = '&times;';
            closeBtn.addEventListener('click', togglePanel);

            header.appendChild(titleEl);
            header.appendChild(closeBtn);

            const list = document.createElement('div');
            list.className = 'rt-widget-list';

            const empty = document.createElement('div');
            empty.className = 'rt-widget-empty';
            empty.textContent = l('RealtimeWidget:Empty');
            list.appendChild(empty);

            const compose = document.createElement('div');
            compose.className = 'rt-widget-compose';

            const modeTabs = document.createElement('div');
            modeTabs.className = 'rt-widget-mode-tabs';

            const individualTab = document.createElement('button');
            individualTab.type = 'button';
            individualTab.className = 'rt-widget-mode-tab active';
            individualTab.dataset.mode = 'individual';
            individualTab.textContent = l('RealtimeWidget:Individual');

            const tenantTab = document.createElement('button');
            tenantTab.type = 'button';
            tenantTab.className = 'rt-widget-mode-tab';
            tenantTab.dataset.mode = 'tenant';
            tenantTab.textContent = l('RealtimeWidget:Tenant');

            modeTabs.appendChild(individualTab);
            modeTabs.appendChild(tenantTab);

            const targetRow = document.createElement('div');
            targetRow.className = 'rt-widget-target-row';

            const targetControl = document.createElement('div');
            targetControl.className = 'rt-widget-target-control';

            const targetStatus = document.createElement('span');
            targetStatus.className = 'rt-status-dot rt-widget-target-status rt-status-hidden';

            const target = document.createElement('select');
            target.className = 'rt-widget-compose-target';
            target.innerHTML = `<option value="">${l('RealtimeWidget:To')}</option>`;

            targetControl.appendChild(targetStatus);
            targetControl.appendChild(target);
            targetRow.appendChild(targetControl);

            const composeRow = document.createElement('div');
            composeRow.className = 'rt-widget-compose-row';

            const composeInput = document.createElement('input');
            composeInput.type = 'text';
            composeInput.className = 'rt-widget-compose-input';
            composeInput.placeholder = l('RealtimeWidget:MessagePlaceholder');

            const composeSend = document.createElement('button');
            composeSend.type = 'button';
            composeSend.className = 'rt-widget-compose-send';
            composeSend.textContent = l('RealtimeWidget:Send');

            composeRow.appendChild(composeInput);
            composeRow.appendChild(composeSend);
            compose.appendChild(modeTabs);
            compose.appendChild(targetRow);
            compose.appendChild(composeRow);

            panel.appendChild(header);
            panel.appendChild(list);
            panel.appendChild(compose);

            const bubble = document.createElement('button');
            bubble.type = 'button';
            bubble.className = 'rt-widget-bubble';
            bubble.setAttribute('aria-label', l('RealtimeWidget:Title'));
            bubble.innerHTML = '<svg viewBox="0 0 24 24" width="24" height="24" fill="currentColor" aria-hidden="true">'
                + '<path d="M12 3C6.48 3 2 6.94 2 11.5c0 2.34 1.15 4.44 3.02 5.94L4 21l4.13-1.65A11.6 11.6 0 0 0 12 20c5.52 0 10-3.94 10-8.5S17.52 3 12 3z"/>'
                + '</svg>';
            bubble.addEventListener('click', togglePanel);

            const badge = document.createElement('span');
            badge.className = 'rt-widget-badge rt-widget-hidden';
            bubble.appendChild(badge);

            container.appendChild(panel);
            container.appendChild(bubble);
            document.body.appendChild(container);

            return { container, panel, list, bubble, badge, modeTabs: [individualTab, tenantTab], targetControl, target, targetStatus, composeInput, composeSend };
        }
    }
})();
