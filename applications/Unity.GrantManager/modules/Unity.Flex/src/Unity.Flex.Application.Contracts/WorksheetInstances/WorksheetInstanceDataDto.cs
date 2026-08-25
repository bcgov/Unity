using System;

namespace Unity.Flex.WorksheetInstances
{
    public class WorksheetInstanceDataDto
    {
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid WorksheetId { get; set; }
        public string CurrentValue { get; set; } = "{}";
        public DateTime CreationTime { get; set; }
    }
}
