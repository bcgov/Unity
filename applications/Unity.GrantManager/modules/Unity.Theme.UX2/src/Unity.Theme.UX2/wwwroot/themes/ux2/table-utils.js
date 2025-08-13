const nullPlaceholder = '—';
const FilterDesc = {
    Default: 'Filter',
    With_Filter: 'Filter*',
};

function createNumberFormatter() {
    return new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });
}

function removePlaceholderFromCvsExportButton(actionButtons, useNullPlaceholder, nullPlaceholder) {
    if (!useNullPlaceholder) {
        return actionButtons;
    }
    return actionButtons.map((button) => {
        if (button.extend === 'csv') {
            return {
                ...button,
                exportOptions: {
                    ...button.exportOptions,
                    format: {
                        body: function (data, row, column, node) {
                            return data === nullPlaceholder ? '' : data;
                        },
                    },
                },
            };
        }
        return button;
    });
}

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
        listColumnDefs,
    } = options;

    // If useNullPlaceholder is true, update csv export buttons to include the example format function
    let updatedActionButtons = removePlaceholderFromCvsExportButton(actionButtons, useNullPlaceholder, nullPlaceholder);
    let tableColumns = assignColumnIndices(listColumns);
    let visibleColumns = getVisibleColumnIndexes(tableColumns, defaultVisibleColumns);
    let filterData = {};

    let iDt = dt.DataTable(
        abp.libs.datatables.normalizeConfiguration({
            fixedHeader: {
                header: true,
                footer: false,
                headerOffset: 0
            },
            serverSide: serverSideEnabled,
            paging: pagingEnabled,
            order: [[defaultSortColumn, 'desc']],
            searching: true,
            externalSearchInputId: `#${externalSearchId}`,
            iDisplayLength: 25,
            lengthMenu: [10, 25, 50, 100],
            scrollX: true,
            scrollCollapse: true,
            ajax: abp.libs.datatables.createAjax(
                dataEndpoint,
                data,
                responseCallback ?? function (result) {
                        if (result.totalCount <= maxRowsPerPage) {
                            $('.dataTables_paginate').hide();
                        }
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
            //fixedHeader: true,
            stateSave: true,
            stateDuration: 0,
            oLanguage: languageSetValues,
            dom: 'Blfrtip',
            buttons: updatedActionButtons,
            drawCallback: function () {
                $(`#${dt[0].id}_previous a`).text('<');
                $(`#${dt[0].id}_next a`).text('>');
                $(`#${dt[0].id}_info`).text(function (index, text) {
                    return text
                        .replace('Showing ', '')
                        .replace(' to ', '-')
                        .replace(' entries', '');
                });
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
            stateSaveParams: function (settings, data) {
                let searchValue = $(settings.oInit.externalSearchInputId).val();
                data.search.search = searchValue;

                // Assign unique keys to columns based on their original index
                data.columns.forEach((col, idx) => {
                    let aoCol = settings.aoColumns[idx];
                    let originalIdx =
                        typeof aoCol._ColReorder_iOrigCol !== 'undefined'
                            ? aoCol._ColReorder_iOrigCol
                            : idx;
                    let originalCol = settings.aoColumns.find(
                        (col) => col.index === originalIdx
                    );
                    data.columns[originalIdx].uniqueKey = originalCol.name;
                });

                if (Array.isArray(data.order)) {
                    data.orderByUniqueKey = data.order.map(([colIdx, dir]) => {
                        const col = data.columns[colIdx];
                        return col ? { uniqueKey: col.uniqueKey, dir } : null;
                    }).filter(x => x);
                }

                let hasFilter =
                    data.columns.some((value) => value.search.search !== '') ||
                    searchValue !== '';
                $('#btn-toggle-filter').text(
                    hasFilter ? FilterDesc.With_Filter : FilterDesc.Default
                );
            },
            stateLoadParams: function (settings, data) {
                $(settings.oInit.externalSearchInputId).val(data.search.search);
                let stateCorrupted = false;
                const tableId = settings.sTableId || settings.nTable.id;
                const aoColumns = settings.aoColumns;

                // Restore order from uniqueKey if available
                if (Array.isArray(data.orderByUniqueKey)) {
                    data.order = data.orderByUniqueKey.map(orderObj => {
                        // Find the current index for this uniqueKey
                        const idx = data.columns.findIndex(col => col.uniqueKey === orderObj.uniqueKey);
                        return [idx, orderObj.dir];
                    }).filter(([idx]) => idx !== -1);
                }

                data.columns.forEach((column, index) => {
                    if (aoColumns[index] + '' != 'undefined') {
                        const name = aoColumns[index].name;
                        const dataObj = data.columns.find(
                            (col) => col.uniqueKey === name
                        );

                        const title = aoColumns[index].sTitle;

                        if (typeof dataObj === 'undefined') {
                            localStorage.removeItem(
                                `DataTables_${tableId}_${window.location.pathname}`
                            );
                            cleanInvalidStateRestore(tableId);
                            stateCorrupted = true;
                        } else {
                            const value = dataObj?.search?.search ?? '';
                            filterData[title] = value;
                        }
                    }
                });

                if (stateCorrupted) {
                    window.location.reload();
                    return false;
                }
            },
            stateLoaded: function (settings, data) {
                let dtApi = null;
                const tableId = settings.sTableId || settings.nTable.id;
               
                try {
                    dtApi = new $.fn.dataTable.Api(settings);

                    if (!dtApi?.table()?.node()) {
                        throw new Error('Invalid DataTable instance.');
                    }

                    //Restore Column visibility
                    if (Array.isArray(data.columns)) {
                        data.columns.forEach((savedCol) => {
                            const colIndex = settings.aoColumns.findIndex(col => col.name === savedCol.uniqueKey);
                            if (colIndex !== -1) {
                                dtApi.column(colIndex).visible(savedCol.visible, false);
                            }
                        });
                    }

                    //Re-sync tableColumns based on current table state
                    tableColumns.forEach((col) => {
                        const colIdx = col.index;
                        if (dtApi.column(colIdx).header()) {
                            col.visible = dtApi.column(colIdx).visible();
                        }
                    });

                    //Rebuild custom ColVis
                    const colvisBtn = dtApi.button('customColvis:name');
                    if (colvisBtn) {
                        colvisBtn.collectionRebuild(
                            getColumnToggleButtonsSorted(tableColumns, dtApi)
                        );
                    }

                    if (Array.isArray(data.order) && data.order.length > 0) {
                        const adjustedOrder = data.order.map(([visualIdx, dir]) => {
                            const originalIdx = dtApi.colReorder?.transpose?.(visualIdx);
                            return [originalIdx, dir];
                        });
                        dtApi.order(adjustedOrder).draw();
                    } else {
                        dtApi.columns.adjust().draw(false);
                    }
                } catch (err) {
                    console.warn('StateLoaded failed:', err);
                    const stateKey = `DataTables_${tableId}_${window.location.pathname}`;
                    localStorage.removeItem(stateKey);
                }
            },
        })
    );

    function cleanInvalidStateRestore(tableId) {
        Object.keys(localStorage)
            .filter(
                (key) =>
                    key.includes('DataTables_stateRestore') &&
                    key.includes(`${tableId}`)
            )
            .forEach((key) => {
                try {
                    const value = localStorage.getItem(key);
                    if (!value) return;
                    const obj = JSON.parse(value);
                    if (Array.isArray(obj.columns)) {
                        const hasMissingUniqueKey = obj.columns.some(
                            (col) => !('uniqueKey' in col)
                        );
                        if (hasMissingUniqueKey) {
                            localStorage.removeItem(key);
                        }
                    }
                } catch (e) {
                    console.warn(
                        `Could not process DataTables state for key: ${key}`,
                        e
                    );
                }
            });
    }

    // Add custom manage columns button that remains sorted alphabetically
    if (!disableColumnSelect) {
        iDt.button().add(updatedActionButtons.length + 1, {
            text: 'Columns',
            name: 'customColvis',
            extend: 'collection',
            buttons: getColumnToggleButtonsSorted(tableColumns, iDt),
            className: 'custom-table-btn flex-none btn btn-secondary',
        });
    }

    iDt.buttons().container().prependTo(`#${dynamicButtonContainerId}`);
    $(`#${dataTableName}_wrapper`).append(
        `<div class="length-menu-footer ${dataTableName}"></div>`
    );
    // Move the length menu to the footer container
    $(`#${dataTableName}_length`).appendTo(`.${dataTableName}`);
    init(iDt);

    updateFilter(iDt, dt[0].id, filterData);

    iDt.on('column-reorder.dt', function (e, settings) {
        updateFilter(iDt, dt[0].id, filterData);
    });
    iDt.on(
        'column-visibility.dt',
        function (e, settings, deselectedcolumn, state) {
            updateFilter(iDt, dt[0].id, filterData);
        }
    );

    // On the ITAdministrator pages, bootstrap popover is not loaded and causes the js to fail
    if($('#btn-toggle-filter').popover+"" != "undefined") {        
        initializeFilterButtonPopover(iDt);
    }

    searchFilter(iDt);

    setExternalSearchFilter(iDt);

    // Prevent row selection when clicking on a link inside a cell
    iDt.on('user-select', function (e, dt, type, cell, originalEvent) {
        if (originalEvent.target.nodeName.toLowerCase() === 'a') {
            e.preventDefault();
        }
    });

    return iDt;
}

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
            .map((col) => parseInt(col.index))
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

function setTableHeighDynamic(tableName) {
    let tableHeight = $(`#${tableName}_wrapper`)[0].clientHeight;
    let docHeight = document.body.clientHeight;
    let tableOffset = 425;

    if (tableHeight + tableOffset > docHeight) {
        $(`#${tableName}_wrapper .dataTables_scrollBody`).css({
            height: docHeight - tableOffset,
        });
    } else {
        $(`#${tableName}_wrapper .dataTables_scrollBody`).css({
            height: tableHeight + 10,
        });
    }
}

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

function init(iDt) {
    $('.custom-table-btn').removeClass('dt-button buttons-csv buttons-html5');
    iDt.search('').columns().search('').draw();
}

function initializeFilterButtonPopover(iDt) {
    const UIElements = {
        search: $(iDt.init().externalSearchInputId),
        btnToggleFilter: $('#btn-toggle-filter'),
    };

    UIElements.btnToggleFilter.on('click', function () {
        UIElements.btnToggleFilter.popover('toggle');
    });

    UIElements.btnToggleFilter.popover({
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
            const isChecked = $('.tr-toggle-filter').is(':visible');
            return `
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" id="showFilter" ${
                            isChecked ? 'checked' : ''
                        }>
                        <label class="form-check-label" for="showFilter">Show Filter Row</label>
                    </div>
                    <abp-button id="btnClearFilter" class="btn btn-primary" text="Clear Filter" type="button">CLEAR FILTER</abp-button>
                   `;
        },
        placement: 'bottom',
    });

    UIElements.btnToggleFilter.on('shown.bs.popover', function () {
        const searchElement = $(iDt.init().externalSearchInputId);
        const trToggleElement = $('.tr-toggle-filter');
        const popoverElement = $('.popover.custom-popover');
        const customFilterElement = $('.custom-filter-input');

        popoverElement.find('#showFilter').on('click', () => {
            trToggleElement.toggle();
        });

        popoverElement.find('#btnClearFilter').on('click', () => {
            searchElement.val('');
            customFilterElement.val('');

            $(this).text(FilterDesc.Default);
            iDt.search('').columns().search('').draw();
            iDt.order([]).draw();
            iDt.ajax.reload();
        });

        $(document).on('click.popover', function (e) {
            if (
                !$(e.target).closest(UIElements.btnToggleFilter.selector)
                    .length &&
                !$(e.target).closest('.popover').length
            ) {
                UIElements.btnToggleFilter.popover('hide');
            }
        });

        $(document).on('mouseenter.popover', function (e) {
            if (
                !$(e.target).closest(UIElements.btnToggleFilter.selector)
                    .length &&
                !$(e.target).closest('.popover').length
            ) {
                UIElements.btnToggleFilter.popover('hide');
            }
        });
    });

    UIElements.btnToggleFilter.on('hide.bs.popover', function () {
        const popoverElement = $('.popover.custom-popover');
        popoverElement.find('#showFilter').off('click');
        popoverElement.find('#btnClearFilter').off('click');

        // Remove document event listeners when popover is hidden
        $(document).off('click.popover');
        $(document).off('mouseenter.popover');
    });
}

function toggleFilterRow() {
    $(this).popover('toggle');
    $('#dtFilterRow').toggleClass('hidden');
}

function findColumnByTitle(title, dataTable) {
    let columnIndex = dataTable
        .columns()
        .header()
        .map((c) => $(c).text())
        .indexOf(title);

    let res = dataTable.column(columnIndex);
    return res;
}

function getColumnByName(name, columns) {
    return columns.find((obj) => obj.name === name);
}

function isColumnVisToggled(title, dataTable) {
    let column = findColumnByTitle(title, dataTable);
    if (column.visible()) return ' dt-button-active';
    else return null;
}

function toggleManageColumnButton(config, dataTable) {
    let column = findColumnByTitle(config.text, dataTable);
    column.visible(!column.visible());
}
function getColumnToggleButtonsSorted(displayListColumns, dataTable) {
    let exludeIndxs = [0];
    const res = displayListColumns
        .map((obj) => ({
            title: obj.title,
            data: obj.data,
            visible: obj.visible,
            index: obj.index,
            name: obj.name,
        }))
        .filter((obj) => !exludeIndxs.includes(obj.index))
        .filter((obj) => obj.title !== 'Actions')
        .sort((a, b) => a.title.localeCompare(b.title))
        .map((a) => ({
            text: a.title,
            id: 'managecols-' + a.index,
            action: function (e, dt, node, config) {
                toggleManageColumnButton(config, dataTable);
                if (isColumnVisToggled(a.title, dataTable)) {
                    node.addClass('dt-button-active');
                } else {
                    node.removeClass('dt-button-active');
                }
            },
            className:
                'dt-button dropdown-item buttons-columnVisibility' + isColumnVisToggled(a.title, dataTable),
            columns: a.index,
            name: 'cv-' + a.name
        }));
    return res;
}

function setExternalSearchFilter(dataTableInstance) {
    let searchId = dataTableInstance.init().externalSearchInputId;

    // Exclude default search inputs that have custom logic
    if (searchId !== false && searchId !== '#search') {
        $('.dataTables_filter input').attr('placeholder', 'Search');
        $('.dataTables_filter label')[0].childNodes[0].remove();

        $(searchId).on('input', function () {
            let filter = dataTableInstance.search($(this).val()).draw();
            console.info(`Filter on #${searchId}: ${filter}`);
        });
    }
}

function updateFilter(dt, dtName, filterData) {
    let optionsOpen = false;
    $('#tr-filter').each(function () {
        if ($(this).is(':visible')) optionsOpen = true;
    });
    $('.tr-toggle-filter').remove();
    let newRow = $("<tr class='tr-toggle-filter' id='tr-filter'>");

    dt.columns().every(function () {
            let column = this;
            if (column.visible()) {
                let title = column.header().textContent;
                if (title && title !== 'Actions' && title !== 'Action' && title !== 'Default') {

                    let filterValue = filterData[title] ? filterData[title] : '';

                let input = $('<input>', {
                    type: 'text',
                    class: 'form-control input-sm custom-filter-input',
                    placeholder: title,
                    value: filterValue,
                });

                let newCell = $('<td>').append(input);

                if (column.search() !== filterValue) {
                    column.search(filterValue).draw();
                }

                newCell.find('input').on('keyup', function () {
                    if (column.search() !== this.value) {
                        column.search(this.value).draw();
                        updateFilterButton(dt);
                    }
                });

                newRow.append(newCell);
            } else {
                let newCell = $('<td>');
                newRow.append(newCell);
            }
        }
    });

    updateFilterButton(dt);

    $(`#${dtName} thead`).after(newRow);

    if (optionsOpen) {
        $('.tr-toggle-filter').show();
    }
}

function searchFilter(iDt) {
    let searchValue = $(iDt.init().externalSearchInputId).val();
    if (searchValue) {
        iDt.search(searchValue).draw();
    }

    if ($('#btn-toggle-filter').text() === FilterDesc.With_Filter) {
        $('.tr-toggle-filter').show();
    }
}

function updateFilterButton(dt) {
    let searchValue = $(dt.init().externalSearchInputId).val();
    let columnFiltersApplied = false;
    dt.columns().every(function () {
        let search = this.search();
        if (search) {
            columnFiltersApplied = true;
        }
    });

    let hasFilter = columnFiltersApplied || searchValue !== '';
    $('#btn-toggle-filter').text(
        hasFilter ? FilterDesc.With_Filter : FilterDesc.Default
    );
}

$('.data-table-select-all').click(function () {
    if ($('.data-table-select-all').is(':checked')) {
        PubSub.publish('datatable_select_all', true);
    } else {
        PubSub.publish('datatable_select_all', false);
    }
});

function commonTableActionButtons(exportTitle) {
    return [
        {
            text: 'Filter',
            id: 'btn-toggle-filter',
            className: 'btn-secondary custom-table-btn m-0',
            action: function (e, dt, node, config) {},
            attr: {
                id: 'btn-toggle-filter',
            },
        },
        {
            extend: 'csv',
            text: 'Export',
            title: exportTitle,
            className:
                'custom-table-btn flex-none btn btn-secondary hidden-export-btn d-none',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'fullName',
                format: {
                    body: function (data, row, column, node) {
                        return data === nullPlaceholder ? '' : data;
                    },
                },
            },
        },
    ];
}

// Toggle hidden export buttons for Ctrl+Alt+Shift+E globally
$(document).keydown(function (e) {
    if (e.ctrlKey && e.altKey && e.shiftKey && e.key === 'E') {
        // Toggle d-none class on elements with hidden-export class
        $('.hidden-export-btn').toggleClass('d-none');

        // Prevent default behavior
        e.preventDefault();
        return false;
    }
});