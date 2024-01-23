$(function () {
    let selectedReviewDetails = null;
    abp.localization.getResource('GrantManager');

    function replaceKey(obj, keyToReplace, matchValue, newValue ) {
        for (let key in obj) {
            if (key === keyToReplace && obj[key] === matchValue) {
                obj[key] = newValue;
            } else if (typeof obj[key] === 'object') {
                replaceKey(obj[key], keyToReplace, matchValue, newValue ); 
            }
        }
    }

    function formatChefComponents(data) {
        // Advanced Components
        replaceKey(data, "type", "orgbook", "select");
        replaceKey(data, "type", "simpleaddressadvanced", "address");
        replaceKey(data, "type", "simplebuttonadvanced", "button");
        replaceKey(data, "type", "simplecheckboxadvanced", "checkbox");
        replaceKey(data, "type", "simplecurrencyadvanced", "currency");
        replaceKey(data, "type", "simpledatetimeadvanced", "datetime");
        replaceKey(data, "type", "simpledayadvanced", "day");
        replaceKey(data, "type", "simpleemailadvanced", "email");
        replaceKey(data, "type", "simplenumberadvanced", "number");
        replaceKey(data, "type", "simplepasswordadvanced", "password");
        replaceKey(data, "type", "simplephonenumberadvanced", "phoneNumber");
        replaceKey(data, "type", "simpleradioadvanced", "radio");
        replaceKey(data, "type", "simpleselectadvanced", "select");
        replaceKey(data, "type", "simpleselectboxesadvanced", "selectboxes");
        replaceKey(data, "type", "simplesignatureadvanced", "signature");
        replaceKey(data, "type", "simplesurveyadvanced", "survey");
        replaceKey(data, "type", "simpletagsadvanced", "tags");
        replaceKey(data, "type", "simpletextareaadvanced", "textarea");
        replaceKey(data, "type", "simpletextfieldadvanced", "textfield");
        replaceKey(data, "type", "simpletimeadvanced", "time");
        replaceKey(data, "type", "simpleurladvanced", "url");
       
      

        // Regular components
        replaceKey(data, "type", "simplebcaddress", "address");
        replaceKey(data, "type", "bcaddress", "address");
        replaceKey(data, "type", "simplebtnreset", "button");
        replaceKey(data, "type", "simplebtnsubmit", "button");
        replaceKey(data, "type", "simplecheckboxes", "selectboxes");
        replaceKey(data, "type", "simplecheckbox", "checkbox");
        replaceKey(data, "type", "simplecols2", "columns");
        replaceKey(data, "type", "simplecols3", "columns");
        replaceKey(data, "type", "simplecols4", "columns");
        replaceKey(data, "type", "simplecontent", "content");
        replaceKey(data, "type", "simpledatetime", "datetime");
        replaceKey(data, "type", "simpleday", "day");
        replaceKey(data, "type", "simpleemail", "email");
        replaceKey(data, "type", "simplefile", "file");
        replaceKey(data, "type", "simpleheading", "header");
        replaceKey(data, "type", "simplefieldset", "fieldset");
        replaceKey(data, "type", "simplenumber", "number");
        replaceKey(data, "type", "simplepanel", "panel");
        replaceKey(data, "type", "simpleparagraph", "textarea");
        replaceKey(data, "type", "simplephonenumber", "phoneNumber");
        replaceKey(data, "type", "simpleradios", "radio");
        replaceKey(data, "type", "simpleselect", "select");
        replaceKey(data, "type", "simpletabs", "tabs");
        replaceKey(data, "type", "simpletextarea", "textarea");
        replaceKey(data, "type", "simpletextfield", "textfield");
        replaceKey(data, "type", "simpletime", "time");


        return data;
    }

    async function getSubmission() {
        try {
            let submissionId = document.getElementById('ApplicationFormSubmissionId').value;
            unity.grantManager.intakes.submission
                .getSubmission(submissionId)
                .done(function (result) {
                    $('.spinner-grow').hide();
                    Formio.icons = 'fontawesome';
                    let data = formatChefComponents(result);
                    Formio.createForm(
                        document.getElementById('formio'),
                        data.version.schema,
                        {
                            readOnly: true,
                            renderMode: 'form',
                            flatten: true,
                        }
                    ).then(function (form) {
                        // Set Example Submission Object
                        form.submission = data.submission.submission;
                        addEventListeners();
                    });
                });
        } catch (error) {
            console.error(error);
        }
    }

    // Wait for the DOM to be fully loaded
    function addEventListeners() {
        // Get all the card headers
        const cardHeaders = document.querySelectorAll('.card-header:not(.card-body .card-header)');

        // Add click event listeners to each card header
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
                    PubSub.publish('refresh_review_list', id);
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

        let submissionId = document.getElementById('ApplicationFormSubmissionId').value;
        unity.grantManager.intakes.submission
            .getSubmission(submissionId)
            .done(function (result) {
                
                let data = formatChefComponents(result);
                const formElement = document.createElement('div');
                Formio.createForm(
                    formElement,
                    data.version.schema,
                    {
                        readOnly: true,
                        renderMode: 'form',
                        flatten: true,
                    }
                ).then(function (form) {
                    form.submission = data.submission.submission;

                }).then(data => {

                    const h4Elements = formElement.querySelectorAll('h4');

                    h4Elements.forEach(element => {
                        element.style.wordSpacing = '10px';
                    });

                    printPDF(formElement);
                });

            });

    });

    function printPDF(html) {
        const { jsPDF } = window.jspdf;
        let doc = new jsPDF('p', 'pt', 'a4');

        doc.setCharSpace(0.01);
        doc.setLineHeightFactor(1.5)

        doc.html(html, {
            x: 15,
            y: 15,
            margin: [50, 20, 70, 30],
            width: 180, // Target width in the PDF document
            windowWidth: 650, //window width in CSS pixels,
            autoPaging: 'text',
            html2canvas: {
                allowTaint: true,
                dpi: 300,
                letterRendering: true,
                logging: false,
                scale: 0.8
            },

            callback: function () {
                doc.save('Application.pdf');
              
            },
        });
    }    
    let applicationStatusWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationStatusWidget',
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val()
            };
        }
    });
    let assessmentResultWidgetManager = new abp.WidgetManager({
        wrapper: '#assessmentResultWidget',
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val()
            };
        }
    });
    PubSub.subscribe(
        'application_status_changed',
        (msg, data) => {
            console.log(msg, data);
            applicationStatusWidgetManager.refresh();
            assessmentResultWidgetManager.refresh();
        }
    );
    PubSub.subscribe('application_assessment_results_saved',
        (msg, data) => {
            assessmentResultWidgetManager.refresh();                  
        }
    );

    let summaryWidgetManager = new abp.WidgetManager({
        wrapper: '#summaryWidgetArea',
        filterCallback: function () {
            return {
                'applicationId': $('#DetailsViewApplicationId').val() ?? "00000000-0000-0000-0000-000000000000"
            }
        }
    });
    PubSub.subscribe('refresh_detail_panel_summary',
        (msg, data) => {
            summaryWidgetManager.refresh();
        }
    );

    function initializeDetailsPage() {
        getSubmission();
        initCommentsWidget();
    }

    initializeDetailsPage();

});

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

const update_application_attachment_count_subscription = PubSub.subscribe(
    'update_application_attachment_count',
    (msg, data) => {
        $('#application_attachment_count').html(data)


    }
);

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

