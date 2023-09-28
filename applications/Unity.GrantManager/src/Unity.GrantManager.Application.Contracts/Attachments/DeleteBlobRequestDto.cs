using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Unity.GrantManager.Attachments
{
    public class DeleteBlobRequestDto
    {
        [Required]
        public string S3ObjectKey { get; set; }
        public string Name { get; set; }
    }
}
