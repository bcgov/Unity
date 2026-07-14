$(function () {
    let customTabIds = [];
    let assessmentInfoIds = [];
    let projectInfoIds = [];
    let applicantInfoIds = [];
    let paymentInfoIds = [];
    let fundingAgreementInfoIds = [];


    const UIElements = {
        worksheetForm: $('#worksheet-config-form'),
        saveButton: $('#btn-save-worksheet-config'),
        backButton: $('#btn-back-worksheet-config'),
        scoreSheet: $('#scoresheet')
    };


    function init() {
        bindUIEvents();
    }

    function bindUIEvents() {
        UIElements.scoreSheet.on('change', saveScoresheet);
        UIElements.backButton.on('click', handleBack);
        UIElements.worksheetForm.on('submit', handleSave);
    }

    init();

    function handleSave(e) {

        e.preventDefault(); // prevent full page reload

        let $form = UIElements.worksheetForm;
        let url = $form.attr('action') || location.href;
        let data = $form.serialize(); // serialize form data

        abp.ui.setBusy($form); // optional ABP spinner on form

        $.post(url, data)
            .done(function (response) {
                // Show ABP success toast
                abp.notify.success('Worksheet configuration saved successfully!');
                PubSub.publish('refresh_available_worksheets', { chefsFormVersionId: response.chefsFormVersionId });
            })
            .fail(function (xhr) {
                // Show ABP error toast
                abp.notify.error('Error saving worksheet configuration: ' + xhr.responseText);
            })
            .always(function () {
                abp.ui.clearBusy($form);
            });
       
    }

    function saveScoresheet() {
        let appFormId = $('#applicationFormId').val();
        let originalValue = $('#originalScoresheetId').val();
        let scoresheetId = $('#scoresheet').val();
        if (originalValue === scoresheetId) {
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

    function handleSingleTarget(event, dragOver) {
        if (!dragOver.classList.contains('single-target')) return false;
        
        if (event.target.childElementCount > 0) {
            event.preventDefault();
        } else {
            dropToSingleTarget(event, null, 'published-form');
        }
        return true;
    }

    function handleMultiTarget(event, dragOver) {
        if (!dragOver.classList.contains('multi-target')) return false;

        const multiTargetHandlers = {
            'available-worksheets': () => dropToAvailableWorksheets(event, 'published-form', null),
            'custom-tabs-list': () => dropToCustomTabs(event, null, 'published-form'),
            'assessment-info-list': () => dropToAssessmentInfo(event, null, 'published-form'),
            'project-info-list': () => dropToProjectInfo(event, null, 'published-form'),
            'applicant-info-list': () => dropToApplicantInfo(event, null, 'published-form'),
            'payment-info-list': () => dropToPaymentInfo(event, null, 'published-form'),
            'funding-agreement-info-list': () => dropToFundingAgreementInfo(event, null, 'published-form')
        };

        for (const [className, handler] of Object.entries(multiTargetHandlers)) {
            if (event.target.classList.contains(className)) {
                handler();
                return true;
            }
        }
        return false;
    }

    document.addEventListener('dragover', function (event) {
        let beingDragged = document.querySelector('.dragging');
        let dragOver = event.target;

        if (!beingDragged.classList.contains('draggable-card')) return;

        if (handleSingleTarget(event, dragOver)) return;
        handleMultiTarget(event, dragOver);
    });

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

    function dropToSingleTarget(event, addClass, removeClass) {
        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');
        updateDraggedClasses(beingDragged, addClass, removeClass);
        dragOver.appendChild(beingDragged);
    }
});

function updateDraggedClasses(beingDragged, addClass, removeClass) {
    if (addClass) {
        beingDragged.classList.add('published-form');
    }
    if (removeClass) {
        beingDragged.classList.remove('published-form');
    }
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

function beingDragged(ev) {
    let draggedEl = ev.target;
    if (draggedEl.classList + "" != "undefined") {
        draggedEl.classList.add('dragging');
    }
}

function handleBack() {
    location.href = '/ApplicationForms';
}


