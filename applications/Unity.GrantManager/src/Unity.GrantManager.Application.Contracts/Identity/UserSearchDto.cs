using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Identity
{
    public class UserSearchDto
    {
        [Required]
        public string Directory { get; set; } = "IDIR";

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string UserGuid { get; set; } = string.Empty;
    }
}
