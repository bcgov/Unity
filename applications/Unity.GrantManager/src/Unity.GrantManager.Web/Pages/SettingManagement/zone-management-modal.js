var abp = abp || {};
(function ($) {
    abp.modals = abp.modals || {};

    abp.modals.ZoneManagement = function () {
        function checkParents($tab, $checkBox) {
            let parentName = $checkBox
                .closest('.custom-checkbox')
                .attr('data-parent-name');
            if (!parentName) {
                return;
            }

            $tab.find('.custom-checkbox')
                .filter('[data-zone-name="' + parentName + '"]')
                .find('input[type="checkbox"]')
                .each(function () {
                    var $parent = $(this);
                    $parent.prop('checked', true);
                    checkParents($tab, $parent);
                });
        }

        function uncheckChildren($tab, $checkBox) {
            var zoneName = $checkBox
                .closest('.custom-checkbox')
                .attr('data-zone-name');
            if (!zoneName) {
                return;
            }

            $tab.find('.custom-checkbox')
                .filter('[data-zone-name="' + zoneName + '"]')
                .find('input[type="checkbox"]')
                .each(function () {
                    var $child = $(this);
                    $child.prop('checked', false);
                    uncheckChildren($tab, $child);
                });
        }

        this.initDom = function ($el) {
            $el.find('input[type="checkbox"]')
                .not('[name="SelectAllInThisTab"]')
                .each(function () {
                    var $checkBox = $(this);
                    $checkBox.change(function () {
                        if ($checkBox.is(':checked')) {
                            checkParents($el, $checkBox);
                        } else {
                            uncheckChildren($el, $checkBox);
                        }
                    });
                });

            var $form = $("#ZoneManagementForm");
            var $submitButton = $form.find("button[type='submit']")
            if ($submitButton) {
                $submitButton.click(function (e) {
                    e.preventDefault();

                    if (!$form.find("input:checked").length > 0) {
                        abp.message.confirm("All good")
                            .then(function (confirmed) {
                                if (confirmed) {
                                    $form.submit();
                                }
                            });
                    }
                    else {
                        $form.submit();
                    }
                });
            }
        }
    };
})(jQuery);