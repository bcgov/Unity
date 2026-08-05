using System;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.PaymentRequests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Payments
{    
    public class AccountCodingAppService :
        CrudAppService<
            AccountCoding,
            AccountCodingDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateAccountCodingDto>, IAccountCodingAppService
    {
        public AccountCodingAppService(IRepository<AccountCoding, Guid> repository)
            : base(repository)
        {
            GetPolicyName = UnitySettingManagementPermissions.ConfigurePayments;
            GetListPolicyName = UnitySettingManagementPermissions.ConfigurePayments;
            CreatePolicyName = UnitySettingManagementPermissions.ConfigurePayments;
            UpdatePolicyName = UnitySettingManagementPermissions.ConfigurePayments;
        }

        [RemoteService(false)]
        public override Task DeleteAsync(Guid id) => base.DeleteAsync(id);
    }
}
