using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Identity
{
    public class AssignHostRoleDto
    {
        [Required]
        public string Directory { get; set; } = "IDIR";

        [Required]
        public string Guid { get; set; } = string.Empty;

        [MinLength(1, ErrorMessage = "At least one IT role must be selected")]
        public string[] RoleNames { get; set; } = [];
    }
}
