$(function () {
    let selectedReviewDetails = null;
    let hasRenderedHtml = document.getElementById('HasRenderedHTML').value;
    abp.localization.getResource('GrantManager');

    function initializeDetailsPage() {
        initCommentsWidget();
        updateLinksCounters();
        renderSubmission();
    }

    initializeDetailsPage();

    function renderSubmission() {
        if (hasRenderedHtml == "False") {
            getSubmission();
        } else {
            $('.spinner-grow').hide();
            addEventListeners();
        }
    }

    async function getSubmission() {
        try {
            $('.spinner-grow').hide();
            let submissionString = document.getElementById('ApplicationFormSubmissionData').value;
            let submissionData = JSON.parse(submissionString);
            Formio.icons = 'fontawesome';

            Formio.createForm(
                document.getElementById('formio'),
                submissionData.version.schema,
                {
                    readOnly: true,
                    renderMode: 'form',
                    flatten: true,
                }
            ).then(function (form) {
                // Set Example Submission Object
                form.submission = submissionData.submission.submission;
                form.resetValue();
                form.refresh();
                form.on('render', function () {
                    addEventListeners();
                    storeRenderedHtml();
                });
            });
        } catch (error) {
            console.error(error);
        }
    }

    async function storeRenderedHtml() {
        let innerHTML = document.getElementById('formio').innerHTML;
        let submissionId = document.getElementById('ApplicationFormSubmissionId').value;
        $.ajax(
            {
                url: "/api/app/submission",
                data: JSON.stringify({ "SubmissionId": submissionId, "InnerHTML": innerHTML }),
                contentType: "application/json",
                type: "POST",
                success: function (data) {
                    console.log(data);
                },
                error: function () {
                    console.log('error');
                }
            },
        );
    }

    // Wait for the DOM to be fully loaded
    function addEventListeners() {
        // Get all the card headers
        const cardHeaders = document.querySelectorAll('.card-header:not(.card-body .card-header)');
        if (cardHeaders.length) {
            cardHeaders.forEach((header) => {
                header.addEventListener('click', function () {
                    // Toggle the display of the corresponding card body

                    const cardBody = this.nextElementSibling;
                    if (
                        cardBody.style.display === 'none' ||
                        cardBody.style.display === ''
                    ) {
                        cardBody.style.display = 'block';
                        header.classList.add('custom-active');


                    } else {
                        cardBody.style.display = 'none';
                        header.classList.remove('custom-active');
                    }

                    // Hide all other card bodies except the one that is being clicked
                    cardHeaders.forEach((otherHeader) => {
                        if (otherHeader !== header) {
                            const otherCardBody = otherHeader.nextElementSibling;
                            otherCardBody.style.display = 'none';
                            otherHeader.classList.remove('custom-active');
                        }
                    });
                });
            });

            // Collapse all card bodies initially
            const cardBodies = document.querySelectorAll('.card-body:not(.card-body .card-body)');
            cardBodies.forEach((body) => {
                body.style.display = 'none';
            });
        }
        // Add click event listeners to each card header
    }

    $('#assessment_upload_btn').click(function () { $('#assessment_upload').trigger('click'); });

    $('#application_attachment_upload_btn').click(function () { $('#application_attachment_upload').trigger('click'); });

    $('#recommendation_select').change(function () {
        let value = $(this).val();
        updateRecommendation(value, selectedReviewDetails.id);
    });

    function updateRecommendation(value, id) {
        try {
            let data = { "approvalRecommended": value, "assessmentId": id }
            unity.grantManager.assessments.assessment.updateAssessmentRecommendation(data)
                .done(function () {
                    abp.notify.success(
                        'The recommendation has been updated.'
                    );
                    PubSub.publish('refresh_review_list_without_select', id);
                });

        }
        catch (error) {
            console.log(error);
        }
    }

    let assessmentUserDetailsWidgetManager = new abp.WidgetManager({
        wrapper: '#assessmentUserDetailsWidget',
        filterCallback: function () {
            return {
                'displayName': selectedReviewDetails.assessorDisplayName,
                'badge': selectedReviewDetails.assessorBadge,
                'title': 'Title, Role'
            };
        }
    });

    let assessmentScoresWidgetManager = new abp.WidgetManager({
        wrapper: '#assessmentScoresWidgetArea',
        filterCallback: function () {
            return {
                'assessmentId': decodeURIComponent($("#AssessmentId").val()),
                'currentUserId': decodeURIComponent(abp.currentUser.id),
            }
        }
    });

    PubSub.subscribe(
        'refresh_assessment_scores',
        (msg, data) => {
            assessmentScoresWidgetManager.refresh();
            updateSubtotal();
        }
    );

    PubSub.subscribe(
        'select_application_review',
        (msg, data) => {
            if (data) {
                selectedReviewDetails = data;
                setDetailsContext('assessment');
                let selectElement = document.getElementById("recommendation_select");
                selectElement.value = data.approvalRecommended;
                PubSub.publish('AssessmentComment_refresh', { review: selectedReviewDetails });
                assessmentUserDetailsWidgetManager.refresh();
                assessmentScoresWidgetManager.refresh();
                updateSubtotal();
                checkCurrentUser(data);
            }
            else {
                setDetailsContext('application');
            }
        }
    );

    PubSub.subscribe(
        'deselect_application_review',
        (msg, data) => {
            setDetailsContext('application');
        }
    );

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        $($.fn.dataTable.tables(true)).DataTable()
            .columns.adjust();
    });

    $('#printPdf').click(function () {
        let submissionId = document.getElementById('ChefsSubmissionId').value;
        unity.grantManager.intakes.submission
            .getSubmission(submissionId)
            .done(function (result) {

                let data = result;
                let newHiddenInput = $('<input>');

                // Set attributes for the hidden input
                newHiddenInput.attr({
                    'type': 'hidden',
                    'name': 'ApplicationFormSubmissionId',
                    'value': submissionId
                });

                let newDiv = $('<div>');

                // Set the ID for the new div
                newDiv.attr('id', 'new-rendering');

                // Add some content to the new div if needed
                newDiv.html('Content for the new div');

                // Store the outer HTML of the new div in divToStore
                let divToStore = newDiv.prop('outerHTML');
                let inputToStore = newHiddenInput.prop('outerHTML');

                // Open a new tab
                let newTab = window.open('', '_blank');

                // Start writing the HTML content to the new tab
                newTab.document.write('<html><head><title>Print</title>');
                newTab.document.write('<script src="/libs/jquery/jquery.js"></script>');
                newTab.document.write('<script src="/libs/formiojs/formio.form.js"></script>');
                newTab.document.write('<link rel="stylesheet" href="/libs/bootstrap-4/dist/css/bootstrap.min.css">');
                newTab.document.write('<link rel="stylesheet" href="/libs/formiojs/formio.form.css">');
                newTab.document.write('</head><body>');
                newTab.document.write(inputToStore);
                newTab.document.write(divToStore);
                newTab.document.write('</body></html>');

                newTab.onload = function () {
                    let script = newTab.document.createElement('script');
                    script.src = '/Pages/GrantApplications/loadPrint.js';
                    script.onload = function () {
                        newTab.executeOperations(data);

                    };

                    newTab.document.head.appendChild(script);

                };

                newTab.document.close();
            });


    });


    let applicationBreadcrumbWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationBreadcrumbWidget',
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val()
            };
        }
    });

    let applicationStatusWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationStatusWidget',
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val()
            };
        }
    });

    const assessmentResultWidgetDiv = "assessmentResultWidget";

    let assessmentResultWidgetManager = new abp.WidgetManager({
        wrapper: '#' + assessmentResultWidgetDiv,
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val(),
                'applicationFormVersionId': $('#AssessmentResultViewApplicationFormVersionId').val()
            };
        }
    });

    const assessmentResultTargetNode = document.querySelector('#' + assessmentResultWidgetDiv);
    const widgetConfig = { attributes: true, childList: true, subtree: true };
    const widgetCallback = function (mutationsList, observer) {
        for (const mutation of mutationsList) {
            if (mutation.type === 'childList') {
                initCustomFieldCurrencies(); 
                break;
            }
        }
    };
    const assessmentResultObserver = new MutationObserver(widgetCallback);
    assessmentResultObserver.observe(assessmentResultTargetNode, widgetConfig);


    PubSub.subscribe(
        'application_status_changed',
        (msg, data) => {            
            applicationBreadcrumbWidgetManager.refresh();
            applicationStatusWidgetManager.refresh();
            assessmentResultWidgetManager.refresh();            
        }
    );

    function initCustomFieldCurrencies() {
        $('.custom-currency-input').maskMoney({
            thousands: ',',
            decimal: '.'
        }).maskMoney('mask');
    }
    
    PubSub.subscribe('application_assessment_results_saved',
        (msg, data) => {
            assessmentResultWidgetManager.refresh();
        }
    );

    const summaryWidgetDiv = "summaryWidgetArea";

    let summaryWidgetManager = new abp.WidgetManager({
        wrapper: '#' + summaryWidgetDiv,
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val() ?? "00000000-0000-0000-0000-000000000000"
            }
        }
    });
    
    const summaryWidgetTargetNode = document.querySelector('#' + summaryWidgetDiv);
    const summaryWidgetObserver = new MutationObserver(widgetCallback);
    summaryWidgetObserver.observe(summaryWidgetTargetNode, widgetConfig);


    PubSub.subscribe('refresh_detail_panel_summary',
        (msg, data) => {
            summaryWidgetManager.refresh();
        }
    );


    let attachCounters = {
        files: 0,
        chefs: 0
    };

    PubSub.subscribe(
        'update_application_attachment_count',
        (msg, data) => {
            if (data.files || data.files === 0) {
                attachCounters.files = data.files;
            }
            if (data.chefs || data.chefs === 0) {
                attachCounters.chefs = data.chefs;
            }
            $('#application_attachment_count').html(attachCounters.files + attachCounters.chefs);
        }
    );

    let applicationRecordsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationRecordsWidget',
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val(),
            }
        }
    });

    PubSub.subscribe('ApplicationLinks_refresh',
        (msg, data) => {
            applicationRecordsWidgetManager.refresh();
            updateLinksCounters();
        }
    );

    // custom fields
    $('body').on('click', '.custom-tab-save', function (event) {
        let id = $(this).attr('id');
        let uiAnchor = $(this).attr('data-ui-anchor');
        let worksheetId = $(this).attr('data-ui-worksheetId');
        let formDataName = id.replace('save_', '').replace('_btn', '') + '_form';
        let applicationId = decodeURIComponent($("#DetailsViewApplicationId").val());
        let formData = $(`#${formDataName}`).serializeArray();
        let customFormObj = {};
        let formVersionId = $("#ApplicationFormVersionId").val();        

        $.each(formData, function (_, input) {
            customFormObj[input.name] = input.value;
        });

        $(`#${formDataName} input:checkbox`).each(function () {
            customFormObj[this.name] = (this.checked).toString();
        });

        updateCustomForm(applicationId, formVersionId, customFormObj, uiAnchor, id, formDataName, worksheetId);
    });

    PubSub.subscribe(
        'fields_tab',
        (_, data) => {          
            let formDataName = data.worksheet + '_form';
            let formValid = $(`form#${formDataName}`).valid();               
            let saveBtn = $(`#save_${data.worksheet}_btn`);
            if (formValid && !formHasInvalidCurrencyCustomFields(`${formDataName}`)) {                
                saveBtn.prop('disabled', false);
            } else {
                saveBtn.prop('disabled', true);
            }
        }
    );
});

