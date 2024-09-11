
const checkKeyInputRegexBase = '[a-zA-Z0-9 ]+';
const checkKeyInputRegexVal = new RegExp('^' + checkKeyInputRegexBase + '$');

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

function getNewInputRow(controlName) {
    let newRow = $('#' + controlName);
    let inputs = newRow.find('input');
    let row = {};
    row.key = inputs[0].value;
    row.label = inputs[1].value;
    return row;
}

function validateInputCharacters(controlName) {
    let row = getNewInputRow(controlName);

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

function bindNewRowKeyCheck(newRowControl) {
    let newKey = $('#new-row-key');

    newKey.on('change', function (event) {
        let valid = validateInputCharacters(newRowControl);
        if (valid === true) {
            clearSummaryError();
        }
    });
}

function bindRowActions(deleteClass, table) {
    let deleteOptions = $('.' + deleteClass);
    if (deleteOptions) {
        bindDeleteAction(deleteOptions, table);
    }
}

function bindDeleteAction(buttons, table) {
    buttons.on('click', function (event) {
        let rowIndex = event.target.closest('tr').rowIndex;
        table.deleteRow(rowIndex);
    })
}

function bindNewKeyValueOption(newItemTable, option, keyStart) {
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

function bindInputChanges(keys, validityClass) {
    if (keys) {
        keys.on('change', function (event) {
            let input = event.target;
            let result = input.checkValidity();

            if (!result) {
                $(input).addClass(validityClass)
                $('#invalid-input-error-summary').removeClass('hidden');
            } else {
                $(input).removeClass(validityClass)
                $('#invalid-input-error-summary').addClass('hidden');
            }
        });
    }
}