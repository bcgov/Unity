$(document).ready(function () {
    // Initialize page
    initializeApplicantDetailsPage();
    scheduleInitialLayoutPasses();

    window.addEventListener('applicant-submissions-layout-changed', function () {
        applyTabHeightOffset();
        debouncedResizeAwareDataTables();
    });
    window.addEventListener('applicant-addresses-layout-changed', function () {
        applyTabHeightOffset();
        debouncedResizeAwareDataTables();
    });

    // Handle breadcrumb back button
    $('#goBackToApplicants').on('click', function () {
        window.location.href = '/GrantApplicants';
    });

    // Handle tab switching animations
    $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        let targetTab = $(e.target).attr('data-bs-target');
        $(targetTab).addClass('fade-in-load visible');
        applyTabHeightOffset();
    });

    // Add event listeners for tab clicks to adjust DataTables
    $('#detailsTab li').on('click', function () {
        debouncedAdjustTables('detailsTab');
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

    resizeSubmissionsScrollBody();
    resizeApplicantAddressesScrollBodies();

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

function scheduleInitialLayoutPasses() {
    [0, 120, 300, 650, 1100].forEach((delay) => {
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

function resizeApplicantAddressesScrollBodies() {
    const addressesPane = document.getElementById('nav-addresses');
    if (!addressesPane) {
        return;
    }

    resizeScrollBodyWithinContainer('ApplicantContactsTable_wrapper', addressesPane);
    resizeScrollBodyWithinContainer('ApplicantAddressesTable_wrapper', addressesPane);
}

function resizeScrollBodyWithinContainer(wrapperId, containerElement) {
    const tableWrapper = document.getElementById(wrapperId);
    const scrollBody = tableWrapper?.querySelector('.dt-scroll-body');
    if (!tableWrapper || !scrollBody || !containerElement) {
        return;
    }

    const style = getComputedStyle(tableWrapper);
    if (style.display === 'none' || style.visibility === 'hidden') {
        return;
    }

    const containerRect = containerElement.getBoundingClientRect();
    const wrapperRect = tableWrapper.getBoundingClientRect();
    const availableWrapperHeight = Math.floor(containerRect.bottom - wrapperRect.top - 8);

    if (availableWrapperHeight <= 0) {
        return;
    }

    let reservedHeight = 0;
    Array.from(tableWrapper.children).forEach((child) => {
        const childRect = child.getBoundingClientRect();

        if (child.contains(scrollBody)) {
            const bodyRect = scrollBody.getBoundingClientRect();
            reservedHeight += Math.max(0, Math.ceil(childRect.height - bodyRect.height));
            return;
        }

        reservedHeight += Math.ceil(childRect.height);
    });

    const maxBodyHeight = Math.max(120, availableWrapperHeight - reservedHeight - 4);
    scrollBody.style.maxHeight = `${maxBodyHeight}px`;
    scrollBody.style.overflowY = 'auto';
    scrollBody.style.overflowX = 'auto';
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




