using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Attachments
{
    public class GetBlobRequestDto
    {
        [Required]
        public string S3ObjectKey { get; set; }
        public string Name { get; set; }
    }
}
