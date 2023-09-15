$(function () {       
    let selectedApplicationIds = decodeURIComponent($("#DetailsViewApplicationId").val());
    let selectedReviewDetails = null;

    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });

    let assignApplicationModal = new abp.ModalManager({
        viewUrl: '/AssigneeSelection/AssigneeSelectionModal'
    });  


    const l = abp.localization.getResource('GrantManager');


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
            let isLoading = true;
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

 
    let result = getSubmission();
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

    function updateRecommendation(value,id) {     
        try {
            let data = { "approvalRecommended": value, "assessmentId": id }
            unity.grantManager.assessments.assessment.updateAssessmentRecommendation
                (data)
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

    PubSub.subscribe(
        'select_application_review',
        (msg, data) => {
            if (data) {                
                selectedReviewDetails = data;
                $('#reviewDetails').show();
                let selectElement = document.getElementById("recommendation_select");
                selectElement.value = data.approvalRecommended;
                PubSub.publish('AssessmentComment_refresh', { review: selectedReviewDetails });
            }
            else {
                $('#reviewDetails').hide();
            }         
        }
    );

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        $($.fn.dataTable.tables(true)).DataTable()
            .columns.adjust();
    });

    initCommentsWidget();
});

function uploadFiles(inputId) {
    var input = document.getElementById(inputId);
    var applicationId = decodeURIComponent($("#DetailsViewApplicationId").val());    
    var currentUserId = decodeURIComponent($("#CurrentUserId").val()); 
    var files = input.files;
    var formData = new FormData();
    const allowedTypes = JSON.parse(decodeURIComponent($("#Extensions").val())); 
    const maxFileSize = decodeURIComponent($("#MaxFileSize").val()); 
    let isAllowedTypeError = false;
    let isMaxFileSizeError = false;
    if (files.length == 0) {
        return;
    }
    for (var i = 0; i != files.length; i++) {   
        console.log(files[i]);
        if (!allowedTypes.includes(files[i].type)) {
            isAllowedTypeError = true;
        }
        if ((files[i].size * 0.000001) > maxFileSize) {
            isMaxFileSizeError = true;
        }

        formData.append("files", files[i]);
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
            url: "/uploader?ApplicationId=" + applicationId + "&CurrentUserId=" + currentUserId,
            data: formData,
            processData: false,
            contentType: false,
            type: "POST",
            success: function (data) {
                abp.notify.success(
                    data.responseText,
                    'File Upload Is Successful'
                ); 

                PubSub.publish('refresh_application_attachment_list');  
            },
            error: function (data) {                
                abp.notify.error(
                    data.responseText,
                    'File Upload Not Successful'
                );
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

function updateCommentsCounters() {
    setTimeout(() => {
        $('.comments-container').map(function () {
            $('#' + $(this).data('counttag')).html($(this).data('count'));
        }).get();
    }, 100);
}

function initCommentsWidget() {
    let selectedReviewDetails;
    let applicationCommentsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationCommentsWidget',
        filterCallback: function () {
            return {
                'ownerId': $('#DetailsViewApplicationId').val(),
                'commentType': 0
            };
        }
    });

    let assessmentCommentsWidgetManager = new abp.WidgetManager({
        wrapper: '#assessmentCommentsWidget',
        filterCallback: function () {            
            return {
                'ownerId': selectedReviewDetails.id,
                'commentType': 1
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