using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.GrantApplications
{
    public class CreateUpdateGrantApplicationDto
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }
    }
}
