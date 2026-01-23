/**
 * Grant Application Details Page
 * Dependencies: ai-analysis.js - handles AI analysis rendering and management
 */

$(function () {
    let selectedReviewDetails = null;
    let renderFormIoToHtml =
        document.getElementById('RenderFormIoToHtml').value;
    let hasRenderedHtml = document.getElementById('HasRenderedHTML').value;
    abp.localization.getResource('GrantManager');

    const divider = document.getElementById('main-divider');
    const container = document.getElementById('main-container');
    const mainLeftDiv = document.getElementById('main-left');
    const mainRightDiv = document.getElementById('main-right');
    const detailsTabContent = document.getElementById('detailsTabContent');
    const detailsTabs = $('ul#detailsTab');
    const mainLoading = document.getElementById('main-loading');

    $('.fade-in-load').each(function () {
        // Add the visible class to trigger the fade-in effect
        $(this).addClass('visible');
    });

    mainLoading.classList.add('hidden');

    function initializeDetailsPage() {
        setStoredDividerWidth();
        initCommentsWidget();
        initEmailsWidget();
        updateLinksCounters();
        renderSubmission();
        loadAIAnalysis();
        applyTabHeightOffset();
    }

    initializeDetailsPage();

    function setStoredDividerWidth() {
        // Check if there's a saved width in localStorage
        if (localStorage.getItem('leftWidth')) {
            const leftWidth = localStorage.getItem('leftWidth');
            const rightWidth = container.clientWidth - leftWidth;

            mainLeftDiv.style.width = `${leftWidth}px`;
            mainRightDiv.style.width = `${rightWidth}px`;

            applyTabHeightOffset();
        }
    }

    function initializeShadowDOM() {
        const formioContainer = document.getElementById('formio');

        if (!formioContainer) {
            console.error('Formio container not found');
            return null;
        }

        // Check if shadow root already exists
        if (formioContainer.shadowRoot) {
            return formioContainer.shadowRoot;
        }

        // Create shadow DOM with open mode (allows external JS access)
        const shadowRoot = formioContainer.attachShadow({mode: 'open'});

        // Load form.io CSS inside shadow DOM
        const formioStyle = document.createElement('link');
        formioStyle.rel = 'stylesheet';
        formioStyle.href = '/libs/formiojs/formio.form.css';
        shadowRoot.appendChild(formioStyle);

        // Load bootstrap CSS
        const bootstrapStyle = document.createElement('link');
        bootstrapStyle.rel = 'stylesheet';
        bootstrapStyle.href = '/libs/bootstrap-4/dist/css/bootstrap.min.css';
        shadowRoot.appendChild(bootstrapStyle);

        // Load Details-shadow-dom.css into shadow DOM (CRITICAL for accordion, styling, etc.)
        const detailsStyle = document.createElement('link');
        detailsStyle.rel = 'stylesheet';
        detailsStyle.href = '/Pages/GrantApplications/Details-shadow-dom.css';
        shadowRoot.appendChild(detailsStyle);

        return shadowRoot;
    }

    function renderSubmission() {
        // Initialize shadow DOM first
        const shadowRoot = initializeShadowDOM();

        if (renderFormIoToHtml == 'False' || hasRenderedHtml == 'False') {
            getSubmission(shadowRoot);
        } else {
            $('.spinner-grow').hide();

            // Inject pre-rendered HTML into shadow DOM
            if (shadowRoot) {
                const htmlContent = document.getElementById('ApplicationFormSubmissionHtml');
                if (htmlContent && htmlContent.value) {
                    shadowRoot.innerHTML += htmlContent.value;
                }
            }

            addEventListeners(shadowRoot);
        }
    }


    async function getSubmission(shadowRoot) {
        try {
            $('.spinner-grow').hide();
            let submissionDataString = document.getElementById(
                'ApplicationFormSubmissionData'
            ).value;
            let formSchemaString = document.getElementById(
                'ApplicationFormSchema'
            ).value;
            let submissionJson = JSON.parse(submissionDataString);
            let formSchema;
            let submissionData;

            // Check if the submission data is pure data or the entire form
            if (
                submissionJson.version !== undefined &&
                submissionJson.submission !== undefined
            ) {
                // The submission data is in the form of a version and submission object
                formSchema = submissionJson.version.schema;
                submissionData = submissionJson.submission.submission;
            } else if (
                formSchemaString !== undefined &&
                formSchemaString !== ''
            ) {
                formSchema = JSON.parse(formSchemaString);
                submissionData = submissionJson.submission;
            }

            Formio.icons = 'fontawesome';
            const evaluator =
                Formio.Utils?.Evaluator ||
                Formio.Evaluator;
            if (evaluator) {
                evaluator.noeval = true;
                evaluator.protectedEval = true;
            }

            // Create container inside shadow DOM
            const container = document.createElement('div');
            container.id = 'formio-container';
            shadowRoot.appendChild(container);

            await Formio.createForm(
                container, // Render inside shadow DOM
                formSchema,
                {
                    readOnly: true,
                    renderMode: 'form',
                    flatten: true,
                    noeval: true,
                }
            ).then(function (form) {
                handleForm(form, submissionData, shadowRoot);
            });
        } catch (error) {
            console.error(error);
        }
    }

    function handleForm(form, submission, shadowRoot) {
        form.submission = submission;
        form.resetValue();
        form.refresh();
        form.on('render', () => addEventListeners(shadowRoot));

        waitFor(() => isFormChanging(form)).then(() => {
            setTimeout(storeRenderedHtml, 2000);
        });
    }

    async function storeRenderedHtml() {
        if (renderFormIoToHtml == 'False') {
            return;
        }
        const formioContainer = document.getElementById('formio');
        const shadowRoot = formioContainer.shadowRoot;
        let innerHTML = shadowRoot ? shadowRoot.innerHTML : formioContainer.innerHTML;
        let submissionId = document.getElementById(
            'ApplicationFormSubmissionId'
        ).value;
        $.ajax({
            url: '/api/app/submission',
            data: JSON.stringify({
                SubmissionId: submissionId,
                InnerHTML: innerHTML,
            }),
            contentType: 'application/json',
            type: 'POST',
            success: function (data) {
                console.log(data);
            },
            error: function () {
                console.log('error');
            },
        });
    }

    // Wait for the DOM to be fully loaded
    function addEventListeners(shadowRoot) {
        const cardHeaders = getCardHeaders();
        const cardBodies = getCardBodies();

        // Collapse all card bodies initially
        //This also affects textarea in datagrid
        hideAllCardBodies(cardBodies);

        // Add event listeners to headers
        cardHeaders.forEach((header) => {
            header.addEventListener('click', () =>
                onCardHeaderClick(header, cardHeaders)
            );
        });
    }

    // Get all card headers
    function getCardHeaders() {
        const formioContainer = document.getElementById('formio');
        const root = formioContainer.shadowRoot || document;
        return root.querySelectorAll(
            '.card-header:not(.card-body .card-header)'
        );
    }

    // Get all card bodies
    function getCardBodies() {
        const formioContainer = document.getElementById('formio');
        const root = formioContainer.shadowRoot || document;
        return root.querySelectorAll(
            '.card-body:not(.card-body .card-body)'
        );
    }

   

    $('#assessment_upload_btn').click(function () {
        $('#assessment_upload').trigger('click');
    });

    $('#application_attachment_upload_btn').click(function () {
        $('#application_attachment_upload').trigger('click');
    });



    const $recommendationSelect = $('#recommendation_select');
    const $recommendationResetBtn = $('#recommendation_reset_btn');

    $recommendationResetBtn.click(
        debounce(function () {
            $recommendationSelect.prop('selectedIndex', 0).trigger('change');
        }, 400)
    );

    $recommendationSelect.change(function () {
        let value = $(this).val();
        updateRecommendation(value, selectedReviewDetails.id);
    });

    function disableRecommendationControls(state) {
        $recommendationSelect.prop('disabled', state);
        $recommendationResetBtn
            .prop('disabled', state ? true : !$recommendationSelect.val())
            .toggleClass('d-none', state ? true : !$recommendationSelect.val());
    }

    function updateRecommendation(value, id) {
        try {
            // Disable the select and reset button during update
            disableRecommendationControls(true);

            let data = { approvalRecommended: value, assessmentId: id };
            unity.grantManager.assessments.assessment
                .updateAssessmentRecommendation(data)
                .done(function () {
                    abp.notify.success('The recommendation has been updated.');
                    PubSub.publish('refresh_review_list_without_sidepanel', id);
                })
                .always(function () {
                    // Re-enable the select and reset button
                    disableRecommendationControls(false);
                });
        } catch (error) {
            console.log(error);
            // Re-enable the select and reset button in case of error
            disableRecommendationControls(false);
        }
    }

    let assessmentUserDetailsWidgetManager = new abp.WidgetManager({
        wrapper: '#assessmentUserDetailsWidget',
        filterCallback: function () {
            return {
                displayName: selectedReviewDetails.assessorDisplayName,
                badge: selectedReviewDetails.assessorBadge,
                title: 'Title, Role',
            };
        },
    });

    let assessmentScoresWidgetManager = new abp.WidgetManager({
        wrapper: '#assessmentScoresWidgetArea',
        filterCallback: function () {
            return {
                assessmentId: decodeURIComponent($('#AssessmentId').val()),
                currentUserId: decodeURIComponent(abp.currentUser.id),
            };
        },
    });

    PubSub.subscribe('refresh_assessment_scores', (msg, data) => {
        assessmentScoresWidgetManager.refresh();
        updateSubtotal();
    });

    PubSub.subscribe('select_application_review', (msg, data) => {
        if (data) {
            selectedReviewDetails = data;
            setDetailsContext('assessment');
            let selectElement = document.getElementById(
                'recommendation_select'
            );
            selectElement.value = data.approvalRecommended;
            PubSub.publish('AssessmentComment_refresh', {
                review: selectedReviewDetails,
            });
            assessmentUserDetailsWidgetManager.refresh();
            assessmentScoresWidgetManager.refresh();
            updateSubtotal();
            checkCurrentUser(data);
        } else {
            setDetailsContext('application');
        }
    });

    PubSub.subscribe('deselect_application_review', (msg, data) => {
        setDetailsContext('application');
    });

    $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function () {
        $($.fn.dataTable.tables(true)).DataTable().columns.adjust();
    });

    $('#printAssessmentPdf').click(function () {
        openScoreSheetDataInNewTab($('#reviewDetails').html());
    });

    $('#printPdf').click(function () {
        let submissionId = document.getElementById('ChefsSubmissionId').value;
        let submissionDataString = document.getElementById(
            'ApplicationFormSubmissionData'
        ).value;
        let formSchemaString = document.getElementById(
            'ApplicationFormSchema'
        ).value;
        let submissionJson = JSON.parse(submissionDataString);
        let formSchema;
        let submissionData;

        // Initialize data with correct structure
        let data = {
            version: {
                schema: null,
            },
            submission: {
                submission: null,
            },
        };

        // Determine how to extract form schema and submission
        if (
            submissionJson.version !== undefined &&
            submissionJson.submission !== undefined
        ) {
            formSchema = submissionJson.version.schema;
            submissionData = submissionJson.submission.submission;
        } else if (formSchemaString !== undefined && formSchemaString !== '') {
            formSchema = JSON.parse(formSchemaString);
            submissionData = submissionJson.submission;
        }

        data.version.schema = formSchema;
        data.submission.submission = submissionData;

        // Open a new tab
        let newTab = globalThis.open('', '_blank');

        // Wait for the new tab's document to be available
        const doc = newTab.document;

        // Set title
        doc.title = 'Print';

        // HEAD
        const head = doc.head;

        const jqueryScript = doc.createElement('script');
        jqueryScript.src = '/libs/jquery/jquery.js';

        const formioScript = doc.createElement('script');
        formioScript.src = '/libs/formiojs/formio.form.min.js';

        const bootstrapCSS = doc.createElement('link');
        bootstrapCSS.rel = 'stylesheet';
        bootstrapCSS.href = '/libs/bootstrap-4/dist/css/bootstrap.min.css';

        const formioCSS = doc.createElement('link');
        formioCSS.rel = 'stylesheet';
        formioCSS.href = '/libs/formiojs/formio.form.css';

        // Add inline CSS to disable links (more reliable than JavaScript)
        const inlineStyle = doc.createElement('style');
        inlineStyle.textContent = `
            a {
                pointer-events: none !important;
                cursor: default !important;
                text-decoration: none !important;
                color: black !important;
            }
            button {
                display: none !important;
            }
        `;

        head.appendChild(jqueryScript);
        head.appendChild(formioScript);
        head.appendChild(bootstrapCSS);
        head.appendChild(formioCSS);
        head.appendChild(inlineStyle);

        // BODY
        const body = doc.body;

        // Hidden input
        const hiddenInput = doc.createElement('input');
        hiddenInput.type = 'hidden';
        hiddenInput.name = 'ApplicationFormSubmissionId';
        hiddenInput.value = submissionId;
        body.appendChild(hiddenInput);

        // Placeholder div
        const formContainer = doc.createElement('div');
        formContainer.id = 'new-rendering';
        formContainer.textContent = 'Loading form...';
        body.appendChild(formContainer);

        // Load your custom script after Form.io is ready
        formioScript.onload = () => {
            const customScript = doc.createElement('script');
            customScript.src = '/Pages/GrantApplications/loadPrint.js';
            customScript.onload = function () {
                // Call your global executeOperations function
                newTab.executeOperations(data);
            };
            head.appendChild(customScript);
        };
    });

    function openScoreSheetDataInNewTab(assessmentScoresheet) {
        const newTab = globalThis.open('', '_blank');
        const doc = newTab.document;
        
        doc.open();
        doc.close();
        doc.title = 'Print';
        
        // Create and append stylesheets
        const stylesheets = [
            { href: '/libs/bootstrap-4/dist/css/bootstrap.min.css' },
            { href: '/Pages/GrantApplications/ScoresheetPrint.css' }
        ];
        
        stylesheets.forEach(({ href }) => {
            const link = doc.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            doc.head.appendChild(link);
        });
        
        // Add jQuery script
        const jqueryScript = doc.createElement('script');
        jqueryScript.src = '/libs/jquery/jquery.js';
        doc.head.appendChild(jqueryScript);
        
        doc.body.innerHTML = assessmentScoresheet;
        
        // Load and execute print script
        newTab.onload = function () {
            const script = doc.createElement('script');
            script.src = '/Pages/GrantApplications/loadScoresheetPrint.js';
            script.onload = () => newTab.executeOperations();
            doc.head.appendChild(script);
        };
    }

    let applicationBreadcrumbWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationBreadcrumbWidget',
        filterCallback: function () {
            return {
                applicationId: $('#DetailsViewApplicationId').val(),
            };
        },
    });

    let applicationStatusWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationStatusWidget',
        filterCallback: function () {
            return {
                applicationId: $('#DetailsViewApplicationId').val(),
            };
        },
    });

    let applicationActionWidgetManager = new abp.WidgetManager({
        wrapper:
            '.abp-widget-wrapper[data-widget-name="ApplicationActionWidget"]',
        filterCallback: function () {
            return {
                applicationId: $('#DetailsViewApplicationId').val(),
            };
        },
    });

    const assessmentResultWidgetDiv = 'assessmentResultWidget';

    let assessmentResultWidgetManager = new abp.WidgetManager({
        wrapper: '#' + assessmentResultWidgetDiv,
        filterCallback: function () {
            return {
                applicationId: $('#DetailsViewApplicationId').val(),
                applicationFormVersionId: $(
                    '#AssessmentResultViewApplicationFormVersionId'
                ).val(),
            };
        },
    });

    const assessmentResultTargetNode = document.querySelector(
        '#' + assessmentResultWidgetDiv
    );
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

    if (assessmentResultTargetNode) {
        assessmentResultObserver.observe(
            assessmentResultTargetNode,
            widgetConfig
        );
    }

    PubSub.subscribe('application_status_changed', (msg, data) => {
        applicationBreadcrumbWidgetManager.refresh();
        applicationStatusWidgetManager.refresh();
        assessmentResultWidgetManager.refresh();
        applicationActionWidgetManager.refresh();
    });

    function initCustomFieldCurrencies() {
        $('.custom-currency-input')
            .maskMoney({
                thousands: ',',
                decimal: '.',
            })
            .maskMoney('mask');
    }

    PubSub.subscribe('application_assessment_results_saved', (msg, data) => {
        assessmentResultWidgetManager.refresh();
    });

    const summaryWidgetDiv = 'summaryWidgetArea';

    let summaryWidgetManager = new abp.WidgetManager({
        wrapper: '#' + summaryWidgetDiv,
        filterCallback: function () {
            return {
                applicationId:
                    $('#DetailsViewApplicationId').val() ??
                    '00000000-0000-0000-0000-000000000000',
            };
        },
    });

    const summaryWidgetTargetNode = document.querySelector(
        '#' + summaryWidgetDiv
    );
    const summaryWidgetObserver = new MutationObserver(widgetCallback);
    summaryWidgetObserver.observe(summaryWidgetTargetNode, widgetConfig);

    PubSub.subscribe('refresh_detail_panel_summary', (msg, data) => {
        summaryWidgetManager.refresh();
    });

    let tabCounters = {
        files: 0,
        chefs: 0,
        emails: 0,
    };

    PubSub.subscribe('update_application_attachment_count', (msg, data) => {
        if (data.files || data.files === 0) {
            tabCounters.files = data.files;
        }
        if (data.chefs || data.chefs === 0) {
            tabCounters.chefs = data.chefs;
        }
        $('#application_attachment_count').html(
            tabCounters.files + tabCounters.chefs
        );
    });
    
    PubSub.subscribe('update_ai_analysis_count', (msg, data) => {
        if (data.itemCount || data.itemCount === 0) {
            tabCounters.ai_analysis = data.itemCount;
        }
        $('#ai_analysis_count').html(tabCounters.ai_analysis);
    });

    PubSub.subscribe('update_application_emails_count', (msg, data) => {
        if (data.itemCount || data.itemCount === 0) {
            tabCounters.emails = data.itemCount;
        }
        $('#application_emails_count').html(tabCounters.emails);
    });

    let applicationRecordsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationRecordsWidget',
        filterCallback: function () {
            return {
                applicationId: $('#DetailsViewApplicationId').val(),
            };
        },
    });

    PubSub.subscribe('ApplicationLinks_refresh', (msg, data) => {
        applicationRecordsWidgetManager.refresh();
        updateLinksCounters();
    });

    // custom fields
    $('body').on('click', '.custom-tab-save', function (event) {
        let id = $(this).attr('id');
        let uiAnchor = $(this).attr('data-ui-anchor');
        let worksheetId = $(this).attr('data-ui-worksheetId');
        let formDataName =
            id.replace('save_', '').replace('_btn', '') + '_form';
        let applicationId = decodeURIComponent(
            $('#DetailsViewApplicationId').val()
        );
        let formData = $(`#${formDataName}`).serializeArray();
        let customFormObj = {};
        let formVersionId = $('#ApplicationFormVersionId').val();

        $.each(formData, function (_, input) {
            customFormObj[input.name] = input.value;
        });

        $(`#${formDataName} input:checkbox`).each(function () {
            customFormObj[this.name] = this.checked.toString();
        });

        updateCustomForm(
            applicationId,
            formVersionId,
            customFormObj,
            uiAnchor,
            id,
            formDataName,
            worksheetId
        );
    });

    PubSub.subscribe('fields_tab', (_, data) => {
        let formDataName = data.worksheet + '_form';
        let formValid = $(`form#${formDataName}`).valid();
        let saveBtn = $(`#save_${data.worksheet}_btn`);
        if (
            formValid &&
            !formHasInvalidCurrencyCustomFields(`${formDataName}`)
        ) {
            saveBtn.prop('disabled', false);
        } else {
            saveBtn.prop('disabled', true);
        }
    });

    divider.addEventListener('mousedown', function (e) {
        e.preventDefault();

        document.addEventListener('mousemove', resize);
        document.addEventListener('mouseup', stopResize);
    });

    function resize(e) {
        const containerRect = container.getBoundingClientRect();
        const leftWidth = e.clientX - containerRect.left;
        const rightWidth = containerRect.right - e.clientX;

        mainLeftDiv.style.width = `${leftWidth}px`;
        mainRightDiv.style.width = `${rightWidth}px`;

        // Apply the height offset depending on tabs height
        applyTabHeightOffset();

        // Resize DataTables
        debouncedResizeAwareDataTables();

        // Save the left width to localStorage
        localStorage.setItem('leftWidth', leftWidth);
    }

    function applyTabHeightOffset() {
        const detailsTabHeight = 235 + detailsTabs[0].clientHeight;
        detailsTabContent.style.height = `calc(100vh - ${detailsTabHeight}px)`;
    }

    // Debounced DataTable resizing function
    const debouncedResizeAwareDataTables = debounce(() => {
        $('table[data-resize-aware="true"]:visible').each(function () {
            const table = $(this).DataTable();
            try {
                table.columns.adjust().draw();
            } catch {
                console.error(
                    `Adjust width failed for table ${$(this).id}:`,
                    error
                );
            }
        });
    }, 15); // Adjust the delay as needed

    // Add event listeners to the li items under #detailsTab and #myTab
    $('#detailsTab li').on('click', function () {
        debouncedAdjustTables('detailsTab');
    });

    $('#myTab li').on('click', function () {
        debouncedAdjustTables('myTabContent');
    });

    function stopResize() {
        document.removeEventListener('mousemove', resize);
        document.removeEventListener('mouseup', stopResize);
    }

    function recalcAndAdjustSplit() {
        const containerWidth = container.clientWidth;
        const savedLeftWidth = localStorage.getItem('leftWidth');

        if (savedLeftWidth) {
            const savedPercentage = savedLeftWidth / containerWidth;

            // Recalculate the new widths based on the saved percentage
            const newLeftWidth = containerWidth * savedPercentage;
            const newRightWidth = containerWidth - newLeftWidth;

            mainLeftDiv.style.width = `${newLeftWidth}px`;
            mainRightDiv.style.width = `${newRightWidth}px`;

            // Save the new left width to localStorage
            localStorage.setItem('leftWidth', newLeftWidth);
        }
    }

    const debouncedAdjustTables = debounce(adjustVisibleTablesInContainer, 15);

    function adjustVisibleTablesInContainer(containerId) {
        const activeTab = $(`#${containerId} div.active`);
        activeTab
            .find('table[data-resize-aware="true"]:visible')
            .each(function () {
                const table = $(this).DataTable();
                try {
                    table.columns.adjust().draw();
                } catch (error) {
                    console.error(
                        `Adjust width failed for table in container ${containerId}:`,
                        error
                    );
                }
            });
    }

    function windowResize() {
        recalcAndAdjustSplit();
    }

    globalThis.addEventListener('resize', windowResize);
});


