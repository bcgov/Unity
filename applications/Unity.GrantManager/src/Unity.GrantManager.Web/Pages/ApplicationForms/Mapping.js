$(function () {

    let availableChefFieldsString = document.getElementById('availableChefsFields').value;
    let existingMappingString = document.getElementById('existingMapping').value;
    let intakeFieldsString = document.getElementById('intakeProperties').value;
    let chefsFormId = document.getElementById('chefsFormId').value;
    let formVersionId = document.getElementById('formVersionId').value;
    let intakeMapColumn = document.querySelector('#intake-map-available-fields-column');
    let excludedIntakeMappings = ['ConfirmationId', 'SubmissionId'];
    let dataTable;
    toastr.options.positionClass = 'toast-top-center';

    let allowableTypes = ['textarea', 
                          'orgbook',
                          'textfield', 
                          'currency', 
                          'datetime', 
                          'checkbox',
                          'select',
                          'radio',
                          'simpletextfield', 
                          'simpletextfieldadvanced',
                          'simpletime',
                          'simpletimeadvanced',
                          'simplenumber',
                          'simplenumberadvance',
                          'simplephonenumber',
                          'simplephonenumberadvanced',
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
                          'simplecheckboxes',
                          'simplecheckboxadvanced',
                          'simplecurrencyadvanced', 
                          'simpletextarea',
                          'simpletextareaadvanced'];

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
    };

    init();
    
    function init() {
        bindUIEvents();
        dataTable = initializeDataTable();
        let availableChefsFields = JSON.parse(availableChefFieldsString)
        initializeIntakeMap(availableChefsFields);
        bindExistingMaps();
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
    
            setTimeout(function(){
                window.location.reload();
            }, 2000);

          }
          catch (err) {
            abp.notify.error(
                '',
                'The JSON is not valid'
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

        setTimeout(function(){
            if(indexOfVersion > 0) {
                location.href = location.href.substring(0, indexOfVersion + searchStr.length) + chefsFormVersionGuid;
            } else {
                location.href = location.href+"&ChefsFormVersionGuid="+chefsFormVersionGuid;
            }
        }, 2000);

    }

    function bindExistingMaps() {
        if (existingMappingString+"" != "undefined" && existingMappingString != null && existingMappingString != "") {
            try {
                let existingMapping = JSON.parse(existingMappingString);
                let keys = Object.keys(existingMapping);
                for (let key of keys) {
                    let intakeProperty = key;
                    let chefsMappingProperty = existingMapping[intakeProperty];
                    let intakeMappingCard = document.getElementById(intakeProperty);
                    let chefsMappingDiv = document.getElementById(chefsMappingProperty);
                    chefsMappingDiv.appendChild(intakeMappingCard);
                }
            } catch (err) {
                console.log('Existing Mapping error');
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
                        return '<div id="'+data+'" class="col map-div" draggable="true"></div>';
                    },
                    targets: 3
                }
            ]
        });
    }
    
    function handleSync() {
        let chefsFormVersionId = document.getElementById('chefsFormVersionId').value;
        if(!validateGuid(chefsFormVersionId)) {
            abp.notify.error(
                '',
                'The Form Version ID is not in a GUID format'
            );
            return;
        }

        if(chefsFormVersionId == "") {
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
                    mappingJson[intakeMappingChild.innerHTML] = chefsKey;
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
                let intakeFieldJson = JSON.parse(intakeField);
                if(!excludedIntakeMappings.includes(intakeFieldJson.Name)) {
                    let dragableDiv = document.createElement('div');
                    dragableDiv.id = intakeFieldJson.Name;
                    dragableDiv.className = 'card';
                    dragableDiv.setAttribute("draggable", "true");
                    dragableDiv.innerHTML = intakeFieldJson.Name;
                    intakeMapColumn.appendChild(dragableDiv);
                }
            }

            let keys = Object.keys(availableChefsFields);
            dataTable.clear();
            for (let key of keys) {
                let jsonObj = JSON.parse(availableChefsFields[key]);

                if(allowableTypes.includes(jsonObj.type.trim())) {
                    dataTable.row.add([stripHtml(jsonObj.label), key, jsonObj.type, key]).draw();
                }
            }		
        }
        catch (err) {
            console.log('Mapping error');
        }
    }

    function stripHtml(html)
    {
       let tmp = document.createElement("DIV");
       tmp.innerHTML = html;
       return tmp.textContent || tmp.innerText || "";
    }

    document.addEventListener('dragstart', function (ev) {
        beingDragged(ev);
    });

    document.addEventListener('dragend', function (ev) {
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
        if(draggedEl.classList+"" != "undefined") {
            draggedEl.classList.add('dragging');
        }
    }

    function dragEnd(ev) {
        let draggedEl = ev.target;
        if(draggedEl.classList+"" != "undefined") {
            draggedEl.classList.remove('dragging');
        }
        enableSave();
    }

    function enableSave() {
        let disableSave = true;

        let mappingDivs = $('.map-div');
        for (let mappingDiv of mappingDivs) {
            if(mappingDiv.childElementCount > 0) {
                disableSave = false;
                break;
            }
        }
        UIElements.btnSave.prop("disabled", disableSave);
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
        if(!jsonText) {
            return jsonText;
        }
        
        var prettyJson = new Array();
        var depth = 0;
        var currChar;
        var prevChar;
        var doubleQuoteIn = false;
        
        for(var i = 0; i < jsonText.length; i++) {
            currChar = jsonText.charAt(i);
            
            if(currChar == '\"') {
                if(prevChar != '\\') {
                    doubleQuoteIn = !doubleQuoteIn;
                }
            }

            switch(currChar) {
            case '{':
                prettyJson.push(currChar);
                if(!doubleQuoteIn) {
                    prettyJson.push('\n');
                    insertTab(prettyJson, ++depth);
                }
                break;
            case '}':
                if(!doubleQuoteIn) {
                    prettyJson.push('\n');
                    insertTab(prettyJson, --depth);
                }
                prettyJson.push(currChar);
                break;
            case ',':
                prettyJson.push(currChar);
                if(!doubleQuoteIn) {
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
        for(var i = 0; i < depth; i++) {
            prettyJson.push(TAB);
        }
    }

});
