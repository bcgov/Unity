using Unity.Flex.Localization;
using Unity.Notifications.Localization;
using Unity.Payments.Localization;
using Volo.Abp.Features;
using Volo.Abp.Localization;
using Volo.Abp.Validation.StringValues;

namespace Unity.GrantManager
{
    public class GrantManagerFeaturesDefinitionProvider : FeatureDefinitionProvider
    {
        public override void Define(IFeatureDefinitionContext context)
        {
            var myGroup = context.AddGroup("GrantManager");

            myGroup.AddFeature("Unity.Payments", 
                defaultValue: "false",
                displayName: LocalizableString
                                .Create<PaymentsResource>("Payments"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.Notifications",
                defaultValue: "false",
                displayName: LocalizableString
                                .Create<NotificationsResource>("Allow Notifications"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.Flex",
                defaultValue: "false",
                   displayName: LocalizableString
                                .Create<FlexResource>("Flex"),
                valueType: new ToggleStringValueType());
        }
    }
}
