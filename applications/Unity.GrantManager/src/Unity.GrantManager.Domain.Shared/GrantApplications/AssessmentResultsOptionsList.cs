using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications;

public static class AssessmentResultsOptionsList
{
    public static Dictionary<string, string> FundingList => new Dictionary<string, string> { { "HIGH", "High" }, { "LOW", "Low" }, { "MEDIUM", "Medium" } };

    public static Dictionary<string, string> DueDiligenceList => new Dictionary<string, string> { { "COMPLETE", "Complete" }, { "FAILED", "Failed" }, { "INELIGIBLE", "Ineligible" }, { "PAUSED", "Paused" }, { "UNDERWAY", "Underway" }, { "WITHDRAWN", "Withdrawn" } };

    public static Dictionary<string, string> AssessmentResultStatusList => new Dictionary<string, string> { { "FAIL", "Fail" }, { "INELIGIBLE", "Ineligible" }, { "PASS", "Pass" } };

    public static Dictionary<string, string> DeclineRationalActionList => new Dictionary<string, string> { { "INELIGIBLE_APPLICANT", "Ineligible Applicant" }, { "INELIGIBLE_PROJECT", "Ineligible Project" }, { "NOT_ENOUGH_INFO", "Insufficient information provided" }, { "INSUFFICIENT_READINESS", "Insufficient Readiness" }, { "NO_READINESS", "Lack of readiness" }, { "LOW_PRIORITY", "Lower priority relative to other requests" }, { "SMALL_PROJECT", "Project too small" }, { "OTHER", "Other" } };

    public static Dictionary<string, string> SubStatusActionList => new Dictionary<string, string> { { "REQUIRES_SECONDARY_REVIEW", "Requires Secondary Review" }, { "REQUIRES_TEAM_LEAD_REVIEW", "Requires Team Lead Review" }, {"MISSING_INFORMATION","Missing Information" },{"BATCH_FOR_DECISION","Batch For Decision" },{ "NOTIFIED","Notified"} };

}
