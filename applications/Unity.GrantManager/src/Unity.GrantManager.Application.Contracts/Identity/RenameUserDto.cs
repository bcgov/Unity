using System;

namespace Unity.GrantManager.Identity
{
    public class RenameUserDto
    {
        public Guid UserId { get; set; }
        public string NewUserGuid { get; set; } = string.Empty;
    }
}
