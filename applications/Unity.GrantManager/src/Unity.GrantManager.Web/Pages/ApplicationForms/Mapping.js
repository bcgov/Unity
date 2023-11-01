$(function () {

	let dataTable;
    let availableChefFieldsString = document.getElementById('availableChefsFields').value;
	let existingMappingString = document.getElementById('existingMapping').value;
	let intakeFieldsString = document.getElementById('intakeProperties').value;
	let applicationFormId = document.getElementById('applicationFormId').value;
	let allowableTypes = ['textarea', 'orgbook', 'simpletextfield', 'textfield', 'currency', 'datetime', 'checkbox'];	
	let excludedIntakeMappings = ['ConfirmationId', 'SubmissionId'];

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

				for (i = 0; i < keys.length; ++i) {
					let intakeProperty = keys[i];
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
		return dt = new DataTable('#ApplicationFormsTable', {
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

        for (let i = 0; i < mappingDivs.length; ++i) {
            let chefMappingDiv = mappingDivs[i];
            if (chefMappingDiv.childElementCount > 0) {
                
                let chefsKey = mappingDivs[i].id;
                let intakeMappingChildren = chefMappingDiv.children;
				
                for (let j = 0; j < intakeMappingChildren.length; j++) {
                    let intakeMappingChild = intakeMappingChildren[j];
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
			
			for (i = 0; i < intakeFields.length; ++i) {
				let intakeField = JSON.parse(intakeFields[i]);
				if(!excludedIntakeMappings.includes(intakeField.Name)) {
					let dragableDiv = document.createElement('div');
					dragableDiv.id = intakeField.Name;
					dragableDiv.className = 'card';
					dragableDiv.setAttribute("draggable", "true");
					dragableDiv.innerHTML = intakeField.Name;
					intakeMapColumn.appendChild(dragableDiv);
				}
			}

			let keys = Object.keys(availableChefsFields);
			for (i = 0; i < keys.length; ++i) {

				let jsonObj = JSON.parse(availableChefsFields[keys[i]]);
				if(allowableTypes.includes(jsonObj.type.trim())) {
					dt.row.add([jsonObj.label, keys[i], jsonObj.type, keys[i]]).draw();
				}
			}		
		}
		catch (err) {
			console.log('Mapping error');
		}
	}

	document.addEventListener('dragstart', function () {
        beingDragged(event);
    });

    document.addEventListener('dragend', function () {
        dragEnd(event);
    });

    document.addEventListener('dragover', function () {
        var beingDragged = document.querySelector('.dragging');
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
        var draggedEl = ev.target;
		if(draggedEl.classList+"" != "undefined") {
			draggedEl.classList.add('dragging');
		}
    }

    function dragEnd(ev) {
        var draggedEl = ev.target;
		if(draggedEl.classList+"" != "undefined") {
        	draggedEl.classList.remove('dragging');
		}
		enableSave();
    }

	function enableSave() {
		let disableSave = true;

		let mappingDivs = $('.map-div');
		for (i = 0; i < mappingDivs.length; ++i) {
			if(mappingDivs[i].childElementCount > 0) {
				disableSave = false;
				UIElements.btnSave.prop("disabled", false);
				return;
			}
		}
		UIElements.btnSave.prop("disabled", disableSave);
	}

    function allowDrop(ev) {
        ev.preventDefault();

        var dragOver = ev.target;
        var dragOverParent = dragOver.parentElement;
        var beingDragged = document.querySelector('.dragging');
        var draggedParent = beingDragged.parentElement;

        var draggedIndex = whichChild(beingDragged);
        var dragOverIndex = whichChild(dragOver);

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
        var dragOver = event.target;
        var beingDragged = document.querySelector('.dragging');
        var draggedParent = beingDragged.parentElement;
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
        var i = 0;
        while ((el = el.previousSibling) != null) ++i;
        return i;
    }

});
