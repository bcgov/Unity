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

    function shouldInit() {
        shouldInit.depth = (shouldInit.depth || 0) + 1;

        try {
            if (shouldInit.depth > 10) {
                console.error('RECURSIVE shouldInit DETECTED');
                console.trace();
                return false;
            }

            const path = window.location.pathname.toLowerCase();
            const guardedPaths = ['/account/login', '/login', '/splash'];

            for (const guardedPath of guardedPaths) {
                if (path.includes(guardedPath)) {
                    return false;
                }
            }

            if (path === '/') {
                return false;
            }

            if (!window.abp) {
                return false;
            }

            if (!window.abp?.currentUser?.isAuthenticated) {
                return false;
            }

            if (!window.signalR) {
                return false;
            }

            return true;
        } finally {
            shouldInit.depth--;
        }
    }

    function init() {
        if (!shouldInit()) {
            return;
        }

        fetch('/api/notifications/realtime/feature-enabled', { credentials: 'same-origin' })
            .then(function (response) {
                return response.ok ? response.json() : false;
            })
            .then(function (isEnabled) {
                if (isEnabled) {
                    initializeWidget();
                }
            })
            .catch(function () {
                // Do not show or connect the widget when feature state cannot be confirmed.
            });
    }

    function initializeWidget() {

        const l = window.abp?.localization?.getResource?.('Notifications')
            || function (key) { return key; };

        const myUserId = abp.currentUser.id || null;

        let unreadCount = 0;
        let panelOpen = false;
        let peers = [];
        let activeMode = 'individual';
        let currentTenant = null;
        const selectedTargets = { individual: '', tenant: '' };
        const modeNotificationCounts = { individual: 0, tenant: 0 };
        let targetSelect2Open = false;
        let targetOptionsRefreshPending = false;
        const histories = {};
        const MAX_HISTORY_ITEMS = 100;
        const STATUS_GREEN_MS = 10 * 60 * 1000;
        const STATUS_ORANGE_MS = 30 * 60 * 1000;
        const BUBBLE_POSITION_STORAGE_KEY = 'unity.notifications.realtime.bubble-position';
        const PANEL_SIZE_STORAGE_KEY = 'unity.notifications.realtime.panel-size';
        const PANEL_POSITION_STORAGE_KEY = 'unity.notifications.realtime.panel-position';
        const widget = buildWidget();
        setupBubbleDragging();
        setupPanelResizing();
        setupPanelDragging();
        setupTargetSelect2();

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/signalr/notifications')
            .withAutomaticReconnect()
            .build();
        let connectionStartTask = null;

        function startConnection() {
            if (connection.state === signalR.HubConnectionState.Connected) {
                return Promise.resolve();
            }

            if (!connectionStartTask) {
                connectionStartTask = connection.start().finally(function () {
                    connectionStartTask = null;
                });
            }

            return connectionStartTask;
        }

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
                ensurePeerOption(senderId, sender);
            }

            if (senderId !== myUserId) {
                modeNotificationCounts[mode] += 1;
                updateModeTabCounts();
            }

            if (eventData.source === 'UnityMessagingController' && senderId && senderId !== myUserId) {
                showIncomingToast(sender, message);
            }

        });

        connection.on('tenantPresenceUpdated', function (result) {
            applyPeers(result);
        });

        const HEARTBEAT_INTERVAL_MS = 5 * 60 * 1000;
        const ACTIVITY_HEARTBEAT_THROTTLE_MS = 60 * 1000;
        const ACTIVITY_EVENTS = ['click', 'keydown', 'scroll'];
        let lastHeartbeatSentAt = 0;

        startConnection().then(function () {
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
                let firstUnreadMode = null;
                let firstUnreadTarget = null;

                (Array.isArray(messages) ? messages : []).forEach(function (eventData) {
                    const scope = eventData.scope === 'tenant' ? 'tenant' : 'user';
                    const senderId = eventData.senderId || null;
                    const targetId = scope === 'tenant'
                        ? eventData.targetId
                        : eventData.targetId || senderId;

                    if (!targetId) {
                        return;
                    }

                    modeNotificationCounts[scope === 'tenant' ? 'tenant' : 'individual'] += 1;

                    if (!firstUnreadTarget) {
                        firstUnreadMode = scope === 'tenant' ? 'tenant' : 'individual';
                        firstUnreadTarget = targetId;
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

                if (firstUnreadTarget) {
                    activeMode = firstUnreadMode;
                    widget.modeTabs.forEach(function (tab) {
                        tab.classList.toggle('active', tab.dataset.mode === activeMode);
                    });
                    renderTargetOptions();
                    widget.target.value = firstUnreadTarget;
                    refreshTargetSelect2();
                    renderConversation();
                }

                updateModeTabCounts();
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

            if (targetSelect2Open) {
                targetOptionsRefreshPending = true;
                return;
            }

            renderTargetOptions();
        }

        function refreshTenant() {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            connection.invoke('GetCurrentTenantAsync').then(function (result) {
                currentTenant = result || null;

                if (targetSelect2Open) {
                    targetOptionsRefreshPending = true;
                    return;
                }

                renderTargetOptions();
            }).catch(function () {
                currentTenant = null;
            });
        }

        function renderTargetOptions() {
            if (targetSelect2Open || (window.jQuery?.('.select2-container--open').length > 0)) {
                targetOptionsRefreshPending = true;
                return;
            }

            const currentValue = selectedTargets[activeMode] || widget.target.value;

            if (activeMode === 'tenant') {
                widget.targetControl.style.display = 'none';
                widget.target.innerHTML = currentTenant
                    ? `<option value="${escapeAttribute(currentTenant.id)}">${escapeHtml(currentTenant.name || currentTenant.id)}</option>`
                    : `<option value="">${l('RealtimeWidget:NoTenant')}</option>`;
                widget.target.disabled = !currentTenant;
                if (currentTenant) {
                    widget.target.value = currentTenant.id;
                    selectedTargets.tenant = currentTenant.id;
                }
                refreshTargetSelect2();
                updateTargetStatusDot();
                renderConversation();
                return;
            }

            widget.targetControl.style.display = '';

            if (peers.length === 0) {
                widget.target.innerHTML = `<option value="">${l('RealtimeWidget:NoOnlineUsers')}</option>`;
                widget.target.disabled = true;
                refreshTargetSelect2();
                updateTargetStatusDot();
                return;
            }

            widget.target.disabled = false;
            widget.target.innerHTML = `<option value="">${l('RealtimeWidget:To')}</option>` + peers
                .map(function (p) {
                    return `<option value="${escapeAttribute(p.userId)}">${escapeHtml(p.userName || p.userId)}</option>`;
                })
                .join('');

            if (currentValue && peers.some(function (p) { return p.userId === currentValue; })) {
                widget.target.value = currentValue;
                selectedTargets.individual = currentValue;
            }

            refreshTargetSelect2();
            updateTargetStatusDot();
            renderConversation();
        }

        function setupTargetSelect2() {
            if (!window.jQuery || !window.jQuery.fn?.select2) {
                return;
            }

            window.jQuery(widget.target).select2({
                theme: 'bootstrap-5',
                width: '100%',
                placeholder: l('RealtimeWidget:To'),
                allowClear: true,
                dropdownParent: window.jQuery(widget.targetControl),
                templateResult: renderTargetSelect2Option,
                templateSelection: renderTargetSelect2Option,
                escapeMarkup: function (markup) { return markup; }
            });

            window.jQuery(widget.target)
                .on('select2:open', function () {
                    targetSelect2Open = true;
                    syncTargetSelect2DropdownSize();
                })
                .on('select2:close', function () {
                    targetSelect2Open = false;
                    if (targetOptionsRefreshPending) {
                        targetOptionsRefreshPending = false;
                        renderTargetOptions();
                    } else {
                        refreshTargetSelect2();
                    }
                });

            window.addEventListener('resize', syncTargetSelect2DropdownSize);
        }

        function syncTargetSelect2DropdownSize() {
            if (!window.jQuery?.fn?.select2 || !window.jQuery(widget.target).data('select2')) {
                return;
            }

            const dropdown = widget.targetControl.querySelector('.select2-dropdown');
            if (dropdown && window.jQuery?.('.select2-container--open').length > 0) {
                dropdown.style.setProperty('width', `${widget.targetControl.getBoundingClientRect().width}px`, 'important');
            }
        }

        function refreshTargetSelect2() {
            const select2MenuOpen = targetSelect2Open
                || (window.jQuery?.('.select2-container--open').length > 0);

            if (!select2MenuOpen
                && window.jQuery
                && window.jQuery.fn?.select2
                && window.jQuery(widget.target).data('select2')) {
                window.jQuery(widget.target).trigger('change.select2');
            }
        }

        function renderTargetSelect2Option(data) {
            if (!data.id) {
                return data.text;
            }

            const peer = peers.find(function (item) { return item.userId === data.id; });
            const status = activeMode === 'tenant' ? null : getPeerStatus(peer);
            const statusMarkup = status
                ? `<span class="rt-status-dot rt-status-${status.className}" title="${escapeAttribute(status.label)}"></span>`
                : '';

            return `<span class="rt-widget-target-option-content">${statusMarkup}<span>${escapeHtml(data.text)}</span></span>`;
        }

        function loadConversationHistory(mode, targetId) {
            if (!targetId || connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            connection.invoke('GetConversationHistoryAsync', mode, targetId).then(function (messages) {
                if (activeMode !== mode || widget.target.value !== targetId) {
                    return;
                }

                const key = `${mode}:${targetId}`;
                histories[key] = (Array.isArray(messages) ? messages : []).map(function (eventData) {
                    return {
                        sender: eventData.senderName || eventData.senderId || 'unknown',
                        senderId: eventData.senderId || null,
                        message: eventData.message || '',
                        timestamp: eventData.timestamp,
                        mode
                    };
                }).slice(-MAX_HISTORY_ITEMS);
                renderConversation();
            }).catch(function () {
                // Conversation history is optional; realtime and unread messages remain available.
            });
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
            if (!peer || !widget.target.value) {
                widget.targetStatus.className = 'rt-status-dot rt-widget-target-status rt-status-hidden';
                widget.targetStatus.removeAttribute('title');
                return;
            }

            const status = getPeerStatus(peer);
            widget.targetStatus.className = `rt-status-dot rt-widget-target-status rt-status-${status.className}`;
            widget.targetStatus.title = status.label;
        }

        function setupBubbleDragging() {
            let dragState = null;
            let suppressNextClick = false;

            restoreBubblePosition();

            widget.bubble.addEventListener('pointerdown', function (event) {
                if (event.button !== 0) {
                    return;
                }

                const bounds = widget.container.getBoundingClientRect();
                dragState = {
                    startX: event.clientX,
                    startY: event.clientY,
                    left: bounds.left,
                    top: bounds.top,
                    moved: false
                };
                widget.bubble.setPointerCapture(event.pointerId);
                event.preventDefault();
            });

            widget.bubble.addEventListener('pointermove', function (event) {
                if (!dragState) {
                    return;
                }

                const deltaX = event.clientX - dragState.startX;
                const deltaY = event.clientY - dragState.startY;
                dragState.moved = dragState.moved || Math.abs(deltaX) > 3 || Math.abs(deltaY) > 3;

                if (!dragState.moved) {
                    return;
                }

                const position = clampBubblePosition(
                    dragState.left + deltaX,
                    dragState.top + deltaY
                );
                setBubblePosition(position.left, position.top);
                event.preventDefault();
            });

            widget.bubble.addEventListener('pointerup', finishDrag);
            widget.bubble.addEventListener('pointercancel', finishDrag);

            widget.bubble.addEventListener('click', function (event) {
                if (suppressNextClick) {
                    suppressNextClick = false;
                    event.preventDefault();
                    event.stopImmediatePropagation();
                }
            }, true);

            function finishDrag(event) {
                if (!dragState) {
                    return;
                }

                if (dragState.moved) {
                    const bounds = widget.container.getBoundingClientRect();
                    saveBubblePosition(bounds.left, bounds.top);
                    suppressNextClick = true;
                }

                if (widget.bubble.hasPointerCapture(event.pointerId)) {
                    widget.bubble.releasePointerCapture(event.pointerId);
                }
                dragState = null;
            }
        }

        function clampBubblePosition(left, top) {
            const bounds = widget.bubble.getBoundingClientRect();
            const margin = 8;
            const maxLeft = Math.max(margin, window.innerWidth - bounds.width - margin);
            const maxTop = Math.max(margin, window.innerHeight - bounds.height - margin);

            return {
                left: Math.min(Math.max(left, margin), maxLeft),
                top: Math.min(Math.max(top, margin), maxTop)
            };
        }

        function setBubblePosition(left, top) {
            widget.container.style.left = `${left}px`;
            widget.container.style.top = `${top}px`;
            widget.container.style.right = 'auto';
            widget.container.style.bottom = 'auto';
        }

        function reportNonFatalError(message, error) {
            console.warn(message, error);
        }

        function saveBubblePosition(left, top) {
            try {
                localStorage.setItem(BUBBLE_POSITION_STORAGE_KEY, JSON.stringify({ left, top }));
            } catch (error) {
                reportNonFatalError('Unable to save bubble position.', error);
            }
        }

        function restoreBubblePosition() {
            try {
                const storedPosition = JSON.parse(localStorage.getItem(BUBBLE_POSITION_STORAGE_KEY));
                if (Number.isFinite(storedPosition?.left) && Number.isFinite(storedPosition?.top)) {
                    const position = clampBubblePosition(storedPosition.left, storedPosition.top);
                    setBubblePosition(position.left, position.top);
                }
            } catch (error) {
                reportNonFatalError('Unable to restore bubble position.', error);
            }
        }

        function setupPanelResizing() {
            let resizeState = null;

            restorePanelSize();

            widget.resizeHandle.addEventListener('pointerdown', function (event) {
                if (event.button !== 0) {
                    return;
                }

                const bounds = widget.panel.getBoundingClientRect();
                resizeState = {
                    startX: event.clientX,
                    startY: event.clientY,
                    width: bounds.width,
                    height: bounds.height
                };
                widget.resizeHandle.setPointerCapture(event.pointerId);
                event.preventDefault();
                event.stopPropagation();
            });

            widget.resizeHandle.addEventListener('pointermove', function (event) {
                if (!resizeState) {
                    return;
                }

                const size = clampPanelSize(
                    resizeState.width + event.clientX - resizeState.startX,
                    resizeState.height + event.clientY - resizeState.startY
                );
                widget.panel.style.width = `${size.width}px`;
                widget.panel.style.height = `${size.height}px`;
                syncTargetSelect2DropdownSize();
                event.preventDefault();
            });

            widget.resizeHandle.addEventListener('pointerup', finishResize);
            widget.resizeHandle.addEventListener('pointercancel', finishResize);

            function finishResize(event) {
                if (!resizeState) {
                    return;
                }

                const bounds = widget.panel.getBoundingClientRect();
                savePanelSize(bounds.width, bounds.height);

                if (widget.resizeHandle.hasPointerCapture(event.pointerId)) {
                    widget.resizeHandle.releasePointerCapture(event.pointerId);
                }
                resizeState = null;
            }
        }

        function setupPanelDragging() {
            let dragState = null;

            restorePanelPosition();

            widget.header.addEventListener('pointerdown', function (event) {
                if (event.button !== 0 || event.target.closest('button')) {
                    return;
                }

                const bounds = widget.panel.getBoundingClientRect();
                setPanelPosition(bounds.left, bounds.top);
                dragState = {
                    startX: event.clientX,
                    startY: event.clientY,
                    left: bounds.left,
                    top: bounds.top
                };
                widget.header.setPointerCapture(event.pointerId);
                event.preventDefault();
            });

            widget.header.addEventListener('pointermove', function (event) {
                if (!dragState) {
                    return;
                }

                const position = clampPanelPosition(
                    dragState.left + event.clientX - dragState.startX,
                    dragState.top + event.clientY - dragState.startY
                );
                setPanelPosition(position.left, position.top);
                event.preventDefault();
            });

            widget.header.addEventListener('pointerup', finishDrag);
            widget.header.addEventListener('pointercancel', finishDrag);

            function finishDrag(event) {
                if (!dragState) {
                    return;
                }

                const bounds = widget.panel.getBoundingClientRect();
                savePanelPosition(bounds.left, bounds.top);

                if (widget.header.hasPointerCapture(event.pointerId)) {
                    widget.header.releasePointerCapture(event.pointerId);
                }
                dragState = null;
            }
        }

        function clampPanelPosition(left, top) {
            const bounds = widget.panel.getBoundingClientRect();
            const margin = 8;
            const maxLeft = Math.max(margin, window.innerWidth - bounds.width - margin);
            const maxTop = Math.max(margin, window.innerHeight - bounds.height - margin);

            return {
                left: Math.min(Math.max(left, margin), maxLeft),
                top: Math.min(Math.max(top, margin), maxTop)
            };
        }

        function setPanelPosition(left, top) {
            widget.panel.style.left = `${left}px`;
            widget.panel.style.top = `${top}px`;
            widget.panel.style.right = 'auto';
            widget.panel.style.bottom = 'auto';
        }

        function savePanelPosition(left, top) {
            try {
                localStorage.setItem(PANEL_POSITION_STORAGE_KEY, JSON.stringify({ left, top }));
            } catch (error) {
                reportNonFatalError('Unable to save panel position.', error);
            }
        }

        function restorePanelPosition() {
            try {
                const storedPosition = JSON.parse(localStorage.getItem(PANEL_POSITION_STORAGE_KEY));
                if (Number.isFinite(storedPosition?.left) && Number.isFinite(storedPosition?.top)) {
                    setPanelPosition(storedPosition.left, storedPosition.top);
                }
            } catch (error) {
                reportNonFatalError('Unable to restore panel position.', error);
            }
        }

        function clampPanelSize(width, height) {
            const margin = 16;
            const minWidth = 260;
            const minHeight = 240;
            const maxWidth = Math.max(minWidth, window.innerWidth - margin * 2);
            const maxHeight = Math.max(minHeight, window.innerHeight - margin * 2);

            return {
                width: Math.min(Math.max(width, minWidth), maxWidth),
                height: Math.min(Math.max(height, minHeight), maxHeight)
            };
        }

        function savePanelSize(width, height) {
            try {
                localStorage.setItem(PANEL_SIZE_STORAGE_KEY, JSON.stringify({ width, height }));
            } catch (error) {
                reportNonFatalError('Unable to save panel size.', error);
            }
        }

        function restorePanelSize() {
            try {
                const storedSize = JSON.parse(localStorage.getItem(PANEL_SIZE_STORAGE_KEY));
                if (Number.isFinite(storedSize?.width) && Number.isFinite(storedSize?.height)) {
                    const size = clampPanelSize(storedSize.width, storedSize.height);
                    widget.panel.style.width = `${size.width}px`;
                    widget.panel.style.height = `${size.height}px`;
                }
            } catch (error) {
                reportNonFatalError('Unable to restore panel size.', error);
            }
        }

        widget.composeSend.addEventListener('click', sendComposeMessage);
        widget.target.addEventListener('change', function () {
            selectedTargets[activeMode] = widget.target.value;
            updateTargetStatusDot();
            renderConversation();
        });
        if (window.jQuery) {
            window.jQuery(widget.target).on('select2:select select2:clear', function () {
                selectedTargets[activeMode] = widget.target.value;
                updateTargetStatusDot();
                renderConversation();

                if (widget.target.value) {
                    loadConversationHistory(activeMode, widget.target.value);
                }
            });
        }
        widget.modeTabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                selectedTargets[activeMode] = widget.target.value;
                activeMode = tab.dataset.mode;
                modeNotificationCounts[activeMode] = 0;
                updateModeTabCounts();
                widget.modeTabs.forEach(item => item.classList.toggle('active', item === tab));
                renderTargetOptions();
                renderConversation();

                if (widget.target.value) {
                    loadConversationHistory(activeMode, widget.target.value);
                }
            });
        });
        widget.composeInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendComposeMessage();
            }
        });

        function sendComposeMessage() {
            const messageMode = activeMode;
            const targetId = widget.target.value;
            const message = widget.composeInput.value.trim();

            if (!targetId || !message) {
                return;
            }

            if (message.length > 4000) {
                return;
            }

            const sendTask = startConnection().then(function () {
                if (connection.state !== signalR.HubConnectionState.Connected) {
                    throw new Error('Realtime connection is unavailable.');
                }

                return activeMode === 'tenant'
                    ? connection.invoke('SendTenantMessageAsync', message)
                    : connection.invoke('SendPeerMessageAsync', targetId, message);
            });

            sendTask.then(function () {
                if (messageMode !== 'tenant') {
                    addMessage(messageMode, targetId, l('RealtimeWidget:You'), null, message, new Date().toISOString());
                }

                activeMode = messageMode;
                widget.modeTabs.forEach(function (tab) {
                    tab.classList.toggle('active', tab.dataset.mode === activeMode);
                });
                renderTargetOptions();
                widget.target.value = targetId;
                refreshTargetSelect2();
                renderConversation();
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

        function updateModeTabCounts() {
            widget.modeTabs.forEach(function (tab) {
                const count = modeNotificationCounts[tab.dataset.mode] || 0;
                const countElement = tab.querySelector('.rt-widget-mode-count');

                if (!countElement) {
                    return;
                }

                countElement.textContent = count > 9 ? '9+' : String(count);
                countElement.classList.toggle('rt-widget-hidden', count === 0);
            });
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

            const resizeHandle = document.createElement('span');
            resizeHandle.className = 'rt-widget-resize-handle';
            resizeHandle.setAttribute('role', 'presentation');
            resizeHandle.setAttribute('aria-hidden', 'true');
            panel.appendChild(resizeHandle);

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
            appendModeCount(individualTab);

            const tenantTab = document.createElement('button');
            tenantTab.type = 'button';
            tenantTab.className = 'rt-widget-mode-tab';
            tenantTab.dataset.mode = 'tenant';
            tenantTab.textContent = l('RealtimeWidget:Tenant');
            appendModeCount(tenantTab);

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

            return { container, panel, list, bubble, badge, resizeHandle, header, modeTabs: [individualTab, tenantTab], targetControl, target, targetStatus, composeInput, composeSend };

            function appendModeCount(tab) {
                const count = document.createElement('span');
                count.className = 'rt-widget-mode-count rt-widget-hidden';
                tab.appendChild(count);
            }
        }
    }
})();
