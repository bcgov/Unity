using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Scoresheets
{
    public class ScoresheetImportDto
    {
        public string Content { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
