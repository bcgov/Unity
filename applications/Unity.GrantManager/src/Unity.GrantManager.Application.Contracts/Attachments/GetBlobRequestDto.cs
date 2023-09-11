using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Unity.GrantManager.Attachments
{
    public class GetBlobRequestDto
    {
        [Required]
        public Guid S3Guid { get; set; }
    }
}
