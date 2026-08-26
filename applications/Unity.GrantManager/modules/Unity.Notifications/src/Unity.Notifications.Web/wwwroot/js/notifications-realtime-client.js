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

        const l = window.abp?.localization?.getResource?.('Notifications')
            || function (key) { return key; };

        const myUserId = abp.currentUser.id || null;

        let unreadCount = 0;
        let panelOpen = false;
        let peers = [];
        let activeMode = 'individual';
        let currentTenant = null;
        const selectedTargets = { individual: '', tenant: '' };
        const unreadIndividualCounts = {};
        let targetSelect2Open = false;
        let targetOptionsRefreshPending = false;
        const histories = {};
        const loadedConversationHistories = new Set();
        const MAX_HISTORY_ITEMS = 100;
        const STATUS_GREEN_MS = 10 * 60 * 1000;
        const STATUS_ORANGE_MS = 30 * 60 * 1000;
        const BUBBLE_POSITION_STORAGE_KEY = 'unity.notifications.realtime.bubble-position';
        const PANEL_SIZE_STORAGE_KEY = 'unity.notifications.realtime.panel-size';
        const PANEL_POSITION_STORAGE_KEY = 'unity.notifications.realtime.panel-position';
        const BANNER_STORAGE_KEY = 'unity.notifications.realtime.banners';
        const widget = buildWidget();
        renderStoredBanners();
        setupBubbleDragging();
        setupPanelResizing();
        setupPanelDragging();
        setupIndividualsResizing();
        setupBubbleMenuItem();

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
                if (mode === 'individual'
                    && !(panelOpen && activeMode === 'individual' && widget.target.value === targetId)) {
                    unreadIndividualCounts[targetId] = (unreadIndividualCounts[targetId] || 0) + 1;
                }
                renderIndividualList();
            }

            if (senderId && senderId !== myUserId) {
                if (isBannerMessage(eventData, scope)) {
                    showIncomingBanner(sender, message, eventData?.timestamp);
                } else {
                    showIncomingToast(sender, message);
                }
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

                    if (scope === 'user' && senderId && senderId !== myUserId) {
                        unreadIndividualCounts[targetId] = (unreadIndividualCounts[targetId] || 0) + 1;
                    }

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

                    if (senderId && senderId !== myUserId) {
                        const senderName = eventData.senderName || senderId;
                        const message = eventData.message || '';
                        if (isBannerMessage(eventData, scope)) {
                            showIncomingBanner(senderName, message, eventData?.timestamp);
                        } else {
                            showIncomingToast(senderName, message);
                        }
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

                renderIndividualList();
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

            peers.slice().sort(compareIndividualActivity).forEach(function (peer) {
                const historyKey = `individual:${peer.userId}`;
                if (!loadedConversationHistories.has(historyKey)) {
                    loadConversationHistory('individual', peer.userId, false);
                }
            });
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
                widget.individualsPanel.classList.add('rt-widget-hidden');
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
            widget.individualsPanel.classList.remove('rt-widget-hidden');

            if (peers.length === 0) {
                widget.target.innerHTML = `<option value="">${l('RealtimeWidget:NoOnlineUsers')}</option>`;
                widget.target.disabled = true;
                refreshTargetSelect2();
                updateTargetStatusDot();
                renderIndividualList();
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
            renderIndividualList();
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

        function loadConversationHistory(mode, targetId, renderActiveConversation) {
            if (!targetId || connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            connection.invoke('GetConversationHistoryAsync', mode, targetId).then(function (messages) {
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
                loadedConversationHistories.add(key);

                if (renderActiveConversation !== false && activeMode === mode && widget.target.value === targetId) {
                    renderConversation();
                } else if (mode === 'individual') {
                    renderIndividualList();
                }
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

        function renderIndividualList() {
            if (!widget.individualsList) {
                return;
            }

            widget.individualsList.innerHTML = '';
            if (peers.length === 0) {
                const empty = document.createElement('div');
                empty.className = 'rt-widget-individuals-empty';
                empty.textContent = l('RealtimeWidget:NoOnlineUsers');
                widget.individualsList.appendChild(empty);
                return;
            }

            peers.forEach(function (peer) {
                const targetId = peer.userId;
                const unreadMessageCount = unreadIndividualCounts[targetId] || 0;
                const status = getPeerStatus(peer);
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'rt-widget-individual' + (selectedTargets.individual === targetId ? ' active' : '');
                button.dataset.userId = targetId;
                button.title = peer.userName || targetId;

                const avatar = document.createElement('span');
                avatar.className = 'rt-widget-individual-avatar';
                avatar.textContent = getInitials(peer.userName || targetId);

                const details = document.createElement('span');
                details.className = 'rt-widget-individual-details';
                const name = document.createElement('span');
                name.className = 'rt-widget-individual-name';
                name.textContent = peer.userName || targetId;
                const presence = document.createElement('span');
                presence.className = 'rt-widget-individual-presence';
                presence.innerHTML = `<span class="rt-status-dot rt-status-${status.className}"></span>${escapeHtml(status.label)}`;
                details.appendChild(name);
                details.appendChild(presence);

                const count = document.createElement('span');
                count.className = 'rt-widget-individual-count';
                count.textContent = unreadMessageCount > 99 ? '99+' : String(unreadMessageCount);
                count.classList.toggle('rt-widget-hidden', unreadMessageCount === 0);

                button.appendChild(avatar);
                button.appendChild(details);
                button.appendChild(count);
                button.addEventListener('click', function () {
                    selectIndividual(targetId);
                });
                widget.individualsList.appendChild(button);
            });
        }

        function compareIndividualActivity(firstPeer, secondPeer) {
            const firstActivity = getIndividualActivityTime(firstPeer);
            const secondActivity = getIndividualActivityTime(secondPeer);

            if (firstActivity !== secondActivity) {
                return secondActivity - firstActivity;
            }

            return String(firstPeer.userName || firstPeer.userId)
                .localeCompare(String(secondPeer.userName || secondPeer.userId));
        }

        function getIndividualActivityTime(peer) {
            const conversation = histories[`individual:${peer.userId}`] || [];
            const messageActivity = conversation.reduce(function (latest, entry) {
                const timestamp = entry.timestamp ? new Date(entry.timestamp).getTime() : Number.NaN;
                return Number.isNaN(timestamp) ? latest : Math.max(latest, timestamp);
            }, 0);

            if (messageActivity > 0) {
                return messageActivity;
            }

            const presenceActivity = peer.lastActivityUtc ? new Date(peer.lastActivityUtc).getTime() : 0;
            return Number.isNaN(presenceActivity) ? 0 : presenceActivity;
        }

        function selectIndividual(targetId) {
            selectedTargets.individual = targetId;
            widget.target.value = targetId;
            refreshTargetSelect2();
            updateTargetStatusDot();
            renderIndividualList();
            renderConversation();
            loadConversationHistory('individual', targetId);
        }

        function getInitials(value) {
            return String(value || '?')
                .split(/\s+/)
                .filter(Boolean)
                .slice(0, 2)
                .map(function (part) { return part.charAt(0).toUpperCase(); })
                .join('') || '?';
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

        function setupIndividualsResizing() {
            let resizeState = null;

            widget.individualsResizeHandle.addEventListener('pointerdown', function (event) {
                if (event.button !== 0) {
                    return;
                }

                resizeState = {
                    startX: event.clientX,
                    width: widget.individualsPanel.getBoundingClientRect().width
                };
                widget.individualsResizeHandle.setPointerCapture(event.pointerId);
                event.preventDefault();
            });

            widget.individualsResizeHandle.addEventListener('pointermove', function (event) {
                if (!resizeState) {
                    return;
                }

                const width = Math.min(320, Math.max(120, resizeState.width + event.clientX - resizeState.startX));
                widget.individualsPanel.style.flexBasis = `${width}px`;
                event.preventDefault();
            });

            widget.individualsResizeHandle.addEventListener('pointerup', finishResize);
            widget.individualsResizeHandle.addEventListener('pointercancel', finishResize);

            function finishResize(event) {
                if (!resizeState) {
                    return;
                }

                if (widget.individualsResizeHandle.hasPointerCapture(event.pointerId)) {
                    widget.individualsResizeHandle.releasePointerCapture(event.pointerId);
                }
                resizeState = null;
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
        widget.modeTabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                selectedTargets[activeMode] = widget.target.value;
                activeMode = tab.dataset.mode;
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
            const showIndividuals = activeMode === 'individual';
            widget.individualsPanel.classList.toggle('rt-widget-hidden', !showIndividuals);
            widget.individualsResizeHandle.classList.toggle('rt-widget-hidden', !showIndividuals);
            renderIndividualList();
            widget.list.innerHTML = '';

            if (panelOpen && targetId) {
                clearUnreadCounts(targetId);
                markMessagesRead();
            }

            if (items.length === 0) {
                const empty = document.createElement('div');
                empty.className = 'rt-widget-empty';
                empty.textContent = l('RealtimeWidget:Empty');
                widget.list.appendChild(empty);
                return;
            }

            let previousDateKey = null;
            items.forEach(function (entry) {
                const entryDate = entry.timestamp ? new Date(entry.timestamp) : new Date();
                const dateKey = Number.isNaN(entryDate.getTime())
                    ? null
                    : `${entryDate.getFullYear()}-${entryDate.getMonth()}-${entryDate.getDate()}`;

                if (dateKey && dateKey !== previousDateKey) {
                    const dateDivider = document.createElement('div');
                    dateDivider.className = 'rt-widget-date-divider';
                    dateDivider.textContent = entryDate.toLocaleDateString();
                    widget.list.appendChild(dateDivider);
                    previousDateKey = dateKey;
                }

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

        function markMessagesRead() {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            connection.invoke('MarkMessagesReadAsync').catch(function () {
                // Read-state failures do not affect the displayed conversation.
            });
        }

        function clearUnreadCounts(targetId) {
            if (targetId) {
                delete unreadIndividualCounts[targetId];
                renderIndividualList();
                return;
            }

            Object.keys(unreadIndividualCounts).forEach(function (userId) {
                delete unreadIndividualCounts[userId];
            });
        }

        function formatTime(value) {
            const date = value ? new Date(value) : new Date();
            if (Number.isNaN(date.getTime())) {
                return '';
            }

            return date.toLocaleTimeString();
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
                    showCloseButton: true,
                    allowOutsideClick: false,
                    allowEscapeKey: false
                });
                return;
            }

            if (window.abp?.notify?.info && typeof window.abp.notify.info === 'function') {
                abp.notify.info(`${sender}: ${message}`, null, {
                    timeOut: 0,
                    extendedTimeOut: 0,
                    closeButton: true,
                    tapToDismiss: false
                });
            }
        }

        function isBannerMessage(eventData, scope) {
            return scope === 'tenant'
                && String(eventData?.messageType || '').toLowerCase() === 'banner';
        }

        function renderStoredBanners() {
            const storedBanners = readStoredBanners();
            if (storedBanners.length === 0) {
                return;
            }

            const navbar = document.getElementById('main-navbar');
            if (!navbar) {
                return;
            }

            const container = document.createElement('div');
            container.className = 'rt-widget-banners';
            storedBanners.forEach(function (banner) {
                container.appendChild(createBannerElement(banner));
            });
            navbar.insertAdjacentElement('afterend', container);
        }

        function showIncomingBanner(sender, message, timestamp) {
            if (timestamp && !isCurrentDay(timestamp)) {
                return;
            }

            const banners = readStoredBanners();
            const bannerKey = buildBannerKey(sender, message, timestamp);
            if (banners.some(function (banner) {
                return banner.key === bannerKey
                    || (banner.sender === String(sender || '') && banner.message === String(message || ''));
            })) {
                return;
            }

            const banner = {
                id: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
                sender: String(sender || ''),
                message: String(message || ''),
                key: bannerKey,
                timestamp: timestamp || new Date().toISOString()
            };
            banners.push(banner);
            saveStoredBanners(banners);

            const navbar = document.getElementById('main-navbar');
            if (!navbar) {
                return;
            }

            let container = document.querySelector('.rt-widget-banners');
            if (!container) {
                container = document.createElement('div');
                container.className = 'rt-widget-banners';
                navbar.insertAdjacentElement('afterend', container);
            }
            container.appendChild(createBannerElement(banner));
        }

        function createBannerElement(banner) {
            const element = document.createElement('div');
            element.className = 'rt-widget-banner';
            element.dataset.bannerId = banner.id;

            const content = document.createElement('div');
            content.className = 'rt-widget-banner-content';
            const senderElement = document.createElement('strong');
            senderElement.className = 'rt-widget-banner-sender';
            senderElement.textContent = banner.sender;
            const messageElement = document.createElement('span');
            messageElement.className = 'rt-widget-banner-message';
            messageElement.textContent = banner.message;
            content.appendChild(senderElement);
            content.appendChild(messageElement);

            const closeButton = document.createElement('button');
            closeButton.type = 'button';
            closeButton.className = 'rt-widget-banner-close';
            closeButton.setAttribute('aria-label', l('RealtimeWidget:CloseBanner'));
            closeButton.innerHTML = '&times;';
            closeButton.addEventListener('click', function () {
                removeStoredBanner(banner.id);
                element.remove();
                const container = element.parentElement;
                if (container && container.children.length === 0) {
                    container.remove();
                }
            });

            element.appendChild(content);
            element.appendChild(closeButton);
            return element;
        }

        function readStoredBanners() {
            try {
                const banners = JSON.parse(localStorage.getItem(BANNER_STORAGE_KEY) || '[]');
                return Array.isArray(banners) ? banners.filter(function (banner) {
                    return banner?.id && typeof banner.sender === 'string'
                        && typeof banner.message === 'string'
                        && isCurrentDay(banner.timestamp || banner.id.split('-')[0]);
                }) : [];
            } catch (error) {
                return [];
            }
        }

        function saveStoredBanners(banners) {
            try {
                localStorage.setItem(BANNER_STORAGE_KEY, JSON.stringify(banners));
            } catch (error) {
                // Storage failures should not prevent realtime messages from displaying.
            }
        }

        function removeStoredBanner(bannerId) {
            saveStoredBanners(readStoredBanners().filter(function (banner) {
                return banner.id !== bannerId;
            }));
        }

        function buildBannerKey(sender, message, timestamp) {
            return [String(sender || ''), String(message || ''), String(timestamp || '')].join('|');
        }

        function isCurrentDay(value) {
            const numericValue = typeof value === 'string' && /^\d+$/.test(value)
                ? Number(value)
                : value;
            const date = new Date(numericValue);
            const today = new Date();
            return !Number.isNaN(date.getTime())
                && date.getFullYear() === today.getFullYear()
                && date.getMonth() === today.getMonth()
                && date.getDate() === today.getDate();
        }

        function togglePanel() {
            panelOpen = !panelOpen;
            widget.panel.classList.toggle('rt-widget-panel-open', panelOpen);
            widget.bubble.classList.toggle('rt-widget-hidden', panelOpen);
            updateBubbleMenuItem();

            if (panelOpen) {
                unreadCount = 0;
                updateBadge();
                renderConversation();
                refreshPeers();
                refreshTenant();
            }
        }

        function setupBubbleMenuItem() {
            const menuItem = document.getElementById('realtimeWidgetBubbleMenuItem');
            if (!menuItem) {
                return;
            }

            menuItem.style.display = '';
            menuItem.addEventListener('click', function (event) {
                event.preventDefault();
                widget.bubble.classList.toggle('rt-widget-hidden');
                updateBubbleMenuItem();
            });
            updateBubbleMenuItem();
        }

        function updateBubbleMenuItem() {
            const menuItem = document.getElementById('realtimeWidgetBubbleMenuItem');
            if (!menuItem) {
                return;
            }

            const bubbleIsHidden = widget.bubble.classList.contains('rt-widget-hidden');
            menuItem.textContent = l(bubbleIsHidden ? 'RealtimeWidget:ShowBubble' : 'RealtimeWidget:HideBubble');
            menuItem.setAttribute('aria-label', menuItem.textContent);
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

            const conversationLayout = document.createElement('div');
            conversationLayout.className = 'rt-widget-conversation-layout';

            const conversationToolbar = document.createElement('div');
            conversationToolbar.className = 'rt-widget-conversation-toolbar';

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
            conversationToolbar.appendChild(modeTabs);

            const conversationBody = document.createElement('div');
            conversationBody.className = 'rt-widget-conversation-body';

            const individualsPanel = document.createElement('aside');
            individualsPanel.className = 'rt-widget-individuals-panel';
            const individualsHeading = document.createElement('div');
            individualsHeading.className = 'rt-widget-individuals-heading';
            individualsHeading.textContent = l('RealtimeWidget:Individual');
            const individualsList = document.createElement('div');
            individualsList.className = 'rt-widget-individuals-list';
            individualsPanel.appendChild(individualsHeading);
            individualsPanel.appendChild(individualsList);

            const individualsResizeHandle = document.createElement('span');
            individualsResizeHandle.className = 'rt-widget-individuals-resize-handle';
            individualsResizeHandle.setAttribute('role', 'separator');
            individualsResizeHandle.setAttribute('aria-label', l('RealtimeWidget:ResizeIndividuals'));

            const conversation = document.createElement('div');
            conversation.className = 'rt-widget-conversation';
            const list = document.createElement('div');
            list.className = 'rt-widget-list';

            const empty = document.createElement('div');
            empty.className = 'rt-widget-empty';
            empty.textContent = l('RealtimeWidget:Empty');
            list.appendChild(empty);
            conversation.appendChild(list);
            conversationBody.appendChild(individualsPanel);
            conversationBody.appendChild(individualsResizeHandle);
            conversationBody.appendChild(conversation);
            conversationLayout.appendChild(conversationToolbar);
            conversationLayout.appendChild(conversationBody);

            const compose = document.createElement('div');
            compose.className = 'rt-widget-compose';

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
            compose.appendChild(targetRow);
            compose.appendChild(composeRow);

            panel.appendChild(header);
            panel.appendChild(conversationLayout);
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

            return { container, panel, list, bubble, badge, resizeHandle, header, modeTabs: [individualTab, tenantTab], targetControl, target, targetStatus, composeInput, composeSend, individualsPanel, individualsList, individualsResizeHandle };
        }
    }
})();
