using Unity.Notifications.Localization;
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

            myGroup.AddFeature("Unity.Notifications", 
                defaultValue: "false",
                displayName: LocalizableString.Create<NotificationsResource>("Allow Notifications"),
                valueType: new ToggleStringValueType());

        }
    }
}