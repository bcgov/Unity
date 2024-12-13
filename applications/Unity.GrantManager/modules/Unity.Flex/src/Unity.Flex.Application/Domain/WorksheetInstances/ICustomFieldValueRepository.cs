using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public interface ICustomFieldValueRepository : IBasicRepository<CustomFieldValue, Guid>
    {
    }
}
