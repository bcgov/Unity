/**
 * Unity DataTables v2 Utilities
 * Refactored for DataTables v2 with plugin architecture and layout API
 */

(function(window, document, DataTable, $) {
    'use strict';

    // Constants
    const CONSTANTS = {
        NULL_PLACEHOLDER: '—',
        FILTER_DESC: {
            DEFAULT: 'Filter',
            WITH_FILTER: 'Filter*'
        },
        DEFAULT_LENGTH: 25,
        KEYBOARD_SHORTCUT: 'ctrl+alt+shift+e'
    };

    // Currency formatter for Canadian dollars
    const currencyFormatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });

    /**
     * Unity Filter Feature Plugin
     * Provides advanced filtering capabilities for DataTables
     */
    DataTable.feature.register('unityFilter', function(settings, opts) {
        const options = Object.assign({
            showToggle: true,
            enablePopover: true,
            placeholder: 'Search'
        }, opts);

        const container = document.createElement('div');
        container.className = 'unity-filter-container';
        
        if (options.showToggle) {
            const filterButton = document.createElement('button');
            filterButton.id = 'btn-toggle-filter';
            filterButton.className = 'btn btn-secondary custom-table-btn m-0';
            filterButton.textContent = CONSTANTS.FILTER_DESC.DEFAULT;
            filterButton.type = 'button';
            
            container.appendChild(filterButton);
            
            if (options.enablePopover && typeof $.fn.popover !== 'undefined') {
                initializeFilterPopover(filterButton, settings);
            }
        }

        return container;
    });

    /**
     * Unity Column Management Feature Plugin
     * Provides column visibility management
     */
    DataTable.feature.register('unityColumns', function(settings, opts) {
        const options = Object.assign({
            text: 'Columns',
            className: 'btn btn-secondary custom-table-btn'
        }, opts);

        const container = document.createElement('div');
        container.className = 'unity-columns-container';
        
        // This will be populated after table initialization
        container.setAttribute('data-unity-columns', 'true');
        
        return container;
    });

    /**
     * Unity Export Feature Plugin
     * Enhanced export functionality with null placeholder handling
     */
    DataTable.feature.register('unityExport', function(settings, opts) {
        const options = Object.assign({
            text: 'Export',
            title: 'Export Data',
            hidden: true,
            useNullPlaceholder: false
        }, opts);

        const container = document.createElement('div');
        container.className = 'unity-export-container';
        
        const exportButton = document.createElement('button');
        exportButton.className = `btn btn-secondary custom-table-btn ${options.hidden ? 'hidden-export-btn d-none' : ''}`;
        exportButton.textContent = options.text;
        exportButton.type = 'button';
        
        container.appendChild(exportButton);
        
        // This will be enhanced with actual export functionality after table init
        container.setAttribute('data-unity-export', JSON.stringify(options));
        
        return container;
    });

    /**
     * Enhanced DataTable initialization with v2 layout API
     */
    function createUnityDataTable(selector, options = {}) {
        const config = Object.assign({
            // Default configuration
            defaultVisibleColumns: [],
            maxRowsPerPage: 1000,
            defaultSortColumn: 0,
            serverSideEnabled: true,
            pagingEnabled: true,
            reorderEnabled: true,
            useNullPlaceholder: false,
            externalSearchId: 'search',
            disableColumnSelect: false,
            enableKeyboardShortcuts: true
        }, options);

        // Prepare columns with indices
        const tableColumns = assignColumnIndices(config.listColumns || []);
        const visibleColumns = getVisibleColumnIndexes(tableColumns, config.defaultVisibleColumns);

        // Build layout configuration using v2 layout API
        const layoutConfig = buildLayoutConfig(config);

        // Enhanced DataTable configuration for v2
        const dtConfig = {
            // Core options
            serverSide: config.serverSideEnabled,
            paging: config.pagingEnabled,
            searching: true,
            processing: true,
            
            // Layout instead of dom (but still support traditional buttons)
            layout: layoutConfig,
            
            // Traditional buttons support for complex button configurations
            ...(config.customButtons ? {
                dom: 'Blfrtip',
                buttons: config.customButtons
            } : {}),
            
            // Data and ordering
            order: [[config.defaultSortColumn, 'desc']],
            pageLength: CONSTANTS.DEFAULT_LENGTH,
            lengthMenu: [10, 25, 50, 100],
            
            // External search configuration for ABP
            ...(config.externalSearchId && config.externalSearchId !== 'search' ? {
                externalSearchInputId: `#${config.externalSearchId}`
            } : {}),
            
            // Enhanced features
            scrollX: true,
            scrollCollapse: true,
            fixedHeader: {
                header: true,
                footer: false,
                headerOffset: 0
            },
            
            // Data source
            ajax: createAjaxConfig(config),
            
            // Selection
            select: {
                style: 'multiple',
                selector: 'td:not(:nth-child(8))'
            },
            
            // Column reordering
            colReorder: config.reorderEnabled,
            
            // State management
            stateSave: true,
            stateDuration: 0,
            
            // Language and display
            language: config.languageSetValues || {},
            
            // Columns configuration
            columns: tableColumns,
            columnDefs: buildColumnDefs(visibleColumns, config.useNullPlaceholder, config.listColumnDefs),
            
            // Callbacks
            drawCallback: function() {
                enhanceTableDisplay(this);
            },
            
            initComplete: function() {
                const api = this.api();
                initializeTableFeatures(api, config);
            },
            
            stateLoadParams: function(settings, data) {
                return handleStateLoad(settings, data, config);
            },
            
            stateSaveParams: function(settings, data) {
                return handleStateSave(settings, data, config);
            }
        };

        // Initialize the DataTable with ABP normalization
        const dt = new DataTable(selector, abp.libs.datatables.normalizeConfiguration(dtConfig));
        
        // Initialize keyboard shortcuts
        if (config.enableKeyboardShortcuts) {
            initializeKeyboardShortcuts();
        }
        
        return dt;
    }

    /**
     * Build layout configuration for DataTables v2
     */
    function buildLayoutConfig(config) {
        // If using custom buttons, use a simpler layout to avoid conflicts
        if (config.customButtons) {
            return {
                topStart: 'pageLength',
                topEnd: 'search',
                bottomStart: 'info',
                bottomEnd: 'paging'
            };
        }

        const layout = {
            topStart: null,
            topEnd: null,
            bottomStart: 'info',
            bottomEnd: 'paging'
        };

        // Add filter feature if needed
        if (!config.disableFilter) {
            layout.topStart = {
                unityFilter: {
                    showToggle: true,
                    enablePopover: true
                }
            };
        }

        // Add search in top end by default
        layout.topEnd = ['search'];

        // Add column management if enabled
        if (!config.disableColumnSelect) {
            if (Array.isArray(layout.topEnd)) {
                layout.topEnd.push({
                    unityColumns: {
                        text: 'Columns'
                    }
                });
            } else {
                layout.topEnd = [layout.topEnd, {
                    unityColumns: {
                        text: 'Columns'
                    }
                }];
            }
        }

        // Add export functionality
        if (config.enableExport !== false) {
            if (Array.isArray(layout.topEnd)) {
                layout.topEnd.push({
                    unityExport: {
                        title: config.exportTitle || 'Export Data',
                        useNullPlaceholder: config.useNullPlaceholder
                    }
                });
            } else {
                layout.topEnd = [layout.topEnd, {
                    unityExport: {
                        title: config.exportTitle || 'Export Data',
                        useNullPlaceholder: config.useNullPlaceholder
                    }
                }];
            }
        }

        // Add page length control if needed
        if (config.showPageLength !== false) {
            layout.topStart = layout.topStart ? 
                [layout.topStart, 'pageLength'] : 
                'pageLength';
        }

        return layout;
    }

    /**
     * Create Ajax configuration using ABP DataTables helper
     */
    function createAjaxConfig(config) {
        if (!config.dataEndpoint && !config.data) {
            return null;
        }

        // Use ABP's createAjax helper function
        return abp.libs.datatables.createAjax(
            config.dataEndpoint,
            config.data,
            config.responseCallback || function(result) {
                if (result.totalCount <= config.maxRowsPerPage) {
                    $('.dataTables_paginate').hide();
                }
                return {
                    recordsTotal: result.totalCount,
                    recordsFiltered: result.totalCount,
                    data: result?.items ?? result
                };
            }
        );
    }

    /**
     * Build column definitions for DataTable
     */
    function buildColumnDefs(visibleColumns, useNullPlaceholder, customColumnDefs) {
        const columnDefs = [
            {
                targets: visibleColumns,
                visible: true
            },
            {
                targets: '_all',
                visible: false,
                ...(useNullPlaceholder ? { 
                    defaultContent: CONSTANTS.NULL_PLACEHOLDER,
                    render: function(data, type, row) {
                        return data === null || data === undefined ? CONSTANTS.NULL_PLACEHOLDER : data;
                    }
                } : {})
            }
        ];

        // Add custom column definitions if provided
        if (Array.isArray(customColumnDefs) && customColumnDefs.length > 0) {
            columnDefs.push(...customColumnDefs);
        }

        return columnDefs;
    }

    /**
     * Initialize table features after table creation
     */
    function initializeTableFeatures(api, config) {
        // Set column names as data attributes
        const aoColumns = api.settings()[0].aoColumns;
        api.columns().every(function(i) {
            const name = aoColumns[i].name;
            $(api.column(i).header()).attr('data-name', name);
        });

        // Initialize column management
        if (!config.disableColumnSelect) {
            initializeColumnManagement(api, config);
        }

        // Initialize export functionality
        if (config.enableExport !== false) {
            initializeExportFunctionality(api, config);
        }

        // Initialize filter management
        initializeFilterManagement(api, config);

        // Initialize external search
        if (config.externalSearchId && config.externalSearchId !== 'search') {
            initializeExternalSearch(api, config.externalSearchId);
        }

        // Set up event listeners
        setupTableEventListeners(api);
    }

    /**
     * Initialize column management functionality
     */
    function initializeColumnManagement(api, config) {
        const container = document.querySelector('[data-unity-columns="true"]');
        if (!container) return;

        const columnButton = document.createElement('button');
        columnButton.className = 'btn btn-secondary dropdown-toggle custom-table-btn';
        columnButton.textContent = 'Columns';
        columnButton.setAttribute('data-bs-toggle', 'dropdown');

        const dropdown = document.createElement('div');
        dropdown.className = 'dropdown-menu';

        // Get columns for toggle buttons
        const toggleButtons = getColumnToggleButtons(config.listColumns || [], api);
        toggleButtons.forEach(buttonConfig => {
            const item = document.createElement('a');
            item.className = `dropdown-item ${buttonConfig.active ? 'active' : ''}`;
            item.href = '#';
            item.textContent = buttonConfig.text;
            item.addEventListener('click', (e) => {
                e.preventDefault();
                buttonConfig.action();
                item.classList.toggle('active');
            });
            dropdown.appendChild(item);
        });

        container.appendChild(columnButton);
        container.appendChild(dropdown);
    }

    /**
     * Initialize export functionality
     */
    function initializeExportFunctionality(api, config) {
        const container = document.querySelector('[data-unity-export]');
        if (!container) return;

        const exportConfig = JSON.parse(container.getAttribute('data-unity-export'));
        const button = container.querySelector('button');

        button.addEventListener('click', () => {
            // Create CSV export
            const csvData = generateCSVData(api, exportConfig);
            downloadCSV(csvData, exportConfig.title);
        });
    }

    /**
     * Generate CSV data for export
     */
    function generateCSVData(api, config) {
        const headers = [];
        const rows = [];

        // Get visible column headers
        api.columns(':visible').every(function() {
            const header = $(this.header()).text();
            if (!$(this.header()).hasClass('notexport')) {
                headers.push(header);
            }
        });

        // Get data rows
        api.rows().every(function() {
            const row = [];
            const data = this.data();
            
            api.columns(':visible').every(function(index) {
                if (!$(this.header()).hasClass('notexport')) {
                    let cellData = data[this.dataSrc()];
                    
                    // Handle null placeholder replacement
                    if (config.useNullPlaceholder && cellData === CONSTANTS.NULL_PLACEHOLDER) {
                        cellData = '';
                    }
                    
                    row.push(cellData);
                }
            });
            
            rows.push(row);
        });

        return { headers, rows };
    }

    /**
     * Download CSV file
     */
    function downloadCSV(csvData, filename) {
        const csvContent = [
            csvData.headers.join(','),
            ...csvData.rows.map(row => row.map(cell => `"${cell}"`).join(','))
        ].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${filename}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
    }

    /**
     * Initialize filter management
     */
    function initializeFilterManagement(api, config) {
        // Set up filter row management
        updateFilterRow(api);
        
        // Listen for column visibility changes
        api.on('column-visibility.dt', () => {
            updateFilterRow(api);
        });

        api.on('column-reorder.dt', () => {
            updateFilterRow(api);
        });
    }

    /**
     * Initialize filter popover
     */
    function initializeFilterPopover(button, settings) {
        if (typeof $.fn.popover === 'undefined') return;

        $(button).popover({
            html: true,
            container: 'body',
            sanitize: false,
            placement: 'bottom',
            template: `
                <div class="popover custom-popover" role="tooltip">
                    <div class="popover-arrow"></div>
                    <div class="popover-body"></div>
                </div>
            `,
            content: function() {
                const isChecked = $('.tr-toggle-filter').is(':visible');
                return `
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" id="showFilter" ${isChecked ? 'checked' : ''}>
                        <label class="form-check-label" for="showFilter">Show Filter Row</label>
                    </div>
                    <button id="btnClearFilter" class="btn btn-primary" type="button">Clear Filter</button>
                `;
            }
        });

        // Handle popover events
        $(button).on('shown.bs.popover', function() {
            const api = $(`#${settings.sTableId}`).DataTable();
            
            $('#showFilter').on('click', function() {
                $('.tr-toggle-filter').toggle();
            });

            $('#btnClearFilter').on('click', function() {
                api.search('').columns().search('').draw();
                $('.custom-filter-input').val('');
                updateFilterButton(api);
            });
        });
    }

    /**
     * Update filter row
     */
    function updateFilterRow(api) {
        $('.tr-toggle-filter').remove();
        
        const newRow = $("<tr class='tr-toggle-filter' id='tr-filter' style='display: none;'>");
        
        api.columns().every(function() {
            if (this.visible()) {
                const column = this;
                const th = $('<th></th>');
                
                const input = $('<input type="text" class="form-control custom-filter-input" placeholder="Filter">');
                input.on('keyup change', function() {
                    if (column.search() !== this.value) {
                        column.search(this.value).draw();
                        updateFilterButton(api);
                    }
                });
                
                th.append(input);
                newRow.append(th);
            }
        });

        $(`#${api.table().node().id} thead`).after(newRow);
    }

    /**
     * Update filter button text based on active filters
     */
    function updateFilterButton(api) {
        const searchValue = api.search();
        let hasColumnFilters = false;
        
        api.columns().every(function() {
            if (this.search()) {
                hasColumnFilters = true;
                return false;
            }
        });

        const hasFilter = hasColumnFilters || searchValue !== '';
        $('#btn-toggle-filter').text(
            hasFilter ? CONSTANTS.FILTER_DESC.WITH_FILTER : CONSTANTS.FILTER_DESC.DEFAULT
        );
    }

    /**
     * Setup table event listeners
     */
    function setupTableEventListeners(api) {
        // Prevent row selection when clicking on links
        api.on('user-select', function(e, dt, type, cell, originalEvent) {
            if (originalEvent.target.nodeName.toLowerCase() === 'a') {
                e.preventDefault();
            }
        });

        // Handle checkbox selections
        api.on('draw', function() {
            $('.checkbox-select.select-all').on('change', function() {
                const isChecked = $(this).is(':checked');
                $('.checkbox-select.chkbox').prop('checked', isChecked);
                
                if (typeof PubSub !== 'undefined') {
                    PubSub.publish('datatable_select_all', isChecked);
                }
            });
        });
    }

    /**
     * Initialize keyboard shortcuts
     */
    function initializeKeyboardShortcuts() {
        $(document).on('keydown', function(e) {
            if (e.ctrlKey && e.altKey && e.shiftKey && e.key === 'E') {
                $('.hidden-export-btn').toggleClass('d-none');
                e.preventDefault();
                return false;
            }
        });
    }

    /**
     * Enhanced table display
     */
    function enhanceTableDisplay(table) {
        const tableId = table.api().table().node().id;
        
        // Update pagination button text
        $(`#${tableId}_previous a`).text('<');
        $(`#${tableId}_next a`).text('>');
        
        // Update info text
        $(`#${tableId}_info`).text(function(index, text) {
            return text
                .replace('Showing ', '')
                .replace(' to ', '-')
                .replace(' entries', '');
        });
    }

    /**
     * Handle state save
     */
    function handleStateSave(settings, data, config) {
        const searchValue = $(config.externalSearchId ? `#${config.externalSearchId}` : '#search').val();
        data.search.search = searchValue;

        // Store order by unique keys
        if (Array.isArray(data.order)) {
            data.orderByUniqueKey = data.order.map(order => {
                const columnIndex = order[0];
                const aoCol = settings.aoColumns[columnIndex];
                return [aoCol.name || aoCol.data, order[1]];
            });
        }

        updateFilterButton($(`#${settings.sTableId}`).DataTable());
    }

    /**
     * Handle state load
     */
    function handleStateLoad(settings, data, config) {
        $(config.externalSearchId ? `#${config.externalSearchId}` : '#search').val(data.search.search);

        // Restore order from unique keys
        if (Array.isArray(data.orderByUniqueKey)) {
            const aoColumns = settings.aoColumns;
            data.order = data.orderByUniqueKey.map(orderKey => {
                const columnName = orderKey[0];
                const direction = orderKey[1];
                
                const columnIndex = aoColumns.findIndex(col => 
                    (col.name && col.name === columnName) || col.data === columnName
                );
                
                return columnIndex !== -1 ? [columnIndex, direction] : null;
            }).filter(order => order !== null);
        }

        return true;
    }

    /**
     * Initialize external search functionality
     */
    function initializeExternalSearch(api, searchId) {
        $(`#${searchId}`).on('input', function() {
            api.search(this.value).draw();
        });
    }

    /**
     * Utility function to assign column indices
     */
    function assignColumnIndices(columnsArray) {
        if (!Array.isArray(columnsArray) || columnsArray.length === 0) {
            return [];
        }

        const maxExistingIndex = Math.max(
            ...columnsArray
                .filter(col => 'index' in col && col.index !== undefined && col.index !== '')
                .map(col => parseInt(col.index))
                .concat(-1)
        );

        let nextIndex = maxExistingIndex + 1;
        return columnsArray.map(column => {
            if (column.index !== undefined && column.index !== '') {
                return column;
            }
            return { ...column, index: nextIndex++ };
        });
    }

    /**
     * Get visible column indexes
     */
    function getVisibleColumnIndexes(columns, visibleColumnsArray) {
        let indexes = [];

        if (Array.isArray(visibleColumnsArray) && visibleColumnsArray.length > 0) {
            indexes = visibleColumnsArray
                .map(colName => columns.find(col => col.name === colName || col.data === colName)?.index)
                .filter(index => typeof index !== 'undefined');
        } else {
            indexes = columns
                .map(col => col.index)
                .filter(index => typeof index !== 'undefined');
        }

        if (!indexes.includes(0)) {
            indexes.push(0);
        }

        return indexes.sort();
    }

    /**
     * Get column toggle buttons configuration
     */
    function getColumnToggleButtons(columns, api) {
        return columns
            .filter(col => col.title !== 'Actions' && col.index !== 0)
            .sort((a, b) => a.title.localeCompare(b.title))
            .map(col => ({
                text: col.title,
                active: api.column(col.index).visible(),
                action: () => {
                    const column = api.column(col.index);
                    column.visible(!column.visible());
                }
            }));
    }

    /**
     * Create select column configuration
     */
    function createSelectColumn(title, dataField, uniqueTableId) {
        return {
            title: `<input class="checkbox-select select-all-${uniqueTableId}" type="checkbox">`,
            orderable: false,
            className: 'notexport text-center',
            data: dataField,
            name: 'select',
            index: 0,
            render: function(data) {
                return `<input class="checkbox-select chkbox" id="row_${data}" type="checkbox" value="" title="${title}">`;
            }
        };
    }

    /**
     * Legacy compatibility wrapper for existing code
     */
    function initializeDataTable(options) {
        console.warn('initializeDataTable is deprecated. Use createUnityDataTable instead.');
        
        const selector = options.dt;
        return createUnityDataTable(selector, options);
    }

    // Export public API
    window.Unity = window.Unity || {};
    window.Unity.DataTables = {
        // Main API
        create: createUnityDataTable,
        
        // Utilities
        createSelectColumn: createSelectColumn,
        assignColumnIndices: assignColumnIndices,
        getVisibleColumnIndexes: getVisibleColumnIndexes,
        
        // Constants
        CONSTANTS: CONSTANTS,
        
        // Currency formatter
        currencyFormatter: currencyFormatter,
        
        // Legacy compatibility
        initializeDataTable: initializeDataTable
    };

    // Also expose legacy function for backward compatibility
    window.initializeDataTable = initializeDataTable;
    window.createSelectColumn = createSelectColumn;

})(window, document, DataTable, jQuery);
