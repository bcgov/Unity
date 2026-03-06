using Volo.Abp.Reflection;
using Volo.Abp.Features;

namespace Unity.AI.Permissions;

public static class AIPermissions
{
    public const string GroupName = "AI";

    public static class Default
    {
        public const string Management = GroupName + ".Management";
        public const string GroupName = "AI";

        public static class Reporting
        {
            public const string Default = GroupName + ".Reporting";
        }

        public static class ApplicationAnalysis
        {
            public const string Default = GroupName + ".ApplicationAnalysis";
        }

        public static class AttachmentSummary
        {
            public const string Default = GroupName + ".AttachmentSummary";
        }

        public static class ScoringAssistant
        {
            public const string Default = GroupName + ".ScoringAssistant";
        }        
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AIPermissions));
    }
}
