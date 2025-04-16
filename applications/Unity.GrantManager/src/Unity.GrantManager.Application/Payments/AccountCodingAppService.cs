using System;
using System.Threading.Tasks;
using Unity.Payments.Domain.AccountCodings;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Payments
{    
    public class AccountCodingAppService :
            CrudAppService<
            AccountCoding,
            AccountCodingDto,
            System.Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateAccountCodingDto>, IAccountCodingAppService
    {
        public AccountCodingAppService(IRepository<AccountCoding, Guid> repository)
            : base(repository)
        {
        }
    }
}
