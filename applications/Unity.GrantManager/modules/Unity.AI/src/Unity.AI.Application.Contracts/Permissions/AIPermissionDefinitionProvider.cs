using Unity.AI.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.Features;

namespace Unity.AI.Permissions;

public class AIPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
            // AI Permission Group
            var aiPermissionsGroup = context.AddGroup(
                AIPermissions.GroupName,
                L("Permission:AI"));


            aiPermissionsGroup.AddPermission(
                AIPermissions.Default.Reporting.Default,
                L("Permission:AI.Reporting"))
                .RequireFeatures("Unity.AIReporting");                

            aiPermissionsGroup.AddPermission(
                AIPermissions.Default.ApplicationAnalysis.Default,
                L("Permission:AI.ApplicationAnalysis"))
                 .RequireFeatures("Unity.AI.ApplicationAnalysis");

            aiPermissionsGroup.AddPermission(
                AIPermissions.Default.AttachmentSummary.Default ,
                L("Permission:AI.AttachmentSummary"))
                 .RequireFeatures("Unity.AI.AttachmentSummaries");

            aiPermissionsGroup.AddPermission(
                AIPermissions.Default.ScoringAssistant.Default,
                L("Permission:AI.ScoringAssistant"))
                 .RequireFeatures("Unity.AI.Scoring");

    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AIResource>(name);
    }
}
