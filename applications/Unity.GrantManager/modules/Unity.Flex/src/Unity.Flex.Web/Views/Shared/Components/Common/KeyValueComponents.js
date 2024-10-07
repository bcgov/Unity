function clearSummaryError() {
    $('#invalid-input-summary-text').text();
    $('#invalid-input-error-summary').addClass('hidden');
}

function addSummaryError(message) {
    $('#invalid-input-summary-text').text(message);
    $('#invalid-input-error-summary').removeClass('hidden');
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

    if (existing) {
        addSummaryError('Duplicate Keys are not allowed');
        return false;
    }

    if (isEmptyOrSpaces(row.key)) {
        addSummaryError('You must provide a Key');
        return false;
    }

    if (isEmptyOrSpaces(row.label)) {
        addSummaryError('You must provide a Value');
        return false;
    }

    return true;
}

function isEmptyOrSpaces(str) {
    return str === null || str.match(/^ *$/) !== null;
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

function sanitizeInput(string) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#x27;',
        "/": '&#x2F;',
    };
    const reg = /[&<>"'/]/ig;
    return string.replace(reg, (match) => (map[match]));
}