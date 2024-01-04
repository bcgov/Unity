namespace Unity.GrantManager.Integrations.Sso
{
    public class CssApiOptions
    { 
        public string TokenUrl { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Env { get; set; } = string.Empty;
    }
}
