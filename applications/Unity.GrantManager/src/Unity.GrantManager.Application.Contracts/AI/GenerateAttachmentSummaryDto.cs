using System;

namespace Unity.GrantManager.AI
{
    public class GenerateAttachmentSummaryDto
    {
        public Guid ApplicationId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ChefsFileId { get; set; } = string.Empty;
        public string ChefsSumbissionId { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}