/**
 * DataTables Feature Plugin: Filter Row
 * 
 * This plugin adds a filter row to DataTables with individual column search inputs.
 * It provides a toggle button with popover controls for showing/hiding the filter row
 * and clearing all filters.
 * 
 * @summary Filter row feature for DataTables
 * @author Unity Grant Manager Team
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

 //(function(factory) {
 //    if (typeof define === 'function' && define.amd) {
 //        // AMD
 //        define(['jquery', 'datatables.net'], function($) {
 //            return factory($, window, document);
 //        });
 //    } else if (typeof exports === 'object') {
 //        // CommonJS
 //        module.exports = function(root, $) {
 //            if (!root) {
 //                root = window;
 //            }
 //            if (!$ || !$.fn.dataTable) {
 //                $ = require('datatables.net')(root, $).$;
 //            }
 //            return factory($, root, root.document);
 //        };
 //    } else {
 //        // Browser
 //        factory(jQuery, window, document);
 //    }
 //}
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
            let that = this;
            let dt = this.s.dt;

            // Create the filter row
            this._buildFilterRow();

            // Initialize popover if enabled
            if (this.s.opts.enablePopover && typeof $.fn.popover !== 'undefined') {
                this._initializePopover();
            }

            // Restore filter state if present
            this._restoreFilterState();

            // Listen for column visibility and reorder events
            dt.on('column-reorder' + this.s.namespace, function () {
                that._rebuildFilterRow();
            });

            dt.on('column-visibility' + this.s.namespace, function () {
                that._rebuildFilterRow();
            });

            // Listen for destroy event to cleanup
            dt.on('destroy' + this.s.namespace, function () {
                that._destroy();
            });

            // Show filter row if autoShow is enabled
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
            let dt = this.s.dt;
            let that = this;
            let filterRow = $('<tr class="tr-toggle-filter" id="tr-filter">').hide();

            dt.columns().every(function () {
                let column = this;
                // Only create filter cells for visible columns
                if (column.visible()) {
                    let title = $(column.header()).text();

                    if (title && title !== 'Actions' && title !== 'Action' && title !== 'Default') {
                        let placeholder = that.s.opts.placeholderPrefix ?
                            that.s.opts.placeholderPrefix + ' ' + title :
                            title;

                        let input = $('<input>', {
                            type: 'text',
                            class: 'form-control input-sm custom-filter-input',
                            placeholder: placeholder,
                            value: that.s.filterData[title] || ''
                        });

                        let cell = $('<td>').append(input);

                        // Set initial search value if exists
                        if (column.search() !== (that.s.filterData[title] || '')) {
                            column.search(that.s.filterData[title] || '');
                        }

                        // Bind keyup event for filtering
                        input.on('keyup' + that.s.namespace, function () {
                            let val = this.value;
                            if (column.search() !== val) {
                                column.search(val).draw();
                                that._updateButtonState();
                            }
                        });

                        filterRow.append(cell);
                    } else {
                        // Empty cell for action columns
                        filterRow.append($('<td>'));
                    }
                }
                // Skip hidden columns - don't add any td element
            });

            // Remove existing filter row if present
            dt.table().header().parentNode.querySelector('.tr-toggle-filter')?.remove();

            // Append to table header
            $(dt.table().header()).after(filterRow);
            this.dom.filterRow = filterRow;
        },

        /**
         * Rebuild the filter row (on column reorder/visibility change)
         * @private
         */
        _rebuildFilterRow: function () {
            let wasVisible = this.dom.filterRow && this.dom.filterRow.is(':visible');
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
            let that = this;
            let btnSelector = '#' + this.s.opts.buttonId;
            let $btn = $(btnSelector);

            if (!$btn.length) {
                return; // Button not found, skip popover
            }

            this.dom.button = $btn;

            $btn.on('click' + this.s.namespace, function () {
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
                content: function () {
                    let isChecked = that.dom.filterRow.is(':visible');
                    return `
                        <div class="form-check form-switch">
                            <input class="form-check-input" type="checkbox" id="showFilter" ${isChecked ? 'checked' : ''}>
                            <label class="form-check-label" for="showFilter">Show Filter Row</label>
                        </div>
                        <abp-button id="btnClearFilter" class="btn btn-primary" text="Clear Filter" type="button">CLEAR FILTER</abp-button>
                    `;
                },
                placement: this.s.opts.popoverPlacement
            });

            // Handle popover shown event
            $btn.on('shown.bs.popover' + this.s.namespace, function () {
                let $popover = $('.popover.custom-popover');

                // Toggle filter row visibility
                $popover.find('#showFilter').on('click', function () {
                    that.dom.filterRow.toggle();
                });

                // Clear all filters
                $popover.find('#btnClearFilter').on('click', function () {
                    that.clearFilters();
                    $btn.popover('hide');
                });

                // Close popover on outside click/hover
                $(document).on('click.popover' + that.s.namespace, function (e) {
                    if (!$(e.target).closest(btnSelector).length &&
                        !$(e.target).closest('.popover').length) {
                        $btn.popover('hide');
                    }
                });

                $(document).on('mouseenter.popover' + that.s.namespace, function (e) {
                    if (!$(e.target).closest(btnSelector).length &&
                        !$(e.target).closest('.popover').length) {
                        $btn.popover('hide');
                    }
                });
            });

            // Cleanup popover events on hide
            $btn.on('hide.bs.popover' + this.s.namespace, function () {
                let $popover = $('.popover.custom-popover');
                $popover.find('#showFilter').off('click');
                $popover.find('#btnClearFilter').off('click');
                $(document).off('click.popover' + that.s.namespace);
                $(document).off('mouseenter.popover' + that.s.namespace);
            });
        },

        /**
         * Update button text based on filter state
         * @private
         */
        _updateButtonState: function () {
            let dt = this.s.dt;
            let hasFilters = false;

            // Check column filters
            dt.columns().every(function () {
                if (this.search()) {
                    hasFilters = true;
                    return false;
                }
            });

            // Check global search
            let externalSearchId = dt.init().externalSearchInputId;
            if (externalSearchId) {
                let searchVal = $(externalSearchId).val();
                if (searchVal && searchVal !== '') {
                    hasFilters = true;
                }
            }

            // Update button text
            if (this.dom.button) {
                this.dom.button.text(hasFilters ?
                    this.s.opts.buttonTextActive :
                    this.s.opts.buttonText
                );
            }
        },

        /**
         * Restore filter state from DataTables state
         * @private
         */
        _restoreFilterState: function () {
            let dt = this.s.dt;
            let that = this;

            dt.columns().every(function (i) {
                let column = this;
                let title = $(column.header()).text();
                let searchVal = column.search();

                if (searchVal) {
                    that.s.filterData[title] = searchVal;
                }
            });

            // Show filter row if filters are active
            let externalSearchId = dt.init().externalSearchInputId;
            let searchVal = externalSearchId ? $(externalSearchId).val() : '';
            let hasFilters = Object.keys(this.s.filterData).length > 0 || searchVal !== '';

            if (hasFilters) {
                this.dom.filterRow.show();
            }
        },

        /**
         * Clear all column and global filters
         * @public
         */
        clearFilters: function () {
            let dt = this.s.dt;
            let externalSearchId = dt.init().externalSearchInputId;

            // Clear external search
            if (externalSearchId) {
                $(externalSearchId).val('');
            }

            // Clear custom filter inputs
            $('.custom-filter-input').val('');

            // Clear DataTable searches
            dt.search('').columns().search('').draw();

            // Clear order
            dt.order([]).draw();

            // Reload data
            dt.ajax.reload();

            // Update button state
            this._updateButtonState();

            // Clear internal filter data
            this.s.filterData = {};
        },

        /**
         * Show the filter row
         * @public
         */
        show: function () {
            this.dom.filterRow.show();
            return this;
        },

        /**
         * Hide the filter row
         * @public
         */
        hide: function () {
            this.dom.filterRow.hide();
            return this;
        },

        /**
         * Toggle the filter row visibility
         * @public
         */
        toggle: function () {
            this.dom.filterRow.toggle();
            return this;
        },

        /**
         * Destroy the filter row feature
         * @private
         */
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

            // Remove filter row from DOM
            if (this.dom.filterRow) {
                this.dom.filterRow.remove();
            }

            // Remove reference from settings
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
