(function () {
    let abp = window.abp || {};
    (function ($) {
    abp.modals = abp.modals || {};
    abp.modals.ZoneManagement = function () {

        function checkParents($tab, $element, className) {
            let parentName = $element.closest(className).attr('data-parent-name');
            if (!parentName) return;

            $tab.find('.custom-checkbox')
                .filter(`[data-parent-name="${parentName}"]`)
                .find('input[type="checkbox"]:disabled')
                .each(function () {
                    let $child = $(this);
                    $child.prop('checked', true);
                    checkChildren($tab, $child, true);
                });

            $tab.find('.custom-checkbox')
                .filter(`[data-zone-name="${parentName}"]`)
                .find('input[type="checkbox"]')
                .each(function () {
                    let $parent = $(this);
                    $parent.prop('checked', true);
                    checkParents($tab, $parent, className);
                });
        }

        function checkChildren($tab, $checkBox, $checkState) {
            let zoneName = $checkBox.closest('.custom-checkbox').attr('data-zone-name');
            if (!zoneName) return;

            $tab.find('.custom-checkbox')
                .filter(`[data-parent-name="${zoneName}"]`)
                .find('input[type="checkbox"]')
                .each(function () {
                    let $child = $(this);
                    $child.prop('checked', $checkState);
                    checkChildren($tab, $child, $checkState);
                });
        }

        function handleCheckboxChange($tab, $checkBox) {
            let $checkState = $checkBox.is(':checked');
            if ($checkState) {
                checkParents($tab, $checkBox, '.custom-checkbox');
            }
            checkChildren($tab, $checkBox, $checkState);
        }

        function handleFormControlChange($tab, $element) {
            checkParents($tab, $element, '.form-group');
        }

        function initializeScrollbars() {
            $('.custom-scroll-content').mCustomScrollbar({ theme: 'minimal-dark' });
            $('.custom-scroll-container > .col-4').mCustomScrollbar({ theme: 'minimal-dark' });
        }

        function initializeCancelButton() {
            $('#btn-cancel').on('click', function (e) {
                e.preventDefault();
                location.href = '/ApplicationForms';
            });
        }

        function initializeTabPane($el) {
            $el.find('.tab-pane').each(function () {
                let $tab = $(this);

                $tab.find('input[type="checkbox"]').each(function () {
                    let $checkBox = $(this);
                    $checkBox.change(function () {
                        handleCheckboxChange($tab, $checkBox);
                    });
                });

                $tab.find('.form-control').each(function () {
                    let $element = $(this);
                    $element.change(function () {
                        handleFormControlChange($tab, $element);
                    });
                });
            });
        }

        this.initDom = function ($el) {
            initializeTabPane($el);
            $(initializeScrollbars);
            $(initializeCancelButton);
        };
    };
    })(jQuery);
})();