// Handle the card header click event
function onCardHeaderClick(clickedHeader, cardHeaders) {
    const clickedCardBody = getNextCardBody(clickedHeader);
    const isVisible = toggleCardBodyVisibility(clickedCardBody);
    toggleHeaderActiveClass(clickedHeader, isVisible);
    hideOtherCardBodies(clickedHeader, cardHeaders);
}

// Hide all other card bodies
function hideOtherCardBodies(currentHeader, cardHeaders) {
    cardHeaders.forEach((otherHeader) => {
        if (otherHeader !== currentHeader) {
            const otherCardBody = getNextCardBody(otherHeader);
            hideCardBody(otherCardBody);
            removeHeaderActiveClass(otherHeader);
        }
    });
}

// Hide a specific card body
function hideCardBody(cardBody) {
    cardBody.classList.add('hidden');
}

// Remove active class from a specific header
function removeHeaderActiveClass(header) {
    header.classList.remove('custom-active');
}


function debounce(func, wait) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(
            function () {
                func.apply(this, args);
            }.bind(this),
            wait
        );
    };
}

function isFormChanging(form) {
    return form.changing === false;
}

// Toggle active class for the header
function toggleHeaderActiveClass(header, isVisible) {
    header.classList.toggle('custom-active', isVisible);
}

