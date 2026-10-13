using System;

namespace Unity.GrantManager.Identity
{
    public class RenameUserSearchDto
    {
        public Guid UserId { get; set; }
        public string Directory { get; set; } = "IDIR";
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
