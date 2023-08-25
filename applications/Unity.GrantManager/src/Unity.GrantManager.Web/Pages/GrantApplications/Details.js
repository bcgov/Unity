$(function () {
    
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });

    var assignApplicationModal = new abp.ModalManager({
        viewUrl: '/AssigneeSelection/AssigneeSelectionModal'
    });    

    const l = abp.localization.getResource('GrantManager');
    setupComments();

    function formatChefComponents(data) {
        // Advanced Components
        var components = JSON.stringify(data).replace(
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
            let submissionId = '8f7b1da6-e131-4059-9ec8-e24fd6d44b5b';
            let isLoading = true;
            unity.grantManager.intake.submission
                .getSubmission(submissionId)
                .done(function (result) {
                    console.log(result);
                    $('.spinner-grow').hide();
                    Formio.icons = 'fontawesome';
                    var data = JSON.parse(formatChefComponents(result));
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

    function setupComments() {
        let commentTextArea = document.getElementById('addCommentTextArea');
        let placeHolderString = commentTextArea.placeholder;
        let widgets = document.getElementsByName('widget-div');
        let editCommentsIcons = document.getElementsByName('edit-comment');
        let saveCommentBtn = document.getElementById('saveCommentBtn');
        let submissionId = document.getElementById('ApplicationFormSubmissionId');

        for (var i = 0; i < widgets.length; i++) {
            let editIcon = widgets[i].children.overlay.children[0];
            let deleteIcon = widgets[i].children.overlay.children[1];
            let cancelBtn = widgets[i].children[2].children[0];
            let updateBtn = widgets[i].children[2].children[1];

            widgets[i].addEventListener('mouseover', (e) => {
                let textArea =
                    e.currentTarget.firstElementChild.nextElementSibling;
                if (textArea.readOnly == true) {
                    e.currentTarget.children[2].style.display = 'none';
                    e.currentTarget.children.overlay.style.display = 'block';
                    textArea.style.cursor = 'default';
                } else {
                    e.currentTarget.children[2].style.display = 'flex';
                    e.currentTarget.children.overlay.style.display = 'none';
                    textArea.style.cursor = 'text';
                }
            });

            widgets[i].addEventListener('mouseout', (e) => {
                e.currentTarget.children.overlay.style.display = 'none';
            });

            cancelBtn.addEventListener('click', (e) => {
                let textArea =
                    e.currentTarget.parentElement.parentElement.querySelector(
                        'textarea'
                    );
                textArea.readOnly = true;
                $(textArea).removeClass('selected');
                e.currentTarget.parentElement.style.display = 'none';
                textArea.value = $(textArea).attr('value');
                showEditIcons();
            });

            updateBtn.addEventListener('click', (e) => {
                let parent = e.currentTarget.parentElement;
                let textArea =
                parent.parentElement.querySelector(
                        'textarea'
                    );
                let commentId = textArea.id;
                let commentValue = textArea.value;
                try {
                    unity.grantManager.grantApplications.assessmentComment
                        .updateAssessmentComment(commentId, commentValue, {})
                        .done(function () {
                            abp.notify.success(
                                'The comment has been updated.'
                            );

                            textArea.readOnly = true;
                            parent.style.display = 'none';
                            $(textArea).removeClass('selected');
                            textArea.setAttribute('value', textArea.value);
                            showEditIcons();
                        });
                    
                } catch (error) {}
            });

            editIcon.addEventListener('click', (e) => {
                e.currentTarget.parentElement.style.display = 'none';
                let textArea =
                    e.currentTarget.parentElement.parentElement.querySelector(
                        'textarea'
                    );
                textArea.readOnly = false;
                $(textArea).addClass('selected');
                if ($(textArea).attr('value') !== textArea.value) {
                    updateBtn.disabled = false;
                } else {
                    updateBtn.disabled = true;
                }
                textArea.addEventListener('keyup', (e) => {
                    if ($(textArea).attr('value') !== textArea.value) {
                        updateBtn.disabled = false;
                    } else {
                        updateBtn.disabled = true;
                    }
                });

                hideEditIcons();
            });
        }

        function cloneTextAreaWidget(assessmentComment) {
            let comment =  assessmentComment.comment;

            let widgetHtml = document.getElementById("widget-example").innerHTML;
            let commentsDiv = document.getElementById("comments-div");
            commentsDiv.innerHTML = widgetHtml + commentsDiv.innerHTML;

            let textArea = commentsDiv.firstElementChild.querySelector('textarea');
            let commentIdInput = commentsDiv.firstElementChild.querySelector('#CommentId');
            commentIdInput.value = assessmentComment.id;
            textArea.id = assessmentComment.id;
            textArea.value = comment;
            setupComments();
        }

        saveCommentBtn.addEventListener('click', (e) => {
            let commentValue = commentTextArea.value;
            try {
                unity.grantManager.grantApplications.assessmentComment
                    .createAssessmentComment(commentValue, submissionId.value, {})
                    .then((response) => {
                        return response;
                    })                    
                    .done(function (result) {
                        abp.notify.success(
                            'The comment has been created.'
                        );
                        commentTextArea.value = "";
                        cloneTextAreaWidget(result);
                    });
                
            } catch (error) {}

            
        });

        function hideEditIcons() {
            for (let i = 0; i < $(editCommentsIcons).length; i++) {
                $(editCommentsIcons)[i].style.display = 'none';
            }
        }

        function showEditIcons() {
            for (let i = 0; i < $(editCommentsIcons).length; i++) {
                $(editCommentsIcons)[i].style.display = 'inline-block';
            }
        }

        commentTextArea.addEventListener('focus', () => {
            commentTextArea.placeholder = '';
            $(commentTextArea).addClass('selected');
        });

        commentTextArea.addEventListener('blur', () => {
            commentTextArea.placeholder = placeHolderString;
            $(commentTextArea).removeClass('selected');
        });

        $('[data-toggle="tooltip"]').tooltip();
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
    $('#addReviewBtn').click(function () {
        $('#adjudicationMainView').fadeOut(1000);
        setTimeout(()=>{
            $('#adjudicationAddReviewView').fadeIn(1000);
        },800)
       
    });
    $('#backBtn').click(function () {
        $('#adjudicationAddReviewView').fadeOut(1000);
        setTimeout(()=>{
            $('#adjudicationMainView').fadeIn(1000);
        },800)
       
    });
});
