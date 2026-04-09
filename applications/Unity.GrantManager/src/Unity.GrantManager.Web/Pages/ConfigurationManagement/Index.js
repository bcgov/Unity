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
    }

    function menuItemClick(e) {
        const clickedItem = $(e.currentTarget);
        const targetId = clickedItem.data('target');

        // Update active menu item
        menuItems.removeClass('active');
        clickedItem.addClass('active');

        // Toggle content sections
        configSections.addClass('hide');
        $('#' + targetId).removeClass('hide');

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
