$(document).ready(function () {
    // Initialize page
    initializeApplicantDetailsPage();
    scheduleInitialLayoutPasses();

    window.addEventListener('applicant-submissions-layout-changed', function () {
        applyTabHeightOffset();
        debouncedResizeAwareDataTables();
        scheduleDeferredLayoutPass();
    });
    window.addEventListener('applicant-addresses-layout-changed', function () {
        applyTabHeightOffset();
        debouncedResizeAwareDataTables();
        scheduleDeferredLayoutPass();
    });

    // Handle breadcrumb back button
    $('#goBackToApplicants').on('click', function () {
        window.location.href = '/GrantApplicants';
    });

    // Handle tab switching animations
    $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        let targetTab = $(e.target).attr('data-bs-target');
        $(targetTab).addClass('fade-in-load visible');
        if ($(e.target).closest('#detailsTab').length) {
            syncLeftTabScrollPosition(targetTab);
            scheduleLeftTabScrollReset(targetTab);
        }
        applyTabHeightOffset();
        scheduleDeferredLayoutPass();
    });

    // Add event listeners for tab clicks to adjust DataTables
    $('#detailsTab li').on('click', function () {
        debouncedAdjustTables('detailsTab');
        scheduleDeferredLayoutPass();
    });

    $('#myTabContent li').on('click', function () {
        debouncedAdjustTables('myTabContent');
    });

    // Handle resizable divider
    initializeResizableDivider();
});

function initializeApplicantDetailsPage() {
    // Hide loading spinner and show content
    setTimeout(function () {
        $('#main-loading').fadeOut(300, function () {
            $('.fade-in-load').addClass('visible');
            applyTabHeightOffset();
        });
    }, 500);

    // Initialize tooltips if any
    let tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Debounce utility function
function debounce(func, wait) {
    let timeout;
    return function (...args) {
        const context = this;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), wait);
    };
}

// Debounced DataTable resizing function (called during panel resize)
const debouncedResizeAwareDataTables = debounce(() => {
    $('table[data-resize-aware="true"]:visible').each(function () {
        try {
            const table = $(this).DataTable();
            table.columns.adjust().draw();
        }
        catch (error) {
            console.error('Failed to adjust DataTable columns:', error);
        }
    });
}, 15);

// Debounced function for adjusting tables in specific container when tab is clicked
const debouncedAdjustTables = debounce(adjustVisibleTablesInContainer, 15);

function adjustVisibleTablesInContainer(containerId) {
    const activeTab = $(`#${containerId} div.active`);
    const tables = activeTab.find('table[data-resize-aware="true"]:visible');

    tables.each(function () {
        try {
            const table = $(this).DataTable();
            table.columns.adjust().draw();
        }
        catch (error) {
            console.error('Failed to adjust DataTable in tab:', error);
        }
    });
}

function applyTabHeightOffset() {
    const detailsTab = document.getElementById('detailsTab');
    const mainContainer = document.getElementById('main-container');
    if (mainContainer) {
        const bottomSpacing = 12;
        const minMainHeight = 360;
        const availableMainHeight = Math.max(
            minMainHeight,
            Math.floor(window.innerHeight - mainContainer.getBoundingClientRect().top - bottomSpacing)
        );
        mainContainer.style.height = `${availableMainHeight}px`;
        mainContainer.style.overflow = 'hidden';
    }

    if (!detailsTab) return;
    const tabContent = detailsTab.querySelector('.tab-content');
    if (!tabContent) return;

    const bottomSpacing = 12;
    const minContentHeight = 220;
    const availableLeftHeight = Math.max(
        minContentHeight,
        Math.floor(window.innerHeight - tabContent.getBoundingClientRect().top - bottomSpacing)
    );

    tabContent.style.height = `${availableLeftHeight}px`;

    const activeLeftPane = tabContent.querySelector('.tab-pane.active');
    const activeLeftPaneId = activeLeftPane?.id;
    const usesInternalScroll = activeLeftPaneId === 'nav-submissions' || activeLeftPaneId === 'nav-addresses';
    tabContent.style.overflowY = usesInternalScroll ? 'hidden' : 'auto';
    if (usesInternalScroll) {
        tabContent.scrollTop = 0;
    }
    if (activeLeftPaneId === 'nav-addresses' && activeLeftPane) {
        activeLeftPane.style.height = '100%';
        activeLeftPane.style.minHeight = '0';
        activeLeftPane.style.overflow = 'hidden';
        resizeApplicantAddressesPane(activeLeftPane);
    }

    resizeSubmissionsScrollBody();
    const rightTabContent = document.getElementById('myTabContent');
    if (rightTabContent) {
        const availableRightHeight = Math.max(
            minContentHeight,
            Math.floor(window.innerHeight - rightTabContent.getBoundingClientRect().top - bottomSpacing)
        );
        rightTabContent.style.height = `${availableRightHeight}px`;
        rightTabContent.style.overflowY = 'auto';
    }
}

