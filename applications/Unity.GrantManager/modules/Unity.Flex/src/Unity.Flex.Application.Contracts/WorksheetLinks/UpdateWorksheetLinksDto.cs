using System;
using System.Collections.Generic;

namespace Unity.Flex.WorksheetLinks
{
    [Serializable]
    public class UpdateWorksheetLinksDto
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;

        public List<(Guid worksheetId, string anchor, uint order)> WorksheetAnchors { get; set; } = [];
    }
}
