$(function () {
    let lastDroppedLocation = {};
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

        var $form = UIElements.worksheetForm;
        var url = $form.attr('action') || window.location.href;
        var data = $form.serialize(); // serialize form data

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

    function handleBack() {
        location.href = '/ApplicationForms';
    }

    PubSub.subscribe(
        'refresh_configure_worksheets',
        () => {
            lastDroppedLocation = {};
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

        if (dragOver.classList.contains('single-target')
            && event.target.childElementCount > 0) {
            event.preventDefault();
            return;
        }

        if (dragOver.classList.contains('single-target')
            && event.target.childElementCount === 0) {
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
            return;
        }

        if (dragOver.classList.contains('multi-target')
            && event.target.classList.contains('funding-agreement-info-list')) {
            dropToFundingAgreementInfo(event, null, 'published-form');
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
        lastDroppedLocation = dragOver;
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


