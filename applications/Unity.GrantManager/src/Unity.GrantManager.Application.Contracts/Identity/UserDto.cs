namespace Unity.GrantManager.Identity
{
    public class UserDto
    {
        public string? Username { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? UserGuid { get; set; } = string.Empty;
        public string? DisplayName { get; set; } = string.Empty;
        public string? OidcSub { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
    }
}
