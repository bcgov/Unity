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

    public static class ApplicationAnalysis
    {
        public const string ApplicationAnalysisDefault = GroupName + ".ApplicationAnalysis";
    }

    public static class AttachmentSummary
    {
        public const string AttachmentSummaryDefault = GroupName + ".AttachmentSummary";
    }

    public static class ScoringAssistant
    {
        public const string ScoringAssistantDefault = GroupName + ".ScoringAssistant";
    }        


    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AIPermissions));
    }
}
