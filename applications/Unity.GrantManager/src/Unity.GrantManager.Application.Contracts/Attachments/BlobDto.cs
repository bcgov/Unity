using System;

namespace Unity.GrantManager.Attachments
{
    public class BlobDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string Name { get; set; }  = string.Empty;
        public string ContentType { get; set; } = string.Empty;

    }
}
