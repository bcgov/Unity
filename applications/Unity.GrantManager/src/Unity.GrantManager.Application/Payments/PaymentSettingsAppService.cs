using System.Collections.Generic;
using Volo.Abp.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.Payments.Domain.PaymentThresholds;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Unity.Payments.PaymentThresholds;
using Volo.Abp.Users;

namespace Unity.GrantManager.Payments;


[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(PaymentSettingsAppService), typeof(IPaymentSettingsAppService))]
public class PaymentSettingsAppService(
        IPaymentThresholdRepository paymentThresholdRepository,
        IIdentityUserIntegrationService identityUserLookupAppService) : GrantManagerAppService, IPaymentSettingsAppService
{

public async Task<List<PaymentThresholdDto>> GetL2ApproversThresholds()
    {
        // lookup users with the l2_approval role
        List<PaymentThresholdDto> l2UsersData = new List<PaymentThresholdDto>();

        var userListResult = await identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
        var users = userListResult.Items;
        if (users != null)
        {
            foreach (UserData user in users)
            {
                var roles = await identityUserLookupAppService.GetRoleNamesAsync(user.Id);
                if(roles != null && roles.Contains(UnityRoles.L2Approver) )
                {
                    PaymentThreshold? paymentThreshold = await paymentThresholdRepository.FirstOrDefaultAsync(x => x.UserId == user.Id);

                    if (paymentThreshold != null)
                    {
                        l2UsersData.Add(new PaymentThresholdDto()
                        {
                            Id = paymentThreshold.Id,
                            UserId = user.Id,
                            UserName = user.Name + " " + user.Surname,
                            Threshold = paymentThreshold.Threshold,
                            Description = paymentThreshold.Description
                        });
                    }
                    else
                    {
                        // If the user does not have a payment threshold, create a new one with null values
                        PaymentThreshold paymentThresholdNew = await paymentThresholdRepository.InsertAsync(new PaymentThreshold()
                        {
                            UserId = user.Id,
                            Threshold = null,
                            Description = null
                        });

                        l2UsersData.Add(new PaymentThresholdDto()
                        {
                            Id = paymentThresholdNew.Id,
                            UserId = user.Id,
                            UserName = user.Name + " " + user.Surname,
                            Threshold = null,
                            Description = null
                        });
                    }
                }
            }
        }
        return l2UsersData;
    }
}