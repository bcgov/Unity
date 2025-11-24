/**
 * Placeholder string displayed for null or undefined values in table cells.
 * @constant {string}
 */
const nullPlaceholder = '—';

/**
 * Filter button text descriptions indicating filter state.
 * @constant {Object}
 * @property {string} Default - Text when no filters are applied
 * @property {string} With_Filter - Text when filters are active
 */
const FilterDesc = {
    Default: 'Filter',
    With_Filter: 'Filter*',
};

/**
 * Creates a number formatter for Canadian currency (CAD).
 * @returns {Intl.NumberFormat} Configured number formatter instance
 */
function createNumberFormatter() {
    return new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });
}

// ============================================================================
// DataTables Button Extensions
// ============================================================================

/**
 * CSV export button that removes null placeholder values.
 * Extends the standard DataTables CSV button to replace placeholder characters
 * with empty strings in the exported file.
 * 
 * @example
 * buttons: [
 *     {
 *         extend: 'csvNoPlaceholder',
 *         text: 'Export',
 *         title: 'MyData',
 *         nullPlaceholder: '—'  // Optional, defaults to global nullPlaceholder
 *     }
 * ]
 */
if ($.fn.dataTable !== 'undefined' && $.fn.dataTable.ext) {
    $.fn.dataTable.ext.buttons.csvNoPlaceholder = {
        extend: 'csv',
        exportOptions: {
            columns: ':visible:not(.notexport)',
            orthogonal: 'fullName',
            format: {
                body: function (data, row, column, node) {
                    let placeholder = this.nullPlaceholder || nullPlaceholder;
                    return data === placeholder ? '' : data;
                }
            }
        },
        customize: function (csv, config) {
            // Additional customization can be added here if needed
            return csv;
        }
    };
}

// ============================================================================
// DataTables API Extensions
// ============================================================================

/**
 * Binds an external search input to the DataTable's search functionality.
 * Provides proper event namespacing and automatic cleanup on table destroy.
 * 
 * @name externalSearch
 * @summary Link an external input element to DataTable's search
 * 
 * @param {string|jQuery} selector - jQuery selector or element for the search input
 * @param {object} [options] - Configuration options
 * @param {number} [options.delay=300] - Debounce delay in milliseconds
 * @param {boolean} [options.syncOnInit=true] - Sync search value on initialization
 * @returns {DataTable.Api} DataTables API instance for chaining
 * 
 * @example
 *   // Basic usage
 *   let table = $('#example').DataTable();
 *   table.externalSearch('#mySearchInput');
 * 
 * @example
 *   // With options
 *   table.externalSearch('#mySearchInput', {
 *       delay: 500,
 *       syncOnInit: false
 *   });
 */
if ($.fn.dataTable !== 'undefined' && $.fn.dataTable.Api) {
    $.fn.dataTable.Api.register('externalSearch()', function (selector, options) {
        let opts = $.extend({
            delay: 300,
            syncOnInit: true
        }, options);

        return this.iterator('table', function (settings) {
            let api = new $.fn.dataTable.Api(settings);
            let $input = $(selector);
            let namespace = '.dtExternalSearch';
            let timer;

            if (!$input.length) {
                console.warn('DataTables externalSearch: Selector "' + selector + '" not found');
                return;
            }

            // Sync initial value if enabled
            if (opts.syncOnInit) {
                let currentSearch = api.search();
                if (currentSearch) {
                    $input.val(currentSearch);
                }
            }

            // Debounced search handler
            let doSearch = function () {
                clearTimeout(timer);
                timer = setTimeout(function () {
                    let val = $input.val();
                    if (api.search() !== val) {
                        api.search(val).draw();
                    }
                }, opts.delay);
            };

            // Bind input event with namespace
            $input.on('input' + namespace + ' keyup' + namespace, doSearch);

            // Cleanup on table destroy
            api.one('destroy' + namespace, function () {
                clearTimeout(timer);
                $input.off(namespace);
            });
        });
    });
}

