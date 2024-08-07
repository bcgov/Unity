using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Worksheets
{
    public class WorksheetImportDto
    {
        public string Content { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
