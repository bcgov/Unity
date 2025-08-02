$(function () {
    let lastDroppedLocation = {};
    let lastDragFromLocation = {};
    let customTabIds = [];
    let assessmentInfoIds = [];
    let projectInfoIds = [];
    let applicantInfoIds = [];
    let paymentInfoIds = [];

    PubSub.subscribe(
        'refresh_configure_worksheets',
        () => {
            lastDroppedLocation = {};
            customTabIds = [];
            assessmentInfoIds = [];
            projectInfoIds = [];
            applicantInfoIds = [];
            paymentInfoIds = [];
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
            return;
        }

        if (dragOver.classList.contains('multi-target')
            && event.target.classList.contains('assessment-info-list')) {
            dropToAssessmentInfo(event, null, 'published-form');
            return;
        }

        if (dragOver.classList.contains('multi-target')
            && event.target.classList.contains('project-info-list')) {
            dropToProjectInfo(event, null, 'published-form');
            return;
        }

        if (dragOver.classList.contains('multi-target')
            && event.target.classList.contains('applicant-info-list')) {
            dropToApplicantInfo(event, null, 'published-form');
            return;
        }

        if (dragOver.classList.contains('multi-target')
            && event.target.classList.contains('payment-info-list')) {
            dropToPaymentInfo(event, null, 'published-form');
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
            storeAssessmentInfoIdChange();
            storeProjectInfoIdChange();
            storeApplicantInfoIdChange();
            storePaymentInfoIdChange();
        }
    }

    function clearSlotId() {
        switch (lastDragFromLocation?.dataset?.target) {
            case 'fundingAgreementInfo':
                $('#FundingAgreementInfoSlotId').val(null);
                break;
        }
    }

    function updateSlotId(draggedEl) {
        switch (lastDroppedLocation?.dataset?.target) {
            case 'fundingAgreementInfo':
                $('#FundingAgreementInfoSlotId').val(draggedEl.dataset.worksheetId);
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

    function dropToAssessmentInfo(event, addClass, removeClass) {
        event.preventDefault();

        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');

        // handle reordering in the ui

        updateDraggedClasses(beingDragged, addClass, removeClass);
        dragOver.appendChild(beingDragged);
        lastDroppedLocation = dragOver;
        storeAssessmentInfoIdChange();
    }

    function storeCustomTabsIdChange() {
        customTabIds= [];
        let items = Array.from($('.custom-tabs-list').children());
        items.forEach((item) => {
            customTabIds.push(item.dataset.worksheetId);
        });
        $('#CustomTabsSlotIds').val(customTabIds.join(';'));
    }

    function dropToProjectInfo(event, addClass, removeClass) {
        event.preventDefault();

        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');

        // handle reordering in the ui

        updateDraggedClasses(beingDragged, addClass, removeClass);
        dragOver.appendChild(beingDragged);
        lastDroppedLocation = dragOver;
        storeProjectInfoIdChange();
    }

    function storeAssessmentInfoIdChange() {
        assessmentInfoIds = [];
        let items = Array.from($('.assessment-info-list').children());
        items.forEach((item) => {
            assessmentInfoIds.push(item.dataset.worksheetId);
        });
        $('#AssessmentInfoSlotIds').val(assessmentInfoIds.join(';'));
    }

    function dropToApplicantInfo(event, addClass, removeClass) {
        event.preventDefault();

        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');

        // handle reordering in the ui

        updateDraggedClasses(beingDragged, addClass, removeClass);
        dragOver.appendChild(beingDragged);
        lastDroppedLocation = dragOver;
        storeApplicantInfoIdChange();
    }

    function storeProjectInfoIdChange() {
        projectInfoIds = [];
        let items = Array.from($('.project-info-list').children());
        items.forEach((item) => {
            projectInfoIds.push(item.dataset.worksheetId);
        });
        $('#ProjectInfoSlotIds').val(projectInfoIds.join(';'));
    }

    function dropToPaymentInfo(event, addClass, removeClass) {
        event.preventDefault();

        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');

        // handle reordering in the ui

        updateDraggedClasses(beingDragged, addClass, removeClass);
        dragOver.appendChild(beingDragged);
        lastDroppedLocation = dragOver;
        storePaymentInfoIdChange();
    }

    function storeApplicantInfoIdChange() {
        applicantInfoIds = [];
        let items = Array.from($('.applicant-info-list').children());
        items.forEach((item) => {
            applicantInfoIds.push(item.dataset.worksheetId);
        });
        $('#ApplicantInfoSlotIds').val(applicantInfoIds.join(';'));
    }

    function storePaymentInfoIdChange() {
        paymentInfoIds = [];
        let items = Array.from($('.payment-info-list').children());
        items.forEach((item) => {
            paymentInfoIds.push(item.dataset.worksheetId);
        });
        $('#PaymentInfoSlotIds').val(paymentInfoIds.join(';'));
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


