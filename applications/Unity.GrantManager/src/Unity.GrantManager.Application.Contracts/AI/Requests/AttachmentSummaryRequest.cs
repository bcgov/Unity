namespace Unity.GrantManager.AI
{
    public class AttachmentSummaryRequest
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = System.Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
