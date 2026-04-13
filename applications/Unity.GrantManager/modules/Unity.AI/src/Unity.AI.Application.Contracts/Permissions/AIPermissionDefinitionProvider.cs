using Unity.AI.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.Features;
using Volo.Abp.SettingManagement;

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
                AIPermissions.Reporting.ReportingDefault,
                L("Permission:AI.Reporting"))
                .RequireFeatures("Unity.AIReporting");                

            var analysisParent = aiPermissionsGroup.AddPermission(
                AIPermissions.Analysis.AnalysisDefault,
                L("Permission:AI.Analysis"));

            analysisParent.AddChild(
                AIPermissions.Analysis.ViewApplicationAnalysis,
                L("Permission:AI.Analysis.ViewApplicationAnalysis"))
                .RequireFeatures("Unity.AI.ApplicationAnalysis");

            analysisParent.AddChild(
                AIPermissions.Analysis.ViewAttachmentSummary,
                L("Permission:AI.Analysis.ViewAttachmentSummary"))
                .RequireFeatures("Unity.AI.AttachmentSummaries");

            analysisParent.AddChild(
                AIPermissions.Analysis.ViewScoringResult,
                L("Permission:AI.Analysis.ViewScoringResult"))
                .RequireFeatures("Unity.AI.Scoring");

            analysisParent.AddChild(
                AIPermissions.Analysis.GenerateApplicationAnalysis,
                L("Permission:AI.Analysis.GenerateApplicationAnalysis"))
                .RequireFeatures("Unity.AI.ApplicationAnalysis");

            analysisParent.AddChild(
                AIPermissions.Analysis.GenerateAttachmentSummaries,
                L("Permission:AI.Analysis.GenerateAttachmentSummaries"))
                .RequireFeatures("Unity.AI.AttachmentSummaries");

            analysisParent.AddChild(
                AIPermissions.Analysis.GenerateScoring,
                L("Permission:AI.Analysis.GenerateScoring"))
                .RequireFeatures("Unity.AI.Scoring");

            var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
            settingManagement.AddPermission(
                AIPermissions.Configuration.ConfigureAI,
                L("Permission:AI.ConfigureAI"))
                .RequireFeatures("Unity.AI.Scoring");

    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AIResource>(name);
    }
}
