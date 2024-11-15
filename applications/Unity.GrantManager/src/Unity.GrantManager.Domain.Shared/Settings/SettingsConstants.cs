namespace Unity.GrantManager.Settings
{
    public static class SettingsConstants
    {
        public const string SectorFilterName = "GrantManager.Locality.SectorFilter";
        public const string RegionalDistrictsCacheKey = "RegionalDistrictCache";
        public const string ElectoralDistrictsCacheKey = "ElectoralDistrictCache";        
        public const string EconomicRegionsCacheKey = "EconomicRegionCache";
        public const string CommunitiesCacheKey = "CommunitiesCache";

        public static class UI
        {
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
    }
}