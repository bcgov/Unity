using System;

namespace Unity.Flex.Scoresheets
{
    public class ExportScoresheetDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string Name { get; set; }  = string.Empty;
        public string ContentType { get; set; } = string.Empty;

    }
}