function updateCustomForm(applicationId, formVersionId, customFormObj, uiAnchor, saveId, formDataName, worksheetId) {
    let customFormUpdate = {
        instanceCorrelationId: applicationId,
        instanceCorrelationProvider: 'Application',
        sheetCorrelationId: formVersionId,
        sheetCorrelationProvider: 'FormVersion',
        uiAnchor: uiAnchor,
        customFields: customFormObj,
        formDataName: formDataName,
        worksheetId: worksheetId
    }

    $(`#${saveId}`).prop('disabled', true);
    unity.flex.worksheetInstances.worksheetInstance.update(customFormUpdate)
        .done(function () {
            abp.notify.success(
                'Information has been updated.'
            );
        });
}

// custom fields
function notifyFieldChange(worksheet, uianchor, field) {
    let value = document.getElementById(field.id).value;
    let anchor = uianchor.toLowerCase();
    if (PubSub) {
        if (isKnownAnchor(anchor)) {
            PubSub.publish('fields_' + anchor, value);
        } else {
            PubSub.publish('fields_tab', { worksheet: worksheet, fieldId: field.id });
        }
    }
}

function isKnownAnchor(anchor) {
    if (anchor === 'projectinfo'
        || anchor === 'applicantinfo'
        || anchor === 'assessmentinfo'
        || anchor === 'paymentinfo') {
        return true;
    }
}

