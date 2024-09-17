$(function () {
    let addCheckboxOption;   
    let checkboxOptionsTable;
    let newRowControl = 'new-checkbox-row';
    let deleteClass = 'delete-checkbox-option';
    let newCheckboxTable = 'add-new-checkbox-table';
    let validityClass = 'checkbox-input-valid';

    function bindRootActions() {
        addCheckboxOption = $('#add-checkbox-option-btn');

        bindNewKeyValueOption(newCheckboxTable, addCheckboxOption, 'check');
        bindSaveOption();
        bindCancelOption();
        bindNewRowKeyCheck(newRowControl);
    }

    function bindSaveOption() {
        let save = $('#save-checkbox-option-btn');
        if (save) {
            save.on('click', function (event) {               
                let row = getNewInputRow(newRowControl);

                if (!validateInputCharacters(newRowControl))
                    return;                    

                // Add valid row to table
                $('#checkbox-options-table').find('tbody')
                    .append(getRowTemplate(row.key, row.label));

                cancelAddRow();

                // bind actions only for last item added
                bindNewRowActions();
                bindNewRowInputChanges();
            });
        }
    }

    function getRowTemplate(key, label) {
        return `<tr><td><input type="text" class="form-control key-input" name="CheckboxKeys" pattern="${checkKeyInputRegexBase}" value="${key}" minlength="1" maxlength="25" required id="new-chk-key-${key}" />
        </td><td><input type="text" class="form-control" name="CheckboxLabels" value="${label}" maxlength="25" required id="new-chk-label-${key}" />
        </td><td><button id="data-btn-${key}" class="delete-checkbox-option btn btn-danger" type="button" data-busy-text="Processing..." data-bs-toggle="tooltip" data-bs-placement="top" aria-label="Delete" data-bs-original-title="Delete">
        <i class="fl fl-delete"></i></button></td></tr>`
    }

    function bindCancelOption() {
        let cancel = $('#cancel-checkbox-option-btn');
        if (cancel) {
            cancel.on('click', function () {
                let newRowTable = $('#add-new-checkbox-table');
                if (newRowTable) {
                    cancelAddRow();
                }
            });
        }
    }

    function cancelAddRow() {
        let newRowTable = $('#add-new-checkbox-table');

        $('#new-row-key').val('');
        $('#new-row-label').val('');

        $('#new-row-label').blur();
        newRowTable.toggleClass('hidden');
        addCheckboxOption.toggleClass('hidden');
        clearSummaryError();
    }

    function bindNewRowActions() {
        // get last row of datatable
        let lastRow = checkboxOptionsTable.rows[checkboxOptionsTable.rows.length - 1];
        let deleteBtn = $(lastRow).find('.delete-checkbox-option');
        if (deleteBtn) {
            bindDeleteAction(deleteBtn, checkboxOptionsTable);            
        }        
    }

    function bindNewRowInputChanges() {
        // get last row of datatable
        let lastRow = checkboxOptionsTable.rows[checkboxOptionsTable.rows.length - 1];
        let keyInput = $(lastRow).find('.key-input');
        if (keyInput) {
            bindInputChanges(keyInput, validityClass);
        }
    }

    function init() {
        checkboxOptionsTable = document.getElementById('checkbox-options-table');
        bindRootActions();
        bindRowActions(deleteClass, checkboxOptionsTable);       
        bindInputChanges($('.key-input'), validityClass);
    }

    PubSub.subscribe(
        'checkbox_group_widget_fired',
        () => {
            init();
        }
    );
});

