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

        // Create the edit button HTML and append it to the last cell of the new row
        $(newRowNode).find('td:last').html(getEditRowButtonTemplate());

        // Attach click event handler to the newly added button 
        $(newRowNode).find('.row-edit-btn').on('click', function () {
            let button = this; // `this` refers to the button element 
            editDataRow(button);
        });

        abp.notify.success('Row added successfully.', 'New Row');
    }

    // Function to set data attributes on the row
    function setRowDataAttributes(row, rowIndex) {
        row.attr('data-row-no', rowIndex);
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
            if (columnIndex !== -1) {
                table.cell(rowIndex, columnIndex).data(newValue);
            } else {
                console.warn('Column not found:', columnName);
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
                let columnName = $(header).text();
                return dataToUpdate[columnName] !== undefined ? dataToUpdate[columnName] : '';
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
            let total = 0;
            let headerFound = false;

            // Find the corresponding column in the DataTable by matching the key
            table.columns().header().each(function (header, index) {
                if ($(header).text() === key) {
                    headerFound = true;
                    // Sum up all numeric values in the column
                    table.column(index).data().each(function (value, rowIndex) {
                        // Remove currency symbols and commas for numeric check
                        let cleanedValue = value.replace(/[^\d.-]/g, '');

                        if (isNumeric(cleanedValue)) {
                            total += parseFloat(cleanedValue);
                        }
                    });
                }
            });

            // Update the input field with the calculated total only if the header is found 
            if (headerFound) {
                if ($(this).data('field-type') === 'Currency') {
                    $(this).val(formatCurrency(total));
                }
                else {
                    $(this).val(total);
                }
            }
        });
    }

    // Function to format currency as CAD 
    function formatCurrency(value) {
        return new Intl.NumberFormat('en-CA',
            { style: 'currency', currency: 'CAD', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
    }

    // Function to check if a value is numeric
    function isNumeric(value) {
        return !isNaN(value) && isFinite(value);
    }

    // Function to get the index of a column by its name
    function getColumnIndex(table, columnName) {
        let headers = table.columns().header().toArray();
        for (let i = 0; i < headers.length; i++) {
            if ($(headers[i]).text() === columnName) {
                return i;
            }
        } return -1; // Return -1 if the column is not found 
    }

    function openEditDatagridRowModal(valueId,
        fieldId,
        worksheetId,
        worksheetInstanceId,
        row,
        isNew,
        uiAnchor) {

        let formVersionId = $('#ApplicationFormVersionId').val();
        let applicationId = $('#DetailsViewApplicationId').val();

        editDatagridRowModal.open({
            valueId: valueId,
            fieldId: fieldId,
            row: row,
            isNew: isNew,
            worksheetId: worksheetId,
            worksheetInstanceId: worksheetInstanceId,
            // There is dependency here on the core module and details page !
            formVersionId: formVersionId,
            applicationId: applicationId,
            uiAnchor: uiAnchor
        });
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

                openEditDatagridRowModal(tableDataSet.valueId,
                    tableDataSet.fieldId,
                    tableDataSet.wsId,
                    tableDataSet.wsiId,
                    0,
                    true,
                    tableDataSet.wsAnchor);
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
                bInfo: false,
                searching: true,
                serverside: false,
                info: false,
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
        let options = ($(`#table-options-${fieldId}`).val()).split(',');
        // Always include ColumnVisibility button regardless of options
        let availableOptions = actionButtons.filter(item => options.includes(item.id) || item.id === 'ColumnVisibility');
        return availableOptions;
    }

    function configureTable(table, fieldId) {
        // Move buttons to custom container
        table.buttons().container().prependTo(`#btn-container-${fieldId}`);

        // Add edit buttons to the last column (Actions)
        table.columns().every(function (index) {
            if (index === table.columns().count() - 1) { // Check if it is the last column
                table.column(index).header().innerHTML = 'Actions'; // Update column header if needed
                table.column(index).nodes().each(function (cell) {
                    cell.innerHTML = getEditRowButtonTemplate(); // Add edit button to each cell

                    // Attach click event handler to the newly added button 
                    $(cell).find('.row-edit-btn').on('click', function () {
                        let button = this; // `this` refers to the button element 
                        editDataRow(button);
                    });
                });
            }
        });
    }

    function getEditRowButtonTemplate() {
        return '<input type="button" class="btn btn-edit row-edit-btn" value="Edit"></input>';
    }

    function editDataRow(button) {
        // Get the parent <tr> element of the button
        let row = $(button).closest('tr');
        let rowDataSet = row[0].dataset;

        // Retrieve the data attributes from the <tr> element
        let table = $(button).closest('table');
        let tableDataSet = table[0].dataset;

        openEditDatagridRowModal(tableDataSet.valueId,
            tableDataSet.fieldId,
            tableDataSet.wsId,
            tableDataSet.wsiId,
            rowDataSet.rowNo,
            false,
            tableDataSet.uiAnchor);
    }

    PubSub.subscribe(
        'worksheet_preview_datagrid_refresh',
        () => {
            // refresh the dom elements binding and init the datagrid view
            buildDataTables($('.custom-dynamic-table'));
        }
    );
});
