$(function () {
    let worksheetsModal = new abp.ModalManager(abp.appPath + 'ApplicationForms/LinkWorksheetsModal');
    let availableChefFieldsString = document.getElementById('availableChefsFields').value;
    let existingMappingString = document.getElementById('existingMapping').value;
    let intakeFieldsString = document.getElementById('intakeProperties').value;
    let chefsFormId = document.getElementById('chefsFormId').value;
    let formVersionId = document.getElementById('formVersionId').value;
    let applicationFormId = document.getElementById('applicationFormId').value;
    let intakeMapColumn = document.querySelector('#intake-map-available-fields-column');
    let excludedIntakeMappings = ['ConfirmationId', 'SubmissionId', 'SubmissionDate'];
    let dataTable;
    toastr.options.positionClass = 'toast-top-center';

    let allowableTypes = ['textarea',
        'orgbook',
        'textfield',
        'currency',
        'datetime',
        'checkbox',
        'select',
        'selectboxes',
        'radio',
        'phoneNumber',
        'email',
        'number',
        'time',
        'day',
        'hidden',
        'simpletextfield',
        'simpletextfieldadvanced',
        'simpletime',
        'simpletimeadvanced',
        'simplenumber',
        'simplenumberadvanced',
        'simplephonenumber',
        'simplephonenumberadvanced',
        'simpleselect',
        'simpleselectadvanced',
        'simpleday',
        'simpledayadvanced',
        'simpleemail',
        'simpleemailadvanced',
        'simpledatetime',
        'simpledatetimeadvanced',
        'simpleurladvanced',
        'simplecheckbox',
        'simpleradios',
        'simpleradioadvanced',
        'simplecheckboxes',
        'simplecheckboxadvanced',
        'simplecurrencyadvanced',
        'simpletextarea',
        'simpletextareaadvanced',
        'bcaddress',
        'datagrid'];

    const UIElements = {
        btnBack: $('#btn-back'),
        btnSave: $('#btn-save'),
        btnEdit: $('#btn-edit'),
        btnSync: $('#btn-sync'),
        btnReset: $('#btn-reset'),
        btnClose: $('.btn-close'),
        btnSaveMapping: $('#btn-save-mapping'),
        btnCancel: $('#btn-cancel-mapping'),
        inputSearchBar: $('#search-bar'),
        selectVersionList: $('#applicationFormVersion'),
        editMappingModal: $('#editMappingModal'),
        linkWorksheets: $('#btn-link-worksheets'),
        uiConfigurationTab: $('#nav-ui-configuration')
    };

    init();

    worksheetsModal.onResult(function (_, response) {
        navigateToVersion(response.responseText.chefsFormVersionId);
    });

    function init() {
        bindUIEvents();
        dataTable = initializeDataTable();
        let availableChefsFields = JSON.parse(availableChefFieldsString)
        initializeIntakeMap(availableChefsFields);
        bindExistingMaps();
        setupTooltips();
        initializeUIConfiguration();
    }

    function setupTooltips() {
        $('[data-toggle="tooltip"]').tooltip({
            placement: 'top'
        });
    }

    function bindUIEvents() {
        UIElements.btnBack.on('click', handleBack);
        UIElements.btnSave.on('click', handleSave);
        UIElements.btnSaveMapping.on('click', handleSaveEditMapping);
        UIElements.btnSync.on('click', handleSync);
        UIElements.btnEdit.on('click', handleEdit);
        UIElements.btnReset.on('click', handleReset);
        UIElements.btnCancel.on('click', handleCancelMapping);
        UIElements.btnClose.on('click', handleCancelMapping);
        UIElements.inputSearchBar.on('keyup', handleSeearchBar);
        UIElements.selectVersionList.on('change', handleSelectVersion);
        UIElements.linkWorksheets.on('click', handleLinkWorksheets);
    }

    function handleLinkWorksheets() {
        worksheetsModal.open({ formVersionId: $('#chefsFormVersionId').val(), formName: $('#formName').val(), size: 'Large' });
    }

    function initializeUIConfiguration() {
        const providerName = 'F';
        const providerKey = $('#applicationFormId').val();
        const providerKeyDisplayName = 'Test.Display.Name';

        $.ajax({
            url: abp.appPath + 'SettingManagement/ZoneManagement',
            type: 'GET',
            data: {
                providerName: providerName,
                providerKey: providerKey,
                providerKeyDisplayName: providerKeyDisplayName
            },
            success: function (response) {
                UIElements.uiConfigurationTab.html(response);
            },
            error: function () {
                abp.notify.error('Failed to load UI Configuration.');
            }
        });
    }

    function handleEdit() {
        $('#jsonText').val(prettyJson(existingMappingString));
        UIElements.editMappingModal.addClass('display-modal');
    }

    function handleSaveEditMapping() {
        try {
            let jsonText = $('#jsonText').val();
            $.parseJSON(jsonText);
            let mappingJsonStr = jsonText.replace(/\s+/g, ' ').replace(/(\r\n|\n|\r)/gm, "");
            handleSaveMapping($.parseJSON(mappingJsonStr));
            handleCancelMapping();

            abp.notify.success(
                '',
                'Edit mapping save successful. Reloading page to new version'
            );

            setTimeout(function () {
                window.location.href = location.href;
            }, 500);

        }
        catch (err) {
            abp.notify.error(
                '',
                'The JSON is not valid:' + err
            );
        }
    }

    function handleCancelMapping() {
        UIElements.editMappingModal.removeClass('display-modal');
    }

    function handleSeearchBar(e) {
        let filterValue = e.currentTarget.value;
        let oTable = $('#ApplicationFormsTable').dataTable();
        oTable.fnFilter(filterValue);
    }

    function handleSelectVersion(e) {
        let chefsFormVersionGuid = e.currentTarget.value;
        navigateToVersion(chefsFormVersionGuid);
    }

    function navigateToVersion(chefsFormVersionGuid) {
        let searchStr = "&ChefsFormVersionGuid=";
        let indexOfVersion = location.href.indexOf(searchStr);

        abp.notify.success(
            '',
            'Reloading page to new version'
        );

        setTimeout(function () {
            if (indexOfVersion > 0) {
                location.href = location.href.substring(0, indexOfVersion + searchStr.length) + chefsFormVersionGuid;
            } else {
                location.href = location.href + "&ChefsFormVersionGuid=" + chefsFormVersionGuid;
            }
        }, 500);
    }

    function bindExistingMaps() {
        if (existingMappingString + "" != "undefined" && existingMappingString != null && existingMappingString != "") {
            try {
                let existingMapping = JSON.parse(existingMappingString);
                let keys = Object.keys(existingMapping);
                for (let key of keys) {
                    let intakeProperty = key;
                    let chefsMappingProperty = existingMapping[intakeProperty];
                    let intakeMappingCard = document.getElementById("unity_" + intakeProperty);
                    let chefsMappingDiv = document.getElementById(chefsMappingProperty);
                    if (chefsMappingDiv != null && intakeMappingCard != null) {
                        chefsMappingDiv.appendChild(intakeMappingCard);
                    } else {
                        abp.notify.error(
                            '',
                            'Could not map existing: ' + chefsMappingProperty
                        );
                    }
                }
            } catch (err) {
                console.log(err);
            }
        }
    }

    function initializeDataTable() {
        return new DataTable('#ApplicationFormsTable', {
            info: false,
            ordering: false,
            fixedHeader: true,
            paging: false,
            columnDefs: [
                {
                    render: function (data) {
                        return '<div id="' + data + '" class="col map-div non-drag" draggable="false"></div>';
                    },
                    targets: 3
                }
            ]
        });
    }

    function handleSync() {
        let chefsFormVersionId = document.getElementById('chefsFormVersionId').value;
        if (!validateGuid(chefsFormVersionId)) {
            abp.notify.error(
                '',
                'The Form Version ID is not in a GUID format'
            );
            return;
        }

        if (chefsFormVersionId == "") {
            abp.notify.error(
                '',
                'ChefsFormVersionGuid is neeeded - Mapping Not Synchronized Successful'
            );

        } else {
            $.ajax(
                {
                    url: `/api/app/form/${chefsFormId}/version/${chefsFormVersionId}`,
                    type: "POST",
                    success: function (data) {
                        let availableChefsFields = JSON.parse(data.availableChefsFields)
                        document.getElementById('availableChefsFields').value = JSON.stringify(availableChefsFields);
                        initializeIntakeMap(availableChefsFields);

                        abp.notify.success(
                            '',
                            'Synchronized Successful'
                        );
                        navigateToVersion(data.chefsFormVersionGuid);
                    },
                    error: function () {
                        abp.notify.error(
                            '',
                            'Mapping Not Synchronized Successful'
                        );
                    }
                }
            );
        }
    }

    function validateGuid(textString) {
        return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-5][0-9a-f]{3}-[089ab][0-9a-f]{3}-[0-9a-f]{12}$/.test(textString);
    }

    function handleSave() {
        let mappingDivs = $('.map-div');
        let mappingJson = {};

        for (let mappingDiv of mappingDivs) {
            let chefMappingDiv = mappingDiv;
            if (chefMappingDiv.childElementCount > 0) {

                let chefsKey = mappingDiv.id;
                let intakeMappingChildren = chefMappingDiv.children;

                for (let intakeMappingChild of intakeMappingChildren) {
                    mappingJson[intakeMappingChild.id.replace('unity_', '')] = chefsKey;
                }
            }
        }
        handleSaveMapping(mappingJson);
        saveScoresheet();
    }

    function saveScoresheet() {
        let appFormId = $('#applicationFormId').val();
        let originalValue = $('#originalScoresheetId').val();
        let scoresheetId = $('#scoresheet').val();
        if (originalValue == scoresheetId) {
            return;
        }
        unity.grantManager.applicationForms.applicationForm.saveApplicationFormScoresheet({ applicationFormId: appFormId, scoresheetId: scoresheetId })
            .then(response => {
                abp.notify.success(
                    'Scoresheet is successfully saved.',
                    'Application Form Scoresheet'
                );
                $('#originalScoresheetId').val(scoresheetId);
                Swal.fire({
                    title: "Note",
                    text: "Please note that any changes made to the scoresheet template will not impact assessments that have already been scored using the previous scoresheet template.",
                    confirmButtonText: 'Ok',
                    customClass: {
                        confirmButton: 'btn btn-primary'
                    }
                });
            });
    }

    function handleSaveMapping(mappingJson) {
        let formData = JSON.parse(document.getElementById('applicationFormVersionDtoString').value);
        formData["submissionHeaderMapping"] = JSON.stringify(mappingJson);
        formData["availableChefsFields"] = document.getElementById('availableChefsFields').value;
        formData["ChefsApplicationFormGuid"] = document.getElementById('applicationFormId').value;

        $.ajax(
            {
                url: "/api/app/application-form-version/" + formVersionId,
                data: JSON.stringify(formData),
                contentType: "application/json",
                type: "PUT",
                success: function (data) {
                    $('#existingMapping').val(data.submissionHeaderMapping);
                    existingMappingString = data.submissionHeaderMapping;
                    abp.notify.success(
                        data.responseText,
                        'Mapping Saved Successfully'
                    );
                },
                error: function (data) {
                    abp.notify.error(
                        data.responseText,
                        'Mapping Not Saved Successful'
                    );
                }
            }
        );
    }

    function handleReset() {
        $(intakeMapColumn).empty();
        let availableChefsFields = JSON.parse(availableChefFieldsString)
        initializeIntakeMap(availableChefsFields);
        bindExistingMaps();
    }

    function handleBack() {
        location.href = '/ApplicationForms';
    }

    function initializeIntakeMap(availableChefsFields) {
        try {

            let intakeFields = JSON.parse(intakeFieldsString);

            for (let intakeField of intakeFields) {
                let intakeFieldJson = intakeField;
                if (!excludedIntakeMappings.includes(intakeFieldJson.Name)) {
                    let dragableDiv = document.createElement('div');
                    dragableDiv.id = 'unity_' + intakeFieldJson.Name;
                    dragableDiv.className = 'card mapping-field';
                    dragableDiv.setAttribute("draggable", "true");
                    dragableDiv.innerHTML = `${setTypeIndicator(intakeField)}` + intakeFieldJson.Label + (intakeFieldJson.IsCustom ? " *" : "");
                    intakeMapColumn.appendChild(dragableDiv);
                }
            }

            let keys = Object.keys(availableChefsFields);
            dataTable.clear();

            let rowsToAdd = [];
            for (let key of keys) {
                let jsonObj = JSON.parse(availableChefsFields[key]);
                if (allowableTypes.includes(jsonObj.type.trim())) {
                    rowsToAdd.push([stripHtml(jsonObj.label), key, jsonObj.type, key]);
                }
            }

            if (rowsToAdd.length > 0) {
                dataTable.rows.add(rowsToAdd);
            }
            dataTable.draw();
        }
        catch (err) {
            console.info('Mapping error: ' + err);
        }
    }

    function setTypeIndicator(intakeField) {
        switch (intakeField.Type) {
            case 'String':
            case 'Phone':
            case 'Date':
            case 'Email':
            case 'Radio':
            case 'Checkbox':
            case 'CheckboxGroup':
            case 'SelectList':
            case 'BCAddress':
            case 'TextArea':
            case 'DataGrid':
                return `<i class="${setTypeIcon(intakeField)}"></i> `;
            case 'Number':
                return setTypeIndicatorText('123');
            case 'Currency':
                return setTypeIndicatorText('$');
            case 'YesNo':
                return setTypeIndicatorText('Y/N');
            default:
                return '';
        }
    }

    function setTypeIcon(intakeField) {
        switch (intakeField.Type) {
            case 'String':
                return 'fl fl-font';
            case 'Phone':
                return 'fl fl-phone';
            case 'Date':
                return 'fl fl-datetime';
            case 'Email':
                return 'fl fl-mail';
            case 'Radio':
                return 'fl fl-radio';
            case 'Checkbox':
                return 'fl fl-checkbox-checked';
            case 'CheckboxGroup':
                return 'fl fl-multi-select';
            case 'SelectList':
                return 'fl fl-list';
            case 'BCAddress':
                return 'fl fl-globe';
            case 'TextArea':
                return 'fl fl-text-area';
            case 'DataGrid':
                return 'fl fl-datagrid';
            default:
                return '';
        }
    }

    function setTypeIndicatorText(text) {
        return `<span class="mapping-indicator-text">${text}</span>`;
    }

    function stripHtml(html) {
        let tmp = document.createElement("DIV");
        tmp.innerHTML = html;
        return tmp.textContent || tmp.innerText || "";
    }

    document.addEventListener('dragstart', function (ev) {
        if (ev.target.classList.contains('non-drag')) {
            ev.preventDefault();
            return;
        }
        beingDragged(ev);
    });

    document.addEventListener('dragend', function (ev) {
        if (ev.target.classList.contains('non-drag')) {
            ev.preventDefault();
            return;
        }
        dragEnd(ev);
    });

    document.addEventListener('dragover', function (event) {
        let beingDragged = document.querySelector('.dragging');
        if (event.target.matches('.card')) {
            if (beingDragged.classList.contains('card')) {
                allowDrop(event);
            }
        }
        if (event.target.matches('.col')) {
            if (beingDragged.classList.contains('card')) {
                colDraggedOver(event);
            }
            if (beingDragged.classList.contains('col')) {
                allowDrop(event);
            }
        }
    });

    function beingDragged(ev) {
        let draggedEl = ev.target;
        if (draggedEl.classList + "" != "undefined") {
            draggedEl.classList.add('dragging');
        }
    }

    function dragEnd(ev) {
        let draggedEl = ev.target;
        if (draggedEl.classList + "" != "undefined") {
            draggedEl.classList.remove('dragging');
        }
    }

    function allowDrop(ev) {
        ev.preventDefault();

        let dragOver = ev.target;
        let dragOverParent = dragOver.parentElement;
        let beingDragged = document.querySelector('.dragging');
        let draggedParent = beingDragged.parentElement;

        let draggedIndex = whichChild(beingDragged);
        let dragOverIndex = whichChild(dragOver);

        if (draggedParent === dragOverParent) {
            if (draggedIndex < dragOverIndex) {
                draggedParent.insertBefore(dragOver, beingDragged);
            }

            if (draggedIndex > dragOverIndex) {
                draggedParent.insertBefore(dragOver, beingDragged.nextSibling);
            }
        }
        if (draggedParent !== dragOverParent) {
            dragOverParent.insertBefore(beingDragged, dragOver);
        }
    }

    function colDraggedOver(event) {
        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');
        let draggedParent = beingDragged.parentElement;
        if (
            draggedParent.id !== dragOver.id &&
            draggedParent.classList.contains('col') &&
            dragOver.classList.contains('col')
        ) {
            if (dragOver.childElementCount == 0) {
                dragOver.appendChild(beingDragged);
            }
        }
    }

    function whichChild(el) {
        let i = 0;
        while ((el = el.previousSibling) != null) ++i;
        return i;
    }

    const TAB = '    ';

    function prettyJson(jsonText) {
        if (!jsonText) {
            return jsonText;
        }

        let prettyJson = new Array();
        let depth = 0;
        let currChar;
        let prevChar;
        let doubleQuoteIn = false;

        for (let i = 0; i < jsonText.length; i++) {
            currChar = jsonText.charAt(i);

            if (currChar == '\"') {
                if (prevChar != '\\') {
                    doubleQuoteIn = !doubleQuoteIn;
                }
            }

            switch (currChar) {
                case '{':
                    prettyJson.push(currChar);
                    if (!doubleQuoteIn) {
                        prettyJson.push('\n');
                        insertTab(prettyJson, ++depth);
                    }
                    break;
                case '}':
                    if (!doubleQuoteIn) {
                        prettyJson.push('\n');
                        insertTab(prettyJson, --depth);
                    }
                    prettyJson.push(currChar);
                    break;
                case ',':
                    prettyJson.push(currChar);
                    if (!doubleQuoteIn) {
                        prettyJson.push('\n');
                        insertTab(prettyJson, depth);
                    }
                    break;
                default:
                    prettyJson.push(currChar);
                    break;
            }

            prevChar = currChar;
        }
        return prettyJson.join('');
    }

    function insertTab(prettyJson, depth) {
        for (let i = 0; i < depth; i++) {
            prettyJson.push(TAB);
        }
    }

    $("#directApproval").on('change', function (e) {

        let config = {
            "isDirectApproval": this.checked
        }
        $.ajax(
            {
                url: `/api/app/application-form/${applicationFormId}/other-config`,
                data: JSON.stringify(config),
                contentType: "application/json",
                type: "PUT",
                success: function (data) {

                    abp.notify.success(
                        data.responseText,
                        'Settings Saved Successfully'
                    );
                },
                error: function (data) {
                    abp.notify.error(
                        data.responseText,
                        'Settings Not Saved Successful'
                    );
                }
            }
        );
    })
});
