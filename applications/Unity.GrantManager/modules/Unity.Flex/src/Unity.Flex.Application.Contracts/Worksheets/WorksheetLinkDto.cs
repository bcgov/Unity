using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Worksheets
{
    public class WorksheetLinkDto : EntityDto<Guid>
    {
        public Guid WorksheetId { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
    }
}
