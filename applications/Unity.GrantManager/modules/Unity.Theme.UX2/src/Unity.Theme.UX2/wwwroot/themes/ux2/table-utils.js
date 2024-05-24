function createNumberFormatter() {
    return new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });
}

function initializeDataTable(dt, defaultVisibleColumns, listColumns, maxRowsPerPage, defaultSortColumn, dataEndpoint, data, actionButtons, dynamicButtonContainerId) {

    let visibleColumnsIndex = defaultVisibleColumns.map((name) => listColumns.find(obj => obj.name === name)?.index ?? 0);

    let iDt = dt.DataTable(
        abp.libs.datatables.normalizeConfiguration({
            fixedHeader: {
                header: true,
                footer: false,
                headerOffset: 0
            },
            serverSide: false,
            paging: true,
            order: [[defaultSortColumn, 'desc']],
            searching: true,
            pageLength: maxRowsPerPage,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                dataEndpoint,
                data,
                function (result) {
                    if (result.totalCount <= maxRowsPerPage) {
                        $('.dataTables_paginate').hide();
                    }
                    return {
                        recordsTotal: result.totalCount,
                        recordsFiltered: result.totalCount,
                        data: result.items
                    };
                }
            ),
            select: {
                style: 'multiple',
                selector: 'td:not(:nth-child(8))',
            },
            colReorder: true,
            orderCellsTop: true,
            //fixedHeader: true,
            stateSave: true,
            stateDuration: 0,
            dom: 'Bfrtip',
            buttons: actionButtons,
            drawCallback: function () {
                $(`#${dt[0].id}_previous a`).text("<");
                $(`#${dt[0].id}_next a`).text(">");
                $(`#${dt[0].id}_info`).text(function (index, text) {
                    return text.replace("Showing ", "").replace(" to ", "-").replace(" entries", "");
                });
            },
            initComplete: function () {
            },
            columns: listColumns,
            columnDefs: [
                {
                    targets: visibleColumnsIndex,
                    visible: true
                },
                {
                    targets: '_all',
                    visible: false // Hide all other columns initially
                }
            ],
            processing: true,
        })
    );

    // Add custom manage columns button that remains sorted alphabetically
    iDt.button().add(actionButtons.length + 1 ,{
        text: 'Columns',
        extend: 'collection',
        buttons: getColumnToggleButtonsSorted(listColumns, iDt),
        className: 'custom-table-btn flex-none btn btn-secondary'
    });

    iDt.buttons().container().prependTo(`#${dynamicButtonContainerId}`);

    init(iDt);

    updateFilter(iDt, dt[0].id);

    iDt.on('column-reorder.dt', function (e, settings) {
        updateFilter(iDt, dt[0].id);
    });
    iDt.on('column-visibility.dt', function (e, settings, deselectedcolumn, state) {
        updateFilter(iDt, dt[0].id);
    });

    return iDt;
}

function setTableHeighDynamic(tableName) {        
    let tableHeight = $(`#${tableName}_wrapper`)[0].clientHeight;    
    let docHeight = document.body.clientHeight;
    let tableOffset = 425;

    if ((tableHeight + tableOffset) > docHeight) {
        $(`#${tableName}_wrapper .dataTables_scrollBody`).css({ height: docHeight - tableOffset });
    } else {
        $(`#${tableName}_wrapper .dataTables_scrollBody`).css({ height: tableHeight + 10 });
    }
}

function getSelectColumn(title) {
    return {
        title: '<span class="btn btn-secondary btn-light fl fl-filter" title="Toggle Filter" id="btn-toggle-filter"></span>',
        orderable: false,
        className: 'notexport text-center',
        data: 'rowCount',
        name: 'select',
        render: function (data) {           
            return `<input class="checkbox-select chkbox" id="row_${data}" type="checkbox" value="" title="${title}">`
        },
        index: 0
    }
}

function init(iDt) {
    $('.custom-table-btn').removeClass('dt-button buttons-csv buttons-html5');    
    bindUIEvents();
    iDt.search('').columns().search('').draw();
}

function bindUIEvents() {

    const UIElements = {
        searchBar: $('#search-bar'),
        btnToggleFilter: $('#btn-toggle-filter'),
    };
    
    UIElements.btnToggleFilter.on('click', toggleFilterRow);
}

function toggleFilterRow() {    
    $('#dtFilterRow').toggleClass('hidden');
}

function findColumnByTitle(title, dataTable) {
    let columnIndex = dataTable
        .columns()
        .header()
        .map(c => $(c).text())
        .indexOf(title);
    return dataTable.column(columnIndex);
}

function getColumnByName(name, columns) {
    return columns.find(obj => obj.name === name);
}

function isColumnVisToggled(title, dataTable) {
    let column = findColumnByTitle(title, dataTable);
    if (column.visible())
        return ' dt-button-active';
    else
        return null;
}

function toggleManageColumnButton(config, dataTable) {
    let column = findColumnByTitle(config.text, dataTable);
    column.visible(!column.visible());
}

function getColumnsVisibleByDefault(columns, listColumns) {
    return columns
        .map((name) => {
            return TableUtils.getColumnByName(name, listColumns).index;
        });
}

function getColumnToggleButtonsSorted(listColumns, dataTable) {    
    let exludeIndxs = [0];
    return listColumns
        .map((obj) => ({ title: obj.title, data: obj.data, visible: obj.visible, index: obj.index }))
        .filter(obj => !exludeIndxs.includes(obj.index))
        .sort((a, b) => a.title.localeCompare(b.title))
        .map(a => ({
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
            className: 'dt-button dropdown-item buttons-columnVisibility' + isColumnVisToggled(a.title, dataTable)
        }));
}

function updateFilter(dt, dtName) {
    let optionsOpen = false;
    $("#tr-filter").each(function () {
        if ($(this).is(":visible"))
            optionsOpen = true;
    })
    $('.tr-toggle-filter').remove();
    let newRow = $("<tr class='tr-toggle-filter' id='tr-filter'>");

    dt.columns().every(function () {
            let column = this;
            if (column.visible()) {
                let title = column.header().textContent;
                if (title) {
                    let newCell = $("<td>").append("<input type='text' class='form-control input-sm custom-filter-input' placeholder='" + title + "'>");
                    newCell.find("input").on("keyup", function () {
                        if (column.search() !== this.value) {
                            column.search(this.value).draw();
                        }
                    });

                    newRow.append(newCell);

                }
                else {
                    let newCell = $("<td>");
                    newRow.append(newCell);
                }
            }
        });
    
    $(`#${dtName} thead`).after(newRow);

    if (optionsOpen) {
        $(".tr-toggle-filter").show();
    }
}

