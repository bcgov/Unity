using Unity.Flex.Localization;
using Unity.GrantManager.Localization;
using Unity.Modules.Shared.Specializations;
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
            var defaultValue = "false";

            myGroup.AddFeature("Unity.Payments",
                defaultValue: defaultValue,
                displayName: LocalizableString
                                .Create<PaymentsResource>("Payments"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.Notifications",
                defaultValue: defaultValue,
                displayName: LocalizableString
                                .Create<NotificationsResource>("Allow Notifications"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.Flex",
                defaultValue: defaultValue,
                   displayName: LocalizableString
                                    .Create<FlexResource>("Flex"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.Reporting",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("Reporting"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.AIReporting",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("AI Reporting"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.AI.AttachmentSummaries",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("AI Attachment Summaries"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.AI.ApplicationAnalysis",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("AI Application Analysis"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.AI.Scoring",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("AI Scoring"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.AI.FormMapping",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("AI Form Mapping"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.AI.FormWorksheet",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("AI Form Worksheet"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.AI.FormScoresheet",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("AI Form Scoresheet"),
                valueType: new ToggleStringValueType());

            myGroup.AddFeature("Unity.Analytics",
                defaultValue: defaultValue,
                    displayName: LocalizableString
                                    .Create<GrantManagerResource>("Analytics"),
                valueType: new ToggleStringValueType());

            var specializationGroup = context.AddGroup(SpecializationConsts.GroupName);

            specializationGroup.AddFeature(SpecializationConsts.Onboarding,
                defaultValue: defaultValue,
                displayName: LocalizableString
                                .Create<GrantManagerResource>("Onboarding"),
                valueType: new ToggleStringValueType());
        }
    }
}
