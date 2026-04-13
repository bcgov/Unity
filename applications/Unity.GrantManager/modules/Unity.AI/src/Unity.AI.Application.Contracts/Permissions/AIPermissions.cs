using Volo.Abp.Reflection;

namespace Unity.AI.Permissions;

public static class AIPermissions
{
    public const string GroupName = "AI";

    public const string Management = GroupName + ".Management";

    public static class Reporting
    {
        public const string ReportingDefault = GroupName + ".Reporting";
    }

    public static class Analysis
    {
        public const string AnalysisDefault = GroupName + ".Analysis";

        public const string ViewApplicationAnalysis = GroupName + ".Analysis.ViewApplicationAnalysis";
        public const string ViewAttachmentSummary   = GroupName + ".Analysis.ViewAttachmentSummary";
        public const string ViewScoringResult       = GroupName + ".Analysis.ViewScoringResult";

        public const string GenerateApplicationAnalysis = GroupName + ".Analysis.GenerateApplicationAnalysis";
        public const string GenerateAttachmentSummaries = GroupName + ".Analysis.GenerateAttachmentSummaries";
        public const string GenerateScoring             = GroupName + ".Analysis.GenerateScoring";
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
