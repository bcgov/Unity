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
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using System.Text;
using Volo.Abp.EventBus.Local;

namespace Unity.Payments.PaymentRequests
{
    public class FinancialSummaryService : ApplicationService
    {
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;
        private readonly ILocalEventBus _localEventBus;
        public const string FinancialAnalyst = "financial_analyst";

        public FinancialSummaryService (
            IIdentityUserIntegrationService identityUserIntegrationService,
            IPaymentRequestRepository paymentRequestsRepository,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            ILocalEventBus localEventBus)
        {          
            _identityUserLookupAppService = identityUserIntegrationService;
            _paymentRequestsRepository = paymentRequestsRepository;
            _tenantRepository = tenantRepository;
            _currentTenant = currentTenant;
            _localEventBus = localEventBus;
        }

        public async Task NotifyFinancialAdvisorsOfNightlyFailedPayments()
        {
            var tenants = await _tenantRepository.GetListAsync();
            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id))
                {
                    List<PaymentRequest> failedPaymentList = await GetFailedPayments();
                    if (failedPaymentList != null && failedPaymentList.Count > 0)
                    {
                        string failedContent = GetFailedPaymentContent(failedPaymentList);
                        if (!failedContent.IsNullOrEmpty())
                        {
                            List<string> financialAnalystEmails = await GetFinancialAnalystEmails();

                            await _localEventBus.PublishAsync(
                                new EmailNotificationEvent
                                {
                                    Action = EmailAction.SendFailedSummary,  
                                    RetryAttempts = 0,
                                    Body = failedContent,
                                    EmailAddressList = financialAnalystEmails
                                }
                            );
                        }                       
                    }
                    
                }
            }
        }

        private static string GetFailedPaymentContent(List<PaymentRequest> failedPaymentRequests)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("<b>Failed CAS Payment Requests</b>\n");
            using (Table table = new Table(sb))
            {
                Row header = new Row(sb, true);

                header.AddCell("Payment Id");
                header.AddCell("Amount");
                header.AddCell("Applicant Name");
                header.AddCell("CAS Response");
                header.Dispose();

                foreach (var paymentRequest in failedPaymentRequests)
                {
                    using (Row row = table.AddRow())
                    {
                        row.AddCell(paymentRequest.ReferenceNumber ?? string.Empty);
                        row.AddCell(paymentRequest.Amount.ToString());
                        row.AddCell(paymentRequest.PayeeName);
                        row.AddCell(paymentRequest.CasResponse ?? string.Empty);                        
                    }
                }
            }
            return sb.ToString();
        }

        public async Task<List<string>> GetFinancialAnalystEmails()
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
            return financialAnalystEmails;
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
