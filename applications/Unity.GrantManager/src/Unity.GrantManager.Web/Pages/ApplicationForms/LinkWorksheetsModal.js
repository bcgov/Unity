$(function () {
    let customTabIds = [];
    let assessmentInfoIds = [];
    let projectInfoIds = [];
    let applicantInfoIds = [];
    let paymentInfoIds = [];
    let fundingAgreementInfoIds = [];

    PubSub.subscribe(
        'refresh_configure_worksheets',
        () => {
            customTabIds = [];
            assessmentInfoIds = [];
            projectInfoIds = [];
            applicantInfoIds = [];
            paymentInfoIds = [];
            fundingAgreementInfoIds = [];
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

        if (dragOver.classList.contains('single-target')) {
            if (event.target.childElementCount > 0) {
                event.preventDefault();
            } else {
                dropToSingleTarget(event, null, 'published-form');
            }
            return;
        }

        if (!dragOver.classList.contains('multi-target')) return;

        const multiTargetHandlers = [
            { className: 'available-worksheets', handler: dropToAvailableWorksheets, addClass: 'published-form', removeClass: null },
            { className: 'custom-tabs-list', handler: dropToCustomTabs, addClass: null, removeClass: 'published-form' },
            { className: 'assessment-info-list', handler: dropToAssessmentInfo, addClass: null, removeClass: 'published-form' },
            { className: 'project-info-list', handler: dropToProjectInfo, addClass: null, removeClass: 'published-form' },
            { className: 'applicant-info-list', handler: dropToApplicantInfo, addClass: null, removeClass: 'published-form' },
            { className: 'payment-info-list', handler: dropToPaymentInfo, addClass: null, removeClass: 'published-form' },
            { className: 'funding-agreement-info-list', handler: dropToFundingAgreementInfo, addClass: null, removeClass: 'published-form' }
        ];

        const match = multiTargetHandlers.find(m => event.target.classList.contains(m.className));
        if (match) {
            match.handler(event, match.addClass, match.removeClass);
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

            storeCustomTabsIdChange();
            storeAssessmentInfoIdChange();
            storeProjectInfoIdChange();
            storeApplicantInfoIdChange();
            storePaymentInfoIdChange();
            storeFundingAgreementInfoIdChange();
        }
    }

    function getMultiTargetIds(cssSelector) {
        let ids = [];
        let items = Array.from($(cssSelector).children());
        items.forEach((item) => {
            ids.push(item.dataset.worksheetId);
        });
        return ids;
    }

    function dropToMultiTarget(event, addClass, removeClass, storeFunction) {
        event.preventDefault();
        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');
        updateDraggedClasses(beingDragged, addClass, removeClass);
        dragOver.appendChild(beingDragged);
        storeFunction();
    }

    function dropToCustomTabs(event, addClass, removeClass) {
        dropToMultiTarget(event, addClass, removeClass, storeCustomTabsIdChange);
    }

    function dropToAssessmentInfo(event, addClass, removeClass) {
        dropToMultiTarget(event, addClass, removeClass, storeAssessmentInfoIdChange);
    }

    function storeCustomTabsIdChange() {
        customTabIds = getMultiTargetIds('.custom-tabs-list');
        $('#CustomTabsSlotIds').val(customTabIds.join(';'));
    }

    function dropToProjectInfo(event, addClass, removeClass) {
        dropToMultiTarget(event, addClass, removeClass, storeProjectInfoIdChange);
    }

    function storeAssessmentInfoIdChange() {
        assessmentInfoIds = getMultiTargetIds('.assessment-info-list');
        $('#AssessmentInfoSlotIds').val(assessmentInfoIds.join(';'));
    }

    function dropToApplicantInfo(event, addClass, removeClass) {
        dropToMultiTarget(event, addClass, removeClass, storeApplicantInfoIdChange);
    }

    function storeProjectInfoIdChange() {
        projectInfoIds = getMultiTargetIds('.project-info-list');
        $('#ProjectInfoSlotIds').val(projectInfoIds.join(';'));
    }

    function dropToPaymentInfo(event, addClass, removeClass) {
        dropToMultiTarget(event, addClass, removeClass, storePaymentInfoIdChange);
    }

    function storeApplicantInfoIdChange() {
        applicantInfoIds = getMultiTargetIds('.applicant-info-list');
        $('#ApplicantInfoSlotIds').val(applicantInfoIds.join(';'));
    }

    function dropToFundingAgreementInfo(event, addClass, removeClass) {
        dropToMultiTarget(event, addClass, removeClass, storeFundingAgreementInfoIdChange);
    }

    function storePaymentInfoIdChange() {
        paymentInfoIds = getMultiTargetIds('.payment-info-list');
        $('#PaymentInfoSlotIds').val(paymentInfoIds.join(';'));
    }

    function storeFundingAgreementInfoIdChange() {
        fundingAgreementInfoIds = getMultiTargetIds('.funding-agreement-info-list');
        $('#FundingAgreementInfoSlotIds').val(fundingAgreementInfoIds.join(';'));
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


