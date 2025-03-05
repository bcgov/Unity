var abp = abp || {};
(function ($) {
    abp.modals = abp.modals || {};

    console.log('test');

    abp.modals.ZoneManagement = function () {
        function checkParents($tab, $element, className) {
            let parentName = $element
                .closest(className)
                .attr('data-parent-name');

            if (!parentName) {
                return;
            }

            $tab.find('.custom-checkbox')
                .filter('[data-parent-name="' + parentName + '"]')
                .find('input[type="checkbox"]:disabled')
                .each(function () {
                    let $child = $(this);
                    $child.prop('checked', true);
                    checkChildren($tab, $child, true);
                });

            $tab.find('.custom-checkbox')
                .filter('[data-zone-name="' + parentName + '"]')
                .find('input[type="checkbox"]')
                .each(function () {
                    let $parent = $(this);
                    $parent.prop('checked', true);
                    checkParents($tab, $parent, className);
                });
        }

        function checkChildren($tab, $checkBox, $checkState) {
            let zoneName = $checkBox
                .closest('.custom-checkbox')
                .attr('data-zone-name');
            if (!zoneName) {
                return;
            }

            $tab.find('.custom-checkbox')
                .filter('[data-parent-name="' + zoneName + '"]')
                .find('input[type="checkbox"]')
                .each(function () {
                    let $child = $(this);
                    $child.prop('checked', $checkState);
                    checkChildren($tab, $child, $checkState);
                });
        }

        this.initDom = function ($el) {
            console.log('dom');
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


            $(function () {
                $('.custom-scroll-content').mCustomScrollbar({
                    theme: 'minimal-dark',
                });
                $('.custom-scroll-container > .col-4').mCustomScrollbar({
                    theme: 'minimal-dark',
                });

                $('#btn-cancel').on('click', function (e) {
                    e.preventDefault();
                    console.log('cancel button clicked');
                    location.href = '/ApplicationForms';
                });
            });
        };
    };
})(jQuery);
