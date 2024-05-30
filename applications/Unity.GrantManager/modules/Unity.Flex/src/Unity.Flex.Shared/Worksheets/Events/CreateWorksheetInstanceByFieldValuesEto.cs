using System;
using System.Collections.Generic;

namespace Unity.Flex.WorksheetInstances
{
    [Serializable]
    public class  CreateWorksheetInstanceByFieldValuesEto
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public List<KeyValuePair<string, object?>> CustomFields { get; set; } = [];
    }
}