// Get the next sibling card body
function getNextCardBody(header) {
    return header.nextElementSibling;
}

// Hide all card bodies initially
function hideAllCardBodies(cardBodies) {
    cardBodies.forEach((body) => body.classList.add('hidden'));
}

// Toggle visibility of the card body
function toggleCardBodyVisibility(cardBody) {
    return !cardBody.classList.toggle('hidden');
}

function waitFor(conditionFunction) {
    const poll = (resolve) => {
        if (conditionFunction()) resolve();
        else setTimeout((_) => poll(resolve), 400);
    };

    return new Promise(poll);
}


function updateCustomForm(
    applicationId,
    formVersionId,
    customFormObj,
    uiAnchor,
    saveId,
    formDataName,
    worksheetId
) {
    let customFormUpdate = {
        instanceCorrelationId: applicationId,
        instanceCorrelationProvider: 'Application',
        sheetCorrelationId: formVersionId,
        sheetCorrelationProvider: 'FormVersion',
        uiAnchor: uiAnchor,
        customFields: customFormObj,
        formDataName: formDataName,
        worksheetId: worksheetId,
    };

    $(`#${saveId}`).prop('disabled', true);
    unity.flex.worksheetInstances.worksheetInstance
        .update(customFormUpdate)
        .done(function () {
            abp.notify.success('Information has been updated.');
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
            PubSub.publish('fields_tab', {
                worksheet: worksheet,
                fieldId: field.id,
            });
        }
    }
}

