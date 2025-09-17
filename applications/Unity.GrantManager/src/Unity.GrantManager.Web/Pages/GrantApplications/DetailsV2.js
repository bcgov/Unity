$(function () {
    let selectedReviewDetails = null;
    let renderFormIoToHtml =
        document.getElementById('RenderFormIoToHtml').value;
    let hasRenderedHtml = document.getElementById('HasRenderedHTML').value;
    abp.localization.getResource('GrantManager');

    //LAZY LOADING VARIABLES HERE
    let loadedTabs = new Set(['nav-summery']); // Track loaded tabs
    let loadingComponents = new Set(); // Track currently loading components

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

        loadInitiallyActiveTabs();
    }

    function loadInitiallyActiveTabs() {
        // Load components in the initially active right-side tab (details)
        setTimeout(() => {
            const activeRightTab = $('#myTabContent .tab-pane.active');
            if (activeRightTab.length > 0) {
                const tabId = activeRightTab.attr('id');
                if (!loadedTabs.has(tabId)) {
                    loadTabComponents(tabId);
                    loadedTabs.add(tabId);
                }
            }

            // Load components in the initially active left-side tab (nav-summery)
            const activeLeftTab = $('#detailsTabContent .tab-pane.active');
            if (activeLeftTab.length > 0) {
                const tabId = activeLeftTab.attr('id');
                if (!loadedTabs.has(tabId)) {
                    loadTabComponents(tabId);
                    loadedTabs.add(tabId);
                }
            }
        }, 100);
    }

    initializeDetailsPage();

    // Handle Bootstrap tab activation for right-side tabs (not ABP tabs)
    $('#myTabContent')
        .closest('.right-card')
        .find('ul.nav-tabs')
        .on('shown.bs.tab', 'button[data-bs-toggle="tab"]', function (e) {
            const $target = $(e.target);
            const tabTarget = $target.attr('data-bs-target'); // Gets #history, #attachments, etc.
            const tabName = tabTarget ? tabTarget.substring(1) : null; // Remove the # to get 'history'

            console.log('Right-side tab activated:', tabName);
            console.log('Target element:', $target);

            // Lazy load logic
            if (tabName && !loadedTabs.has(tabName)) {
                console.log(`Loading components for tab: ${tabName}`);
                loadedTabs.add(tabName);
                loadTabComponents(tabName);
            } else {
                console.log(`Tab ${tabName} already loaded or invalid`);
            }

            // Adjust DataTables
            $($.fn.dataTable.tables(true)).DataTable().columns.adjust();
        });

    // Handle ABP tab activation for lazy loading - ABP tabs use different event
    $('#detailsTab').on(
        'shown.bs.tab',
        'a[data-bs-toggle="tab"]',
        function (e) {
            const $target = $(e.target);
            const tabName = $target.attr('aria-controls'); // ABP tabs use aria-controls for tab name

            console.log('ABP Tab activated:', tabName);
            console.log('Target element:', $target);

            // Lazy load logic
            if (tabName && !loadedTabs.has(tabName)) {
                console.log('Loading tab components for:', tabName);
                loadTabComponents(tabName);
                loadedTabs.add(tabName);
            } else {
                console.log('Tab already loaded:', tabName);
            }

            // Adjust DataTables
            $($.fn.dataTable.tables(true)).DataTable().columns.adjust();
        }
    );

    function loadTabComponents(tabName) {
        console.log('=== Loading Tab Components Debug ===');
        console.log('Tab name:', tabName);

        // Multiple strategies to find the tab pane - ABP tabs can have different structures
        let tabPane = $(`#${tabName}`); // Should find #history, #attachments, etc.

        // For custom tabs, also try finding by nav-* prefix
        if (tabPane.length === 0 && tabName.startsWith('nav-')) {
            const customTabName = tabName.replace('nav-', '');
            tabPane = $(`[name="${customTabName}"]`);
        }

        if (tabPane.length > 0) {
            console.log(`Found tab pane by ID: ${tabName}`);
            console.log('Tab pane HTML content:', tabPane.html());
        } else {
            console.error(`Could not find tab pane with ID: ${tabName}`);
            return;
        }

        // Find lazy components within the tab pane
        const lazyComponents = tabPane.find(
            '.lazy-component-container:not([data-loaded="true"])'
        );
        console.log(
            `Found ${lazyComponents.length} lazy components in tab pane`
        );

        if (lazyComponents.length === 0) {
            console.warn(`No lazy components found in tab pane: ${tabName}`);
            console.log(
                'Looking for any .lazy-component-container:',
                tabPane.find('.lazy-component-container').length
            );
            console.log(
                'Looking for any [data-component]:',
                tabPane.find('[data-component]').length
            );
            console.log('All divs in tab pane:', tabPane.find('div').length);
            return;
        }

        loadComponentsFromContainers(lazyComponents);
    }

    function loadComponentsFromContainers($containers) {
        $containers.each(function (index) {
            const $container = $(this);
            const loadUrl = $container.data('load-url');
            const componentName = $container.data('component');
            const tabName = $container.data('tab');

            console.log(
                `Processing component ${index + 1}/${$containers.length}:`,
                {
                    name: componentName,
                    url: loadUrl,
                    tab: tabName,
                    containerHtml:
                        $container[0].outerHTML.substring(0, 150) + '...',
                }
            );

            // Prevent duplicate loading
            if (
                loadingComponents.has(componentName) ||
                $container.data('loaded')
            ) {
                console.log(
                    `Skipping already loading/loaded component: ${componentName}`
                );
                return;
            }

            // Validate required data
            if (!loadUrl || !componentName) {
                console.error(`Missing required data for component:`, {
                    componentName: componentName,
                    loadUrl: loadUrl,
                    container: $container[0],
                });
                return;
            }

            console.log(`Loading component: ${componentName}`);
            loadComponent($container, loadUrl, componentName);
        });
    }

    // Component asset mapping
    const COMPONENT_ASSETS = {
        SummaryWidget: {
            js: '/Views/Shared/Components/SummaryWidget/Default.js',
            css: '/Views/Shared/Components/SummaryWidget/Default.css',
        },
        ProjectInfo: {
            js: '/Views/Shared/Components/ProjectInfo/Default.js',
            css: '/Views/Shared/Components/ProjectInfo/Default.css',
        },
        ApplicantInfo: {
            js: '/Views/Shared/Components/ApplicantInfo/Default.js',
            css: '/Views/Shared/Components/ApplicantInfo/Default.css',
        },
        AssessmentResults: {
            js: '/Views/Shared/Components/AssessmentResults/Default.js',
            css: '/Views/Shared/Components/AssessmentResults/Default.css',
        },
        ReviewList: {
            js: '/Views/Shared/Components/ReviewList/ReviewList.js',
            css: '/Views/Shared/Components/ReviewList/ReviewList.css',
        },
        FundingAgreementInfo: {
            js: '/Views/Shared/Components/FundingAgreementInfo/Default.js',
            css: '/Views/Shared/Components/FundingAgreementInfo/Default.css',
        },
        PaymentInfo: {
            js: '/Views/Shared/Components/PaymentInfo/Default.js',
            css: '/Views/Shared/Components/PaymentInfo/Default.css',
        },
        HistoryWidget: {
            js: '/Views/Shared/Components/HistoryWidget/Default.js',
            css: '/Views/Shared/Components/HistoryWidget/Default.css',
        },
        ApplicationStatusWidget: {
            js: '/Views/Shared/Components/ApplicationStatusWidget/Default.js',
            css: '/Views/Shared/Components/ApplicationStatusWidget/Default.css',
        },
        ApplicationAttachments: {
            js: '/Views/Shared/Components/ApplicationAttachments/ApplicationAttachments.js',
            css: '/Views/Shared/Components/ApplicationAttachments/ApplicationAttachments.css',
        },
        CustomTabWidget: {
            js: '/Views/Shared/Components/CustomTabWidget/Default.js',
            css: '/Views/Shared/Components/CustomTabWidget/Default.css',
        },
        WorksheetInstanceWidget: {
            js: '/Views/Shared/Components/WorksheetInstanceWidget/Default.js',
            css: '/Views/Shared/Components/WorksheetInstanceWidget/Default.css',
        },
    };

    // Track loaded assets to avoid duplicate loading
    let loadedAssets = new Set();

    function loadComponentAssets(componentName) {
        const assets = COMPONENT_ASSETS[componentName];
        if (!assets) {
            console.warn(`No assets defined for component: ${componentName}`);
            return Promise.resolve();
        }

        const promises = [];

        // Load CSS if not already loaded
        if (assets.css && !loadedAssets.has(assets.css)) {
            promises.push(loadCSS(assets.css));
            loadedAssets.add(assets.css);
        }

        // Load JS if not already loaded
        if (assets.js && !loadedAssets.has(assets.js)) {
            promises.push(loadJS(assets.js));
            loadedAssets.add(assets.js);
        }

        return Promise.all(promises);
    }

    function loadCSS(href) {
        return new Promise((resolve, reject) => {
            // Check if CSS is already loaded
            if (document.querySelector(`link[href="${href}"]`)) {
                console.log(`CSS already loaded: ${href}`);
                resolve();
                return;
            }

            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.type = 'text/css';
            link.href = href;

            link.onload = () => {
                console.log(`CSS loaded successfully: ${href}`);
                resolve();
            };

            link.onerror = () => {
                console.error(`Failed to load CSS: ${href}`);
                // Don't reject - CSS failure shouldn't block component loading
                resolve();
            };

            document.head.appendChild(link);
        });
    }

    function loadJS(src) {
        return new Promise((resolve, reject) => {
            // Check if JS is already loaded
            if (document.querySelector(`script[src="${src}"]`)) {
                console.log(`JavaScript already loaded: ${src}`);
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.type = 'text/javascript';
            script.src = src;

            script.onload = () => {
                console.log(`JavaScript loaded successfully: ${src}`);
                resolve();
            };

            script.onerror = () => {
                console.error(`Failed to load JavaScript: ${src}`);
                // Don't reject - JS failure shouldn't block component loading completely
                resolve();
            };

            document.head.appendChild(script);
        });
    }

    function initializeABPWidget(componentName, $container) {
        // Wait for scripts to be fully processed
        setTimeout(() => {
            try {
                // console.log(
                //     `Attempting to initialize ABP widget: ${componentName}`
                // );

                // Find the widget wrapper within the loaded content
                const $widget = $container.find(
                    `[data-widget-name="${componentName}"]`
                );

                if ($widget.length === 0) {
                    console.warn(
                        `Widget wrapper not found for ${componentName}. Looking for alternative selectors...`
                    );

                    // Try alternative selectors based on component name
                    let altSelector = '';
                    switch (componentName) {
                        case 'ProjectInfo':
                            altSelector = '.project-info-container';
                            break;
                        case 'ApplicantInfo':
                            altSelector = '.applicant-info-container';
                            break;
                        case 'AssessmentResults':
                            altSelector = '.assessment-container';
                            break;
                        case 'PaymentInfo':
                            altSelector = '.payment-container';
                            break;
                        case 'FundingAgreementInfo':
                            altSelector = '.funding-container';
                            break;
                        default:
                            altSelector = `[class*="${componentName.toLowerCase()}"]`;
                    }

                    const $altWidget = $container.find(altSelector);
                    if ($altWidget.length > 0) {
                        console.log(
                            `Found alternative widget container for ${componentName}`
                        );
                        $altWidget.attr('data-widget-name', componentName);
                        initializeWidgetAPI(componentName, $altWidget);
                    } else {
                        console.warn(
                            `No suitable container found for ${componentName} widget`
                        );
                    }
                    return;
                }

                initializeWidgetAPI(componentName, $widget);
            } catch (error) {
                console.error(
                    `Error initializing widget ${componentName}:`,
                    error
                );
            }
        }, 3000); // Increased delay to ensure script execution
    }

    function initializeWidgetAPI(componentName, $widget) {
        // Check if the widget function exists in abp.widgets
        if (abp.widgets && typeof abp.widgets[componentName] === 'function') {
            console.log(`Initializing ABP widget API: ${componentName}`);

            try {
                // Initialize the widget using ABP's system
                const widgetApi = abp.widgets[componentName]($widget);

                if (widgetApi && typeof widgetApi.init === 'function') {
                    // Get filters if the widget supports them
                    const filters = widgetApi.getFilters
                        ? widgetApi.getFilters()
                        : {};
                    widgetApi.init(filters);

                    // Store the API on the widget element for future reference
                    $widget.data('abp-widget-api', widgetApi);

                    console.log(
                        `Widget ${componentName} initialized successfully with API`
                    );

                    // For ApplicantInfo, store the API globally for access
                    if (componentName === 'ApplicantInfo') {
                        window.applicantInfoWidgetApi = widgetApi;
                    }
                } else {
                    console.warn(
                        `Widget API or init method not found for ${componentName}`
                    );
                }
            } catch (error) {
                console.error(
                    `Error during widget API initialization for ${componentName}:`,
                    error
                );
            }
        } else {
            console.warn(
                `Widget function not found: abp.widgets.${componentName}`
            );
            console.log('Available widgets:', Object.keys(abp.widgets || {}));
        }
    }

    function loadComponent($container, url, componentName) {
        const $skeleton = $container.find('.skeleton-loader');
        const $content = $container.find('.component-content');

        console.log('=== Loading Component Debug ===');
        console.log('Component:', componentName);
        console.log('URL:', url);
        console.log('Container:', $container);
        console.log('Skeleton found:', $skeleton.length);
        console.log('Content div found:', $content.length);

        // Prevent duplicate loading
        if (loadingComponents.has(componentName) || $container.data('loaded')) {
            console.log('Component already loading or loaded:', componentName);
            return;
        }

        // Mark as loading
        loadingComponents.add(componentName);
        $container.addClass('loading');

        // First, load the component's assets (JS/CSS), then load the HTML
        loadComponentAssets(componentName)
            .then(() => {
                return $.get(url);
            })
            .then((response) => {
                console.log(`Response received for ${componentName}:`, {
                    success: response.success,
                    hasHtml: !!response.html,
                    htmlLength: response.html ? response.html.length : 0,
                    htmlPreview: response.html
                        ? response.html.substring(0, 200) + '...'
                        : 'No HTML',
                });

                if (response.success && response.html) {
                    $content.html(response.html);

                    console.log(
                        `HTML injected for ${componentName}, content div:`,
                        $content[0]
                    );
                    console.log(
                        `Table element after injection:`,
                        $content.find('#ReviewListTable').length
                    );

                    // Mark as loaded to prevent reloading
                    $container.attr('data-loaded', 'true');
                    $container.find('.component-content').addClass('loaded');

                    // Smooth fade transition
                    $skeleton.fadeOut(300, function () {
                        $content.show().addClass('loaded');

                        // Initialize the component using ABP's widget system
                        initializeABPWidget(componentName, $container);

                        // Trigger component-specific initialization (fallback)
                        initializeComponent(componentName, $content);

                        console.log(
                            `Triggering componentLoaded event for: ${componentName}`
                        );
                        $(document).trigger('componentLoaded', {
                            component: componentName,
                            container: $container,
                            content: $content,
                        });
                    });
                } else {
                    showError(
                        $container,
                        response.error || 'Failed to load component'
                    );
                }
            })
            .catch((error) => {
                let errorMessage = 'Network error occurred';
                if (error.status === 404) {
                    errorMessage = 'Component not found';
                } else if (error.status === 500) {
                    errorMessage = 'Server error occurred';
                } else if (error.responseJSON && error.responseJSON.message) {
                    errorMessage = error.responseJSON.message;
                } else if (error.message) {
                    errorMessage = error.message;
                }

                showError(
                    $container,
                    `Failed to load ${componentName}: ${errorMessage}`
                );
            })
            .finally(() => {
                // Clean up loading state
                loadingComponents.delete(componentName);
                $container.removeClass('loading');
            });
    }

    function showError($container, message) {
        const $skeleton = $container.find('.skeleton-loader');
        const $content = $container.find('.component-content');
        const componentName = $container.data('component');

        $container.addClass('error');

        $content.html(`
            <div class="alert alert-warning d-flex align-items-center">
                <i class="fa fa-exclamation-triangle me-2"></i>
                <div class="flex-grow-1">
                    <strong>Loading Error</strong><br>
                    <small>${message}</small>
                </div>
                <button class="btn btn-sm btn-outline-primary ms-2 retry-btn" 
                        data-component="${componentName}" 
                        type="button">
                    <i class="fa fa-redo me-1"></i> Retry
                </button>
            </div>
        `);

        $skeleton.fadeOut(200, function () {
            $content.show();
        });

        // Add retry functionality
        $content.find('.retry-btn').on('click', function () {
            const retryComponentName = $(this).data('component');

            // Reset the container
            $container.removeClass('error');
            $content.hide().html('');
            $skeleton.show();

            // Retry loading
            const loadUrl = $container.data('load-url');
            const applicationId = $('#DetailsViewApplicationId').val();
            const applicationFormVersionId = $(
                '#ApplicationFormVersionId'
            ).val();

            let fullUrl = loadUrl;
            if (applicationId) {
                fullUrl += `&applicationId=${applicationId}`;
            }
            if (applicationFormVersionId) {
                fullUrl += `&applicationFormVersionId=${applicationFormVersionId}`;
            }

            loadComponent($container, fullUrl, retryComponentName);
        });
    }

    function initializeComponent(componentName, $content) {
        // Initialize component-specific JavaScript
        switch (componentName) {
            case 'SummaryWidget':
                initializeSummaryWidget($content);
                break;
            case 'AssessmentResults':
                initializeAssessmentResults($content);
                break;
            case 'ProjectInfo':
                initializeProjectInfo($content);
                break;
            case 'ApplicantInfo':
                initializeApplicantInfo($content);
                break;
            case 'ReviewList':
                initializeReviewList($content);
                break;
            case 'PaymentInfo':
                initializePaymentInfo($content);
                break;
            case 'HistoryWidget':
                initializeHistoryWidget($content);
                break;
            case 'ApplicationStatusWidget':
                initializeApplicationStatusWidget($content);
                break;
            case 'ApplicationAttachments':
                initializeApplicationAttachments($content);
                break;
            case 'CustomTabWidget':
                initializeCustomTabWidget($content);
                break;
            case 'WorksheetInstanceWidget':
                initializeWorksheetInstanceWidget($content);
                break;
            default:
                console.log(
                    `Component ${componentName} loaded - no specific initialization needed`
                );
        }
    }

    function initializeSummaryWidget($content) {
        console.log('Initializing Summary Widget component for lazy loading');

        setTimeout(() => {
            if (typeof window.initializeSummaryWidget === 'function') {
                window.initializeSummaryWidget($content);
            } else {
                console.error('initializeSummaryWidget function not available');
            }
        }, 300);
    }

    function initializePaymentInfo($content) {
        console.log('Initializing Payment Info component');
    }

    function initializeAssessmentResults($content) {
        console.log('Initializing Assessment Results component');
        initCustomFieldCurrencies();
    }

    function initializeProjectInfo($content) {
        console.log('Initializing Project Info component');
    }

    function initializeApplicantInfo($content) {
        console.log('Initializing Applicant Info component for lazy loading');

        setTimeout(() => {
            if (typeof window.initializeApplicantInfo === 'function') {
                window.initializeApplicantInfo($content);
            } else {
                console.error('initializeApplicantInfo function not available');
            }
        }, 500);
    }

    function initializeReviewList($content) {
        console.log('Initializing ReviewList table component');

        setTimeout(() => {
            if (typeof window.initializeReviewListTable === 'function') {
                window.initializeReviewListTable();
            } else {
                console.error(
                    'initializeReviewListTable function not available'
                );
            }
        }, 300);
    }

    function initializeHistoryWidget($content) {
        console.log('Initializing History Widget component for lazy loading');

        setTimeout(() => {
            if (typeof window.initializeHistoryWidget === 'function') {
                window.initializeHistoryWidget($content);
            } else {
                console.error('initializeHistoryWidget function not available');
            }
        }, 300);
    }

    function initializeApplicationStatusWidget($content) {
        console.log(
            'Initializing Application Status Widget component for lazy loading'
        );

        setTimeout(() => {
            if (
                typeof window.initializeApplicationStatusWidget === 'function'
            ) {
                window.initializeApplicationStatusWidget($content);
            } else {
                console.log(
                    'ApplicationStatusWidget loaded - no specific initialization function found'
                );
            }
        }, 300);
    }

    function initializeApplicationAttachments($content) {
        console.log(
            'Initializing Application Attachments component for lazy loading'
        );

        setTimeout(() => {
            if (typeof window.initializeApplicationAttachments === 'function') {
                window.initializeApplicationAttachments($content);
            } else {
                console.error(
                    'initializeApplicationAttachments function not available'
                );
            }
        }, 300);
    }

    function initializeCustomTabWidget($content) {
        console.log(
            'Initializing Custom Tab Widget component for lazy loading'
        );

        setTimeout(() => {
            if (typeof window.initializeCustomTabWidget === 'function') {
                const $container = $content.closest(
                    '.lazy-component-container'
                );
                const tabName = $container.data('tab') || 'custom-tab';

                console.log(
                    `Calling initializeCustomTabWidget with container:`,
                    $content,
                    `tabName:`,
                    tabName
                );
                window.initializeCustomTabWidget($content, tabName);
            } else {
                console.error(
                    'initializeCustomTabWidget function not available'
                );
            }
        }, 300);
    }

    function initializeWorksheetInstanceWidget($content) {
        console.log(
            'Initializing Worksheet Instance Widget component for lazy loading'
        );

        setTimeout(() => {
            if (
                typeof window.initializeWorksheetInstanceWidget === 'function'
            ) {
                window.initializeWorksheetInstanceWidget($content);
            } else {
                console.error(
                    'initializeWorksheetInstanceWidget function not available'
                );
            }
        }, 300);
    }

    /****lazy loading end *****/

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

    function renderSubmission() {
        if (renderFormIoToHtml == 'False' || hasRenderedHtml == 'False') {
            getSubmission();
        } else {
            $('.spinner-grow').hide();
            addEventListeners();
        }
    }

    function waitFor(conditionFunction) {
        const poll = (resolve) => {
            if (conditionFunction()) resolve();
            else setTimeout((_) => poll(resolve), 400);
        };

        return new Promise(poll);
    }

    async function getSubmission() {
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

            await Formio.createForm(
                document.getElementById('formio'),
                formSchema,
                {
                    readOnly: true,
                    renderMode: 'form',
                    flatten: true,
                }
            ).then(function (form) {
                handleForm(form, submissionData);
            });
        } catch (error) {
            console.error(error);
        }
    }

    function handleForm(form, submission) {
        form.submission = submission;
        form.resetValue();
        form.refresh();
        form.on('render', addEventListeners);

        waitFor(() => isFormChanging(form)).then(() => {
            setTimeout(storeRenderedHtml, 2000);
        });
    }

    function isFormChanging(form) {
        return form.changing === false;
    }

    async function storeRenderedHtml() {
        if (renderFormIoToHtml == 'False') {
            return;
        }
        let innerHTML = document.getElementById('formio').innerHTML;
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
    function addEventListeners() {
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
        return document.querySelectorAll(
            '.card-header:not(.card-body .card-header)'
        );
    }

    // Get all card bodies
    function getCardBodies() {
        return document.querySelectorAll(
            '.card-body:not(.card-body .card-body)'
        );
    }

    // Hide all card bodies initially
    function hideAllCardBodies(cardBodies) {
        cardBodies.forEach((body) => body.classList.add('hidden'));
    }

    // Handle the card header click event
    function onCardHeaderClick(clickedHeader, cardHeaders) {
        const clickedCardBody = getNextCardBody(clickedHeader);
        const isVisible = toggleCardBodyVisibility(clickedCardBody);
        toggleHeaderActiveClass(clickedHeader, isVisible);
        hideOtherCardBodies(clickedHeader, cardHeaders);
    }

    // Get the next sibling card body
    function getNextCardBody(header) {
        return header.nextElementSibling;
    }

    // Toggle visibility of the card body
    function toggleCardBodyVisibility(cardBody) {
        return !cardBody.classList.toggle('hidden');
    }

    // Toggle active class for the header
    function toggleHeaderActiveClass(header, isVisible) {
        header.classList.toggle('custom-active', isVisible);
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

    $('#assessment_upload_btn').click(function () {
        $('#assessment_upload').trigger('click');
    });

    $('#application_attachment_upload_btn').click(function () {
        $('#application_attachment_upload').trigger('click');
    });

    function debounce(func, wait) {
        let timeout;
        return function (...args) {
            const context = this;
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(context, args), wait);
        };
    }

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

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
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
        let newTab = window.open('', '_blank');

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

        head.appendChild(jqueryScript);
        head.appendChild(formioScript);
        head.appendChild(bootstrapCSS);
        head.appendChild(formioCSS);

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
        let newTab = window.open('', '_blank');
        newTab.document.write('<html><head><title>Print</title>');
        newTab.document.write('<script src="/libs/jquery/jquery.js"></script>');
        newTab.document.write(
            '<link rel="stylesheet" href="/libs/bootstrap-4/dist/css/bootstrap.min.css">'
        );
        newTab.document.write(
            '<link rel="stylesheet" href="/Pages/GrantApplications/ScoresheetPrint.css">'
        );
        newTab.document.write('</head><body>');
        newTab.document.write(assessmentScoresheet);
        newTab.document.write('</body></html>');
        newTab.onload = function () {
            let script = newTab.document.createElement('script');
            script.src = '/Pages/GrantApplications/loadScoresheetPrint.js';
            script.onload = function () {
                newTab.executeOperations();
            };

            newTab.document.head.appendChild(script);
        };

        newTab.document.close();
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

    window.addEventListener('resize', windowResize);

    window.DetailsV2Debug = {
        loadedTabs: () => loadedTabs,
        loadingComponents: () => loadingComponents,
        forceLoadTab: (tabName) => loadTabComponents(tabName),
    };
});

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
                $('#' + $(this).data('linkscounttag')).html(
                    $(this).data('count')
                );
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
    let numericValue = parseFloat(originalValue.replace(/,/g, ''));

    let minValue = parseFloat(input.attr('data-min'));
    let maxValue = parseFloat(input.attr('data-max'));

    if (isNaN(numericValue)) {
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
