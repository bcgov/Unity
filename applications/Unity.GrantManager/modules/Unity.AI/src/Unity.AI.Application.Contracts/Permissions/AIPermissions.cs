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
        public const string ViewFormMapping         = GroupName + ".ViewFormMapping";
        public const string ViewFormWorksheet       = GroupName + ".ViewFormWorksheet";
        public const string ViewFormScoresheet      = GroupName + ".ViewFormScoresheet";

        public const string GenerateApplicationAnalysis = GroupName + ".GenerateApplicationAnalysis";
        public const string GenerateAttachmentSummaries = GroupName + ".GenerateAttachmentSummaries";
        public const string GenerateScoring             = GroupName + ".GenerateScoring";
        public const string GenerateFormMapping         = GroupName + ".GenerateFormMapping";
        public const string GenerateFormWorksheet       = GroupName + ".GenerateFormWorksheet";
        public const string GenerateFormScoresheet      = GroupName + ".GenerateFormScoresheet";
    }

    public static class ApplicationAnalysis
    {
        public const string View = Analysis.ViewApplicationAnalysis;
        public const string Generate = Analysis.GenerateApplicationAnalysis;
    }

    public static class AttachmentSummaries
    {
        public const string View = Analysis.ViewAttachmentSummary;
        public const string Generate = Analysis.GenerateAttachmentSummaries;
    }

    public static class ApplicationScoring
    {
        public const string View = Analysis.ViewScoringResult;
        public const string Generate = Analysis.GenerateScoring;
    }

    public static class FormMapping
    {
        public const string View = Analysis.ViewFormMapping;
        public const string Generate = Analysis.GenerateFormMapping;
    }

    public static class FormWorksheet
    {
        public const string View = Analysis.ViewFormWorksheet;
        public const string Generate = Analysis.GenerateFormWorksheet;
    }

    public static class FormScoresheet
    {
        public const string View = Analysis.ViewFormScoresheet;
        public const string Generate = Analysis.GenerateFormScoresheet;
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
