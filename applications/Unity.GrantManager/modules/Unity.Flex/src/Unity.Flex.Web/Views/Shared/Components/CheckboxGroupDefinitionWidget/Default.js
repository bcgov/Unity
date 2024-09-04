$(function () {
    let addCheckboxOption;   
    let checkboxOptionsTable;
    const checkKeyInputRegexBase = '[a-zA-Z0-9 ]+';
    const checkKeyInputRegexVal = new RegExp('^' + checkKeyInputRegexBase + '$');
    
    function bindRootActions() {
        addCheckboxOption = $('#add-checkbox-option-btn');

        bindAddCheckboxOption();
        bindSaveOption();
        bindCancelOption();
        bindNewRowKeyCheck();
    }

    function bindAddCheckboxOption() {
        let newRowTable = $('#add-new-checkbox-table');

        if (addCheckboxOption) {
            addCheckboxOption.on('click', function (event) {
                if (newRowTable) {
                    newRowTable.toggleClass('hidden');
                    $('#new-row-key').val('check' + ($('.key-input')?.toArray()?.length + 1));                    
                    $('#new-row-label').focus();
                    addCheckboxOption.toggleClass('hidden');
                }
            });
        }
    }

    function bindSaveOption() {
        let save = $('#save-checkbox-option-btn');
        if (save) {
            save.on('click', function (event) {               
                let row = getNewInputRow();

                if (!validateInputCharacters())
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

    function getNewInputRow() {
        let newRow = $('#new-checkbox-row');
        let inputs = newRow.find('input');
        let row = {};
        row.key = $(inputs[0]).val();
        row.label = $(inputs[1]).val();
        return row;
    }

    function validateInputCharacters() {
        let row = getNewInputRow();

        // check against existing rows
        let existingRows = $('.key-input').toArray();
        let existing = existingRows.find(o => o.value.toLowerCase() == row.key.toLowerCase());

        // validate format of row before adding                
        if (!isAlphanumericWithSpace(row.key)) {
            addSummaryError('Invalid key syntax provided');
            return false;
        }

        if (existing) {
            addSummaryError('Duplicate keys are not allowed');
            return false;
        }

        return true;
    }

    function clearSummaryError() {
        $('#invalid-input-summary-text').text();
        $('#invalid-input-error-summary').addClass('hidden');        
    }

    function addSummaryError(message) {
        $('#invalid-input-summary-text').text(message);        
        $('#invalid-input-error-summary').removeClass('hidden');                
    }

    function isAlphanumericWithSpace(str) {
        // Regular expression to match alphanumeric characters and spaces        
        return checkKeyInputRegexVal.test(str);
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

    function bindNewRowKeyCheck() {
        let newKey = $('#new-row-key');

        newKey.on('change', function (event) {               
            let valid = validateInputCharacters();
            if (valid === true) {
                clearSummaryError();
            }
        });
    }

    function cancelAddRow() {
        let newRowTable = $('#add-new-checkbox-table');

        $('#new-row-key').val('');
        $('#new-row-label').val('');

        newRowTable.toggleClass('hidden');
        addCheckboxOption.toggleClass('hidden');
        clearSummaryError();
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

