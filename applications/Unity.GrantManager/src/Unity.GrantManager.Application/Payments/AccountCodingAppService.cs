using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.PaymentRequests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Payments;

[Authorize]
public class AccountCodingAppService :
    CrudAppService<
        AccountCoding,
        AccountCodingDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateAccountCodingDto>, IAccountCodingAppService
{
    public AccountCodingAppService(IRepository<AccountCoding, Guid> repository) : base(repository)
    {
        DeletePolicyName = UnitySettingManagementPermissions.ConfigurePayments;
    }

    /// <summary>
    /// Deletes the account coding with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The unique identifier of the account coding to delete.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    [RemoteService(false)]
    public override Task DeleteAsync(Guid id)
        => base.DeleteAsync(id);
}
