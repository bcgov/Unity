namespace Unity.GrantManager.Intake;

public class IntakeClientOptions
{
    public string BaseUri { get; set; } = string.Empty;

    public string FormId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string BearerTokenPlaceholder { get; set; } = string.Empty;

    public bool UseBearerToken { get; set; }

    public bool AllowUnregisteredVersions { get; set; }
}
