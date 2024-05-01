namespace Unity.GrantManager;

public static class GrantManagerConsts
{
    public const string DbTablePrefix = "";

    public const string DbSchema = null;

    public const string TenantTablePrefix = "";

    public const string TenantDbSchema = null;

    public const string DefaultTenantName = "Default Grants Program";
    public static string NormalizedDefaultTenantName => DefaultTenantName.ToUpper();

    public const string DefaultTenantConnectionStringName = "Tenant";

    public const string DefaultConnectionStringName = "Default";
}
