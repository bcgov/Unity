using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public interface ICustomFieldValueRepository : IBasicRepository<CustomFieldValue, Guid>
    {
        Task<List<CustomFieldValue>> GetListByWorksheetInstanceAsync(Guid worksheetInstanceId);
    }
}