const Flex = class {
    static isCustomField(input) {
        return input.name.startsWith('custom_');
    }

    static includeCustomFieldObj(formObject, input) {
        if (!formObject.CustomFields) {
            formObject.CustomFields = {};
        }

        formObject.CustomFields[input.name] = input.value;
    }
}

function uploadApplicationFiles(inputId) {
    let applicationId = decodeURIComponent($("#DetailsViewApplicationId").val());
    let currentUserId = decodeURIComponent($("#CurrentUserId").val());
    let currentUserName = decodeURIComponent($("#CurrentUserName").val());
    let url = "/api/app/attachment/application/" + applicationId + "/upload?userId=" + currentUserId + "&userName=" + currentUserName;
    uploadFiles(inputId, url, 'refresh_application_attachment_list');
}

function uploadAssessmentFiles(inputId) {
    let assessmentId = decodeURIComponent($("#AssessmentId").val());
    let currentUserId = decodeURIComponent($("#CurrentUserId").val());
    let currentUserName = decodeURIComponent($("#CurrentUserName").val());
    let url = "/api/app/attachment/assessment/" + assessmentId + "/upload?userId=" + currentUserId + "&userName=" + currentUserName;
    uploadFiles(inputId, url, 'refresh_assessment_attachment_list');
}

function uploadFiles(inputId, urlStr, channel) {
    let input = document.getElementById(inputId);
    let files = input.files;
    let formData = new FormData();
    const disallowedTypes = JSON.parse(decodeURIComponent($("#Extensions").val()));
    const maxFileSize = decodeURIComponent($("#MaxFileSize").val());

    let isAllowedTypeError = false;
    let isMaxFileSizeError = false;
    if (files.length == 0) {
        return;
    }

    for (let file of files) {
        if (disallowedTypes.includes(file.name.slice(file.name.lastIndexOf(".") + 1, file.name.length).toLowerCase())) {
            isAllowedTypeError = true;
        }
        if ((file.size * 0.000001) > maxFileSize) {
            isMaxFileSizeError = true;
        }

        formData.append("files", file);
    }

    if (isAllowedTypeError) {
        input.value = null;
        return abp.notify.error(
            'Error',
            'File type not supported'
        );
    }
    if (isMaxFileSizeError) {
        input.value = null;
        return abp.notify.error(
            'Error',
            'File size exceeds ' + maxFileSize + 'MB'
        );
    }

    $.ajax(
        {
            url: urlStr,
            data: formData,
            processData: false,
            contentType: false,
            type: "POST",
            success: function (data) {
                abp.notify.success(
                    data.responseText,
                    'File Upload Is Successful'

                );
                PubSub.publish(channel);
                input.value = null;
            },
            error: function (data) {
                abp.notify.error(
                    data.responseText,
                    'File Upload Not Successful'
                );
                PubSub.publish(channel);
                input.value = null;
            }
        }
    );
}

