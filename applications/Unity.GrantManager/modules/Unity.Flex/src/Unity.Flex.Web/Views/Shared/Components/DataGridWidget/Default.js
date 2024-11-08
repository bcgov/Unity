$(function () {
    const UIElements = {
        editRowBtns: $('button.row-edit-btn'),
        tables: $('.custom-dynamic-table'),
        tableSearches: $('.custom-tbl-search')
    };

    let actionButtons = [
        {
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
                buttons: actionButtons
            });
            configureTable(table, $element[0].id);
        });
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
        window.alert(e);
    }

    PubSub.subscribe(
        'worksheet_preview_datagrid_refresh',
        () => {
            // refresh the dom elements binding and init the datagrid view
            buildDataTables($('.custom-dynamic-table'));
        }
    );
});
