using System;
using System.Collections.Generic;

namespace Unity.Flex.Worksheets
{
    public class CustomDataFieldDto
    {
        public dynamic? CustomFields { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid WorksheetId { get; set; }        // Keep for backward compatibility
        public List<Guid> WorksheetIds { get; set; } = new List<Guid>(); // NEW - multiple worksheets support
    }
}
