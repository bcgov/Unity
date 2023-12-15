namespace Unity.GrantManager.Integrations.Sso
{
    public class SsoApiOptions
    { 
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiEnv { get; set; } = string.Empty;
    }
}
