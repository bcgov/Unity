using System;

namespace Unity.Flex.WorksheetInstances
{
    [Serializable]
    public class PersistWorksheetIntanceValuesEto
    {
        public Guid InstanceCorrelationId { get; set; }
        public string InstanceCorrelationProvider { get; set; }
        public Guid SheetCorrelationId { get; set; }
        public string SheetCorrelationProvider { get; set; }        
        public string UiAnchor { get; set; } = string.Empty;    
        public dynamic? CustomFields { get; set; }
    }
}
