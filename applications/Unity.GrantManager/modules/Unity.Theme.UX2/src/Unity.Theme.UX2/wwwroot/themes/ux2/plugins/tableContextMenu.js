(function ($) {
    'use strict';

    // Constants
    const MENU_Z_INDEX = 10000;
    const MENU_VIEWPORT_PADDING = 8;
    const OFFSCREEN_OFFSET = '-9999px';
    const MENU_ID = 'dt-context-menu';

    function getTableSettings(dtApi) {
        return dtApi?.settings?.()?.[0] ?? null;
    }

    function getFilterRowPlugin(dtApi) {
        return getTableSettings(dtApi)?._filterRow ?? null;
    }

    function getFilterRowElement(dtApi) {
        return getFilterRowPlugin(dtApi)?.dom?.filterRow ?? $();
    }

    function getFilterInputs(dtApi) {
        return getFilterRowElement(dtApi).find('input.custom-filter-input');
    }

    function getButtonsForTable(dtApi) {
        return $(dtApi?.buttons?.().nodes?.() ?? []);
    }

    function getFilterButton(dtApi) {
        // Try to find filter button through table's button API first
        const $tableButtons = getButtonsForTable(dtApi);
        if ($tableButtons.length > 0) {
            const $filterFromAPI = $tableButtons.filter('[id="btn-toggle-filter"]');
            if ($filterFromAPI.length > 0) {
                return $filterFromAPI;
            }
        }

        // Fallback: search the table's scope or page-wide
        const $scopeRoot = getScopeRoot(dtApi);
        const $filterScoped = $scopeRoot.find('#btn-toggle-filter');
        if ($filterScoped.length > 0) {
            return $filterScoped;
        }

        // Final fallback: page-wide search
        return $('#btn-toggle-filter');
    }

    function getScopeRoot(dtApi) {
        const $container = $(dtApi?.table?.().container?.() ?? []);
        return $container.closest('.tab-pane, .modal, .card, .content, body').first();
    }

    function findScopedElements(dtApi, selector) {
        const $scopeRoot = getScopeRoot(dtApi);
        const $scopedMatches = $scopeRoot.find(selector);
        return $scopedMatches.length > 0 ? $scopedMatches : $(selector);
    }

    function getMenuContainer() {
        return $('#' + MENU_ID);
    }

    function getMenuItems($menuContainer) {
        return $menuContainer.find('.dt-context-menu-link:visible');
    }

    function focusMenuItem($menuContainer, index) {
        const $items = getMenuItems($menuContainer);
        if ($items.length === 0) {
            return;
        }

        const normalizedIndex = ((index % $items.length) + $items.length) % $items.length;
        $items.attr('tabindex', '-1');

        const $target = $items.eq(normalizedIndex);
        $target.attr('tabindex', '0').trigger('focus');
        $menuContainer.data('activeIndex', normalizedIndex);
    }

    function focusFirstMenuItem($menuContainer) {
        focusMenuItem($menuContainer, 0);
    }

    function rememberFocusTarget(element) {
        const $menuContainer = getMenuContainer();
        $menuContainer.data('returnFocus', element ?? null);
    }

    function restoreFocus() {
        const $menuContainer = getMenuContainer();
        const returnFocus = $menuContainer.data('returnFocus');
        if (!returnFocus || typeof returnFocus.focus !== 'function') {
            return;
        }

        const hadTabIndex = returnFocus.hasAttribute('tabindex');
        if (!hadTabIndex) {
            returnFocus.setAttribute('tabindex', '-1');
        }

        returnFocus.focus();

        if (!hadTabIndex) {
            returnFocus.addEventListener('blur', function cleanupFocusTarget() {
                returnFocus.removeAttribute('tabindex');
            }, { once: true });
        }
    }

    function handleMenuKeydown(e) {
        const $menuContainer = getMenuContainer();
        const $items = getMenuItems($menuContainer);
        const currentIndex = $items.index(globalThis.document.activeElement);

        if ($items.length === 0) {
            return;
        }

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                focusMenuItem($menuContainer, currentIndex + 1);
                break;
            case 'ArrowUp':
                e.preventDefault();
                focusMenuItem($menuContainer, currentIndex - 1);
                break;
            case 'Home':
                e.preventDefault();
                focusMenuItem($menuContainer, 0);
                break;
            case 'End':
                e.preventDefault();
                focusMenuItem($menuContainer, $items.length - 1);
                break;
            case 'Tab':
                hideMenu();
                break;
            case ' ':
                if (currentIndex > -1) {
                    e.preventDefault();
                    $items.eq(currentIndex).trigger('click');
                }
                break;
            case 'Escape':
                e.preventDefault();
                hideMenu();
                break;
            default:
                break;
        }
    }

    function appendMenuAction($menuContainer, label, handler) {
        $menuContainer.append(
            $('<li class="dt-context-menu-item" role="none"></li>').append(
                $('<a href="#" class="dt-context-menu-link" role="menuitem" tabindex="-1">')
                    .text(label)
                    .on('click', handler)
                    .on('mouseover', function () {
                        getMenuItems($menuContainer).blur(); // When mouse enters an item, remove focus from all items so :hover takes precedence
                    })
            )
        );
    }

    function positionMenu($menuContainer, clientX, clientY) {
        $menuContainer.css({
            position: 'fixed',
            display: 'block',
            visibility: 'hidden',
            zIndex: MENU_Z_INDEX
        });

        const menuWidth = $menuContainer.outerWidth() ?? 0;
        const menuHeight = $menuContainer.outerHeight() ?? 0;
        const maxLeft = Math.max(MENU_VIEWPORT_PADDING, globalThis.innerWidth - menuWidth - MENU_VIEWPORT_PADDING);
        const maxTop = Math.max(MENU_VIEWPORT_PADDING, globalThis.innerHeight - menuHeight - MENU_VIEWPORT_PADDING);
        const left = Math.min(Math.max(MENU_VIEWPORT_PADDING, clientX), maxLeft);
        const top = Math.min(Math.max(MENU_VIEWPORT_PADDING, clientY), maxTop);

        $menuContainer.css({
            left: left + 'px',
            top: top + 'px',
            visibility: 'visible'
        });
    }

    function showMenu($menuContainer, clientX, clientY, focusTarget) {
        rememberFocusTarget(focusTarget);
        positionMenu($menuContainer, clientX, clientY);
        $menuContainer.attr('aria-hidden', 'false');
        focusFirstMenuItem($menuContainer);
    }

    /**
     * Handle filter column lookup and auto-population.
     */
    function handleFilterAction(e, $cell, dtApi) {
        e.preventDefault();
        hideMenu();

        const cellText = ($cell.text() ?? '').trim();
        const filterRow = getFilterRowPlugin(dtApi);

        // Show the filter row if not already visible
        filterRow?.show?.();

        // Resolve the column name for the clicked cell
        try {
            const cellInfo = dtApi.cell($cell[0])?.index?.();
            if (!cellInfo) {
                return;
            }

            const colIdx = cellInfo.column;
            const settings = dtApi.settings?.();
            const aoColumns = settings?.[0]?.aoColumns;
            if (!aoColumns?.[colIdx]) {
                return;
            }

            const colName = aoColumns[colIdx].name;
            if (!colName) {
                return;
            }

            // Find the filter input for this column and set the value
            const filterRowElement = getFilterRowElement(dtApi);
            const $input = filterRowElement.find('input.custom-filter-input').filter(function () {
                return $(this).data('column-name') === colName;
            });

            if ($input.length > 0) {
                $input.val(cellText).trigger('keyup');
            }
        } catch (err) {
            console.debug('Filter action error:', err);
        }
    }

    /**
     * Handle clear filter action.
     */
    function handleClearFilterAction(e, dtApi) {
        e.preventDefault();
        hideMenu();

        const filterRow = getFilterRowPlugin(dtApi);
        filterRow?.clearFilters?.();
    }

    /**
     * Handle generic toolbar button click.
     */
    function handleToolbarButtonClick(e, $btn) {
        e.preventDefault();
        hideMenu();
        $btn?.[0]?.click?.();
    }

    /**
     * Check if a button is enabled (not hidden/disabled).
     */
    function isButtonEnabled($btn) {
        return !$btn.hasClass('action-bar-btn-unavailable')
            && !$btn.prop('disabled')
            && !$btn.hasClass('d-none')
            && !$btn.hasClass('dt-button-disabled');
    }

    /**
     * Handle dismiss on outside click.
     */
    function dismissClickHandler(e) {
        if (!$(e.target).closest('#' + MENU_ID).length) {
            hideMenu();
        }
    }

    /**
     * Handle dismiss on scroll.
     */
    function dismissScrollHandler() {
        hideMenu();
    }

    /**
     * Handle row selection with callback.
     */
    function handleRowSelection(dtApi, rowIndex, callback) {
        requestAnimationFrame(function () {
            const selectedRows = dtApi.rows({ selected: true }).indexes();
            if (!selectedRows.includes(rowIndex)) {
                dtApi.rows({ selected: true }).deselect();
                dtApi.row(rowIndex).select();
            }
            requestAnimationFrame(callback);
        });
    }

    /**
     * Hide the context menu and clean up event handlers.
     */
    function hideMenu() {
        const $menuContainer = getMenuContainer();
        if ($menuContainer.length > 0) {
            $menuContainer
                .hide()
                .attr('aria-hidden', 'true')
                .off('keydown.dt-context-menu')
                .removeData('activeIndex');
        }

        $(document).off('click.dt-context-menu');
        $(globalThis).off('scroll.dt-context-menu resize.dt-context-menu');

        restoreFocus();
    }

    /**
     * Copy text to clipboard with fallback.
     */
    function copyToClipboard(text) {
        if (navigator.clipboard?.writeText) {
            navigator.clipboard.writeText(text)
                .then(() => {
                    abp.notify.success((abp.localization.getResource('GrantManager')('DataTable:ContextMenu:CopiedToClipboard') ?? 'Copied to clipboard'),'Success');
                })
                .catch(() => {
                    fallbackCopy(text);
                });
        } else {
            fallbackCopy(text);
        }
    }

    /**
     * Fallback copy using textarea hack (legacy support).
     */
    function fallbackCopy(text) {
        const $textarea = $('<textarea>')
            .val(text)
            .css({
                position: 'fixed',
                left: OFFSCREEN_OFFSET,
                top: OFFSCREEN_OFFSET
            })
            .appendTo('body');

        try {
            $textarea[0]?.select?.();
            const execCommand = document['execCommand']?.bind(document);
            const success = execCommand ? execCommand('copy') : false;
            if (success) {
                abp.notify.success(
                    (abp.localization.getResource('GrantManager')('DataTable:ContextMenu:CopiedToClipboard') ?? 'Copied to clipboard'),
                    'Success'
                );
            } else {
                abp.notify.error('Copy failed. Please try again.', 'Error');
            }
        } catch (err) {
            console.debug('Fallback copy error:', err);
            abp.notify.error('Copy failed. Please try again.', 'Error');
        } finally {
            $textarea.remove();
        }
    }

    /**
     * Add filter toolbar item to menu.
     */
    function addFilterItem(toolbarItems, labels, dtApi) {
        const $filterBtn = getFilterButton(dtApi);
        const filterRowPlugin = getFilterRowPlugin(dtApi);
        if ($filterBtn.length > 0 && $filterBtn.is(':visible') && filterRowPlugin) {
            toolbarItems.push({
                label: labels.filter,
                filterAction: true
            });
        }
    }

    /**
     * Add clear filter toolbar item to menu if filters are active.
     */
    function addClearFilterItem(toolbarItems, labels, dtApi) {
        const filterRowPlugin = getFilterRowPlugin(dtApi);
        if (filterRowPlugin) {
            const hasColumnFilters = getFilterInputs(dtApi).toArray().some(function (element) {
                return Boolean($(element).val()?.trim?.());
            });
            const hasSearchFilter = Boolean(dtApi.search?.()?.trim?.());
            const hasActiveFilters = hasColumnFilters || hasSearchFilter;

            if (hasActiveFilters) {
                toolbarItems.push({
                    label: labels.clearFilter,
                    clearFilterAction: true
                });
            }
        }
    }

    /**
     * Render filter menu item.
     */
    function renderFilterMenuItem($menuContainer, item, $cell, dtApi) {
        appendMenuAction($menuContainer, item.label, function (e) {
            handleFilterAction(e, $cell, dtApi);
        });
    }

    /**
     * Render clear filter menu item.
     */
    function renderClearFilterMenuItem($menuContainer, item, dtApi) {
        appendMenuAction($menuContainer, item.label, function (e) {
            handleClearFilterAction(e, dtApi);
        });
    }

    /**
     * Render toolbar button menu item.
     */
    function renderToolbarButtonMenuItem($menuContainer, item) {
        appendMenuAction($menuContainer, item.label, function (e) {
            handleToolbarButtonClick(e, item.$btn);
        });
    }

    /**
     * Render custom action menu item.
     */
    function renderCustomActionMenuItem($menuContainer, $btn, btnText) {
        appendMenuAction($menuContainer, btnText, function (e) {
            handleToolbarButtonClick(e, $btn);
        });
    }

    /**
     * Initialize a context menu for a DataTable.
     * Features:
     * - Right-click selection: if row not selected, select it; if already selected, keep selection
     * - Copy: copies the right-clicked cell's rendered text to clipboard
     * - Toolbar items: Filtering
     * - Custom actions: mirrors visible/enabled ActionBar buttons
     * 
     * @param {DataTables.Api} dtApi - The initialized DataTable API instance
     * @param {Object} options - Configuration options
     * @param {boolean} [options.enabled=true] - Whether context menu is enabled
     * @param {string} [options.actionsSelector='[data-selector$="-table-actions"]'] - Selector for custom action buttons
     * @param {boolean} [options.copyEnabled=true] - Whether Copy action is available
     * @param {Object} [options.labels={}] - Localization overrides { copy, filter, clearFilter }
     */
    globalThis.initializeTableContextMenu = function (dtApi, options) {
        options = options ?? {};

        const enabled = options.enabled !== false;
        const actionsSelector = options.actionsSelector ?? '[data-selector$="-table-actions"]';
        const copyEnabled = options.copyEnabled !== false;
        const labels = options.labels ?? {};

        if (!enabled || !dtApi?.table) {
            return;
        }

        // Initialize labels with defaults
        const l = abp.localization.getResource('GrantManager');
        const defaultLabels = {
            copy: l('DataTable:ContextMenu:Copy') ?? 'Copy',
            filter: l('DataTable:ContextMenu:Filter') ?? 'Filter',
            clearFilter: l('DataTable:ContextMenu:ClearFilter') ?? 'Clear Filters'
        };

        const finalLabels = $.extend({}, defaultLabels, labels);

        // Create menu container (single, reused)
        let $menuContainer = getMenuContainer();
        if ($menuContainer.length === 0) {
            $menuContainer = $('<ul id="dt-context-menu" class="dt-context-menu" style="display: none;" role="menu" aria-hidden="true"></ul>');
            $('body').append($menuContainer);
        }

        // Event handler for right-click on table
        const dtBody = dtApi.table()?.body?.();
        if (dtBody) {
            $(dtBody).off('contextmenu.dt-context-menu').on('contextmenu.dt-context-menu', function (e) {
                // Allow browser's default context menu when Ctrl is held
                if (e.ctrlKey) {
                    return;
                }

                e.preventDefault();
                e.stopPropagation();

                const $cell = $(e.target).closest('td, th');
                if ($cell.length === 0) {
                    return;
                }

                const $row = $cell.closest('tr');
                const rowNode = $row[0];
                if (!rowNode) {
                    return;
                }

                const rowIndex = dtApi.row(rowNode)?.index?.();
                if (rowIndex === undefined || rowIndex === -1) {
                    return;
                }

                handleRowSelection(dtApi, rowIndex, () => {
                    buildAndShowMenu(e.clientX, e.clientY, $cell, dtApi, finalLabels, actionsSelector, copyEnabled);
                });
            });
        }

        /**
         * Build and display the context menu at the given coordinates.
         */
        function buildAndShowMenu(pageX, pageY, $cell, dtApi, labels, actionsSelector, copyEnabled) {
            const $menuContainer = getMenuContainer();
            $menuContainer.empty();

            // Copy
            if (copyEnabled) {
                const cellText = ($cell.text() ?? '').trim();
                appendMenuAction($menuContainer, labels.copy, function (e) {
                    e.preventDefault();
                    copyToClipboard(cellText);
                    hideMenu();
                });
            }

            // Toolbar
            const toolbarItems = [];

            addFilterItem(toolbarItems, labels, dtApi);
            addClearFilterItem(toolbarItems, labels, dtApi);

            if (toolbarItems.length > 0) {
                if (copyEnabled)
                    $menuContainer.append($('<li class="dt-context-menu-separator"></li>'));

                toolbarItems.forEach(function (item) {
                    if (item.filterAction) {
                        renderFilterMenuItem($menuContainer, item, $cell, dtApi);
                    } else if (item.clearFilterAction) {
                        renderClearFilterMenuItem($menuContainer, item, dtApi);
                    } else if (item.$btn?.length > 0) {
                        renderToolbarButtonMenuItem($menuContainer, item);
                    }
                });
            }

            // Custom Actions (ActionBar buttons)
            const $customBtns = findScopedElements(dtApi, actionsSelector).filter(function () {
                return isButtonEnabled($(this));
            });

            if ($customBtns.length > 0) {
                if (copyEnabled || toolbarItems.length > 0)
                    $menuContainer.append($('<li class="dt-context-menu-separator"></li>'));

                $customBtns.each(function () {
                    const $btn = $(this);
                    const btnText = ($btn.text() ?? '').trim();

                    if (btnText?.length > 0) {
                        renderCustomActionMenuItem($menuContainer, $btn, btnText);
                    }
                });
            }

            // Attach event handlers
            $(document).off('click.dt-context-menu').on('click.dt-context-menu', dismissClickHandler);
            $(globalThis)
                .off('scroll.dt-context-menu resize.dt-context-menu')
                .on('scroll.dt-context-menu', dismissScrollHandler)
                .on('resize.dt-context-menu', dismissScrollHandler);
            $menuContainer.off('keydown.dt-context-menu').on('keydown.dt-context-menu', handleMenuKeydown);

            showMenu($menuContainer, pageX, pageY, $cell[0]);
        }
    };

}(jQuery));
