$(function () {
    let availableChefFieldsString = document.getElementById('availableChefsFields').value;
    let existingMappingString = document.getElementById('existingMapping').value;
    let intakeFieldsString = document.getElementById('intakeProperties').value;
    let chefsFormId = document.getElementById('chefsFormId').value;
    let formVersionId = document.getElementById('formVersionId').value;
    let intakeMapColumn = document.querySelector('#intake-map-available-fields-column');
    let worksheetMapColumn = document.querySelector('#worksheet-map-available-fields-column');
    let excludedIntakeMappings = new Set(['ConfirmationId', 'SubmissionId', 'SubmissionDate']);
    let dataTable;
    if (globalThis.toastr) { toastr.options.positionClass = 'toast-top-center'; }

    let allowableTypes = new Set(['textarea',
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
        'datagrid']);

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
        uiConfigurationTab: $('#nav-ui-configuration'),
        mappingTab: $('#nav-mapping-tab'),
        customFieldsTab: $('#nav-worksheet-fields-tab'),
        intakeFieldsTab: $('#nav-intake-fields-tab'),
        refreshAvailableWorksheetsHidden: $('#refresh_available_worksheets')
    };

    init();

    function init() {
        bindUIEvents();
        restoreActiveTab();
        dataTable = initializeApplicationFormsTable();
        let availableChefsFields = availableChefFieldsString ? JSON.parse(availableChefFieldsString) : []
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
        UIElements.mappingTab.on('click', handleMappingTabClick);

        // Persist active tab to localStorage on switch
        $('#nav-tab').on('shown.bs.tab', 'button[data-bs-toggle="tab"]', function () {
            const formId = document.getElementById('applicationFormId')?.value;
            if (formId) {
                localStorage.setItem('mapping-active-tab:' + formId, this.id);
            }
        });
    }

    function restoreActiveTab() {
        const formId = document.getElementById('applicationFormId')?.value;
        if (!formId) return;
        const savedTabId = localStorage.getItem('mapping-active-tab:' + formId);
        if (!savedTabId) return;
        const tabEl = document.getElementById(savedTabId);
        if (tabEl) {
            bootstrap.Tab.getOrCreateInstance(tabEl).show();
        }
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
            UIElements.btnSaveMapping.prop('disabled', true);
            handleSaveMapping($.parseJSON(mappingJsonStr));
            handleCancelMapping();

            abp.notify.success(
                '',
                'Edit mapping save successful. Reloading page to new version'
            );

            setTimeout(function () {
                globalThis.location.href = location.href;
            }, 500);

        }
        catch (err) {
            UIElements.btnSaveMapping.prop('disabled', false);
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
        abp.notify.success(
            '',
            'Reloading page to new version'
        );

        setTimeout(function () {
            const url = new URL(globalThis.location.href);

            // If this really is a GUID, validate it defensively
            if (!/^[0-9a-fA-F-]{36}$/.test(chefsFormVersionGuid)) {
                abp.notify.error("The CHEFS Form Version ID is not in a GUID format");
                return; // or handle error                
            }

            url.searchParams.set("ChefsFormVersionGuid", chefsFormVersionGuid);
            globalThis.location.href = url.toString();
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
                        let formVersion = data.formVersion;
                        let updatedApplicationFormName = data.updatedFormName;
                        let updatedNameMessage = updatedApplicationFormName ? 'Form name updated to ' + updatedApplicationFormName : 'Form name is unchanged';
                        if (updatedApplicationFormName) {
                            document.getElementById('applicationFormName').textContent = updatedApplicationFormName;
                        }

                        let availableChefsFields = JSON.parse(formVersion.availableChefsFields)
                        document.getElementById('availableChefsFields').value = JSON.stringify(availableChefsFields);
                        initializeIntakeMap(availableChefsFields);

                        abp.notify.success(
                            '',
                            'Synchronized Successful' + updatedNameMessage
                        );
                        navigateToVersion(formVersion.chefsFormVersionGuid);
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
    }

    function handleSaveMapping(mappingJson) {
        let formData = JSON.parse(document.getElementById('applicationFormVersionDtoString').value);
        formData["submissionHeaderMapping"] = JSON.stringify(mappingJson);
        formData["availableChefsFields"] = document.getElementById('availableChefsFields').value;
        formData["ChefsApplicationFormGuid"] = document.getElementById('applicationFormId').value;

        UIElements.btnSave.prop('disabled', true);
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
                },
                complete: function () {
                    UIElements.btnSave.prop('disabled', false);
                }
            }
        );
    }

    function handleReset() {
        $(intakeMapColumn).empty();
        $(worksheetMapColumn).empty();
        let availableChefsFields = availableChefFieldsString ? JSON.parse(availableChefFieldsString) : []
        initializeIntakeMap(availableChefsFields);
        bindExistingMaps();
    }


    function createIntakeFieldCard(intakeField) {
        let intakeFieldJson = intakeField;
        let dragableDiv = document.createElement('div');
        dragableDiv.id = 'unity_' + intakeFieldJson.Name;
        dragableDiv.className = 'card mapping-field';
        dragableDiv.setAttribute("draggable", "true");

        // Set icon HTML (internal code, safe)
        dragableDiv.innerHTML = `${setTypeIndicator(intakeField)}`;

        // Append label as text node to prevent HTML injection
        dragableDiv.appendChild(document.createTextNode(intakeFieldJson.Label));

        // Append asterisk and route to the appropriate column based on custom status
        if (intakeFieldJson.IsCustom) {
            dragableDiv.appendChild(document.createTextNode(" *"));
            dragableDiv.className += ' custom-field';
            worksheetMapColumn.appendChild(dragableDiv);
        } else {
            intakeMapColumn.appendChild(dragableDiv);
        }
    }

    function buildAvailableChefsFieldsRows(availableChefsFields) {
        let rowsToAdd = [];
        for (let key of Object.keys(availableChefsFields)) {
            let jsonObj = JSON.parse(availableChefsFields[key]);
            if (allowableTypes.has(jsonObj.type.trim())) {
                rowsToAdd.push([stripHtml(jsonObj.label), key, jsonObj.type, key]);
            }
        }
        return rowsToAdd;
    }

    function initializeIntakeMap(availableChefsFields) {
        try {

            let intakeFields = JSON.parse(intakeFieldsString);

            for (let intakeField of intakeFields) {
                if (!excludedIntakeMappings.has(intakeField.Name)) {
                    createIntakeFieldCard(intakeField);
                }
            }

            dataTable.clear();

            let rowsToAdd = buildAvailableChefsFieldsRows(availableChefsFields);

            if (rowsToAdd.length > 0) {
                dataTable.rows.add(rowsToAdd);
            }
            dataTable.draw();
        }
        catch (err) {
            console.info('Mapping error: ' + err);
        }
    }


    document.addEventListener('dragstart', function (ev) {
        if (ev.target.classList.contains('non-drag')) {
            ev.preventDefault();
            return;
        } else if (ev.target.classList.contains('custom-field')) {
            UIElements.customFieldsTab.trigger('click');
        } else if (!ev.target.classList.contains('custom-field')) {
            UIElements.intakeFieldsTab.trigger('click');
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
                beingDragged.before(dragOver);
            } else if (draggedIndex > dragOverIndex) {
                beingDragged.after(dragOver);
            }
        } else {
            dragOver.before(beingDragged);
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






    function handleMappingTabClick() {
        // Refresh the hidden field with the latest form version ID
        let refreshAvailableWorkSheets = UIElements.refreshAvailableWorksheetsHidden.val();
        if (refreshAvailableWorkSheets && refreshAvailableWorkSheets !== "undefined") {
            navigateToVersion(refreshAvailableWorkSheets);
        }
    }

    PubSub.subscribe(
        'refresh_available_worksheets',
        (_, data) => {
            UIElements.refreshAvailableWorksheetsHidden.val(data.chefsFormVersionId);
        }
    );

    // AI Configuration tab
    const btnSaveAIConfig = document.getElementById('btn-save-ai-config');
    const btnCancelAIConfig = document.getElementById('btn-cancel-ai-config');

    if (btnSaveAIConfig) {
        const aiFormId = document.getElementById('applicationFormId').value;
        const automaticCheckbox = document.getElementById('AutomaticallyGenerateAIAnalysis');
        const manualCheckbox = document.getElementById('ManuallyInitiateAIAnalysis');

        let lastSavedAIValues = {
            automaticallyGenerateAIAnalysis: automaticCheckbox ? automaticCheckbox.checked : false,
            manuallyInitiateAIAnalysis: manualCheckbox ? manualCheckbox.checked : false
        };

        btnSaveAIConfig.addEventListener('click', function () {
            btnSaveAIConfig.disabled = true;
            abp.ajax({
                url: `/api/app/application-form/${aiFormId}/ai-config`,
                type: 'PATCH',
                data: JSON.stringify({
                    automaticallyGenerateAIAnalysis: automaticCheckbox ? automaticCheckbox.checked : false,
                    manuallyInitiateAIAnalysis: manualCheckbox ? manualCheckbox.checked : false
                }),
                contentType: 'application/json'
            })
                .done(function () {
                    lastSavedAIValues = {
                        automaticallyGenerateAIAnalysis: automaticCheckbox ? automaticCheckbox.checked : false,
                        manuallyInitiateAIAnalysis: manualCheckbox ? manualCheckbox.checked : false
                    };
                    abp.notify.success('AI configuration saved successfully.');
                })
                .fail(function () {
                    abp.notify.error('Failed to save AI configuration.');
                })
                .always(function () {
                    btnSaveAIConfig.disabled = false;
                });
        });

        if (btnCancelAIConfig) {
            btnCancelAIConfig.addEventListener('click', function () {
                if (automaticCheckbox) automaticCheckbox.checked = lastSavedAIValues.automaticallyGenerateAIAnalysis;
                if (manualCheckbox) manualCheckbox.checked = lastSavedAIValues.manuallyInitiateAIAnalysis;
            });
        }
    }
});


function handleBack() {
    location.href = '/ApplicationForms';
}

function beingDragged(ev) {
    let draggedEl = ev.target;
    if (draggedEl.classList + "" !== "undefined") {
        draggedEl.classList.add('dragging');
    }
}

function dragEnd(ev) {
    let draggedEl = ev.target;
    if (draggedEl.classList + "" !== "undefined") {
        draggedEl.classList.remove('dragging');
    }
}