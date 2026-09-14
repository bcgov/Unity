namespace Unity.TenantManagement.Onboarding;

public static class OnboardingColumnConfigSettings
{
    public const string TenantNameFieldKey = "Onboarding.ColumnConfig.TenantNameFieldKey";
    public const string DisplayNameFieldKey = "Onboarding.ColumnConfig.DisplayNameFieldKey";
    // Value deliberately left as the pre-rename "SuperUsersFieldKey" string - this is the actual
    // persisted ABP setting name. Changing the value (not just the C# constant name below) would
    // orphan every admin's already-saved onboarding column mapping, silently forcing them to
    // remap columns after upgrade. Only the terminology visible to users (labels, messages) was
    // renamed to "Program Managers"; the underlying setting key stays stable.
    public const string ProgramManagersFieldKey = "Onboarding.ColumnConfig.SuperUsersFieldKey";
    public const string BranchFieldKey = "Onboarding.ColumnConfig.BranchFieldKey";
    public const string FeaturesFieldKey = "Onboarding.ColumnConfig.FeaturesFieldKey";
    public const string MinistryFieldKey = "Onboarding.ColumnConfig.MinistryFieldKey";
    public const string DivisionFieldKey = "Onboarding.ColumnConfig.DivisionFieldKey";
    public const string ProgramAreaFieldKey = "Onboarding.ColumnConfig.ProgramAreaFieldKey";
}
