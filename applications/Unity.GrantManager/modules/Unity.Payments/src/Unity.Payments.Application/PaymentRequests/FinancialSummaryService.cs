using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using System;
using Volo.Abp.Application.Services;
using Volo.Abp.TenantManagement;
using Volo.Abp.MultiTenancy;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Identity;
using System.Linq;

namespace Unity.Payments.PaymentRequests
{
    public class FinancialSummaryService : ApplicationService
    {
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;

        public const string FinancialAnalyst = "financial_analyst";

        public FinancialSummaryService (
            IIdentityUserIntegrationService identityUserIntegrationService,
            IPaymentRequestRepository paymentRequestsRepository,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant)
        {          
            _identityUserLookupAppService = identityUserIntegrationService;
            _paymentRequestsRepository = paymentRequestsRepository;
            _tenantRepository = tenantRepository;
            _currentTenant = currentTenant;
        }

        public async Task NotifyFinancialAdvisorsOfNightlyFailedPayments()
        {
            var tenants = await _tenantRepository.GetListAsync();
            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id))
                {
                    List<PaymentRequest> failedPaymentList = await GetFailedPayments();
                    await GetFinancialAnalysts();
                }
            }
        }

        public async Task GetFinancialAnalystEmails()
        {
            List<string> financialAnalystEmails = new List<string>();
            var users = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
            if (users != null)
            {
                foreach (var user in users.Items)
                {
                    var roles = await _identityUserLookupAppService.GetRoleNamesAsync(user.Id);
                    if(roles != null && roles.Contains(FinancialAnalyst) )
                    {
                        financialAnalystEmails.Add(user.Email);
                    }
                }
            }
        }
        
        public async Task<List<PaymentRequest>> GetFailedPayments()
        {
            List <PaymentRequest> failedPaymentList = new List<PaymentRequest>();

            try
            {
                failedPaymentList = await _paymentRequestsRepository.GetPaymentRequestsByFailedsStatusAsync();
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogInformation(ex, "GetFailedPayments: Exception: {ExceptionMessage}", ExceptionMessage);
            }

            return failedPaymentList;
        }
    }
}
