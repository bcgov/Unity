using System;

namespace Unity.Flex.WorksheetInstances
{    
    [Serializable]
    public class PersistWorksheetIntanceValuesDto
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;
        public dynamic? CustomFields { get; set; }
    }
}