function isKnownAnchor(anchor) {
    if (
        anchor === 'projectinfo' ||
        anchor === 'applicantinfo' ||
        anchor === 'assessmentinfo' ||
        anchor === 'paymentinfo' ||
        anchor === 'fundingagreementinfo'
    ) {
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

    static setCustomFields(customFieldsObj) {
        for (const key in customFieldsObj) {
            if (
                customFieldsObj.hasOwnProperty(key) &&
                key.startsWith('custom_')
            ) {
                customFieldsObj.CustomFields[key] = customFieldsObj[key];
            }
        }
    }
};

function uploadApplicationFiles(inputId) {
    let applicationId = decodeURIComponent(
        $('#DetailsViewApplicationId').val()
    );
    let currentUserId = decodeURIComponent($('#CurrentUserId').val());
    let currentUserName = decodeURIComponent($('#CurrentUserName').val());
    let url =
        '/api/app/attachment/application/' +
        applicationId +
        '/upload?userId=' +
        currentUserId +
        '&userName=' +
        currentUserName;
    uploadFiles(inputId, url, 'refresh_application_attachment_list');
}

function uploadAssessmentFiles(inputId) {
    let assessmentId = decodeURIComponent($('#AssessmentId').val());
    let currentUserId = decodeURIComponent($('#CurrentUserId').val());
    let currentUserName = decodeURIComponent($('#CurrentUserName').val());
    let url =
        '/api/app/attachment/assessment/' +
        assessmentId +
        '/upload?userId=' +
        currentUserId +
        '&userName=' +
        currentUserName;
    uploadFiles(inputId, url, 'refresh_assessment_attachment_list');
}

function uploadFiles(inputId, urlStr, channel) {
    let input = document.getElementById(inputId);
    let files = input.files;
    let formData = new FormData();
    const disallowedTypes = JSON.parse(
        decodeURIComponent($('#Extensions').val())
    );
    const maxFileSize = decodeURIComponent($('#MaxFileSize').val());

    let isAllowedTypeError = false;
    let isMaxFileSizeError = false;
    if (files.length == 0) {
        return;
    }

    for (let file of files) {
        if (
            disallowedTypes.includes(
                file.name
                    .slice(file.name.lastIndexOf('.') + 1, file.name.length)
                    .toLowerCase()
            )
        ) {
            isAllowedTypeError = true;
        }
        if (file.size * 0.000001 > maxFileSize) {
            isMaxFileSizeError = true;
        }

        formData.append('files', file);
    }

    if (isAllowedTypeError) {
        input.value = null;
        return abp.notify.error('Error', 'File type not supported');
    }
    if (isMaxFileSizeError) {
        input.value = null;
        return abp.notify.error(
            'Error',
            'File size exceeds ' + maxFileSize + 'MB'
        );
    }

    $.ajax({
        url: urlStr,
        data: formData,
        processData: false,
        contentType: false,
        type: 'POST',
        success: function (data) {
            abp.notify.success(data.responseText, 'File Upload Is Successful');
            PubSub.publish(channel);
            input.value = null;
        },
        error: function (data) {
            abp.notify.error(data.responseText, 'File Upload Not Successful');
            PubSub.publish(channel);
            input.value = null;
        },
    });
}

function getCurrentUser() {
    return abp.currentUser.id;
}

const checkCurrentUser = function (data) {
    if (getCurrentUser() == data.assessorId && data.status == 'IN_PROGRESS') {
        $('#recommendation_select').prop('disabled', false);
        $('#assessment_upload_btn').prop('disabled', false);
        $('#recommendation_reset_btn')
            .prop('disabled', !$('#recommendation_select').val())
            .toggleClass('d-none', !$('#recommendation_select').val());
    } else {
        $('#recommendation_select').prop('disabled', 'disabled');
        $('#assessment_upload_btn').prop('disabled', 'disabled');
        $('#recommendation_reset_btn')
            .prop('disabled', 'disabled')
            .toggleClass('d-none', true);
    }
};

function updateEmailsCounters() {
    setTimeout(() => {
        $('.emails-container')
            .map(function () {
                $('#' + $(this).data('emailscounttag')).html(
                    $(this).data('count')
                );
            })
            .get();
    }, 100);
}

function updateCommentsCounters() {
    setTimeout(() => {
        $('.comments-container')
            .map(function () {
                $('#' + $(this).data('counttag')).html($(this).data('count'));
            })
            .get();
    }, 100);
}

function updateLinksCounters() {
    setTimeout(() => {
        $('.links-container')
            .map(function () {
                const tag = $(this).data('linkscounttag');
                const count = $(this).attr('data-count');
                $('#' + tag).text(count);
            })
            .get();
    }, 100);
}

function initEmailsWidget() {
    const currentUserId = decodeURIComponent($('#CurrentUserId').val());

    let applicationEmailsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationEmailsWidget',
        filterCallback: function () {
            return {
                applicationId: $('#DetailsViewApplicationId').val(),
                currentUserId: currentUserId,
            };
        },
    });

    PubSub.subscribe('ApplicationEmail_refresh', () => {
        applicationEmailsWidgetManager.refresh();
        updateEmailsCounters();
    });

    updateEmailsCounters();
    let tagsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationTagsWidget',
        filterCallback: function () {
            return {
                applicationId:
                    $('#DetailsViewApplicationId').val() ??
                    '00000000-0000-0000-0000-000000000000',
            };
        },
    });

    PubSub.subscribe('ApplicationTags_refresh', () => {
        tagsWidgetManager.refresh();
    });
}