/**
 * Initializes a DataTable with comprehensive configuration including filtering, column management,
 * state persistence, and custom button controls.
 * 
 * @param {Object} options - Configuration options for the DataTable
 * @param {jQuery} options.dt - jQuery element to initialize as DataTable
 * @param {Array<string>} [options.defaultVisibleColumns=[]] - Column names visible by default
 * @param {Array<Object>} options.listColumns - Column definitions with name, data, title, render, etc.
 * @param {number} options.maxRowsPerPage - Maximum rows before hiding pagination controls
 * @param {number} options.defaultSortColumn - Index of column to sort by default
 * @param {string} options.dataEndpoint - API endpoint URL for fetching table data
 * @param {Function|Object} options.data - Data source or function returning request parameters
 * @param {Function} [options.responseCallback] - Custom callback to transform API response
 * @param {Array<Object>} options.actionButtons - DataTables button configurations
 * @param {boolean} options.serverSideEnabled - Enable server-side processing
 * @param {boolean} options.pagingEnabled - Enable pagination
 * @param {boolean} options.reorderEnabled - Enable column reordering
 * @param {Object} options.languageSetValues - DataTables language/localization settings
 * @param {string} options.dataTableName - Unique identifier for the table
 * @param {string} options.dynamicButtonContainerId - DOM ID where buttons are rendered
 * @param {boolean} [options.useNullPlaceholder=false] - Replace nulls with placeholder character
 * @param {string} [options.externalSearchId='search'] - ID of external search input element
 * @param {boolean} [options.disableColumnSelect=false] - Disable column visibility toggle
 * @param {Array<Object>} [options.listColumnDefs] - Additional columnDefs configurations
 * @returns {DataTable} Initialized DataTable API instance
 * 
 * @example
 * const table = initializeDataTable({
 *     dt: $('#myTable'),
 *     listColumns: columns,
 *     defaultVisibleColumns: ['id', 'name', 'status'],
 *     dataEndpoint: '/api/data',
 *     actionButtons: [{ extend: 'csvNoPlaceholder', text: 'Export' }],
 *     useNullPlaceholder: true
 * });
 */
