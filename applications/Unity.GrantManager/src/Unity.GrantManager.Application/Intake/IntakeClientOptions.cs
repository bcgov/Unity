namespace Unity.GrantManager.Intake;

public class IntakeClientOptions
{
    public string BaseUri { get; set; }

    public string FormId { get; set; }

    public string ApiKey { get; set; }

    public string BearerTokenPlaceholder { get; set; }

    public bool UseBearerToken { get; set; }
}
