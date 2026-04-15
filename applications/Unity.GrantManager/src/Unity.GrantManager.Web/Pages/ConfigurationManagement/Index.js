$(function () {
    const menuItems = $('#ConfigurationManagementSideMenu .nav-item');
    const configSections = $('.config-section');

    init();

    function init() {
        menuItems.on('click', menuItemClick);

        // Adjust DataTables when Payments internal tabs are shown
        $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function () {
            adjustDataTables();
        });

        // Auto-select the first visible menu item
        const firstMenuItem = menuItems.first();
        if (firstMenuItem.length) {
            firstMenuItem.addClass('active');
            const targetId = firstMenuItem.data('target');
            $('#' + targetId).removeClass('hide');
        }

        // Auto-activate the first Payment tab (if rendered)
        const firstPaymentTab = $('#payments-nav-tab .nav-link').first();
        if (firstPaymentTab.length) {
            firstPaymentTab.addClass('active');
            const targetPane = $(firstPaymentTab.data('bs-target'));
            if (targetPane.length) {
                targetPane.addClass('show active');
            }
        }
    }

    const splitRestoreMap = {
        'custom-fields-div': initResizableSplit('worksheet-split-container', 'worksheet-left', 'worksheet-divider', 'worksheet-right', 'worksheetSplitWidth'),
        'scoresheets-div': initResizableSplit('scoresheet-split-container', 'scoresheet-left', 'scoresheet-divider', 'scoresheet-right', 'scoresheetSplitWidth')
    };

    function menuItemClick(e) {
        const clickedItem = $(e.currentTarget);
        const targetId = clickedItem.data('target');

        // Update active menu item
        menuItems.removeClass('active');
        clickedItem.addClass('active');

        // Toggle content sections
        configSections.addClass('hide');
        $('#' + targetId).removeClass('hide');

        // Restore split widths now that the section is visible
        if (splitRestoreMap[targetId]) {
            splitRestoreMap[targetId]();
        }

        adjustDataTables();
    }

    function adjustDataTables() {
        // Adjust any visible DataTables after tab/section switch
        if (typeof accountCodingDataTable !== 'undefined' && accountCodingDataTable) {
            accountCodingDataTable.columns.adjust().draw();
        }
        if (typeof paymentSettingsDataTable !== 'undefined' && paymentSettingsDataTable) {
            paymentSettingsDataTable.columns.adjust().draw();
        }
        $.fn.dataTable.tables({ visible: true, api: true }).columns.adjust();
    }
});
function initResizableSplit(containerId, leftId, dividerId, rightId, storageKey) {
    const container = document.getElementById(containerId);
    const leftDiv = document.getElementById(leftId);
    const divider = document.getElementById(dividerId);
    const rightDiv = document.getElementById(rightId);

    if (!container || !leftDiv || !divider || !rightDiv) {
        return null;
    }

    function restoreSavedWidth() {
        const saved = localStorage.getItem(storageKey);
        if (saved && container.clientWidth > 0) {
            const containerWidth = container.clientWidth;
            const percentage = Number.parseFloat(saved);
            const leftWidth = containerWidth * percentage;
            const rightWidth = containerWidth - leftWidth - divider.offsetWidth;
            leftDiv.style.width = leftWidth + 'px';
            rightDiv.style.width = rightWidth + 'px';
        }
    }

    function resize(e) {
        const containerRect = container.getBoundingClientRect();
        const leftWidth = e.clientX - containerRect.left;
        const rightWidth = containerRect.right - e.clientX - divider.offsetWidth;

        if (leftWidth > 200 && rightWidth > 200) {
            leftDiv.style.width = leftWidth + 'px';
            rightDiv.style.width = rightWidth + 'px';

            // Save as percentage for responsive recalculation
            localStorage.setItem(storageKey, (leftWidth / container.clientWidth).toString());
        }
    }

    divider.addEventListener('mousedown', function (e) {
        e.preventDefault();

        function stopResize() {
            document.removeEventListener('mousemove', resize);
            document.removeEventListener('mouseup', stopResize);
        }

        document.addEventListener('mousemove', resize);
        document.addEventListener('mouseup', stopResize);
    });

    // Recalculate on window resize (guard prevents no-op on hidden sections)
    globalThis.addEventListener('resize', restoreSavedWidth);

    return restoreSavedWidth;
}
