const TabUtils = (function () {
    'use strict';

    const STORAGE_PREFIX = 'Unity.TabUtils:';
    const MAX_AGE_MS = 8 * 60 * 60 * 1000;
    const TAB_TOGGLE_SELECTOR = '[data-bs-toggle="tab"]';
    let logoutStarted = false;

    function initialize(options) {
        if (
            logoutStarted ||
            !options?.entityType ||
            !options?.entityId ||
            !Array.isArray(options?.groups)
        ) {
            return;
        }

        options.groups.forEach((group) => {
            if (!group?.id || !group?.tabListSelector || !group?.contentSelector) {
                return;
            }

            const tabList = document.querySelector(group.tabListSelector);
            const tabContent = document.querySelector(group.contentSelector);
            if (!tabList || !tabContent) {
                return;
            }

            const storageKey = getStorageKey(options.entityType, options.entityId, group.id);
            const availableTabs = getAvailableTabs(tabList, tabContent);
            if (availableTabs.length === 0) {
                return;
            }

            let restoreTargetId = null;
            let restoreTimeout = null;

            tabList.addEventListener('shown.bs.tab', (event) => {
                const targetId = getTargetId(event.target);
                if (!targetId) {
                    return;
                }

                if (targetId === restoreTargetId) {
                    restoreTargetId = null;
                    globalThis.clearTimeout(restoreTimeout);
                    restoreTimeout = null;
                    return;
                }

                saveSelection(storageKey, targetId);
            });

            const activeTab = availableTabs.find((tab) => tab.classList.contains('active'));
            const savedSelection = readSelection(storageKey);
            const rememberedTab = savedSelection
                ? availableTabs.find((tab) => getTargetId(tab) === savedSelection.targetId)
                : null;

            if (savedSelection && !rememberedTab) {
                removeSelection(storageKey);
            }

            const tabToShow = rememberedTab || activeTab || availableTabs[0];
            if (tabToShow !== activeTab) {
                restoreTargetId = getTargetId(tabToShow);
                restoreTimeout = globalThis.setTimeout(() => {
                    restoreTargetId = null;
                    restoreTimeout = null;
                }, 1000);

                globalThis.bootstrap?.Tab?.getOrCreateInstance(tabToShow).show();
            }
        });
    }

    function getStorageKey(entityType, entityId, groupId) {
        return `${STORAGE_PREFIX}${encodeURIComponent(entityType)}:${encodeURIComponent(entityId)}:${encodeURIComponent(groupId)}`;
    }

    function getAvailableTabs(tabList, tabContent) {
        return Array.from(tabList.querySelectorAll(TAB_TOGGLE_SELECTOR)).filter((tab) => {
            const targetId = getTargetId(tab);
            const targetPane = targetId ? document.getElementById(targetId) : null;
            return isAvailable(tab) && targetPane !== null && tabContent.contains(targetPane);
        });
    }

    function isAvailable(tab) {
        if (
            !tab.isConnected ||
            tab.matches(':disabled') ||
            tab.getAttribute('aria-disabled') === 'true' ||
            tab.closest('[hidden], .d-none')
        ) {
            return false;
        }

        const style = globalThis.getComputedStyle(tab);
        return style.display !== 'none' && style.visibility !== 'hidden' && tab.getClientRects().length > 0;
    }

    function getTargetId(tab) {
        const ariaControls = tab.getAttribute('aria-controls');
        if (ariaControls) {
            return ariaControls.trim().split(/\s+/)[0];
        }

        const target = tab.dataset.bsTarget || tab.getAttribute('href');
        if (!target) {
            return null;
        }

        try {
            const targetUrl = new URL(target, globalThis.location.href);
            return targetUrl.hash ? decodeURIComponent(targetUrl.hash.substring(1)) : null;
        } catch {
            return null;
        }
    }

    function readSelection(storageKey) {
        try {
            const serializedSelection = globalThis.localStorage.getItem(storageKey);
            if (!serializedSelection) {
                return null;
            }

            const selection = JSON.parse(serializedSelection);
            const age = Date.now() - selection.selectedAt;
            if (
                typeof selection.targetId !== 'string' ||
                !Number.isFinite(selection.selectedAt) ||
                age < 0 ||
                age >= MAX_AGE_MS
            ) {
                removeSelection(storageKey);
                return null;
            }

            return selection;
        } catch {
            return null;
        }
    }

    function saveSelection(storageKey, targetId) {
        if (logoutStarted) {
            return;
        }

        try {
            globalThis.localStorage.setItem(storageKey, JSON.stringify({
                targetId: targetId,
                selectedAt: Date.now()
            }));
        } catch {
            return;
        }
    }

    function removeSelection(storageKey) {
        try {
            globalThis.localStorage.removeItem(storageKey);
        } catch {
            return;
        }
    }

    function clearStoredState() {
        logoutStarted = true;
        try {
            const keysToRemove = [];
            for (let index = 0; index < globalThis.localStorage.length; index++) {
                const key = globalThis.localStorage.key(index);
                if (key?.startsWith(STORAGE_PREFIX)) {
                    keysToRemove.push(key);
                }
            }

            keysToRemove.forEach((key) => globalThis.localStorage.removeItem(key));
        } catch {
            return;
        }
    }

    document.addEventListener('click', (event) => {
        const logoutLink = event.target.closest?.('a[href]');
        if (!logoutLink) {
            return;
        }

        try {
            const logoutUrl = new URL(logoutLink.href, globalThis.location.href);
            let pathname = logoutUrl.pathname.toLowerCase();
            while (pathname.endsWith('/')) {
                pathname = pathname.slice(0, -1);
            }
            if (pathname.endsWith('/account/logout')) {
                clearStoredState();
            }
        } catch {
            return;
        }
    }, true);

    return {
        initialize: initialize,
        clearStoredState: clearStoredState
    };
})();