function initCommentsWidget() {
    const currentUserId = decodeURIComponent($('#CurrentUserId').val());
    let selectedReviewDetails;
    let applicationCommentsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationCommentsWidget',
        filterCallback: function () {
            return {
                ownerId: $('#DetailsViewApplicationId').val(),
                commentType: 0,
                currentUserId: currentUserId,
            };
        },
    });

    let assessmentCommentsWidgetManager = new abp.WidgetManager({
        wrapper: '#assessmentCommentsWidget',
        filterCallback: function () {
            return {
                ownerId: selectedReviewDetails.id,
                commentType: 1,
                currentUserId: currentUserId,
            };
        },
    });

    PubSub.subscribe('ApplicationComment_refresh', () => {
        applicationCommentsWidgetManager.refresh();
        updateCommentsCounters();
    });

    PubSub.subscribe('AssessmentComment_refresh', (_, data) => {
        if (data?.review) {
            selectedReviewDetails = data.review;
        }
        assessmentCommentsWidgetManager.refresh();
        updateCommentsCounters();
    });

    updateCommentsCounters();
    let tagsWidgetManager = new abp.WidgetManager({
        wrapper: '#applicationTagsWidget',
        filterCallback: function () {
            return {
                applicationId:
                    $('#DetailsViewApplicationId').val() ??
                    '00000000-0000-0000-0000-000000000000',
            };
        },
    });

    PubSub.subscribe('ApplicationTags_refresh', () => {
        tagsWidgetManager.refresh();
    });
}

