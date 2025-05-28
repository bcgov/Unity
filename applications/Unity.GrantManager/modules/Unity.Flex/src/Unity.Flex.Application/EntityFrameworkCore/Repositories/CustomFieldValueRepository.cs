using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class CustomFieldValueRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, CustomFieldValue, Guid>(dbContextProvider), ICustomFieldValueRepository
    {
        public async Task<List<CustomFieldValue>> GetListByWorksheetInstanceAsync(Guid worksheetInstanceId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x =>
                x.WorksheetInstanceId == worksheetInstanceId)
                .ToListAsync();
        }
    }
}
