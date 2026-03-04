/**
 * DataTables Feature Plugin: ScrollResize
 *
 * Dynamically calculates and sets the DataTable scroll body height so that
 * the table header, body, and pagination footer all remain visible within
 * the browser viewport at 100 % magnification, regardless of the number of
 * columns, rows, or the visibility of auxiliary elements such as filter rows.
 *
 * Instead of relying on static CSS `calc()` values, this plugin measures the
 * actual position of the scroll body in the viewport and subtracts all chrome
 * (navbar, action bar, column headers, pagination) to arrive at the correct
 * height.  It recalculates on every event that can change the layout:
 *   - window resize
 *   - DataTables draw / column-visibility / column-reorder
 *   - FilterRow show / hide  (custom `filterRow-visibility` event)
 *   - ResizeObserver on the scroll-head element (column title wrapping)
 *
 * Inspired by the official DataTables ScrollResize plugin
 * (https://github.com/DataTables/Plugins/tree/main/features/scrollResize)
 * but adapted for a full-viewport layout where `body { overflow: hidden }`.
 *
 * @summary  Dynamic scroll-body sizing for DataTables
 * @requires jQuery, DataTables 2+
 *
 * @example
 *   // Automatic initialisation via initializeDataTable():
 *   initializeDataTable({ ..., fixedHeaders: true });
 *
 * @example
 *   // Manual initialisation after DataTable creation:
 *   let table = $('#example').DataTable({ scrollY: '100px', scrollCollapse: true });
 *   DataTable.ScrollResize(table);
 */

