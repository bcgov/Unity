$(function () {
    const UIElements = {
        tables: $('.custom-dynamic-table'),
        tableSearches: $('.custom-tbl-search'),
    };

    let editDatagridRowModal = new abp.ModalManager({
        viewUrl: '../Components/DataGrid/EditDataRowModal',
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
        attachEditButtonHandler($(newRowNode));

        abp.notify.success('Row added successfully.', 'New Row');
    }

    // Function to attach edit button handler
    function attachEditButtonHandler(rowElement) {
        rowElement.find('.row-edit-btn').on('click', function () {
            editDataRow(this);
        });
    }

    // Function to set data attributes on the row
    function setRowDataAttributes(row, rowIndex) {
        row.attr('data-row-no', rowIndex);
    }

    // Function to update worksheet instance ID for related tables
    function updateRelatedTablesWSI(form, worksheetInstanceId) {
        let otherTables = form.find('table.custom-dynamic-table');
        otherTables.each(function () {
            $(this).attr('data-wsi-id', worksheetInstanceId);
        });
    }

    // Function to reset the table level attributes
    function resetTableAttributes(row, response) {
        let table = row.closest('table');
        let responseData = response.responseText;

        table
            .attr('data-value-id', responseData.valueId)
            .attr('data-field-id', responseData.fieldId)
            .attr('data-wsi-id', responseData.worksheetInstanceId)
            .attr('data-ws-id', responseData.worksheetId)
            .attr('data-ws-anchor', responseData.uiAnchor);

        // Find the form containing the row and update related tables
        let form = row.closest('form');
        updateRelatedTablesWSI(form, responseData.worksheetInstanceId);
    }

    // Function to update a single column in a row
    function updateColumnData(table, rowIndex, columnName, newValue) {
        let columnIndex = getColumnIndex(table, columnName);
        if (columnIndex !== -1) {
            table.cell(rowIndex, columnIndex).data(newValue);
        } else {
            console.warn('Column not found:', columnName);
        }
    }

    // Function to update an existing row
    function updateRow(table, dataToUpdate, rowIndex) {
        $.each(dataToUpdate, function (columnName, newValue) {
            updateColumnData(table, rowIndex, columnName, newValue);
        });

        // Redraw the table to reflect the updates
        table.draw();
        abp.notify.success('Update successful.', 'Update');
    }

    // Function to create new row data array
    function createNewRowData(table, dataToUpdate) {
        let newRowData = table
            .columns()
            .header()
            .toArray()
            .map((header) => {
                let columnName = $(header).text();
                return dataToUpdate[columnName] !== undefined
                    ? dataToUpdate[columnName]
                    : '';
            });

        // Add a placeholder for the button in the last column
        newRowData.push('');
        return newRowData;
    }

    // Main function to handle editDatagridRowModal result
    function handleEditDatagridRowModalResult(response) {
        let table = $(`#${response.responseText.fieldId}`).DataTable();
        let rowIndex = response.responseText.row;
        let dataToUpdate = response.responseText.updates;
        let isNewRow = response.responseText.isNew;

        if (isNewRow) {
            let newRowData = createNewRowData(table, dataToUpdate);
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
        $('#summary-' + fieldId + ' input[id^="total-"]').each(function () {
            let inputId = $(this).attr('id');
            let key = inputId.replace('total-', '');
            let total = calculateColumnTotal(table, key);
            if (total !== null) {
                setTotalInputValue($(this), total);
            }
        });
    }

    // Function to process column data for totals
    function processColumnDataForTotal(table, columnIndex) {
        let total = 0;
        table
            .column(columnIndex)
            .data()
            .each(function (value) {
                let cleanedValue = value.replace(/[^\d.-]/g, '');
                if (isNumeric(cleanedValue)) {
                    total += parseFloat(cleanedValue);
                }
            });
        return total;
    }

    function calculateColumnTotal(table, key) {
        let total = 0;
        let headerFound = false;

        table
            .columns()
            .header()
            .each(function (header, index) {
                if ($(header).text() === key) {
                    headerFound = true;
                    total = processColumnDataForTotal(table, index);
                }
            });

        return headerFound ? total : null;
    }

    function setTotalInputValue($input, total) {
        if ($input.data('field-type') === 'Currency') {
            $input.val(formatCurrency(total));
        } else {
            $input.val(total);
        }
    }

    // Function to format currency as CAD
    function formatCurrency(value) {
        return new Intl.NumberFormat('en-CA', {
            style: 'currency',
            currency: 'CAD',
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        }).format(value);
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
        }
        return -1; // Return -1 if the column is not found
    }

    function openEditDatagridRowModal(
        valueId,
        fieldId,
        worksheetId,
        worksheetInstanceId,
        row,
        isNew,
        uiAnchor
    ) {
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
            uiAnchor: uiAnchor,
        });
    }

    // Function to handle add record action
    function handleAddRecordAction(e, dt, node, config) {
        // Access the DataTable ID
        let tableId = dt.table().node().id;

        // Access the data attributes
        let tableElement = $('#' + tableId);
        let tableDataSet = tableElement[0].dataset;

        openEditDatagridRowModal(
            tableDataSet.valueId,
            tableDataSet.fieldId,
            tableDataSet.wsId,
            tableDataSet.wsiId,
            0,
            true,
            tableDataSet.wsAnchor
        );
    }

    let actionButtons = [
        {
            id: 'AddRecord',
            text: 'Add',
            title: 'Add Record',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: handleAddRecordAction,
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
            },
        },
    ];

    init();

    function init() {
        buildDataTables(UIElements.tables);
        bindUIEvents();
    }

    // Function to create and configure a single DataTable
    function createDataTable(element) {
        let $element = $(element);
        let table = $element.DataTable({
            paging: false,
            bInfo: false,
            searching: true,
            serverside: false,
            info: false,
            lengthChange: false,
            dom: 'Bftip',
            buttons: configureButtons($element[0].id),
            order: [[0, 'desc']],
        });
        configureTable(table, $element[0].id);
    }

    function buildDataTables(tables) {
        tables.each(function () {
            createDataTable(this);
        });
    }

    function configureButtons(fieldId) {
        let options = $(`#table-options-${fieldId}`).val().split(',');
        let availableOptions = actionButtons.filter((item) =>
            options.includes(item.id)
        );
        return availableOptions;
    }

    // Function to collect known columns information
    function collectKnownColumns(table) {
        let knownColumns = [];
        table
            .columns()
            .header()
            .to$()
            .each(function (index) {
                let isActions = $(this).hasClass('custom-actions-header');
                if ($(this).text() != '' && !isActions) {
                    knownColumns.push({
                        title: $(this).text(),
                        visible: true,
                        index: index + 1,
                        isActions: isActions,
                    });
                }
            });
        return knownColumns;
    }

    // Function to setup action buttons for table
    function setupTableActionButtons(table, knownColumns) {
        table.button().add(actionButtons.length + 1, {
            text: 'Columns',
            extend: 'collection',
            buttons: getColumnToggleButtonsSorted(knownColumns, table),
            className: 'custom-table-btn flex-none btn btn-secondary',
        });
    }

    // Function to setup action column cells
    function setupActionColumnCells(table, columnIndex) {
        table
            .column(columnIndex)
            .nodes()
            .each(function (cell) {
                cell.innerHTML = getEditRowButtonTemplate();
                attachEditButtonHandler($(cell));
            });
    }

    // Function to configure action columns
    function configureActionColumns(table) {
        table.columns().every(function (index) {
            let isLastColumn = index === table.columns().count() - 1;
            if (!isLastColumn) return;

            // Update column header and setup cells
            table.column(index).header().innerHTML = 'Actions';
            setupActionColumnCells(table, index);
        });
    }

    function configureTable(table, fieldId) {
        table.buttons().container().prependTo(`#btn-container-${fieldId}`);

        let knownColumns = collectKnownColumns(table);
        setupTableActionButtons(table, knownColumns);
        configureActionColumns(table);
    }

    function getEditRowButtonTemplate() {
        return '<input type="button" class="btn btn-edit row-edit-btn" value="Edit"></input>';
    }

    // Function to handle table search keyup event
    function handleTableSearchKeyup() {
        let table = $(`#${this.dataset.tableId}`).DataTable();
        table.search(this.value).draw();
    }

    // Function to handle table search event
    function handleTableSearch() {
        let table = $(`#${this.dataset.tableId}`).DataTable();
        table.search('').draw();
    }

    function bindUIEvents() {
        UIElements.tableSearches.on('keyup', handleTableSearchKeyup);
        UIElements.tableSearches.on('search', handleTableSearch);
    }

    function editDataRow(button) {
        // Get the parent <tr> element of the button
        let row = $(button).closest('tr');
        let rowDataSet = row[0].dataset;

        // Retrieve the data attributes from the <tr> element
        let table = $(button).closest('table');
        let tableDataSet = table[0].dataset;

        openEditDatagridRowModal(
            tableDataSet.valueId,
            tableDataSet.fieldId,
            tableDataSet.wsId,
            tableDataSet.wsiId,
            rowDataSet.rowNo,
            false,
            tableDataSet.uiAnchor
        );
    }

    PubSub.subscribe('worksheet_preview_datagrid_refresh', () => {
        // refresh the dom elements binding and init the datagrid view
        buildDataTables($('.custom-dynamic-table'));
    });
});
