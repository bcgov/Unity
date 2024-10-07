const FilterDesc = {
    Default: 'Filter',
    With_Filter: 'Filter*'
};
function createNumberFormatter() {
    return new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });
}

function initializeDataTable(dt, defaultVisibleColumns, listColumns, maxRowsPerPage, defaultSortColumn, dataEndpoint, data, responseCallback, actionButtons, dynamicButtonContainerId) {

    let visibleColumnsIndex = defaultVisibleColumns.map((name) => listColumns.find(obj => obj.name === name)?.index ?? 0);

    let filterData = {};

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
            colReorder: true,
            orderCellsTop: true,
            //fixedHeader: true,
            stateSave: true,
            stateDuration: 0,
            dom: 'Blfrtip',
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
            stateSaveParams: function (settings, data) {
                let searchValue = $('#search').val();
                data.search.search = searchValue;

                let hasFilter = data.columns.some(value => value.search.search !== '') || searchValue !== '';
                $('#btn-toggle-filter').text(hasFilter ? FilterDesc.With_Filter : FilterDesc.Default);
            },
            stateLoadParams: function (settings, data) {
                $('#search').val(data.search.search);

                data.columns.forEach((column, index) => {
                    if(settings.aoColumns[index] +"" != "undefined") {
                        const title = settings.aoColumns[index].sTitle;
                        const value = column.search.search;
                        filterData[title] = value;
                    }
                });
            }
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
    $('.dataTables_wrapper').append('<div class="length-menu-footer"></div>');

    // Move the length menu to the footer container
    $('.dataTables_length').appendTo('.length-menu-footer');
    init(iDt);

    updateFilter(iDt, dt[0].id, filterData);

    iDt.on('column-reorder.dt', function (e, settings) {
        updateFilter(iDt, dt[0].id, filterData);
    });
    iDt.on('column-visibility.dt', function (e, settings, deselectedcolumn, state) {
        updateFilter(iDt, dt[0].id, filterData);
    });

    initializeFilterButtonPopover(iDt);

    searchFilter(iDt);

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

function getSelectColumn(title,dataField,uniqueTableId) {
    return {
        title: `<input class="checkbox-select select-all-${uniqueTableId}"  type="checkbox">`,
        orderable: false,
        className: 'notexport text-center',
        data: dataField,
        name: 'select',
        render: function (data) {           
            return `<input class="checkbox-select chkbox" id="row_${data}" type="checkbox" value="" title="${title}">`
        },
        index: 0
    }
}

function init(iDt) {
    $('.custom-table-btn').removeClass('dt-button buttons-csv buttons-html5');    
    iDt.search('').columns().search('').draw();
}

function initializeFilterButtonPopover(iDt) {
    const UIElements = {
        search: $('#search'),
        btnToggleFilter: $('#btn-toggle-filter')
    };

    UIElements.btnToggleFilter.on('click', function() {
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
            const isChecked = $(".tr-toggle-filter").is(':visible');
            return `
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" id="showFilter" ${isChecked ? 'checked' : ''}>
                        <label class="form-check-label" for="showFilter">Show Filter Row</label>
                    </div>
                    <abp-button id="btnClearFilter" class="btn btn-primary" text="Clear Filter" type="button">CLEAR FILTER</abp-button>
                   `;
        },
        placement: 'bottom'
    });

    

    UIElements.btnToggleFilter.on('shown.bs.popover', function () {
        const searchElement = $('#search');
        const trToggleElement = $(".tr-toggle-filter");
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
            if (!$(e.target).closest(UIElements.btnToggleFilter.selector).length &&
                !$(e.target).closest('.popover').length) {
                UIElements.btnToggleFilter.popover('hide');
            }
        });

        $(document).on('mouseenter.popover', function (e) {
            if (!$(e.target).closest(UIElements.btnToggleFilter.selector).length &&
                !$(e.target).closest('.popover').length) {
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

function updateFilter(dt, dtName, filterData) {
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

                    let filterValue = filterData[title] ? filterData[title] : '';

                    let input = $("<input>", {
                        type: 'text',
                        class: 'form-control input-sm custom-filter-input',
                        placeholder: title,
                        value: filterValue
                    });

                    let newCell = $("<td>").append(input);

                    if (column.search() !== filterValue) {
                        column.search(filterValue).draw();
                    }

                    newCell.find("input").on("keyup", function () {
                        if (column.search() !== this.value) {
                            column.search(this.value).draw();
                            updateFilterButton(dt);
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

    updateFilterButton(dt);

    $(`#${dtName} thead`).after(newRow);

    if (optionsOpen) {
        $(".tr-toggle-filter").show();
    }
}

function searchFilter(iDt) {
    let searchValue = $('#search').val();
    if (searchValue) {
        iDt.search(searchValue).draw();
    }

    if ($('#btn-toggle-filter').text() === FilterDesc.With_Filter) {
        $(".tr-toggle-filter").show();
    }
}

function updateFilterButton(dt) {
    let searchValue = $('#search').val();
    let columnFiltersApplied = false;
    dt.columns().every(function () {
        let search = this.search();
        if (search) {
            columnFiltersApplied = true;
        }
    });

    let hasFilter = columnFiltersApplied || searchValue !== '';
    $('#btn-toggle-filter').text(hasFilter ? FilterDesc.With_Filter : FilterDesc.Default);
}

$('.data-table-select-all').click(function () {

    if ($('.data-table-select-all').is(":checked")) {
        PubSub.publish('datatable_select_all',true);
    } else {
        PubSub.publish('datatable_select_all', false);
    }
   
});