function getCurrentUser() {
    return abp.currentUser.id;
}

const checkCurrentUser = function (data) {
    if (getCurrentUser() == data.assessorId && data.status == "IN_PROGRESS") {
        $('#recommendation_select').prop('disabled', false);
        $('#assessment_upload_btn').prop('disabled', false);
    }
    else {
        $('#recommendation_select').prop('disabled', 'disabled');
        $('#assessment_upload_btn').prop('disabled', 'disabled');
    }
};


function updateCommentsCounters() {
    setTimeout(() => {
        $('.comments-container').map(function () {
            $('#' + $(this).data('counttag')).html($(this).data('count'));
        }).get();
    }, 100);
}

function updateLinksCounters() {
    setTimeout(() => {
        $('.links-container').map(function () {
            $('#' + $(this).data('linkscounttag')).html($(this).data('count'));
        }).get();
    }, 100);
}

function initCommentsWidget() {
    const currentUserId = decodeURIComponent($("#CurrentUserId").val());
    let selectedReviewDetails;
    let applicationCommentsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationCommentsWidget',
        filterCallback: function () {
            return {
                'ownerId': $('#DetailsViewApplicationId').val(),
                'commentType': 0,
                'currentUserId': currentUserId,
            };
        }
    });

    let assessmentCommentsWidgetManager = new abp.WidgetManager({
        wrapper: '#assessmentCommentsWidget',
        filterCallback: function () {
            return {
                'ownerId': selectedReviewDetails.id,
                'commentType': 1,
                'currentUserId': currentUserId,
            };
        }
    });

    PubSub.subscribe(
        'ApplicationComment_refresh',
        () => {
            applicationCommentsWidgetManager.refresh();
            updateCommentsCounters();
        }
    );

    PubSub.subscribe(
        'AssessmentComment_refresh',
        (_, data) => {
            if (data?.review) {
                selectedReviewDetails = data.review;
            }
            assessmentCommentsWidgetManager.refresh();
            updateCommentsCounters();
        }
    );

    updateCommentsCounters();
    let tagsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationTagsWidget',
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val() ?? "00000000-0000-0000-0000-000000000000"
            }
        }
    });

    PubSub.subscribe(
        'ApplicationTags_refresh',
        () => {
            tagsWidgetManager.refresh();
        }
    );

}