function initializeDataTable(options) {
    const {
        dt,
        defaultVisibleColumns = [],
        listColumns,
        maxRowsPerPage,
        defaultSortColumn,
        dataEndpoint,
        data,
        responseCallback,
        actionButtons,
        serverSideEnabled,
        pagingEnabled,
        reorderEnabled,
        languageSetValues,
        dataTableName,
        dynamicButtonContainerId,
        useNullPlaceholder = false,
        externalSearchId = 'search',
        disableColumnSelect = false,
        listColumnDefs
    } = options;

    // Process columns and visibility
    let tableColumns = assignColumnIndices(listColumns);
    let visibleColumns = getVisibleColumnIndexes(tableColumns, defaultVisibleColumns);

    // Prepare action buttons - use official colvis instead of custom implementation
    let updatedActionButtons = actionButtons.map(button => {
        // Replace csv buttons with csvNoPlaceholder if useNullPlaceholder is enabled
        if (useNullPlaceholder && button.extend === 'csv') {
            return {
                ...button,
                extend: 'csvNoPlaceholder'
            };
        }
        return button;
    });

    // Add official colvis button if column selection is enabled
    if (!disableColumnSelect) {
        updatedActionButtons.push({
            extend: 'colvis',
            text: 'Columns',
            className: 'custom-table-btn flex-none btn btn-secondary',
            columns: ':not(.notexport):not([data-name="select"])',
            columnText: function (dt, idx, title) {
                // Return title for sorting - will be sorted alphabetically by Buttons extension
                return title;
            }
        });
    }

    DataTable.type('num', 'className', 'dt-head-left dt-body-right');
    DataTable.type('num-fmt', 'className', 'dt-head-left');
    DataTable.type('date', 'className', 'dt-head-left');
    DataTable.type('datetime-YYYY-MM-DD', 'className', 'dt-head-left');

    let iDt = new DataTable(dt, {
            serverSide: serverSideEnabled,
            paging: pagingEnabled,
            order: [[defaultSortColumn, 'desc']],
            searching: true,
            externalSearchInputId: `#${externalSearchId}`,
            scrollX: true,
            scrollCollapse: true,
            deferRender: false,
            ajax: abp.libs.datatables.createAjax(
                dataEndpoint,
                data,
                responseCallback ?? function (result) {
                    return {
                        recordsTotal: result.totalCount,
                        recordsFiltered: result.totalCount,
                        data: result?.items ?? result
                    };
                }
            ),
            select: {
                style: 'multiple',
                selector: 'td:not(:nth-child(8))',
            },
            colReorder: reorderEnabled,
            orderCellsTop: true,
            language: {
                ...languageSetValues,
                lengthMenu: 'Show _MENU_ _ENTRIES_'
            },
            layout: {
                topStart: {
                    search: {
                        placeholder: 'Search'
                    }
                },
                topEnd: {
                    buttons: updatedActionButtons
                },
                bottomStart: null,
                bottomEnd: null,
                bottom1: {
                    info: {
                        text: '_START_-_END_ of _TOTAL_'
                    },
                    paging: {
                        buttons: 3,
                        boundaryNumbers: true,
                        firstLast: false
                    },
                    pageLength: {
                        menu: [1, 10, 25, 50, 100],
                    }
                }
            },
            initComplete: function () {
                const api = this.api();
                const aoColumns = api.settings()[0].aoColumns;

                api.columns().every(function (i) {
                    const name = aoColumns[i].name;
                    $(api.column(i).header()).attr('data-name', name);
                });
            },
            columns: tableColumns,
            columnDefs: [
                {
                    targets: visibleColumns,
                    visible: true,
                },
                {
                    targets: '_all',
                    // Hide all other columns initially
                    visible: false,
                    // Set default content for all cells to placeholder if null
                    ...(useNullPlaceholder
                        ? { defaultContent: nullPlaceholder }
                        : {}),
                },
                // Add listColumnDefs if not null or empty
                ...(Array.isArray(listColumnDefs) && listColumnDefs.length > 0
                    ? listColumnDefs
                    : []),
            ],
            processing: true,
            stateSave: true,
            stateDuration: 0,
            // Simplified state management - let DataTables handle column order/visibility
            // Column names provide stable identifiers across sessions
            stateSaveParams: function (settings, data) {
                // Only save external search value if applicable
                let externalSearch = $(settings.oInit.externalSearchInputId);
                if (externalSearch.length) {
                    data.externalSearch = externalSearch.val();
                }
            },
            stateLoadParams: function (settings, data) {
                // Restore external search value if saved
                if (data.externalSearch) {
                    let externalSearch = $(settings.oInit.externalSearchInputId);
                    if (externalSearch.length) {
                        externalSearch.val(data.externalSearch);
                    }
                }
            }
        }
    );

    // Initialize FilterRow plugin if filter button exists
    if ($('#btn-toggle-filter').length) {
        if ($.fn.dataTable !== 'undefined' && typeof $.fn.dataTable.FilterRow !== 'undefined') {
            const filterRow = new $.fn.dataTable.FilterRow(iDt.settings()[0], {
                buttonId: 'btn-toggle-filter',
                buttonText: FilterDesc.Default,
                buttonTextActive: FilterDesc.With_Filter,
                enablePopover: $.fn.popover !== 'undefined'
            });

            iDt.settings()[0]._filterRow = filterRow;
        } else {
            console.warn('FilterRow plugin not loaded. Include plugins/filterRow.js before table-utils.js');
        }
    }

    // Move buttons to designated container (if buttons exist)
    // Note: In DT 2.x, buttons need to be explicitly created via buttons() API
    if (updatedActionButtons && updatedActionButtons.length > 0) {
        const buttonsContainer = iDt.buttons().container();
        if (buttonsContainer.length && $(`#${dynamicButtonContainerId}`).length) {
            buttonsContainer.prependTo(`#${dynamicButtonContainerId}`);
        } else if (!buttonsContainer.length) {
            console.warn('Buttons container not found. Ensure Buttons extension is loaded and buttons are configured.');
        }
    }

    // Initialize table (clear default styles, reset search)
    init(iDt);

    // Setup external search if provided
    if (externalSearchId && externalSearchId != false && $('#' + externalSearchId).length) {
        if (typeof iDt.externalSearch === 'function') {
            iDt.externalSearch('#' + externalSearchId, { delay: 300 });
        } else {
            console.warn('DataTables externalSearch API not registered. Ensure table-utils.js API extensions are loaded.');
        }
    }

    // Prevent row selection when clicking on links inside cells
    iDt.on('user-select', function (e, dt, type, cell, originalEvent) {
        if (originalEvent.target.nodeName.toLowerCase() === 'a') {
            e.preventDefault();
        }
    });

    return iDt;
}

