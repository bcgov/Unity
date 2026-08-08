using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Intakes;

[Authorize(GrantManagerPermissions.Intakes.Default)]
public class IntakeAppService :
        CrudAppService<
        Intake,
        IntakeDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateIntakeDto>,
        IIntakeAppService
{
    public IntakeAppService(IRepository<Intake, Guid> repository)
        : base(repository)
    {
        DeletePolicyName = GrantManagerPermissions.Intakes.Default;
    }

    /// <summary>
    /// Deletes the intake with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The unique identifier of the intake to delete.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    [RemoteService(false)]
    public override Task DeleteAsync(Guid id)
        => base.DeleteAsync(id);
}
