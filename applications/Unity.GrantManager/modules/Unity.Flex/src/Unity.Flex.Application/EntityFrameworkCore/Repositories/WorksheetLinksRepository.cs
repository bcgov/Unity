using System;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    internal class WorksheetLinksRepository : EfCoreRepository<FlexDbContext, Worksheet, Guid>, IWorksheetLinksRepository
    {
        public WorksheetLinksRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
