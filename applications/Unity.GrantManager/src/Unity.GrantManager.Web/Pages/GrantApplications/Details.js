$(function () {
    let selectedReviewDetails = null;
    abp.localization.getResource('GrantManager');

    function formatChefComponents(data) {
        // Advanced Components
        let components = JSON.stringify(data).replace(
            /simpleaddressadvanced/g,
            'address'
        );
        components = components.replace(/simplebuttonadvanced/g, 'button');
        components = components.replace(/simplecheckboxadvanced/g, 'checkbox');
        components = components.replace(/simplecurrencyadvanced/g, 'currency');
        components = components.replace(/simpledatetimeadvanced/g, 'datetime');
        components = components.replace(/simpledayadvanced/g, 'day');
        components = components.replace(/simpleemailadvanced/g, 'email');
        components = components.replace(/simplenumberadvanced/g, 'number');
        components = components.replace(/simplepasswordadvanced/g, 'password');
        components = components.replace(
            /simplephonenumberadvanced/g,
            'phoneNumber'
        );
        components = components.replace(/simpleradioadvanced/g, 'radio');
        components = components.replace(/simpleselectadvanced/g, 'select');
        components = components.replace(
            /simpleselectboxesadvanced/g,
            'selectboxes'
        );
        components = components.replace(
            /simplesignatureadvanced/g,
            'signature'
        );
        components = components.replace(/simplesurveyadvanced/g, 'survey');
        components = components.replace(/simpletagsadvanced/g, 'tags');
        components = components.replace(/simpletextareaadvanced/g, 'textarea');
        components = components.replace(
            /simpletextfieldadvanced/g,
            'textfield'
        );
        components = components.replace(/simpletimeadvanced/g, 'time');
        components = components.replace(/simpleurladvanced/g, 'url');

        // Regular components

        components = components.replace(/bcaddress/g, 'address');
        components = components.replace(/simplebtnreset/g, 'button');
        components = components.replace(/simplebtnsubmit/g, 'button');
        components = components.replace(/simplecheckboxes/g, 'checkbox');
        components = components.replace(/simplecheckbox/g, 'checkbox');
        components = components.replace(/simplecols2/g, 'columns');
        components = components.replace(/simplecols3/g, 'columns');
        components = components.replace(/simplecols4/g, 'columns');
        components = components.replace(/simplecontent/g, 'content');
        components = components.replace(/simpledatetime/g, 'datetime');
        components = components.replace(/simpleday/g, 'day');
        components = components.replace(/simpleemail/g, 'email');
        components = components.replace(/simplefile/g, 'file');
        components = components.replace(/simpleheading/g, 'header');
        components = components.replace(/simplefieldset/g, 'fieldset');
        components = components.replace(/simplenumber/g, 'number');
        components = components.replace(/simplepanel/g, 'panel');
        components = components.replace(/simpleparagraph/g, 'textarea');
        components = components.replace(/simplephonenumber/g, 'phoneNumber');
        components = components.replace(/simpleradios/g, 'radio');
        components = components.replace(/simpleselect/g, 'select');
        components = components.replace(/simpletabs/g, 'tabs');
        components = components.replace(/simpletextarea/g, 'textarea');
        components = components.replace(/simpletextfield/g, 'textfield');
        components = components.replace(/simpletime/g, 'time');

        return components;
    }
    async function getSubmission() {
        try {
            let submissionId = document.getElementById('ApplicationFormSubmissionId').value;
            unity.grantManager.intake.submission
                .getSubmission(submissionId)
                .done(function (result) {
                    console.log(result);
                    $('.spinner-grow').hide();
                    Formio.icons = 'fontawesome';
                    let data = JSON.parse(formatChefComponents(result));
                    console.log(data);
                    Formio.createForm(
                        document.getElementById('formio'),
                        data.version.schema,
                        {
                            readOnly: true,
                            renderMode: 'html',
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

    getSubmission();
    // Wait for the DOM to be fully loaded
    function addEventListeners() {
        // Get all the card headers
        const cardHeaders = document.querySelectorAll('.card-header');

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

                    header.scrollIntoView(true);
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
        const cardBodies = document.querySelectorAll('.card-body');
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
                'title': 'Title, Role'
            };
        }
    });

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

    initCommentsWidget();

    $('#printPdf').click(function () {

        let submissionId = document.getElementById('ApplicationFormSubmissionId').value;
        unity.grantManager.intake.submission
            .getSubmission(submissionId)
            .done(function (result) {
                
                let data = JSON.parse(formatChefComponents(result));
                const formElement = document.createElement('div');
                Formio.createForm(
                    formElement,
                    data.version.schema,
                    {
                        readOnly: true,
                        renderMode: 'html',
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
        console.log(file);
        if (disallowedTypes.includes(file.type)) {
            isAllowedTypeError = true;
        }
        if ((file.size * 0.000001) > maxFileSize) {
            isMaxFileSizeError = true;
        }

        formData.append("files", file);
    }

    if (isAllowedTypeError) {
        return abp.notify.error(
            'Error',
            'File type not supported'
        );
    }
    if (isMaxFileSizeError) {
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
            },
            error: function (data) {
                abp.notify.error(
                    data.responseText,
                    'File Upload Not Successful'
                );
                PubSub.publish(channel);
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
}

function setDetailsContext(context) {
    switch (context) {
        case 'assessment': $('#reviewDetails').show(); $('#applicationDetails').hide(); break;
        case 'application': $('#reviewDetails').hide(); $('#applicationDetails').show(); break;
    }
}

