/**
 * DataTables Feature Plugin: Filter Row
 * 
 * This plugin adds a filter row to DataTables with individual column search inputs.
 * It provides a toggle button with popover controls for showing/hiding the filter row
 * and clearing all filters.
 * 
 * @summary Filter row feature for DataTables
 * @requires jQuery, DataTables 1.13+, Bootstrap (for popover)
 * 
 * @example
 *   // Basic usage - manual initialization for DataTables 1.x
 *   let table = $('#example').DataTable({
 *       // ... other options
 *   });
 *   
 *   // Initialize filter row
 *   let filterRow = new DataTable.FilterRow(table.settings()[0], {
 *       buttonId: 'btn-toggle-filter',
 *       placeholderPrefix: 'Filter by'
 *   });
 *   
 *   // For DataTables 2.x, use layout option:
 *   let table = $('#example').DataTable({
 *       layout: {
 *           top2: 'filterRow'
 *       }
 *   });
 */

(function ($, window, document) {
    'use strict';

    let DataTable = $.fn.dataTable;

    // Ensure DataTable is loaded
    if (!DataTable) {
        throw new Error('DataTables FilterRow requires DataTables 1.13 or newer');
    }

    /**
     * FilterRow constructor
     * @param {object} settings - DataTables settings object
     * @param {object} opts - Configuration options
     */
    DataTable.FilterRow = function (settings, opts) {
        // Sanity check
        if (!(this instanceof DataTable.FilterRow)) {
            throw new Error("FilterRow must be initialized with 'new' keyword.");
        }

        if (settings.oInit.filterRow || settings._filterRow) {
            throw new Error("FilterRow already initialized on this table.");
        }

        this.s = {
            dt: new DataTable.Api(settings),
            namespace: '.dtFilterRow',
            filterData: {},
            opts: $.extend({}, DataTable.FilterRow.defaults, opts)
        };

        this.dom = {
            container: null,
            filterRow: null,
            button: null
        };

        settings._filterRow = this;
        this._constructor();
    };

    /**
     * Default configuration options
     */
    DataTable.FilterRow.defaults = {
        buttonId: 'btn-toggle-filter',
        buttonText: 'Filter',
        buttonTextActive: 'Filter*',
        placeholderPrefix: '',
        autoShow: false,
        enablePopover: true,
        popoverPlacement: 'bottom'
    };

    DataTable.FilterRow.prototype = {
        /**
         * Initialize the filter row feature
         * @private
         */
        _constructor: function () {
            const dt = this.s.dt;

            // Create the filter row
            this._buildFilterRow();

            // Initialize popover if enabled
            if (this.s.opts.enablePopover && typeof $.fn.popover !== 'undefined') {
                this._initializePopover();
            }

            // Restore filter state if present
            this._restoreFilterState();

            // Listen for column visibility and reorder events
            dt.on('column-reorder' + this.s.namespace, () => {
                this._rebuildFilterRow();
            });

            dt.on('column-visibility' + this.s.namespace, () => {
                this._rebuildFilterRow();
            });

            // Listen for destroy event to cleanup
            dt.on('destroy' + this.s.namespace, () => {
                this._destroy();
            });

            if (this.s.opts.autoShow) {
                this.dom.filterRow.show();
            }

            // Update button state based on existing filters
            this._updateButtonState();
        },

        /**
         * Build the filter row and append to table
         * @private
         */
        _buildFilterRow: function () {
            const dt = this.s.dt;
            const namespace = this.s.namespace;
            const opts = this.s.opts;
            const filterData = this.s.filterData;
            const updateButtonState = this._updateButtonState.bind(this);
            const filterRow = $('<tr class="tr-toggle-filter" id="tr-filter">').hide();

            dt.columns().every(function () {
                const column = this;
                // Only create filter cells for visible columns
                if (column.visible()) {
                    const title = $(column.header()).text();
                    const colName = dt.settings()[0].aoColumns[column.index()].name || title;

                    if (title && title !== 'Actions' && title !== 'Action' && title !== 'Default') {

                        const placeholder = opts.placeholderPrefix
                            ? opts.placeholderPrefix + ' ' + title
                            : title;

                        const filterValue = filterData[colName] || column.search() || '';

                        const input = $('<input>', {
                            type: 'text',
                            class: 'form-control input-sm custom-filter-input',
                            placeholder: placeholder,
                            value: filterValue,
                            'data-column-name': colName
                        });

                        const cell = $('<td>').append(input);

                        // Apply search value if it differs from current column search
                        if (column.search() !== filterValue) {
                            column.search(filterValue);
                        }

                        // Bind keyup event for filtering
                        input.on('keyup' + namespace, function () {
                            const val = this.value;

                            if (column.search() !== val) {
                                column.search(val).draw();
                                // Store by column name for persistence
                                filterData[colName] = val;
                                updateButtonState();
                            }
                        });

                        filterRow.append(cell);
                    } else {
                        filterRow.append($('<td>'));
                    }
                }
            });

            dt.table().header().parentNode.querySelector('.tr-toggle-filter')?.remove();

            $(dt.table().header()).after(filterRow);
            this.dom.filterRow = filterRow;
        },

        /**
         * Rebuild the filter row (on column reorder/visibility change)
         * @private
         */
        _rebuildFilterRow: function () {
            // Preserve current filter values before rebuilding
            this.dom.filterRow?.find('.custom-filter-input').each((_, el) => {
                const colName = $(el).data('column-name');
                const val = $(el).val();
                if (colName && val) {
                    this.s.filterData[colName] = val;
                }
            });

            const wasVisible = this.dom.filterRow?.is(':visible');

            this._buildFilterRow();

            if (wasVisible) {
                this.dom.filterRow.show();
            }

            this._updateButtonState();
        },

        /**
         * Initialize Bootstrap popover for filter controls
         * @private
         */
        _initializePopover: function () {
            const btnSelector = '#' + this.s.opts.buttonId;
            const $btn = $(btnSelector);

            if (!$btn.length) return;

            this.dom.button = $btn;

            $btn.on('click' + this.s.namespace, () => {
                $btn.popover('toggle');
            });

            $btn.popover({
                html: true,
                container: 'body',
                sanitize: false,
                template: `
                    <div class="popover custom-popover" role="tooltip">
                        <div class="popover-arrow"></div>
                        <div class="popover-body"></div>
                    </div>
                `,
                content: () => {
                    const isChecked = this.dom.filterRow.is(':visible');

                    return `
                        <div class="form-check form-switch">
                            <input class="form-check-input" type="checkbox" id="showFilter" ${isChecked ? 'checked' : ''}>
                            <label class="form-check-label">Show Filter Row</label>
                        </div>
                        <button id="btnClearFilter" class="btn btn-primary" type="button">
                            Clear Filter
                        </button>
                    `;
                },
                placement: this.s.opts.popoverPlacement
            });

            $btn.on('shown.bs.popover' + this.s.namespace, () => {

                const $popover = $('.popover.custom-popover');

                $popover.find('#showFilter')
                    .off('click' + this.s.namespace)
                    .on('click' + this.s.namespace, () => {
                        this.dom.filterRow.toggle();
                        this.s.dt.trigger('filterRow-visibility', [
                            this.dom.filterRow.is(':visible')
                        ]);
                    });

                $popover.find('#btnClearFilter')
                    .off('click' + this.s.namespace)
                    .on('click' + this.s.namespace, () => {
                        this.clearFilters();
                        $btn.popover('hide');
                    });

                // ✅ FIXED OUTSIDE CLICK
                $(document)
                    .off('mousedown' + this.s.namespace)
                    .on('mousedown' + this.s.namespace, (e) => {

                        const $popoverEl = $('.popover.custom-popover');
                        if (!$popoverEl.is(':visible')) return;

                        const $target = $(e.target);

                        const insidePopover = $target.closest('.popover.custom-popover').length > 0;
                        const insideButton = $target.closest(btnSelector).length > 0;

                        if (!insidePopover && !insideButton) {
                            $btn.popover('hide');
                        }
                    });
            });

            // Cleanup popover events on hide
            $btn.on('hide.bs.popover' + this.s.namespace, () => {
                $(document).off('mousedown' + this.s.namespace);
            });
        },

        /**
         * Update button text based on filter state
         * @private
         */
        _updateButtonState: function () {
            let dt = this.s.dt;
            let hasFilters = false;

            dt.columns().every(function () {
                if (this.search()) {
                    hasFilters = true;
                    return false;
                }
            });

            if (this.dom.button) {
                this.dom.button.text(
                    hasFilters ? this.s.opts.buttonTextActive : this.s.opts.buttonText
                );
            }
        },

        /**
         * Restore filter state from DataTables state
         * @private
         */
        _restoreFilterState: function () {
            const dt = this.s.dt;
            const filterData = this.s.filterData;
            let needsRedraw = false;

            dt.columns().every(function (i) {
                const column = this;
                const colName = dt.settings()[0].aoColumns[i].name;
                const title = $(column.header()).text();
                const searchVal = column.search();

                const key = colName || title;

                if (searchVal) {
                    filterData[key] = searchVal;
                    needsRedraw = true;
                }
            });

            if (Object.keys(this.s.filterData).length > 0) {
                this.dom.filterRow.show();
            }

            // Trigger a redraw if there were column filters to apply
            // This ensures the filtered state is applied on page reload
            if (needsRedraw) {
                dt.draw();
            }
        },

        /**
         * Clear all column and global filters
         * @public
         */
        clearFilters: function () {
            let dt = this.s.dt;
            let dtInit = dt.init();
            let externalSearchId = dtInit.externalSearchInputId;
            let initialSortOrder = (dtInit && dtInit.order) ? dtInit.order : [];

            if (externalSearchId) {
                $(externalSearchId).val('');
            }

            this.dom.filterRow.find('.custom-filter-input').val('');

            dt.search('').columns().search('');
            dt.order(initialSortOrder).draw();

            let $quickDateRange = $('#quickDateRange');
            if ($quickDateRange.length) {
                let defaultVal = $quickDateRange.find('option[selected]').val();
                if (defaultVal) {
                    $quickDateRange.val(defaultVal).trigger('change');
                }
            }

            this.s.filterData = {};
            this._updateButtonState();
        },

        show: function () {
            this.dom.filterRow.show();
            this.s.dt.trigger('filterRow-visibility', [true]);
            return this;
        },

        hide: function () {
            this.dom.filterRow.hide();
            this.s.dt.trigger('filterRow-visibility', [false]);
            return this;
        },

        toggle: function () {
            this.dom.filterRow.toggle();
            this.s.dt.trigger('filterRow-visibility', [this.dom.filterRow.is(':visible')]);
            return this;
        },

        _destroy: function () {
            let dt = this.s.dt;

            // Remove event listeners
            dt.off(this.s.namespace);
            $(document).off(this.s.namespace);

            // Remove popover
            if (this.dom.button) {
                this.dom.button.off(this.s.namespace);
                if (this.dom.button.data('bs.popover')) {
                    this.dom.button.popover('dispose');
                }
            }

            this.dom.filterRow?.remove();
            dt.settings()[0]._filterRow = null;
        }
    };

    /**
     * Register as a DataTables feature for DataTables 2.x
     * For DataTables 1.x, this will be ignored but the constructor can still be used manually
     */
    if (DataTable.feature) {
        DataTable.feature.register('filterRow', function (settings, opts) {
            return new DataTable.FilterRow(settings, opts).dom.container || document.createElement('div');
        });
    }

    /**
     * API method to access FilterRow instance
     */
    DataTable.Api.register('filterRow()', function () {
        let ctx = this.context[0];
        return ctx._filterRow || null;
    });

    return DataTable.FilterRow;

})(jQuery);


$(document).on('mousedown', function (e) {
    const $popover = $('.popover.custom-popover');
    if (!$popover.is(':visible')) return;

    const $target = $(e.target);
    const popoverId = $popover.attr('id');
    const $trigger = $('[aria-describedby="' + popoverId + '"]');

    const insidePopover = $target.closest('.popover.custom-popover').length > 0;
    const insideButton = $trigger.length > 0 && $target.closest($trigger).length > 0;

    if (!insidePopover && !insideButton) {
        $trigger.popover('hide');
    }
});