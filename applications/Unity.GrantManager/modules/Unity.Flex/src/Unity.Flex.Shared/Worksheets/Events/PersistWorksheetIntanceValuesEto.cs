using System;

namespace Unity.Flex.WorksheetInstances
{
    [Serializable]
    public class PersistWorksheetIntanceValuesEto
    {
        public Guid InstanceCorrelationId { get; set; }
        public string InstanceCorrelationProvider { get; set; } = string.Empty;
        public Guid SheetCorrelationId { get; set; }
        public string SheetCorrelationProvider { get; set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;    
        public dynamic? CustomFields { get; set; }
        public string FormDataName { get; set; } = string.Empty;
        public Guid WorksheetId { get; set; }
    }
}
 