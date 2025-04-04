namespace Unity.GrantManager.Settings
{
    public static class SettingsConstants
    {
        public const string SectorFilterName = "GrantManager.Locality.SectorFilter";
        public const string RegionalDistrictsCacheKey = "RegionalDistrictCache";
        public const string ElectoralDistrictsCacheKey = "ElectoralDistrictCache";
        public const string EconomicRegionsCacheKey = "EconomicRegionCache";
        public const string CommunitiesCacheKey = "CommunitiesCache";
        public const double DefaultLocalityCacheHours = 48;

        public static class UI
        {
            public const string Zones = "GrantManager.UI.Zones";

            public static class Tabs
            {
                public const string Default = "GrantManager.UI.Tabs";
                public const string Submission = "GrantManager.UI.Tabs.Submission";
                public const string Assessment = "GrantManager.UI.Tabs.Assessment";
                public const string Project = "GrantManager.UI.Tabs.Project";
                public const string Applicant = "GrantManager.UI.Tabs.Applicant";
                public const string Payments = "GrantManager.UI.Tabs.Payments";
                public const string FundingAgreement = "GrantManager.UI.Tabs.FundingAgreement";
            }
        }

        public static class BackgroundJobs
        {
            public const string IntakeResync_Expression = "GrantManager.BackgroundJobs.IntakeResync_Expression";
            public const string IntakeResync_NumDaysToCheck = "GrantManager.BackgroundJobs.IntakeResync_NumDaysToCheck";
         }
    }
}