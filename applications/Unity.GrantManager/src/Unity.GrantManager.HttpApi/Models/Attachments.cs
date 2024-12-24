using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Models
{
    public class AttachmentsDto
	{
        public Guid FormSubmissionId { get; set; }
        public Guid ChefsFileId { get; set; }
        public string? FileName { get; set; }
    }
}

