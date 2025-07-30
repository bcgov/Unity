using System;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Payments
{
    public interface IAccountCodingAppService : ICrudAppService<
            AccountCodingDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateAccountCodingDto>
    {
    }
}
