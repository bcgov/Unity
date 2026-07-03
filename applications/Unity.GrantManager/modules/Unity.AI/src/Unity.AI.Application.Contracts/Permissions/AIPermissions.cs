using Volo.Abp.Reflection;

namespace Unity.AI.Permissions;

public static class AIPermissions
{
    public const string GroupName = "AI";

    public static class Reporting
    {
        public const string ReportingDefault = GroupName + ".Reporting";
        public const string CreateEditDataModel = GroupName + ".Reporting.CreateEditDataModel";
    }

    public static class Analysis
    {
        public const string ViewApplicationAnalysis = GroupName + ".ViewApplicationAnalysis";
        public const string ViewAttachmentSummary   = GroupName + ".ViewAttachmentSummary";
        public const string ViewScoringResult       = GroupName + ".ViewScoringResult";

        public const string GenerateApplicationAnalysis = GroupName + ".GenerateApplicationAnalysis";
        public const string GenerateAttachmentSummaries = GroupName + ".GenerateAttachmentSummaries";
        public const string GenerateScoring             = GroupName + ".GenerateScoring";
    }

    public static class Configuration
    {
        public const string ConfigureAI = "SettingManagement.ConfigureAI";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AIPermissions));
    }
}
