using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Unity.GrantManager.Attachments
{
    public class DeleteBlobRequestDto
    {
        [Required]
        public Guid S3Guid { get; set; }
        public string Name { get; set; }
    }
}
