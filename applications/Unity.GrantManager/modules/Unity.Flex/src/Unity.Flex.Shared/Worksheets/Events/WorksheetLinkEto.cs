using System;

namespace Unity.Flex.WorksheetLinkInstance
{
    [Serializable]
    public class WorksheetLinkEto
    {
        public Guid FormVersionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
 