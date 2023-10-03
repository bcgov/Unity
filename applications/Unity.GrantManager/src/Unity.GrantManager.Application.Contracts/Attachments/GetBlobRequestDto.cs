using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Attachments
{
    public class GetBlobRequestDto
    {
        [Required]
        public Guid S3Guid { get; set; }
    }
}
