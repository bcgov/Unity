/**
 * Configuration for one tab navigation group.
 * @typedef {Object} TabUtilsGroup
 * @property {string} id - Unique identifier for this group within the entity.
 * @property {string} tabListSelector - CSS selector for the group's tab navigation element.
 * @property {string} contentSelector - CSS selector for the group's tab content container.
 */

/**
 * Configuration for restoring tab state for one entity.
 * @typedef {Object} TabUtilsOptions
 * @property {string} entityType - Entity category used to isolate stored selections.
 * @property {string} entityId - Entity identifier used to isolate stored selections.
 * @property {TabUtilsGroup[]} groups - Tab groups to restore and observe.
 */

/**
 * A tab selection stored in localStorage.
 * @typedef {Object} TabUtilsSelection
 * @property {string} targetId - ID of the selected tab pane.
 * @property {number} selectedAt - Selection time in milliseconds since the Unix epoch.
 */

/**
 * Manages the state of Bootstrap tabs for specified groups, 
 * restoring the last selected tab and persisting new selections.
 * @param {TabUtilsOptions} options - Entity identity and tab group selectors.
 * @returns {void}
 * @example
 * TabUtils.initialize({
 *     entityType: 'project',
 *     entityId: '123',
 *     groups: [
 *         {
 *             id: 'main-tabs',
 *             tabListSelector: '#main-tabs-nav',
 *             contentSelector: '#main-tabs-content'
 *         }
 *     ]
 * });
 */
const TabUtils = (function () {
    'use strict';

    const STORAGE_PREFIX = 'Unity.TabUtils:';
    const MAX_AGE_MS = 8 * 60 * 60 * 1000; // 8 hours in milliseconds
    const TAB_TOGGLE_SELECTOR = '[data-bs-toggle="tab"]';
    let logoutStarted = false;

    /**
     * Restores each group's saved tab and persists subsequent selections.
     * Invalid or unavailable saved tabs are ignored, leaving the page's active
     * tab in place (or selecting the first available tab when none is active).
     * @param {TabUtilsOptions} options - Entity identity and tab group selectors.
     * @returns {void}
     */
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

    /**
     * Creates the storage key for a specific entity and tab group.
     * @param {string} entityType - Entity category.
     * @param {string} entityId - Entity identifier.
     * @param {string} groupId - Tab group identifier.
     * @returns {string} Namespaced localStorage key.
     */
    function getStorageKey(entityType, entityId, groupId) {
        return `${STORAGE_PREFIX}${encodeURIComponent(entityType)}:${encodeURIComponent(entityId)}:${encodeURIComponent(groupId)}`;
    }

    /**
     * Finds enabled tab controls whose panes belong to the supplied content container.
     * @param {Element} tabList - Tab navigation element to search.
     * @param {Element} tabContent - Tab pane container.
     * @returns {HTMLElement[]} Available tab controls in document order.
     */
    function getAvailableTabs(tabList, tabContent) {
        return Array.from(tabList.querySelectorAll(TAB_TOGGLE_SELECTOR)).filter((tab) => {
            const targetId = getTargetId(tab);
            const targetPane = targetId ? document.getElementById(targetId) : null;
            return isAvailable(tab) && targetPane !== null && tabContent.contains(targetPane);
        });
    }

    /**
     * Checks whether a tab control is connected, enabled, and visibly rendered.
     * @param {HTMLElement} tab - Tab control to inspect.
     * @returns {boolean} Whether the tab can be selected.
     */
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

    /**
     * Gets a tab pane ID from its ARIA relationship or Bootstrap target attribute.
     * @param {HTMLElement} tab - Tab control to inspect.
     * @returns {string|null} Target pane ID, or null when no valid target is present.
     */
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

    /**
     * Reads and validates a saved selection, removing it when malformed or expired.
     * @param {string} storageKey - localStorage key to read.
     * @returns {TabUtilsSelection|null} Valid saved selection, or null when unavailable.
     */
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

    /**
     * Saves the selected pane and the time of selection.
     * @param {string} storageKey - localStorage key to write.
     * @param {string} targetId - Selected tab pane ID.
     * @returns {void}
     */
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

    /**
     * Removes a saved selection.
     * @param {string} storageKey - localStorage key to remove.
     * @returns {void}
     */
    function removeSelection(storageKey) {
        try {
            globalThis.localStorage.removeItem(storageKey);
        } catch {
            return;
        }
    }

    /**
     * Clears all TabUtils selections and prevents further writes in this page.
     * @returns {void}
     */
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

    // Public API
    return {
        initialize: initialize,
        clearStoredState: clearStoredState
    };
})();