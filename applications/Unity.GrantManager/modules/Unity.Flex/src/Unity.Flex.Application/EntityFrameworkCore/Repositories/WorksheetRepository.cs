using System;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class WorksheetRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, Worksheet, Guid>(dbContextProvider), IWorksheetRepository
    {
    }
}
