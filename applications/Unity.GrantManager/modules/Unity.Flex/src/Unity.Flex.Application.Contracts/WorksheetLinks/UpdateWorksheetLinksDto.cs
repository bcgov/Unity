using System;
using System.Collections.Generic;

namespace Unity.Flex.WorksheetLinks
{
    [Serializable]
    public class UpdateWorksheetLinksDto
    {
        public Dictionary<Guid, string> WorksheetAnchors { get; set; } = [];
    }
}
