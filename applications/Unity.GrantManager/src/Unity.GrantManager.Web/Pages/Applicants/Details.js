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
        $(document).find(targetTab).addClass('fade-in-load visible');
    });

    // Handle resizable divider
    initializeResizableDivider();
});

function initializeApplicantDetailsPage() {
    // Hide loading spinner and show content
    setTimeout(function () {
        $('#main-loading').fadeOut(300, function () {
            $('.fade-in-load').addClass('visible');
        });
    }, 500);
}

function initializeResizableDivider() {
    const divider = document.getElementById('main-divider');
    const leftPanel = document.getElementById('main-left');
    const rightPanel = document.getElementById('main-right');

    if (!divider || !leftPanel || !rightPanel) return;

    let isResizing = false;

    divider.addEventListener('mousedown', function (e) {
        isResizing = true;
        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', handleMouseUp);

        // Add visual feedback
        document.body.style.cursor = 'col-resize';
        divider.style.backgroundColor = '#007bff';
    });

    function handleMouseMove(e) {
        if (!isResizing) return;

        const containerRect = document.getElementById('main-container').getBoundingClientRect();
        const mouseX = e.clientX;
        const containerLeft = containerRect.left;
        const containerWidth = containerRect.width;

        // Calculate percentage
        const leftPercentage = ((mouseX - containerLeft) / containerWidth) * 100;
        const rightPercentage = 100 - leftPercentage;

        // Set minimum and maximum widths
        if (leftPercentage >= 20 && leftPercentage <= 80) {
            leftPanel.style.width = leftPercentage + '%';
            rightPanel.style.width = rightPercentage + '%';
        }
    }

    function handleMouseUp() {
        isResizing = false;
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);

        // Remove visual feedback
        document.body.style.cursor = '';
        divider.style.backgroundColor = '#ccc';
    }
}




