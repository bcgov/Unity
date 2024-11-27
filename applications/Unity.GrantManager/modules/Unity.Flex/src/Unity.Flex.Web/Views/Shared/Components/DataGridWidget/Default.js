$(function () {
    const UIElements = {
        editRowBtns: $('button.row-edit-btn'),
        tables: $('.custom-dynamic-table'),
        tableSearches: $('.custom-tbl-search')
    };

    let editDatagridRowModal = new abp.ModalManager({
        viewUrl: '../Components/DataGrid/EditDataRowModal'
    });

    editDatagridRowModal.onResult(function (_, response) {
        let table = $(`#${response.responseText.fieldId}`).DataTable();
        let rowIndex = response.responseText.row;
        let dataToUpdate = response.responseText.updates;

        // Iterate through the JSON object and update the specified row
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

        abp.notify.success(
            'Update successful.',
            'Update'
        );
    });

    // Function to get the index of a column by its name
    function getColumnIndex(table, columnName) {
        let headers = table.columns().header().toArray();
        for (let i = 0; i < headers.length; i++) {
            if ($(headers[i]).text() === columnName) {
                return i;
            }
        } return -1; // Return -1 if the column is not found 
    }

    function openEditDatagridRowModal(valueId, fieldId, row) {
        editDatagridRowModal.open({
            fieldId: fieldId,
            valueId: valueId,
            row: row
        });
    }

    let actionButtons = [
        {
            id: 'AddRecord',
            text: 'Add',
            title: 'Add Record',
            className: 'custom-table-btn flex-none btn btn-secondary'
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
    ];

    init();

    function init() {
        buildDataTables(UIElements.tables);
        bindUIEvents();
    }

    function buildDataTables(tables) {
        tables.each(function () {
            let $element = $(this);
            let table = $(this).DataTable({
                paging: false,
                bInfo: false,
                searching: true,
                serverside: false,
                info: false,
                lengthChange: false,
                dom: 'Bftip',
                buttons: configureButtons($element[0].id),
                order: [[0, 'desc']]
            });
            configureTable(table, $element[0].id);
        });
    }

    function configureButtons(fieldId) {
        let options = ($(`#table-options-${fieldId}`).val()).split(',');
        let availableOptions = actionButtons.filter(item => options.includes(item.id));
        return availableOptions;
    }

    function configureTable(table, fieldId) {
        table.buttons().container().prependTo(`#btn-container-${fieldId}`);
        let knownColumns = [];
        table.columns().header().to$().each(function (index) {
            if ($(this).text() != '') {
                knownColumns.push({ title: $(this).text(), visible: true, index: index + 1 });
            }
        });

        table.button().add(actionButtons.length + 1, {
            text: 'Columns',
            extend: 'collection',
            buttons: getColumnToggleButtonsSorted(knownColumns, table),
            className: 'custom-table-btn flex-none btn btn-secondary'
        });
    }

    function bindUIEvents() {
        UIElements.editRowBtns.on('click', handleEditRowClick);
        UIElements.tableSearches.on('keyup', function () {
            let table = $(`#${this.dataset.tableId}`).DataTable();
            table.search(this.value).draw();
        });

        UIElements.tableSearches.on('search', function () {
            let table = $(`#${this.dataset.tableId}`).DataTable();
            table.search('').draw();
        });
    }

    function handleEditRowClick(e) {
        let dataSet = e.currentTarget.dataset;
        openEditDatagridRowModal(dataSet.valueId, dataSet.fieldId, dataSet.rowNo);
    }

    PubSub.subscribe(
        'worksheet_preview_datagrid_refresh',
        () => {
            // refresh the dom elements binding and init the datagrid view
            buildDataTables($('.custom-dynamic-table'));
        }
    );
});
