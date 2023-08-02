$(function () {
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    const l = abp.localization.getResource('GrantManager');

    function formatChefComponents(data) {
        var components = JSON.stringify(data).replace(/simpleradioadvanced/g, 'radio');
        components = components.replace(/simpletextareaadvanced/g, 'textarea');
        components = components.replace(/simplenumberadvanced/g, 'number');
        components = components.replace(/simpleradios/g, 'radio');
        components = components.replace(/simplefile/g, 'file');
        components = components.replace(/simplecontent/g, 'content');
        components = components.replace(/simpletextfield/g, 'textfield');
        components = components.replace(/textfieldadvanced/g, 'textfield');
        components = components.replace(/simplenumber/g, 'number');
        components = components.replace(/simplecurrencyadvanced/g, 'number');
        components = components.replace(/simpledatetimeadvanced/g, 'textfield');
        components = components.replace(/simpleday/g, 'textfield');
        components = components.replace(/simpletextarea/g, 'textfield');
        components = components.replace(/simpleemail/g, 'textfield');
        components = components.replace(/simplephonenumber/g, 'number');
        components = components.replace(/simpleselectboxesadvanced/g, 'selectboxes');
        components = components.replace(/simplecheckboxes/g, 'checkbox');
        components = components.replace(/simplecheckbox/g, 'checkbox');
        components = components.replace(/simpleform/g, 'form');
        // components = components.replace(/tabs/g, 'panel');

        return components;
    }
    async function getSubmission() {
        try {
            axios
                .get("http://localhost:8083/app/api/v1/submissions/c85f81ce-07ff-4a31-ad0d-0f3a15796528")
                .then((response) => {
                    const data = response.data;
                    console.log(data);
                    Formio.icons = 'fontawesome';
                    var newArray = JSON.parse(formatChefComponents(data));
                    console.log(newArray)
                    Formio.createForm(document.getElementById('formio'), newArray.version.schema, {
                        readOnly: true,
                        renderMode: "html",
                        flatten: true
                    }).then(function (form) {

                        // Set Example Submission Object
                        form.submission = newArray.submission.submission;
                        addEventListeners();

                    });
                })
                .catch((error) => console.error(error));


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
        cardHeaders.forEach(header => {
            header.addEventListener('click', function () {
                // Toggle the display of the corresponding card body
               
                const cardBody = this.nextElementSibling;
                if (cardBody.style.display === 'none' || cardBody.style.display === '') {
                    cardBody.style.display = 'block';
                    header.classList.add('custom-active');

            
                    header.scrollIntoView(true);


                } else {
                    cardBody.style.display = 'none';
                    header.classList.remove('custom-active');
                }

                // Hide all other card bodies except the one that is being clicked
                cardHeaders.forEach(otherHeader => {
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
        cardBodies.forEach(body => {
            body.style.display = 'none';
        });
    };

});