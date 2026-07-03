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

        var aiReporting = aiPermissionsGroup.AddPermission(
            AIPermissions.Reporting.ReportingDefault,
            L("Permission:AI.Reporting"))
            .RequireFeatures("Unity.AIReporting");

        aiReporting.AddChild(
            AIPermissions.Reporting.CreateEditDataModel,
            L("Permission:AI.Reporting.CreateEditDataModel"))
            .RequireFeatures("Unity.AIReporting");

        var viewApplicationAnalysis = aiPermissionsGroup.AddPermission(
            AIPermissions.Analysis.ViewApplicationAnalysis,
            L("Permission:AI.ViewApplicationAnalysis"))
            .RequireFeatures("Unity.AI.ApplicationAnalysis");

        viewApplicationAnalysis.AddChild(
            AIPermissions.Analysis.GenerateApplicationAnalysis,
            L("Permission:AI.GenerateApplicationAnalysis"))
            .RequireFeatures("Unity.AI.ApplicationAnalysis");

        var viewAttachmentSummary = aiPermissionsGroup.AddPermission(
            AIPermissions.Analysis.ViewAttachmentSummary,
            L("Permission:AI.ViewAttachmentSummary"))
            .RequireFeatures("Unity.AI.AttachmentSummaries");

        viewAttachmentSummary.AddChild(
            AIPermissions.Analysis.GenerateAttachmentSummaries,
            L("Permission:AI.GenerateAttachmentSummaries"))
            .RequireFeatures("Unity.AI.AttachmentSummaries");

        var viewScoringResult = aiPermissionsGroup.AddPermission(
            AIPermissions.Analysis.ViewScoringResult,
            L("Permission:AI.ViewScoringResult"))
            .RequireFeatures("Unity.AI.Scoring");

        viewScoringResult.AddChild(
            AIPermissions.Analysis.GenerateScoring,
            L("Permission:AI.GenerateScoring"))
            .RequireFeatures("Unity.AI.Scoring");

        var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
        var configureAI = settingManagement.AddPermission(
            AIPermissions.Configuration.ConfigureAI,
            L("Permission:AI.ConfigureAI"));
        configureAI.StateCheckers.Add(new AnyFeaturePermissionStateProvider(
            "Unity.AI.Scoring",
            "Unity.AI.AttachmentSummaries",
            "Unity.AI.ApplicationAnalysis"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AIResource>(name);
    }
}