function syncLeftTabScrollPosition(targetTabSelector) {
    const leftTabContent = document.querySelector('#detailsTab .tab-content');
    if (!leftTabContent) {
        return;
    }

    leftTabContent.scrollTop = 0;

    const targetPane = targetTabSelector ? document.querySelector(targetTabSelector) : null;
    if (targetPane) {
        targetPane.scrollTop = 0;
    }
}

function scheduleLeftTabScrollReset(targetTabSelector) {
    [0, 40, 120, 220].forEach((delay) => {
        setTimeout(() => {
            const leftTabContent = document.querySelector('#detailsTab .tab-content');
            if (leftTabContent) {
                leftTabContent.scrollTop = 0;
            }

            const targetPane = targetTabSelector ? document.querySelector(targetTabSelector) : null;
            if (targetPane) {
                targetPane.scrollTop = 0;
                targetPane.querySelectorAll('div, section, form').forEach((element) => {
                    if (element.scrollTop > 0) {
                        element.scrollTop = 0;
                    }
                });
            }
        }, delay);
    });
}

function scheduleInitialLayoutPasses() {
    [0, 120, 300, 650, 1100].forEach((delay) => {
        setTimeout(() => {
            applyTabHeightOffset();
            debouncedResizeAwareDataTables();
        }, delay);
    });
}

function scheduleDeferredLayoutPass() {
    [0, 30, 120, 250].forEach((delay) => {
        setTimeout(() => {
            applyTabHeightOffset();
            debouncedResizeAwareDataTables();
        }, delay);
    });
}

function resizeSubmissionsScrollBody() {
    const submissionsWidget = document.querySelector(
        '#nav-submissions .applicant-submissions-widget'
    );
    const submissionsTableWrapper = document.getElementById(
        'ApplicantSubmissionsTable_wrapper'
    );
    const submissionsScrollBody = submissionsTableWrapper?.querySelector(
        '.dt-scroll-body'
    );

    if (!submissionsWidget || !submissionsTableWrapper || !submissionsScrollBody) {
        return;
    }

    const widgetRect = submissionsWidget.getBoundingClientRect();
    const wrapperRect = submissionsTableWrapper.getBoundingClientRect();
    const availableWrapperHeight = Math.floor(widgetRect.bottom - wrapperRect.top - 8);

    if (availableWrapperHeight <= 0) {
        return;
    }

    let reservedHeight = 0;
    Array.from(submissionsTableWrapper.children).forEach((child) => {
        const childRect = child.getBoundingClientRect();

        if (child.contains(submissionsScrollBody)) {
            const bodyRect = submissionsScrollBody.getBoundingClientRect();
            reservedHeight += Math.max(0, Math.ceil(childRect.height - bodyRect.height));
            return;
        }

        reservedHeight += Math.ceil(childRect.height);
    });

    const maxBodyHeight = Math.max(140, availableWrapperHeight - reservedHeight - 4);
    submissionsScrollBody.style.maxHeight = `${maxBodyHeight}px`;
    submissionsScrollBody.style.overflowY = 'auto';
    submissionsScrollBody.style.overflowX = 'auto';
}