/**
 * Assigns sequential index values to columns that don't have one.
 * Preserves existing indices and continues numbering from the highest existing index.
 * @param {Array<Object>} columnsArray - Array of column definition objects
 * @returns {Array<Object>} Column array with all indices assigned
 */
function assignColumnIndices(columnsArray) {
    if (!Array.isArray(columnsArray) || columnsArray.length === 0) {
        return [];
    }

    const maxExistingIndex = Math.max(
        ...columnsArray
            .filter(
                (col) =>
                    'index' in col &&
                    col.index !== undefined &&
                    col.index !== ''
            )
            .map((col) => Number.parseInt(col.index))
            .concat(-1)
    );

    let nextIndex = maxExistingIndex + 1;
    return columnsArray.map((column) => {
        // Preserve existing index if it exists
        if (column.index !== undefined && column.index !== '') {
            return column;
        }

        // Assign new index starting after max existing index
        return {
            ...column,
            index: nextIndex++,
        };
    });
}

/**
 * Determines which column indices should be visible based on column names.
 * Always includes index 0 (typically the select column).
 * @param {Array<Object>} columns - Array of column definitions with name/data/index properties
 * @param {Array<string>} visibleColumnsArray - Names of columns that should be visible
 * @returns {Array<number>} Sorted array of column indices to show
 */
function getVisibleColumnIndexes(columns, visibleColumnsArray) {
    let indexes = [];

    if (Array.isArray(visibleColumnsArray) && visibleColumnsArray.length > 0) {
        // Get indexes from provided visible column names.
        indexes = visibleColumnsArray
            .map(
                (colName) =>
                    columns.find(
                        (col) => col.name === colName || col.data === colName
                    )?.index
            )
            .filter((index) => typeof index !== 'undefined');
    } else {
        // If visibleColumnsArray is empty, include all column indexes.
        indexes = columns
            .map((col) => col.index)
            .filter((index) => typeof index !== 'undefined');
    }

    // Always add 0 if not already present
    if (!indexes.includes(0)) {
        indexes.push(0);
    }

    return indexes.sort();
}

/**
 * Dynamically adjusts the table scroll body height based on viewport size.
 * Prevents tables from exceeding viewport height while maintaining usability.
 * @param {string} tableName - ID of the DataTable element (without wrapper suffix)
 */
function setTableHeighDynamic(tableName) {
    const dtContainer = $(`#${tableName}`).closest('.dt-container');
    if (!dtContainer.length) return;

    let tableHeight = dtContainer[0].clientHeight;
    let docHeight = document.body.clientHeight;
    let tableOffset = 425;

    if (tableHeight + tableOffset > docHeight) {
        dtContainer.find('.dt-scroll-body').css({
            height: docHeight - tableOffset,
        });
    } else {
        dtContainer.find('.dt-scroll-body').css({
            height: tableHeight + 10,
        });
    }
}

/**
 * Creates a column definition for a checkbox selection column.
 * Typically used as the first column (index 0) for row selection.
 * @param {string} title - Tooltip text for individual row checkboxes
 * @param {string} dataField - Data property to bind for checkbox IDs
 * @param {string} uniqueTableId - Unique identifier for the table instance
 * @returns {Object} DataTables column definition object
 */
