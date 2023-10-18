(function () {
    abp.widgets.ApplicationActionWidget = function ($wrapper) {
        let widgetManager = $wrapper.data('abp-widget-manager');
        let $actionButtons = $wrapper.find('.details-dropdown-action');
        let widgetAppId = decodeURIComponent(document.querySelector("#DetailsViewApplicationId").value);

        function init (filters) {
            $actionButtons.each(function () {
                let $this = $(this);
                $this.on("click", function () {
                    $(this).buttonBusy();
                    $('#ApplicationActionDropdown .dropdown-toggle').buttonBusy();
                    let triggerAction = $(this).data('appAction');
                    unity.grantManager.grantApplications.grantApplication
                        .triggerAction(widgetAppId, triggerAction, {})
                        .then(function (result) {
                            // TODO: PUBSUB & REFRESH WIDGET
                            widgetManager.refresh();
                            abp.notify.success(
                                l(`Enum:GrantApplicationAction.${triggerAction}`),
                                "Application Status Changed"
                            );
                        });
                });
            });
        };

        function getFilters() {
            return {
                applicationId: widgetAppId
            };
        }

        return {
            init: init,
            getFilters: getFilters
        };
    };
})();