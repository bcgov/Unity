namespace Unity.GrantManager.UserImport
{
    public class ImportUserDto
    {
        public string? Username { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? UserGuid { get; set; } = string.Empty;
        public string? DisplayName { get; set; } = string.Empty;
    }
}
