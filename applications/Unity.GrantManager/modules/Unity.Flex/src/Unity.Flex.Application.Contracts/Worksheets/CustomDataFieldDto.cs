using System;

namespace Unity.Flex.Worksheets
{
    public class CustomDataFieldDto
    {
        public dynamic? CustomFields { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid WorksheetId { get; set; }
    }
}
