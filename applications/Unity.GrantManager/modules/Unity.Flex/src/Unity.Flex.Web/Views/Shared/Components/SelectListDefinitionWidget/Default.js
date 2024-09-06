$(function () {
    let addSelectListOption;   
    let selectlistOptionsTable;
    const checkKeyInputRegexBase = '[a-zA-Z0-9 ]+';
    const checkKeyInputRegexVal = new RegExp('^' + checkKeyInputRegexBase + '$');
    
    function bindRootActions() {
        addSelectListOption = $('#add-selectlist-option-btn');

        bindaddSelectListOption();
        bindSaveOption();
        bindCancelOption();
        bindNewRowKeyCheck();
    }

    function bindaddSelectListOption() {
        let newRowTable = $('#add-new-selectlist-table');

        if (addSelectListOption) {
            addSelectListOption.on('click', function (event) {
                if (newRowTable) {
                    newRowTable.toggleClass('hidden');
                    $('#new-row-key').val('key' + ($('.key-input')?.toArray()?.length + 1));                    
                    $('#new-row-label').focus();
                    addSelectListOption.toggleClass('hidden');
                }
            });
        }
    }

    function bindSaveOption() {
        let save = $('#save-selectlist-option-btn');
        if (save) {
            save.on('click', function (event) {               
                let row = getNewInputRow();

                if (!validateInputCharacters())
                    return;                    

                // Add valid row to table
                $('#selectlist-options-table').find('tbody')
                    .append(getRowTemplate(row.key, row.label));

                cancelAddRow();

                // bind actions only for last item added
                bindNewRowActions();
                bindNewRowInputChanges();
            });
        }
    }

    function getNewInputRow() {
        let newRow = $('#new-selectlist-row');
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
        return `<tr><td><input type="text" class="form-control key-input" name="Keys" pattern="${checkKeyInputRegexBase}" value="${key}" minlength="1" maxlength="25" required id="new-list-key-${key}" />
        </td><td><input type="text" class="form-control" name="Values" value="${label}" maxlength="25" required id="new-list-label-${key}" />
        </td><td><button id="data-btn-${key}" class="delete-selectlist-option btn btn-danger" type="button" data-busy-text="Processing..." data-bs-toggle="tooltip" data-bs-placement="top" aria-label="Delete" data-bs-original-title="Delete">
        <i class="fl fl-delete"></i></button></td></tr>`
    }

    function bindCancelOption() {
        let cancel = $('#cancel-selectlist-option-btn');
        if (cancel) {
            cancel.on('click', function () {
                let newRowTable = $('#add-new-selectlist-table');
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
        let newRowTable = $('#add-new-selectlist-table');

        $('#new-row-key').val('');
        $('#new-row-label').val('');

        newRowTable.toggleClass('hidden');
        addSelectListOption.toggleClass('hidden');
        clearSummaryError();
    }

    function bindNewRowActions() {
        // get last row of datatable
        let lastRow = selectlistOptionsTable.rows[selectlistOptionsTable.rows.length - 1];
        let deleteBtn = $(lastRow).find('.delete-selectlist-option');
        if (deleteBtn) {
            bindDeleteAction(deleteBtn);            
        }        
    }

    function bindNewRowInputChanges() {
        // get last row of datatable
        let lastRow = selectlistOptionsTable.rows[selectlistOptionsTable.rows.length - 1];
        let keyInput = $(lastRow).find('.key-input');
        if (keyInput) {
            bindInputChanges(keyInput);
        }
    }

    function bindDeleteAction(buttons) {
        buttons.on('click', function (event) {
            let rowIndex = event.target.closest('tr').rowIndex;
            selectlistOptionsTable.deleteRow(rowIndex);
        })
    }

    function bindRowActions() {
        let deleteOptions = $('.delete-selectlist-option');
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
                    $(input).addClass('selectlist-input-valid')
                    $('#invalid-input-error-summary').removeClass('hidden');
                } else {
                    $(input).removeClass('selectlist-input-valid')
                    $('#invalid-input-error-summary').addClass('hidden');
                }                
            });
        }
    }

    function init() {
        selectlistOptionsTable = document.getElementById('selectlist-options-table');
        bindRootActions();
        bindRowActions();        
        bindInputChanges($('.key-input'));
    }

    PubSub.subscribe(
        'selectlist_widget_fired',
        () => {
            init();
        }
    );
});

