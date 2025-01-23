var abp = abp || {};
(function ($) {
    abp.modals = abp.modals || {};

    abp.modals.ZoneManagement = function () {
        function checkParents($tab, $element, className) {
            let parentName = $element
                .closest(className)
                .attr('data-parent-name');

            if (!parentName) {
                return;
            }

            $tab.find('.custom-checkbox')
                .filter('[data-zone-name="' + parentName + '"]')
                .find('input[type="checkbox"]')
                .each(function () {
                    let $parent = $(this);
                    $parent.prop('checked', true);
                    checkParents($tab, $parent, className);
                });
        }

        function uncheckChildren($tab, $checkBox) {
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
                    $child.prop('checked', false);
                    uncheckChildren($tab, $child);
                });
        }

        this.initDom = function ($el) {
            $el.find('.tab-pane').each(function () {
                let $tab = $(this);
                $tab.find('input[type="checkbox"]')
                    .each(function () {
                        let $checkBox = $(this);
                        $checkBox.change(function () {
                            if ($checkBox.is(':checked')) {
                                checkParents($tab, $checkBox, '.custom-checkbox')
                            } else {
                                uncheckChildren($tab, $checkBox);
                            }
                        });
                    });

                $tab.find('.form-control')
                    .each(function () {
                        let $element = $(this);
                        $element.change(function () {
                            checkParents($tab, $element, '.form-group')
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
            });
        };
    };
})(jQuery);