function setDetailsContext(context) {
    switch (context) {
        case 'assessment':
            $('#reviewDetails').show();
            $('#applicationDetails').hide();
            break;
        case 'application':
            $('#reviewDetails').hide();
            $('#applicationDetails').show();
            break;
    }
}

function formHasInvalidCurrencyCustomFields(formId) {
    let invalidFieldsFound = false;
    $('#' + formId + " input[id^='custom']:visible").each(function (i, el) {
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
    let numericValue = Number.parseFloat(originalValue.replaceAll(',', ''));

    let minValue = Number.parseFloat(input.attr('data-min'));
    let maxValue = Number.parseFloat(input.attr('data-max'));

    if (Number.isNaN(numericValue)) {
        showCurrencyError(input, 'Please enter a valid number.');
        return false;
    } else if (numericValue < minValue) {
        showCurrencyError(
            input,
            `Please enter a value greater than or equal to ${minValue}.`
        );
        return false;
    } else if (numericValue > maxValue) {
        showCurrencyError(
            input,
            `Please enter a value less than or equal to ${maxValue}.`
        );
        return false;
    } else {
        clearCurrencyError(input);
        return true;
    }
}

function showCurrencyError(input, message) {
    let errorSpan = input.attr('id') + '-error';
    document.getElementById(errorSpan).textContent = message;
    input.attr('aria-invalid', 'true');
}

function clearCurrencyError(input) {
    let errorSpan = input.attr('id') + '-error';
    document.getElementById(errorSpan).textContent = '';
    input.attr('aria-invalid', 'false');
}
