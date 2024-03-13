using Unity.GrantManager.Localization;
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

            myGroup.AddFeature("GrantManager.Payments", 
                defaultValue: "false",
                displayName: LocalizableString
                                 .Create<PaymentsResource>("Payments"),
                valueType: new ToggleStringValueType());            
        }
    }
}
