using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications;

public static class AssessmentResultsOptionsList
{
    public static readonly Dictionary<string, string> FundingList = new Dictionary<string, string>
    {
        { "LOW", "Low" },
        { "MEDIUM", "Medium" },
        { "HIGH", "High" },
    };

    public static readonly Dictionary<string, string> DueDilligenceList = new Dictionary<string, string>
    {
        { "COMPLETE", "Complete" },
        { "UNDERWAY", "Underway" },
        { "PAUSED", "Paused" },
        { "WITHDRAWN", "Withdrawn" },
        { "INELIGIBLE", "Ineligible" },
        { "FAILED", "Failed" },
    };

    public static readonly Dictionary<string, string> AssessmentResultStatusList = new Dictionary<string, string>
    {
        { "PASS", "Pass" },
        { "FAIL", "Fail" },
        { "INELIGIBLE", "Ineligible" },
    };

    public static readonly Dictionary<string, string> DeclineRationalActionList = new Dictionary<string, string>
    {
        { "NO_READINESS", "Lack of readiness" },
        { "LOW_PRIORITY", "Lower priority relative to other requests" },
        { "NOT_ENOUGH_INFO", "Insufficient information provided" },
        { "INELIGIBLE_PROJECT", "Ineligible Project" },
        { "INELIGIBLE_APPLICANT", "Ineligible Applicant" },
        { "INSUFFICIENT_READINESS", "Insufficient Readiness" },
        { "SMALL_PROJECT", "Project too small" },
        { "OTHER", "Other" },
    };

    public static readonly Dictionary<string, string> RecommendationActionList = new Dictionary<string, string>
    {
        { "APPROVE", "Recommended for Approval" },
        { "DENY", "Recommended for Denial" },
    };

}
