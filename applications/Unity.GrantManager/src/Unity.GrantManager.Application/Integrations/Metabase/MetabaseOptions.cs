namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseOptions
{
    /// <summary>Admin API key - same key the Metabase admin UI/PowerShell runbook uses (x-api-key header).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Local-dev-only override for the Postgres host passed to Metabase when registering a
    /// tenant's database connection. Tenant readonly connection strings store "localhost" as the
    /// host (correct for the .NET app, which runs on the host machine) - but a dockerized local
    /// Metabase container can't reach "localhost" that way, since that resolves to the container
    /// itself. Set this to whatever hostname your local Metabase container can actually reach
    /// Postgres by - e.g. the Postgres container's name/service (like "unitydb") if both containers
    /// share a Docker network, or "host.docker.internal" if Metabase needs to reach out to the host
    /// machine instead (which may also require a Windows Firewall inbound allow rule for 5432, and
    /// doesn't resolve for every local Docker setup - verify with a direct call to Metabase's
    /// POST /api/database before assuming it's this setting). Leave unset in deployed environments -
    /// there both the app and Metabase reach Postgres via its OpenShift service name, so the stored
    /// host is already correct.
    /// </summary>
    public string DbHostOverride { get; set; } = string.Empty;

    /// <summary>
    /// Local-dev-only override for whether Metabase connects to the tenant's Postgres database
    /// over SSL. Deployed Postgres (Crunchy on OpenShift) requires SSL, so the default (null, no
    /// override) sends <c>ssl: true</c>. A plain local `postgres` Docker image has SSL disabled
    /// out of the box, so set this to <c>false</c> locally or Metabase's connection attempt fails.
    /// </summary>
    public bool? DbSslOverride { get; set; }
}
