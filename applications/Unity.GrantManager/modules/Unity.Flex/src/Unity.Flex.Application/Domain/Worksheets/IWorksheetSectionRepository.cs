using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Worksheets
{
    public interface IWorksheetSectionRepository : IReadOnlyRepository<WorksheetSection, Guid>
    {
    }
}
