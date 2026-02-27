$(document).ready(function () {
    // Initialize page
    initializeApplicantDetailsPage();

    // Handle breadcrumb back button
    $('#goBackToApplicants').on('click', function () {
        window.location.href = '/GrantApplicants';
    });

    // Handle tab switching animations
    $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        let targetTab = $(e.target).attr('data-bs-target');
        $(targetTab).addClass('fade-in-load visible');
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
    if (!detailsTab) return;
    const tabNav = detailsTab.querySelector('ul.nav-tabs, ul.nav');
    const tabContent = detailsTab.querySelector('.tab-content');
    if (!tabNav || !tabContent) return;
    const baseOffset = 175;
    const totalOffset = baseOffset + tabNav.clientHeight;
    tabContent.style.height = `calc(100vh - ${totalOffset}px)`;
    tabContent.style.overflowY = 'auto';
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




