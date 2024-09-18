$(function () {
    let lastDroppedLocation = {};
    let lastDragFromLocation = {};
    let customTabIds = [];

    PubSub.subscribe(
        'refresh_configure_worksheets',
        () => {
            lastDroppedLocation = {};
            customTabIds = [];
        }
    );

    document.addEventListener('dragstart', function (ev) {
        if (ev.target.classList.contains('drag-target')) {
            ev.preventDefault();
            return;
        }
        beingDragged(ev);
    });

    document.addEventListener('dragend', function (ev) {
        if (ev.target.classList.contains('drag-target')) {
            ev.preventDefault();
            return;
        }
        dragEnd(ev);
    });

    document.addEventListener('dragover', function (event) {
        let beingDragged = document.querySelector('.dragging');
        let dragOver = event.target;

        if (!beingDragged.classList.contains('draggable-card')) return;

        if (dragOver.classList.contains('single-target')
            && event.target.childElementCount > 0) {
            event.preventDefault();
            return;
        }

        if (dragOver.classList.contains('single-target')
            && event.target.childElementCount == 0) {
            dropToSingleTarget(event, null, 'published-form');
            return;
        }

        if (dragOver.classList.contains('multi-target')
            && event.target.classList.contains('available-worksheets')) {
            dropToAvailableWorksheets(event, 'published-form', null);
            return;
        }

        if (dragOver.classList.contains('multi-target')
            && event.target.classList.contains('custom-tabs-list')) {
            dropToCustomTabs(event, null, 'published-form');
        }
    });

    function beingDragged(ev) {
        let draggedEl = ev.target;
        if (draggedEl.classList + "" != "undefined") {
            draggedEl.classList.add('dragging');
            lastDragFromLocation = draggedEl.parentNode;
        }
    }

    function dragEnd(ev) {
        let draggedEl = ev.target;
        if (draggedEl.classList + "" != "undefined") {
            draggedEl.classList.remove('dragging');

            clearSlotId();
            updateSlotId(draggedEl);
            storeCustomTabsIdChange();
        }
    }

    function clearSlotId() {
        switch (lastDragFromLocation.dataset.target) {
            case 'assessmentInfo':
                $('#AssessmentInfoSlotId').val(null);
                break;
            case 'projectInfo':
                $('#ProjectInfoSlotId').val(null);
                break;
            case 'applicantInfo':
                $('#ApplicantInfoSlotId').val(null);
                break;
            case 'paymentInfo':
                $('#PaymentInfoSlotId').val(null);
                break;
        }
    }

    function updateSlotId(draggedEl) {
        switch (lastDroppedLocation.dataset.target) {
            case 'assessmentInfo':
                $('#AssessmentInfoSlotId').val(draggedEl.dataset.worksheetId);
                break;
            case 'projectInfo':
                $('#ProjectInfoSlotId').val(draggedEl.dataset.worksheetId);
                break;
            case 'applicantInfo':
                $('#ApplicantInfoSlotId').val(draggedEl.dataset.worksheetId);
                break;
            case 'paymentInfo':
                $('#PaymentInfoSlotId').val(draggedEl.dataset.worksheetId);
                break;
        }
    }


    function dropToCustomTabs(event, addClass, removeClass) {
        event.preventDefault();

        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');

        // handle reordering in the ui

        updateDraggedClasses(beingDragged, addClass, removeClass);
        dragOver.appendChild(beingDragged);
        lastDroppedLocation = dragOver;
        storeCustomTabsIdChange();
    }

    function storeCustomTabsIdChange() {
        customTabIds= [];
        let items = Array.from($('.custom-tabs-list').children());
        items.forEach((item) => {
            customTabIds.push(item.dataset.worksheetId);
        });
        $('#CustomTabsSlotIds').val(customTabIds.join(';'));
    }

    function dropToAvailableWorksheets(event, addClass, removeClass) {
        dropToSingleTarget(event, addClass, removeClass);
        sortAvailableWorksheets();
    }

    function sortAvailableWorksheets() {
        sortUsingNestedText($('.available-worksheets'), "span.published-form-title");
    }

    function sortUsingNestedText(parent, keySelector) {
        let items = parent.children().sort(function (a, b) {
            let vA = $(keySelector, a).text();
            let vB = $(keySelector, b).text();
            return compareSort(vA, vB);
        });
        parent.append(items);
    }

    function compareSort(vA, vB) {
        if (vA < vB) {
            return -1;
        } else if (vA > vB) {
            return 1;
        } else {
            return 0;
        }
    }

    function dropToSingleTarget(event, addClass, removeClass) {
        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');
        updateDraggedClasses(beingDragged, addClass, removeClass);
        dragOver.appendChild(beingDragged);
        lastDroppedLocation = dragOver;
    }

    function updateDraggedClasses(beingDragged, addClass, removeClass) {
        if (addClass) {
            beingDragged.classList.add('published-form');
        }
        if (removeClass) {
            beingDragged.classList.remove('published-form');
        }
    }
});


