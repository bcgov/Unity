using System;
using Unity.Flex.Worksheets;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.WorksheetLinks
{
    [Serializable]
    public class WorksheetLinkDto : EntityDto<Guid>
    {
        public Guid WorksheetId { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;
        public WorksheetBasicDto Worksheet { get; set; } = new WorksheetBasicDto();
    }
}
