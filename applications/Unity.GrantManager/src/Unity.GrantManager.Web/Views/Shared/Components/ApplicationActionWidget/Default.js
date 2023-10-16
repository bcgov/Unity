const detailsActionBarAppId = decodeURIComponent(document.querySelector("#DetailsViewApplicationId").value);

$(function () {

    // TODO: REAPPLY ON REFRESH - THIS IS CURRENTLY BROKEN
    // NOTE: ENSURE DEBOUNCE
    $('.details-dropdown-action').each(function () {
        let $this = $(this);
        $this.on("click", function () {
            let triggerAction = $(this).data("appAction");
            console.log(triggerAction); // TODO: Remove after debugging
            executeApplicationAction(detailsActionBarAppId, triggerAction);
        });

    });
});

function executeApplicationAction(assessmentId, triggerAction) {
    unity.grantManager.grantApplications.grantApplication.triggerAction(assessmentId, triggerAction, {})
        .then(function (result) {
            // TODO: PUBSUB & REFRESH WIDGET
            abp.notify.success(
                l(`Enum:GrantApplicationAction.${triggerAction}`),
                "Application Status Changed"
            );
            applicationActionManager.refresh();
        });
}

let applicationActionManager = new abp.WidgetManager({
    wrapper: '#DetailsActionBarStart', // TODO: Find way to refrence self
    filterCallback: function () {
        return {
            'applicationId': detailsActionBarAppId
        };
    }
});