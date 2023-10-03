using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Attachments
{
    public class SaveBlobInputDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
