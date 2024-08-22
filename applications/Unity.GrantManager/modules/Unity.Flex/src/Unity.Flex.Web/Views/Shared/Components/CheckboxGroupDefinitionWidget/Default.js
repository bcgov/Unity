$(function () {
    let addCheckboxOption;   
    let checkboxOptionsTable;

    function bindRootActions() {
        addCheckboxOption = $('#add-checkbox-option-btn');

        bindAddCheckboxOption();
        bindSaveOption();
        bindCancelOption();
    }

    function bindAddCheckboxOption() {
        let newRowTable = $('#add-new-checkbox-table');

        if (addCheckboxOption) {
            addCheckboxOption.on('click', function (event) {
                if (newRowTable) {
                    newRowTable.toggleClass('hidden');
                    addCheckboxOption.toggleClass('hidden');
                }
            });
        }
    }

    function bindSaveOption() {
        let save = $('#save-checkbox-option-btn');
        if (save) {
            save.on('click', function (event) {
                let newRow = $('#new-checkbox-row');
                let inputs = newRow.find('input');
                let row = {};
                row.key = $(inputs[0]).val();
                row.label = $(inputs[1]).val();

                // check against existing rows
                let existingRows = $('.key-input').toArray();
                let existing = existingRows.find(o => o.value.toLowerCase() == row.key.toLowerCase());

                // validate format of row before adding
                let pattern = /[a-zA-Z0-9]+/;
                if (!pattern.test(row.key)) {
                    window.alert('Invalid key syntax provided');
                    return;
                }

                if (existing) {
                    window.alert('Duplicate keys are not allowed');
                    return;
                }

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
        return `<tr><td><input type="text" class="form-control key-input" name="CheckboxKeys" pattern="[a-zA-Z0-9]+" value="${key}" minlength="1" maxlength="25" required id="new-chk-key-${key}" />
        </td><td><input type="text" class="form-control" name="CheckboxLabels" value="${label}" maxlength="25" required id="new-chk-label-${key}" />
        </td><td><button id="data-btn-${key}" class="delete-checkbox-option btn btn-light" type="button" data-busy-text="Processing..." data-bs-toggle="tooltip" data-bs-placement="top" aria-label="Delete" data-bs-original-title="Delete">
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

        newRowTable.toggleClass('hidden');
        addCheckboxOption.toggleClass('hidden');
    }

    function bindNewRowActions() {
        // get last row of datatable
        let lastRow = checkboxOptionsTable.rows[checkboxOptionsTable.rows.length - 1];
        let deleteBtn = $(lastRow).find('.delete-checkbox-option');
        if (deleteBtn) {
            bindDeleteAction(deleteBtn);            
        }        
    }

    function bindNewRowInputChanges() {
        // get last row of datatable
        let lastRow = checkboxOptionsTable.rows[checkboxOptionsTable.rows.length - 1];
        let keyInput = $(lastRow).find('.key-input');
        if (keyInput) {
            bindInputChanges(keyInput);
        }
    }

    function bindDeleteAction(buttons) {
        buttons.on('click', function (event) {
            let rowIndex = event.target.closest('tr').rowIndex;
            checkboxOptionsTable.deleteRow(rowIndex);
        })
    }

    function bindRowActions() {
        let deleteOptions = $('.delete-checkbox-option');
        if (deleteOptions) {
            bindDeleteAction(deleteOptions); 
        }
    }

    function bindInputChanges(keys) {
        if (keys) {
            keys.on('change', function (event) {
                let input = event.target;
                let result = input.checkValidity();

                if (!result) {
                    $(input).addClass('checkbox-input-valid')
                    $('#invalid-input-error-summary').removeClass('hidden');
                } else {
                    $(input).removeClass('checkbox-input-valid')
                    $('#invalid-input-error-summary').addClass('hidden');
                }                
            });
        }
    }

    function init() {
        checkboxOptionsTable = document.getElementById('checkbox-options-table');
        bindRootActions();
        bindRowActions();        
        bindInputChanges($('.key-input'));
    }

    PubSub.subscribe(
        'checkbox_group_widget_fired',
        () => {
            init();
        }
    );
});

