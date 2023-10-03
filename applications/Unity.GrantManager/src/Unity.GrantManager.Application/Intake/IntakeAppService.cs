using Microsoft.AspNetCore.Authorization;
using System;
using Unity.GrantManager.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Intake
{
    [Authorize(GrantManagerPermissions.Intakes.Default)]
    public class IntakeAppService :
            CrudAppService<
            GrantPrograms.Intake,
            IntakeDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateIntakeDto>,
            IIntakeAppService
    {
        public IntakeAppService(IRepository<GrantPrograms.Intake, Guid> repository)
            : base(repository)
        {
        }
    }
}