function setDetailsContext(context) {
    switch (context) {
        case 'assessment': $('#reviewDetails').show(); $('#applicationDetails').hide(); break;
        case 'application': $('#reviewDetails').hide(); $('#applicationDetails').show(); break;
    }
}

function formHasInvalidCurrencyCustomFields(formId) {
    let invalidFieldsFound = false;
    $("#" + formId + " input[id^='custom']:visible").each(function (i, el) {
        let $field = $(this);
        if ($field.hasClass('custom-currency-input')) {
            if (!isValidCurrencyCustomField($field)) {
                invalidFieldsFound = true;
            }
        }
    });

    return invalidFieldsFound;
}

function isValidCurrencyCustomField(input) {
    let originalValue = input.val();
    let numericValue = parseFloat(originalValue.replace(/,/g, ''));

    let minValue = parseFloat(input.attr('data-min'));
    let maxValue = parseFloat(input.attr('data-max'));

    if (isNaN(numericValue)) {
        showCurrencyError(input, 'Please enter a valid number.');
        return false;
    } else if (numericValue < minValue) {
        showCurrencyError(input, `Please enter a value greater than or equal to ${minValue}.`);
        return false;
    } else if (numericValue > maxValue) {
        showCurrencyError(input, `Please enter a value less than or equal to ${maxValue}.`);
        return false;
    } else {
        clearCurrencyError(input);
        return true;
    }

}
function showCurrencyError(input, message) {
    let errorSpan = input.attr('id') + "-error";
    document.getElementById(errorSpan).textContent = message;
    input.attr('aria-invalid', 'true');
}

function clearCurrencyError(input) {
    let errorSpan = input.attr('id') + "-error";
    document.getElementById(errorSpan).textContent = '';
    input.attr('aria-invalid', 'false');
}

