using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Intakes
{
    public interface IIntakeAppService : ICrudAppService<
            IntakeDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateIntakeDto>
    {
    }
}
