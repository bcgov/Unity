$(function () {
    let addcolumnOption;
    let columnOptionsTable;
    let newRowControl = 'new-column-row';
    let deleteClass = 'delete-column-option';
    let newColumnTable = 'add-new-column-table';
    let validityClass = 'column-input-valid';

    function bindRootActions() {
        addcolumnOption = $('#add-column-option-btn');

        bindNewColumnOption(newColumnTable, addcolumnOption, 'column');
        bindSaveOption();
        bindCancelOption();
        bindNewRowKeyCheck(newRowControl);
    }

    function bindNewColumnOption(newItemTable, option, keyStart) {
        let newRowTable = $('#' + newItemTable);

        if (option) {
            option.on('click', function (event) {
                if (newRowTable) {
                    newRowTable.toggleClass('hidden');
                    $('#new-row-key').val(keyStart + ($('.key-input')?.toArray()?.length + 1));
                    $('#new-row-label').focus();
                    option.toggleClass('hidden');
                }
            });
        }
    }

    function bindNewRowKeyCheck(newRowControl) {
        let newKey = $('#new-row-key');

        newKey.on('change', function (event) {
            let valid = validateColumnName(newRowControl);
            if (valid === true) {
                clearSummaryError();
            }
        });
    }

    function bindSaveOption() {
        let save = $('#save-column-option-btn');
        if (save) {
            save.on('click', function (event) {
                let row = getNewColumnRow(newRowControl);

                if (!validateColumnName(newRowControl))
                    return;

                // Add valid row to table
                $('#column-options-table').find('tbody')
                    .append(getRowTemplate(row.key, row.label));

                cancelAddColumn();

                // bind actions only for last item added
                bindNewRowActions();
                bindNewRowInputChanges();
            });
        }
    }

    function getNewColumnRow(controlName) {
        let newRow = $('#' + controlName);
        let columnName = newRow.find('input');
        let columnType = newRow.find('select');
        let row = {};

        row.key = columnName[0].value;
        row.label = columnType[0].value;

        return row;
    }

    function validateColumnName(controlName) {
        let row = getNewColumnRow(controlName);

        // check against existing rows
        let existingRows = $('.key-input').toArray();
        let existing = existingRows.find(o => o.value.toLowerCase() == row.key.toLowerCase());

        if (existing) {
            addSummaryError('Duplicate Column names are not allowed');
            return false;
        }

        if (isEmptyOrSpaces(row.key)) {
            addSummaryError('You must provide a Column name');
            return false;
        }

        return true;
    }

    function getRowTemplate(name, type) {
        return `<tr><td><input type="text" class="form-control key-input" name="ColumnKeys" value="${name}" minlength="1" maxlength="25" required id="new-column-${name}" />
        </td><td><select class="form-control form-select" name="ColumnTypes" required id="new-column-type-${name}">${generateOptions(type)}</select></td>
        <td><button id="data-btn-${name}" class="delete-column-option btn btn-danger" type="button" data-busy-text="Processing..." data-bs-toggle="tooltip" data-bs-placement="top" aria-label="Delete" data-bs-original-title="Delete">
        <i class="fl fl-delete"></i></button></td></tr>`
    }

    function generateOptions(type) {        
        let options = ($('#SupportedFieldsList').val()).split(',');
        let selectOptions = '';
        options.forEach((element) => selectOptions += `<option ${element == type ? 'selected' : ''}>${element}</option>`);
        return selectOptions;
    }

    function bindCancelOption() {
        let cancel = $('#cancel-column-option-btn');
        if (cancel) {
            cancel.on('click', function () {
                let newRowTable = $('#add-new-column-table');
                if (newRowTable) {
                    cancelAddColumn();
                }
            });
        }
    }

    function cancelAddColumn() {
        let newColumnTable = $('#add-new-column-table');

        $('#new-row-key').val('');
        $('#new-row-label').val('');

        $('#new-row-label').blur();
        newColumnTable.toggleClass('hidden');
        addcolumnOption.toggleClass('hidden');
        clearSummaryError();
    }

    function bindNewRowActions() {
        // get last row of datatable
        let lastRow = columnOptionsTable.rows[columnOptionsTable.rows.length - 1];
        let deleteBtn = $(lastRow).find('.delete-column-option');
        if (deleteBtn) {
            bindDeleteAction(deleteBtn, columnOptionsTable);
        }
    }

    function bindNewRowInputChanges() {
        // get last row of datatable
        let lastRow = columnOptionsTable.rows[columnOptionsTable.rows.length - 1];
        let keyInput = $(lastRow).find('.key-input');
        if (keyInput) {
            bindInputChanges(keyInput, validityClass);
        }
    }

    function init() {
        columnOptionsTable = document.getElementById('column-options-table');
        bindRootActions();
        bindRowActions(deleteClass, columnOptionsTable);
        bindInputChanges($('.key-input'), validityClass);
    }

    PubSub.subscribe(
        'datagrid_widget_fired',
        () => {
            init();
        }
    );
});

