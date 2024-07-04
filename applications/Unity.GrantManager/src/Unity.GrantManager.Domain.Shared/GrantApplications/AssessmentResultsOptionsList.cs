using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications;

public static class AssessmentResultsOptionsList
{
    public static Dictionary<string, string> FundingList => new() { { "HIGH", "High" }, { "LOW", "Low" }, { "MEDIUM", "Medium" } };

    public static Dictionary<string, string> DueDiligenceList => new() { { "COMPLETE", "Complete" }, { "FAILED", "Failed" }, { "INELIGIBLE", "Ineligible" }, { "PAUSED", "Paused" }, { "UNDERWAY", "Underway" }, { "WITHDRAWN", "Withdrawn" } };

    public static Dictionary<string, string> AssessmentResultStatusList => new() { { "FAIL", "Fail" }, { "INELIGIBLE", "Ineligible" }, { "PASS", "Pass" } };

    public static Dictionary<string, string> DeclineRationalActionList => new() { { "INELIGIBLE_PROJECT", "Ineligible Project" }, { "INELIGIBLE_APPLICANT", "Ineligible Applicant" }, { "INSUFFICIENT_READINESS", "Insufficient Readiness" }, { "LOW_PRIORITY", "Lower Priority Relative To Other Requests" }, { "NOT_ENOUGH_INFO", "Insufficient Information Provided" }, { "NOT_ALIGNED", "Not Aligned" }, { "OVERSUBSCRIBED", "Oversubscribed" }, { "OTHER", "Other" } };

    public static Dictionary<string, string> SubStatusActionList => new() { { "BATCH_FOR_DECISION", "Batch For Decision" }, { "COMPLETE", "Complete" }, { "MISSING_INFORMATION", "Missing Information" }, { "NOTIFIED", "Notified" }, { "REQUIRES_SECONDARY_REVIEW", "Requires Secondary Review" }, { "REQUIRES_TEAM_LEAD_REVIEW", "Requires Team Lead Review" } };

    public static Dictionary<string, string> RiskRankingList => new() {  { "LOW", "Low" }, { "MEDIUM", "Medium" }, { "HIGH", "High" } };
}
