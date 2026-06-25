function getDatagridActionsRowButtonTemplate(actions) {
    if (actions.length === 0) return '';

    if (actions.length === 1) {
        if (actions.includes('EDIT'))
            return '<input type="button" class="btn btn-edit row-edit-btn" value="Edit">';
        if (actions.includes('DELETE'))
            return '<input type="button" class="btn btn-delete row-delete-btn" value="Delete">';
        return '';
    }

    let items = '';
    if (actions.includes('EDIT'))
        items += '<li><button type="button" class="dropdown-item row-edit-btn">Edit</button></li>';
    if (actions.includes('DELETE'))
        items += '<li><button type="button" class="dropdown-item row-delete-btn">Delete</button></li>';

    return `<div class="dropdown">` +
               `<button type="button" class="btn btn-secondary dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false">&hellip;</button>` +
               `<ul class="dropdown-menu">${items}</ul>` +
           `</div>`;
}

// Function to set data attributes on the row
function setRowDataAttributes(row, rowIndex) {
    row.attr('data-row-no', rowIndex);
}

// Function to format currency as CAD
function formatDatagridCurrency(value) {
    return new Intl.NumberFormat('en-CA',
        { style: 'currency', currency: 'CAD', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
}

// Function to check if a value is numeric
function isDatagridCellNumeric(value) {
    return !Number.isNaN(value) && Number.isFinite(value);
}

// Function to calculate sum for a specific column
function calculateDatagridColumnSum(table, columnIndex) {
    let total = 0;
    table.column(columnIndex).data().each(function (value) {
        // Remove currency symbols and commas for numeric check
        let cleanedValue = value.replace(/[^\d.-]/g, '');
        if (isNumeric(cleanedValue)) {
            total += Number.parseFloat(cleanedValue);
        }
    });
    return total;
}

$(function () {
    const UIElements = {
        tables: $('.custom-dynamic-table'),
        tableSearches: $('.custom-tbl-search')
    };
    
    let editDatagridRowModal = new abp.ModalManager({
        viewUrl: '../Components/DataGrid/EditDataRowModal'
    });

    // Function to handle new row addition
    function addNewRow(table, newRowData, response, rowIndex) {
        // Convert dataToUpdate object to an array of values in the same order as the columns
        let newRowNode = table.row.add(newRowData).draw().node();

        // Set data attributes on the <tr> element
        setRowDataAttributes($(newRowNode), rowIndex);

        // Refresh any update table level attributes
        resetTableAttributes($(newRowNode), response);

        // Configure action buttons on the last cell of the new row
        let fieldId = $(newRowNode).closest('table')[0].id;
        configureActionButtonsForCell($(newRowNode).find('td:last')[0], getTableActions(fieldId));

        abp.notify.success('Row added successfully.', 'New Row');
    }

    // Function to reset the table level attributes
    function resetTableAttributes(row, response) {
        let table = row.closest('table');

        table.attr('data-value-id', response.responseText.valueId)
            .attr('data-field-id', response.responseText.fieldId)
            .attr('data-wsi-id', response.responseText.worksheetInstanceId)
            .attr('data-ws-id', response.responseText.worksheetId)
            .attr('data-ws-anchor', response.responseText.uiAnchor);

        // Find the form containing the row
        let form = row.closest('form');

        // Find other tables within the same form with class 'custom-dynamic-table'
        let otherTables = form.find('table.custom-dynamic-table');

        // Set the worksheet instance ID for these tables
        otherTables.each(function () {
            $(this).attr('data-wsi-id', response.responseText.worksheetInstanceId);
        });
    }

    // Function to update an existing row
    function updateRow(table, dataToUpdate, rowIndex) {
        $.each(dataToUpdate, function (columnName, newValue) {
            let columnIndex = getColumnIndex(table, columnName);
            if (columnIndex === -1) {
                console.warn('Column not found:', columnName);
            } else {
                table.cell(rowIndex, columnIndex).data(newValue);
            }
        });

        // Redraw the table to reflect the updates
        table.draw();
        abp.notify.success('Update successful.', 'Update');
    }

    // Main function to handle editDatagridRowModal result
    function handleEditDatagridRowModalResult(response) {
        let table = $(`#${response.responseText.fieldId}`).DataTable();
        let rowIndex = response.responseText.row;
        let dataToUpdate = response.responseText.updates;
        let isNewRow = response.responseText.isNew;

        if (isNewRow) {
            // Convert dataToUpdate object to an array of values in the same order as the columns
            let newRowData = table.columns().header().toArray().map(header => {
                let key = $(header).data('key');
                return dataToUpdate[key] === undefined ? '' : dataToUpdate[key];
            });

            // Add a placeholder for the button in the last column
            newRowData.push('');

            addNewRow(table, newRowData, response, rowIndex);
        } else {
            updateRow(table, dataToUpdate, rowIndex);
        }

        // Update any summary totals for the grid
        updateTotals(table, response.responseText.fieldId);
    }

    // Attach the main function to editDatagridRowModal result
    editDatagridRowModal.onResult(function (_, response) {
        handleEditDatagridRowModalResult(response);
    });


    // Function to update totals
    function updateTotals(table, fieldId) {
        // Iterate through each input field that has the id pattern 'total-{key}'
        $('#summary-' + fieldId + ' input[id^="total-"]').each(function () {
            let inputId = $(this).attr('id');
            let key = inputId.replace('total-', '');
            
            // Find the corresponding column in the DataTable by matching the key
            let columnIndex = getColumnIndex(table, key);
            
            if (columnIndex !== -1) {
                let total = calculateDatagridColumnSum(table, columnIndex);
                
                // Update the input field with the calculated total
                if ($(this).data('field-type') === 'Currency') {
                    $(this).val(formatDatagridCurrency(total));
                } else {
                    $(this).val(total);
                }
            }
        });
    }

    // Function to get the index of a column by its key
    function getColumnIndex(table, key) {
        let headers = table.columns().header().toArray();
        for (let i = 0; i < headers.length; i++) {
            if ($(headers[i]).data('key') === key) {
                return i;
            }
        } return -1; // Return -1 if the column is not found 
    }

    function openEditDatagridRowModal(options) {
        let formVersionId = $('#ApplicationFormVersionId').val();
        let applicationId = $('#DetailsViewApplicationId').val();

        editDatagridRowModal.open({
            valueId: options.valueId,
            fieldId: options.fieldId,
            row: options.row,
            isNew: options.isNew,
            worksheetId: options.worksheetId,
            worksheetInstanceId: options.worksheetInstanceId,
            // There is dependency here on the core module and details page !
            formVersionId: formVersionId,
            applicationId: applicationId,
            uiAnchor: options.uiAnchor,
            columnOrder: options.columnOrder || ''
        });
    }

    function getColumnOrder(dt) {
        return dt.columns().header().toArray()
            .map(th => $(th).data('key'))
            .filter(key => key !== undefined && key !== null)
            .join(',');
    }

    let actionButtons = [
        {
            id: 'AddRecord',
            text: 'Add',
            title: 'Add Record',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) {
                // Access the DataTable ID
                let tableId = dt.table().node().id;

                // Access the data attributes
                let tableElement = $('#' + tableId);
                let tableDataSet = tableElement[0].dataset;

                openEditDatagridRowModal({
                    valueId: tableDataSet.valueId,
                    fieldId: tableDataSet.fieldId,
                    worksheetId: tableDataSet.wsId,
                    worksheetInstanceId: tableDataSet.wsiId,
                    row: 0,
                    isNew: true,
                    uiAnchor: tableDataSet.wsAnchor,
                    columnOrder: getColumnOrder(dt)
                });
            }
        },
        {
            id: 'ExportData',
            extend: 'csv',
            text: 'Export',
            title: 'Data Export',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'fullName',
            }
        },
        {
            id: 'ColumnVisibility',
            extend: 'colvisAlpha',
            text: 'Columns',
            className: 'custom-table-btn flex-none btn btn-secondary',
            columns: ':not(.notexport):not(.custom-actions-header)'
        }
    ];

    init();

    function init() {
        DataTable.type('num', 'className', 'dt-head-left dt-body-right');
        DataTable.type('num-fmt', 'className', 'dt-head-left');
        DataTable.type('date', 'className', 'dt-head-left');
        DataTable.type('datetime-YYYY-MM-DD', 'className', 'dt-head-left');

        buildDataTables(UIElements.tables);
    }

    function buildDataTables(tables) {
        tables.each(function () {
            let $element = $(this);
            let fieldId = $element[0].id;
            let table = $(this).DataTable({
                paging: false,
                searching: true,                
                info: false,
                autoWidth: false,
                lengthChange: false,
                deferRender: false, // Required for DOM manipulation in addNewRow and configureTable
                layout: {
                    // Use manual button container positioning
                    topStart: null,
                    topEnd: null,
                    bottomStart: null,
                    bottomEnd: null
                },
                buttons: configureButtons(fieldId),
                order: [[0, 'desc']]
            });
            configureTable(table, fieldId);
            
            // Use the externalSearch API for search input binding
            table.externalSearch(`#table-search-${fieldId}`);
        });
    }

    function configureButtons(fieldId) {
        let options = new Set(($(`#table-options-${fieldId}`).val()).split(','));
        // Always include ColumnVisibility button regardless of options
        let availableOptions = actionButtons.filter(item => options.has(item.id) || item.id === 'ColumnVisibility');
        return availableOptions;
    }

    function getTableActions(fieldId) {
        let options = new Set(($(`#table-options-${fieldId}`).val()).split(','));
        let actions = ['EDIT'];
        if (options.has('AddRecord')) actions.push('DELETE');
        return actions;
    }

    // Function to configure action buttons for a table cell
    function configureActionButtonsForCell(cell, actions) {
        cell.innerHTML = getDatagridActionsRowButtonTemplate(actions);

        $(cell).find('.row-edit-btn').on('click', function () {
            editDataRow(this);
        });

        $(cell).find('.row-delete-btn').on('click', function () {
            deleteDataRow(this);
        });
    }

    // Function to setup the actions column
    function setupActionsColumn(table, columnIndex, actions) {
        table.column(columnIndex).header().innerHTML = 'Actions';
        table.column(columnIndex).nodes().each(function (cell) {
            configureActionButtonsForCell(cell, actions);
        });
    }

    function configureTable(table, fieldId) {
        // Move buttons to custom container
        table.buttons().container().prependTo(`#btn-container-${fieldId}`);

        let actions = getTableActions(fieldId);

        table.columns().every(function (index) {
            if (index === table.columns().count() - 1) {
                setupActionsColumn(table, index, actions);
            }
        });
    }

    function editDataRow(button) {
        // Get the parent <tr> element of the button
        let row = $(button).closest('tr');
        let rowDataSet = row[0].dataset;

        // Retrieve the data attributes from the <tr> element
        let table = $(button).closest('table');
        let tableDataSet = table[0].dataset;

        openEditDatagridRowModal({
            valueId: tableDataSet.valueId,
            fieldId: tableDataSet.fieldId,
            worksheetId: tableDataSet.wsId,
            worksheetInstanceId: tableDataSet.wsiId,
            row: rowDataSet.rowNo,
            isNew: false,
            uiAnchor: tableDataSet.uiAnchor,
            columnOrder: getColumnOrder(table.DataTable())
        });
    }

    function deleteDataRow(button) {
        // Get the parent <tr> element of the button
        let row = $(button).closest('tr');
        let rowDataSet = row[0].dataset;

        // Retrieve the data attributes from the <tr> element
        let table = $(button).closest('table');
        let tableDataSet = table[0].dataset;

        abp.message.confirm(
            'Are you sure you want to delete this row?',
            'Delete Row',
            function (confirmed) {
                if (confirmed) {
                    // TODO: replace with real API call
                    console.log('Calling off to API to delete row', { fieldId: tableDataSet.fieldId, row: rowDataSet.rowNo });
                }
            }
        );
    }

    PubSub.subscribe(
        'worksheet_preview_datagrid_refresh',
        () => {
            // refresh the dom elements binding and init the datagrid view
            buildDataTables($('.custom-dynamic-table'));
        }
    );
});
