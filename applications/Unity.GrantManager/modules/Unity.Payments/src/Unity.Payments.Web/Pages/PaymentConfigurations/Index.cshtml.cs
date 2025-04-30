using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Identity;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.PaymentThresholds;
using Unity.Payments.Web.Pages.PaymentApprovals;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Users;

namespace Unity.Payments.Web.Pages.PaymentConfifurations
{
    public class PaymentConfigurationModel(
        IPaymentThresholdRepository paymentThresholdRepository,
        IPaymentConfigurationRepository paymentConfigurationRepository,
        IIdentityUserIntegrationService identityUserLookupAppService) : AbpPageModel
    {
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid? AccountCodingId { get; set; }

        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public List<PaymentThresholdModel> PaymentThresholdList { get; set; } = new List<PaymentThresholdModel>();

        [BindProperty(SupportsGet = true)]
        public string? PaymentIdPrefix { get; set; }

        public async Task OnGetAsync()
        {
            var paymentConfigurations = await paymentConfigurationRepository.GetListAsync();
            var paymentConfiguration = paymentConfigurations.Count > 0 ? paymentConfigurations[0] : null;

            if (paymentConfiguration != null)
            {
                AccountCodingId = paymentConfiguration.DefaultAccountCodingId;
                PaymentIdPrefix = paymentConfiguration.PaymentIdPrefix;
            }

            PaymentThresholdList = await GetL2ApproversThresholds();           
        }

        public async Task<List<PaymentThresholdModel>> GetL2ApproversThresholds()
        {
            // lookup users with the l2_approval role
            List<PaymentThresholdModel> l2UsersData = new List<PaymentThresholdModel>();

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
                            l2UsersData.Add(new PaymentThresholdModel()
                            {
                                Id = paymentThreshold.Id,
                                UserId = user.Id,
                                UserName = user.Name,
                                PaymentThreshold = paymentThreshold.Threshold,
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

                            l2UsersData.Add(new PaymentThresholdModel()
                            {
                                Id = paymentThresholdNew.Id,
                                UserId = user.Id,
                                UserName = user.Name,
                                PaymentThreshold = null,
                                Description = null
                            });
                        }
                    }
                }
            }
            return l2UsersData;
        }
    }
}