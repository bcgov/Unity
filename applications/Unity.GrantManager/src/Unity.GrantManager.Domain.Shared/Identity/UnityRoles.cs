using System.Collections.Generic;

namespace Unity.GrantManager.Identity
{
    public class UnityRoles
    {
        public const string ProgramManager = "program_manager";
        public const string Reviewer = "reviewer";
        public const string Adjudicator = "adjudicator";
        public const string TeamLead = "team_lead";
        public const string Approver = "approver";
        public const string BusinessAreaAdmin = "business_area_admin";
        public const string SystemAdmin = "system_admin";

        public static readonly IReadOnlyList<string> DefinedRoles =
                new List<string>() { ProgramManager, Reviewer, Adjudicator, TeamLead, Approver, BusinessAreaAdmin, SystemAdmin };        
    }
}
