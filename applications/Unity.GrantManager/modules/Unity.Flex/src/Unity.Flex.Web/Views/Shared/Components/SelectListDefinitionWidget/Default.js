$(function () {
    let addSelectListOption;
    let selectlistOptionsTable;
    let newRowControl = 'new-selectlist-row';
    let deleteClass = 'delete-selectlist-option';
    let newListItemTable = 'add-new-selectlist-table';
    let validityClass = 'selectlist-input-valid';

    function bindRootActions() {
        addSelectListOption = $('#add-selectlist-option-btn');
        
        bindNewKeyValueOption(newListItemTable, addSelectListOption, 'key');
        bindSaveOption();
        bindCancelOption();
        bindNewRowKeyCheck(newRowControl);
    }

    function bindSaveOption() {
        let save = $('#save-selectlist-option-btn');
        if (save) {
            save.on('click', function (event) {
                let row = getNewInputRow(newRowControl);

                if (!validateInputCharacters(newRowControl))
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

    function getRowTemplate(key, label) {
        return `<tr><td><input type="text" class="form-control key-input" name="SelectListKeys" pattern="${checkKeyInputRegexBase}" value="${key}" minlength="1" maxlength="25" required id="new-list-key-${key}" />
        </td><td><input type="text" class="form-control" name="SelectListValues" value="${label}" maxlength="25" required id="new-list-label-${key}" />
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

    function cancelAddRow() {
        let newRowTable = $('#add-new-selectlist-table');

        $('#new-row-key').val('');
        $('#new-row-label').val('');

        $('#new-row-label').blur();
        newRowTable.toggleClass('hidden');
        addSelectListOption.toggleClass('hidden');
        clearSummaryError();
    }

    function bindNewRowActions() {
        // get last row of datatable
        let lastRow = selectlistOptionsTable.rows[selectlistOptionsTable.rows.length - 1];
        let deleteBtn = $(lastRow).find('.delete-selectlist-option');
        if (deleteBtn) {
            bindDeleteAction(deleteBtn, selectlistOptionsTable);
        }
    }

    function bindNewRowInputChanges() {
        // get last row of datatable
        let lastRow = selectlistOptionsTable.rows[selectlistOptionsTable.rows.length - 1];
        let keyInput = $(lastRow).find('.key-input');
        if (keyInput) {
            bindInputChanges(keyInput, validityClass);
        }
    }

    function init() {
        selectlistOptionsTable = document.getElementById('selectlist-options-table');
        bindRootActions();
        bindRowActions(deleteClass, selectlistOptionsTable);
        bindInputChanges($('.key-input'), validityClass);
    }

    PubSub.subscribe(
        'selectlist_widget_fired',
        () => {
            init();
        }
    );
});

