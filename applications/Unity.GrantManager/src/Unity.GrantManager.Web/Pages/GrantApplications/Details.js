$(function () {
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });

    const l = abp.localization.getResource('GrantManager');

    function formatChefComponents(data) {
        // Advanced Components
        var components = JSON.stringify(data).replace(
            /simpleaddressadvanced/g,
            'address'
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
            let submissionId = 'c85f81ce-07ff-4a31-ad0d-0f3a15796528';
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
});
