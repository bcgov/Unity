using System;
using System.Collections.Generic;

namespace Unity.Flex.WorksheetInstances
{
    [Serializable]
    public class  CreateWorksheetInstanceByFieldValuesEto
    {
        public Guid SheetCorrelationId { get; set; }
        public string SheetCorrelationProvider { get; set; } = string.Empty;
        public Guid InstanceCorrelationId { get; set; }
        public string InstanceCorrelationProvider { get; set; } = string.Empty;
        public List<KeyValuePair<string, object?>> CustomFields { get; set; } = [];
        public Guid? VersionId { get; set; }
    }
}