function getSelectColumn(title, dataField, uniqueTableId) {
    return {
        title: `<input class="checkbox-select select-all-${uniqueTableId}"  type="checkbox">`,
        orderable: false,
        className: 'notexport text-center',
        data: dataField,
        name: 'select',
        index: 0,
        render: function (data) {
            return `<input class="checkbox-select chkbox" id="row_${data}" type="checkbox" value="" title="${title}">`;
        },

    };
}

/**
 * Initializes the DataTable by clearing default button classes and resetting search state.
 * @param {DataTable} iDt - DataTable API instance
 */
function init(iDt) {
    $('.custom-table-btn').removeClass('dt-button buttons-csv buttons-html5');
    iDt.search('').columns().search('').draw();
}

// initializeFilterButtonPopover function removed - replaced by FilterRow plugin

/**
 * Toggles the visibility of the filter row in the DataTable.
 * @deprecated This function appears unused in the current codebase
 */
function toggleFilterRow() {
    $(this).popover('toggle');
    $('#dtFilterRow').toggleClass('hidden');
}

/**
 * Finds a DataTable column by its header title text.
 * @param {string} title - Text content of the column header
 * @param {DataTable} dataTable - DataTable API instance
 * @returns {Object} DataTable column API object
 */
function findColumnByTitle(title, dataTable) {
    let columnIndex = dataTable
        .columns()
        .header()
        .map((c) => $(c).text())
        .indexOf(title);

    let res = dataTable.column(columnIndex);
    return res;
}

/**
 * Retrieves a column definition object by its name property.
 * @param {string} name - Name of the column to find
 * @param {Array<Object>} columns - Array of column definition objects
 * @returns {Object|undefined} Column definition or undefined if not found
 */
function getColumnByName(name, columns) {
    return columns.find((obj) => obj.name === name);
}

// isColumnVisToggled, toggleManageColumnButton, and getColumnToggleButtonsSorted functions removed
// Replaced by official DataTables Buttons 'colvis' extension

// setExternalSearchFilter function removed - replaced by externalSearch() API method

// updateFilter, searchFilter, and updateFilterButton functions removed
// Replaced by FilterRow plugin (see plugins/filterRow.js)

/**
 * Event handler for the select-all checkbox in DataTables.
 * Publishes PubSub events to notify subscribers of select/deselect all actions.
 */
$('.data-table-select-all').click(function () {
    if ($('.data-table-select-all').is(':checked')) {
        PubSub.publish('datatable_select_all', true);
    } else {
        PubSub.publish('datatable_select_all', false);
    }
});

/**
 * Returns common action button configurations for DataTables including Filter and Export.
 * Export button is hidden by default and can be toggled with Ctrl+Alt+Shift+E.
 * Uses the csvNoPlaceholder button extension for proper null handling.
 * 
 * @param {string} exportTitle - Title text for the exported CSV file
 * @returns {Array<Object>} Array of DataTables button configuration objects
 * 
 * @example
 * const buttons = commonTableActionButtons('MyReport');
 * // Returns filter button and csvNoPlaceholder export button
 */
function commonTableActionButtons(exportTitle) {
    return [
        {
            text: 'Filter',
            id: 'btn-toggle-filter',
            className: 'btn-secondary custom-table-btn m-0',
            action: function (e, dt, node, config) { },
            attr: {
                id: 'btn-toggle-filter',
            },
        },
        {
            extend: 'csvNoPlaceholder',
            text: 'Export',
            title: exportTitle,
            className:
                'custom-table-btn flex-none btn btn-secondary hidden-export-btn d-none',
        },
    ];
}

/**
 * Global keyboard shortcut handler to toggle visibility of hidden export buttons.
 * Listens for Ctrl+Alt+Shift+E and toggles the d-none class on .hidden-export-btn elements.
 * @listens document#keydown
 */
$(document).keydown(function (e) {
    if (e.ctrlKey && e.altKey && e.shiftKey && e.key === 'E') {
        // Toggle d-none class on elements with hidden-export class
        $('.hidden-export-btn').toggleClass('d-none');

        // Prevent default behavior
        e.preventDefault();
        return false;
    }
});