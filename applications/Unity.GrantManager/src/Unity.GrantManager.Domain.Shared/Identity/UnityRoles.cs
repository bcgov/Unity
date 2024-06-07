using System.Collections.Generic;

namespace Unity.GrantManager.Identity;

public static class UnityRoles
{
    public const string ProgramManager = "program_manager";
    public const string Reviewer = "reviewer";
    public const string Assessor = "assessor";
    public const string TeamLead = "team_lead";
    public const string Approver = "approver";
    public const string SystemAdmin = "system_admin";
    public const string FinancialAnalyst = "financial_analyst";
    public const string L1Approver = "l1_approver";
    public const string L2Approver = "l2_approver";
    public const string L3Approver = "l3_approver";

    public static readonly IReadOnlyList<string> DefinedRoles =
            new List<string>() { ProgramManager, Reviewer, Assessor, TeamLead, Approver, SystemAdmin, FinancialAnalyst, L1Approver, L2Approver, L3Approver };        
}
