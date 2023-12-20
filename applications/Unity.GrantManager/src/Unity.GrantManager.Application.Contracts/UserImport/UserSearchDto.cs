using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.UserImport
{
    public class UserSearchDto
    {
        [Required]
        public string Directory { get; set; } = "IDIR";

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }
}
