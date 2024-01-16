namespace Unity.GrantManager.Identity
{
    public class ImportUserDto
    {
        public string Directory { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
        public string[]? Roles { get; set; } = default;
    }
}
