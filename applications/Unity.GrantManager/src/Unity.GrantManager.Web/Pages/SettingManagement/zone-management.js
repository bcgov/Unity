(function ($) {
    abp.modals = abp.modals || {};
    abp.modals.ZoneManagement = function () {

        // Checks and updates parents when a change occurs
        function checkParents($tab, $element, className) {
            let parentName = $element.closest(className).attr('data-parent-name');
            if (!parentName) return;

            updateChildCheckboxes($tab, parentName);
            updateParentCheckboxes($tab, parentName);
        }

        // Updates child checkboxes based on parent
        function updateChildCheckboxes($tab, parentName) {
            $tab.find('.custom-checkbox')
                .filter(`[data-parent-name="${parentName}"]`)
                .find('input[type="checkbox"]:disabled')
                .each(function () {
                    let $child = $(this);
                    $child.prop('checked', true);
                    checkChildren($tab, $child, true);
                });
        }

        // Updates parent checkboxes based on child
        function updateParentCheckboxes($tab, parentName) {
            $tab.find('.custom-checkbox')
                .filter(`[data-zone-name="${parentName}"]`)
                .find('input[type="checkbox"]')
                .each(function () {
                    let $parent = $(this);
                    $parent.prop('checked', true);
                    checkParents($tab, $parent, '.custom-checkbox');
                });
        }

        // Updates children based on checkbox state
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

        // Handles checkbox changes
        function handleCheckboxChange($tab, $checkBox) {
            let $checkState = $checkBox.is(':checked');
            if ($checkState) {
                checkParents($tab, $checkBox, '.custom-checkbox');
            }
            checkChildren($tab, $checkBox, $checkState);
        }

        // Initializes scrollbars
        function initializeScrollbars() {
            $('.custom-scroll-content').mCustomScrollbar({ theme: 'minimal-dark' });
            $('.custom-scroll-container > .col-4').mCustomScrollbar({ theme: 'minimal-dark' });
        }

        // Initializes the cancel button
        function initializeCancelButton() {
            $('#btn-cancel').on('click', function (e) {
                e.preventDefault();
                location.href = '/ApplicationForms';
            });
        }

        // Initializes all tab panes
        function initializeTabPane($el) {
            $el.find('.tab-pane').each(function () {
                let $tab = $(this);
                initializeTabCheckboxes($tab);
                initializeTabFormControls($tab);
            });
        }

        // Event handler for checkbox changes
        function handleTabCheckboxChange() {
            const $checkBox = $(this);
            const $tab = $checkBox.closest('.tab-pane');
            handleCheckboxChange($tab, $checkBox);
        }

        // Event handler for form control changes
        function handleFormControlChange() {
            const $element = $(this);
            const $tab = $element.closest('.tab-pane');
            handleFormControlChangeHelper($tab, $element);
        }

        // Helper function for form control change logic
        function handleFormControlChangeHelper($tab, $element) {
            checkParents($tab, $element, '.form-group');
        }

        // Initializes checkboxes inside each tab
        function initializeTabCheckboxes($tab) {
            $tab.find('input[type="checkbox"]').each(function () {
                $(this).change(handleTabCheckboxChange);
            });
        }

        // Initializes form controls inside each tab
        function initializeTabFormControls($tab) {
            $tab.find('.form-control').each(function () {
                $(this).change(handleFormControlChange);
            });
        }

        // This is the method that initializes the modal components
        this.initDom = function ($el) {
            initializeTabPane($el);
            initializeScrollbars();
            initializeCancelButton();
        };

        $('#ZoneManagementForm').on('submit', function (e) {
            e.preventDefault();
            $.ajax({
                url: $(this).attr('action'),
                method: $(this).attr('method'),
                data: $(this).serialize(),
                success: function () {
                    abp.notify.success('Configurations have been successfully saved.');
                },
                error: function () {
                    abp.notify.error('An error occurred while saving configurations.');
                }
            });
        });
    };

})(jQuery);
