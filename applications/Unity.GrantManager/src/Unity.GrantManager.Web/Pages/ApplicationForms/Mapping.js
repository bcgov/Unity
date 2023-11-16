$(function () {

    let availableChefFieldsString = document.getElementById('availableChefsFields').value;
    let existingMappingString = document.getElementById('existingMapping').value;
    let intakeFieldsString = document.getElementById('intakeProperties').value;
    let applicationFormId = document.getElementById('applicationFormId').value;

    let allowableTypes = ['textarea', 
                          'orgbook',
                          'textfield', 
                          'currency', 
                          'datetime', 
                          'checkbox',
                          'simpletextfield', 
                          'simpletextfieldadvanced',
                          'simpletime',
                          'simpletimeadvanced',
                          'simplenumber',
                          'simplenumberadvance',
                          'simplephonenumber',
                          'simplephonenumberadvanced',
                          'simpleday',
                          'simpledayadvanced',
                          'simpleemail',
                          'simpleemailadvanced',
                          'simpledatetime',
                          'simpledatetimeadvanced',
                          'simpleurladvanced',
                          'simplecheckbox',
                          'simplecheckboxes',
                          'simplecheckboxadvanced',
                          'simplecurrencyadvanced', 
                          'simpletextarea',
                          'simpletextareaadvanced'];

    let excludedIntakeMappings = ['ConfirmationId', 'SubmissionId'];
    let dataTable;

    const UIElements = {
        btnBack: $('#btn-back'),
        btnSave: $('#btn-save'),
    };

    init();
    
    function init() {
        bindUIEvents();
        dataTable = initializeDataTable();
        initializeIntakeMap();
        bindExistingMaps();
    }

    function bindUIEvents() {
        UIElements.btnBack.on('click', handleBack);
        UIElements.btnSave.on('click', handleSave);
    }

    function bindExistingMaps() {
        if (existingMappingString+"" != "undefined" && existingMappingString != null) {
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

        let formData = JSON.parse(document.getElementById('applicationFormDtoString').value);
        formData["submissionHeaderMapping"] = JSON.stringify(mappingJson);

        $.ajax(
            {
                url: "/api/app/application-form/" + applicationFormId,
                data: JSON.stringify(formData),
                contentType: "application/json",
                type: "PUT",
                success: function (data) {
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

    function handleBack() {
        location.href = '/ApplicationForms';
    }

    function initializeIntakeMap() {
        try {
            let availableChefsFields = JSON.parse(availableChefFieldsString);
            let intakeFields = JSON.parse(intakeFieldsString);
            const intakeMapColumn = document.querySelector('#intake-map-available-fields-column');
            
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
            for (let key of keys) {
                let jsonObj = JSON.parse(availableChefsFields[key]);
                if(allowableTypes.includes(jsonObj.type.trim())) {
                    dataTable.row.add([jsonObj.label, key, jsonObj.type, key]).draw();
                }
            }		
        }
        catch (err) {
            console.log('Mapping error');
        }
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

});
