using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.Payments.Domain.PaymentThresholds;
using Unity.Payments.PaymentThresholds;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Payments;

[Authorize(UnitySettingManagementPermissions.ConfigurePayments)]
public class PaymentThresholdAppService :
        CrudAppService<
        PaymentThreshold,
        PaymentThresholdDto,
        Guid,
        PagedAndSortedResultRequestDto,
        UpdatePaymentThresholdDto>, IPaymentThresholdAppService
{
    public PaymentThresholdAppService(IRepository<PaymentThreshold, Guid> repository)
        : base(repository)
    {
        CreatePolicyName = UnitySettingManagementPermissions.ConfigurePayments;
        UpdatePolicyName = UnitySettingManagementPermissions.ConfigurePayments;
        DeletePolicyName = UnitySettingManagementPermissions.ConfigurePayments;
    }

    // Listing goes through PaymentSettingsAppService.GetL2ApproversThresholds
    [RemoteService(false)]
    public override Task<PagedResultDto<PaymentThresholdDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        => base.GetListAsync(input);

    [RemoteService(false)]
    public override Task<PaymentThresholdDto> CreateAsync(UpdatePaymentThresholdDto input)
        => base.CreateAsync(input);

    [RemoteService(false)]
    public override Task DeleteAsync(Guid id) 
        => base.DeleteAsync(id);
}
