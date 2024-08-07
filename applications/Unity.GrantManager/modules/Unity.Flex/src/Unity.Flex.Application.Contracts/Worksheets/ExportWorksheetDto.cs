using System;

namespace Unity.Flex.Worksheets
{
    public class ExportWorksheetDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string Name { get; set; }  = string.Empty;
        public string ContentType { get; set; } = string.Empty;

    }
}
