using System;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Payments
{    
    public class AccountCodingAppService(
        IRepository<AccountCoding, Guid> repository
    ) : CrudAppService<
            AccountCoding,
            AccountCodingDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateAccountCodingDto>(repository), IAccountCodingAppService
    {
    }
}