function resizeApplicantAddressesPane(addressesPane) {
    const addressesWidget = addressesPane.querySelector('.applicant-addresses-widget');
    const addressesForm = addressesPane.querySelector('.applicant-organization-info');
    const innerTabContent = addressesPane.querySelector('.applicant-organization-info > .tab-content');
    const activeSubPane = innerTabContent?.querySelector('.tab-pane.active');

    if (!addressesWidget || !addressesForm || !innerTabContent || !activeSubPane) {
        return;
    }

    const availableWidgetHeight = Math.max(
        220,
        Math.floor(addressesPane.getBoundingClientRect().height - 8)
    );
    addressesWidget.style.height = `${availableWidgetHeight}px`;
    addressesWidget.style.minHeight = '0';

    const innerTabContentTop = innerTabContent.getBoundingClientRect().top;
    const widgetBottom = addressesWidget.getBoundingClientRect().bottom;
    const availableInnerTabHeight = Math.max(
        140,
        Math.floor(widgetBottom - innerTabContentTop - 8)
    );

    addressesForm.style.height = `${availableWidgetHeight}px`;
    addressesForm.style.minHeight = '0';
    innerTabContent.style.height = `${availableInnerTabHeight}px`;
    innerTabContent.style.minHeight = '0';
    activeSubPane.style.height = `${availableInnerTabHeight}px`;
    activeSubPane.style.minHeight = '0';
    activeSubPane.style.overflowY = 'auto';
    activeSubPane.style.overflowX = 'hidden';
}

function initializeResizableDivider() {
    const divider = document.getElementById('main-divider');
    const leftPanel = document.getElementById('main-left');
    const rightPanel = document.getElementById('main-right');
    const container = document.getElementById('main-container');
    const storageKey = 'applicantDetailsLeftWidth';
    const minPercentage = 20;
    const maxPercentage = 80;

    if (!divider || !leftPanel || !rightPanel || !container) return;

    let isResizing = false;

    const handleMouseMove = (e) => {
        if (!isResizing) return;

        const containerRect = container.getBoundingClientRect();
        const mouseX = e.clientX;
        const containerLeft = containerRect.left;
        const containerWidth = containerRect.width;

        // Calculate percentage
        const leftPercentage = ((mouseX - containerLeft) / containerWidth) * 100;
        const rightPercentage = 100 - leftPercentage;

        // Set minimum and maximum widths
        if (leftPercentage >= minPercentage && leftPercentage <= maxPercentage) {
            leftPanel.style.width = leftPercentage + '%';
            rightPanel.style.width = rightPercentage + '%';

            // Resize DataTables during panel resize
            debouncedResizeAwareDataTables();
            applyTabHeightOffset();
            localStorage.setItem(storageKey, leftPercentage.toString());
        }
    };

    const handleMouseUp = () => {
        isResizing = false;
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);

        // Remove visual feedback
        document.body.style.cursor = '';
    };

    const restoreDividerPosition = () => {
        const savedPercentage = parseFloat(localStorage.getItem(storageKey));
        if (isNaN(savedPercentage)) {
            return;
        }

        const clampedPercentage = Math.min(Math.max(savedPercentage, minPercentage), maxPercentage);
        leftPanel.style.width = clampedPercentage + '%';
        rightPanel.style.width = (100 - clampedPercentage) + '%';
    };

    restoreDividerPosition();

    divider.addEventListener('mousedown', function () {
        isResizing = true;
        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', handleMouseUp);

        // Add visual feedback
        document.body.style.cursor = 'col-resize';
    });

    window.addEventListener('resize', restoreDividerPosition);
    window.addEventListener('resize', applyTabHeightOffset);
}




