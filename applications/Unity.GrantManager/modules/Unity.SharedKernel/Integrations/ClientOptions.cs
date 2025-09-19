namespace Unity.Modules.Shared.Integrations
{
    public class ClientOptions
    {
        public string Url { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string CertificatePath { get; set; } = string.Empty;        
        public string CertificatePassword { get; set; } = string.Empty;
    }
}