(function ($) {
    'use strict';

    let DataTable = $.fn.dataTable;

    if (!DataTable) {
        throw new Error('DataTables ScrollResize requires DataTables');
    }

    // Resolve DataTables CSS class names from the canonical registry so the
    // plugin stays correct if DataTables ever renames its classes.
    let classes = DataTable.ext.classes;
    let scrollClasses = classes.scrolling;
    let CSS_SCROLL_BODY = scrollClasses.body;            // 'dt-scroll-body'
    let CSS_SCROLL_HEAD = scrollClasses.header.self;      // 'dt-scroll-head'
    let CSS_SCROLL_WRAP = scrollClasses.container;        // 'dt-scroll'
    let CSS_LAYOUT_ROW  = classes.layout.row;             // 'dt-layout-row'

    // Custom classes (not from DataTables)
    let CSS_SCROLL_RESIZE = 'dt-scroll-resize';
    let CSS_UNITY_FOOTER  = 'dt-unity-footer';

    /**
     * @param {DataTable.Api} dt   - DataTables API instance
     * @param {Object}        opts - Configuration options
     * @param {number}  [opts.minHeight=150]    Minimum scroll body height in px
     * @param {number}  [opts.buffer=32]        Extra px subtracted as safety margin
     * @param {number}  [opts.throttleDelay=30] Throttle interval for resize (ms)
     */
    class ScrollResize {
        constructor(dt, opts) {
            if (!(this instanceof ScrollResize)) {
                throw new Error("ScrollResize must be initialised with the 'new' keyword.");
            }

            let table = dt.table();
            let container = $(table.container());

            this.s = $.extend({
                minHeight: 150,
                buffer: 32,
                throttleDelay: 30
            }, opts);

            this.s.dt = dt;
            this.s.table = $(table.node());
            this.s.container = container;
            this.s.scrollBody = container.find('div.' + CSS_SCROLL_BODY);
            this.s.scrollHead = container.find('div.' + CSS_SCROLL_HEAD);
            this.s.namespace = '.dtScrollResize' + (ScrollResize._counter++);

            // Guard: scrollY must be enabled for a scroll body to exist
            if (!this.s.scrollBody.length) {
                console.warn('ScrollResize: no .' + CSS_SCROLL_BODY + ' found – is scrollY enabled?');
                return;
            }

            // Mark container so CSS can opt out of static max-height rules
            container.addClass(CSS_SCROLL_RESIZE);

            this._bindEvents();
            // Use a small delay so the table is fully laid out before the first calc
            let that = this;
            setTimeout(function () { that._size(); }, 0);
        }

        /**
         * Core sizing calculation.
         *
         * Uses getBoundingClientRect so we automatically account for every
         * element above the scroll body (navbar, action bar, search row,
         * column headers, filter row, …) without hard-coding selectors.
         */
        _size() {
            let scrollBody = this.s.scrollBody;
            if (!scrollBody.length || !scrollBody.is(':visible')) return;

            // 1. Where does the scroll body start in the viewport?
            let scrollBodyRect = scrollBody[0].getBoundingClientRect();
            let topOffset = scrollBodyRect.top;

            // 2. How tall is the footer area below the scroll body?
            let footerHeight = this._getFooterHeight();

            // 3. Available height = viewport – top – footer – buffer
            let available = window.innerHeight - topOffset - footerHeight - this.s.buffer;
            let newHeight = Math.max(Math.round(available), this.s.minHeight);

            // 4. Apply – only touch the DOM when the value actually changed
            let currentHeight = scrollBody[0].style.height;
            let newHeightPx = newHeight + 'px';
            if (currentHeight !== newHeightPx) {
                scrollBody.css({ 'height': newHeightPx, 'max-height': newHeightPx });
            }
        }

        /**
         * Measure the height of the footer area below the scroll body
         * (pagination, info, page-length controls).
         *
         * Primary:  looks for the .dt-unity-footer element inside the
         *           container and measures its parent .dt-layout-row.
         * Fallback: traverses from .dt-scroll up to its parent
         *           .dt-layout-row and sums every subsequent sibling row.
         */
        _getFooterHeight() {
            let container = this.s.container;
            let total = 0;

            // Primary: use the .dt-unity-footer marker class
            let footer = container.find('.' + CSS_UNITY_FOOTER);
            if (footer.length) {
                // The footer element sits inside a .dt-layout-row wrapper;
                // measure whichever is the outermost so margins are included.
                let row = footer.closest('.' + CSS_LAYOUT_ROW);
                total = (row.length ? row : footer).outerHeight(true) || 0;
                return total;
            }

            // Fallback: DOM traversal for non-standard layouts
            let scrollWrapper = this.s.scrollBody.closest('.' + CSS_SCROLL_WRAP);
            let tableLayoutRow = scrollWrapper.closest('.' + CSS_LAYOUT_ROW);

            if (tableLayoutRow.length) {
                tableLayoutRow.nextAll().each(function () {
                    total += $(this).outerHeight(true) || 0;
                });
            }

            return total;
        }

        /**
         * Bind all the events that should trigger a recalculation.
         */
        _bindEvents() {
            let that = this;
            let ns = this.s.namespace;
            let dt = this.s.dt;

            // --- Window resize (throttled) ---
            let resizeTimer;
            $(window).on('resize' + ns, function () {
                clearTimeout(resizeTimer);
                resizeTimer = setTimeout(function () { that._size(); }, that.s.throttleDelay);
            });

            // --- DataTables events ---
            dt.on('draw' + ns, function () { that._size(); });
            dt.on('column-visibility' + ns, function () {
                // Small delay so the DOM has reflowed after column toggle
                setTimeout(function () { that._size(); }, 30);
            });
            dt.on('column-reorder' + ns, function () {
                setTimeout(function () { that._size(); }, 30);
            });

            // --- FilterRow visibility (custom event emitted by filterRow.js) ---
            dt.on('filterRow-visibility' + ns, function () {
                setTimeout(function () { that._size(); }, 30);
            });

            // --- ResizeObserver on scroll-head (detects header height changes) ---
            if (typeof ResizeObserver !== 'undefined' && this.s.scrollHead.length) {
                this._resizeObserver = new ResizeObserver(function () {
                    that._size();
                });
                this._resizeObserver.observe(this.s.scrollHead[0]);
            }

            dt.on('destroy' + ns, function () {
                that._destroy();
            });
        }

        /**
         * Remove all bound listeners, observers, and inline styles.
         */
        _destroy() {
            let ns = this.s.namespace;

            $(window).off(ns);
            this.s.dt.off(ns);

            if (this._resizeObserver) {
                this._resizeObserver.disconnect();
                this._resizeObserver = null;
            }

            this.s.scrollBody.css({ 'height': '', 'max-height': '' });
            this.s.container.removeClass(CSS_SCROLL_RESIZE);
        }
    }

    ScrollResize._counter = 0;

    DataTable.ScrollResize = ScrollResize;

    return DataTable.ScrollResize;

})(jQuery);
