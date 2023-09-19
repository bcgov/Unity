using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Intake
{
    public interface IIntakeAppService : ICrudAppService<
            IntakeDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateIntakeDto>
    {
    }
}
