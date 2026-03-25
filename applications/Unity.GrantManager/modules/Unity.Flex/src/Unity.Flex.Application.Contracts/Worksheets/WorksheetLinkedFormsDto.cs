using System;
using System.Collections.Generic;

namespace Unity.Flex.Worksheets
{
    public class WorksheetLinkedFormsDto
    {
        public List<Guid> FormVersionIdsWithInstances { get; set; } = [];
        public List<Guid> LinkedFormVersionIds { get; set; } = [];
    }
}
