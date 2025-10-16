using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using System;
using Volo.Abp.Application.Services;
using Volo.Abp.TenantManagement;
using Volo.Abp.MultiTenancy;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Volo.Abp.EventBus.Local;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Settings;
using Unity.Payments.PaymentRequests.Notifications;

namespace Unity.Payments.PaymentRequests
{
    public class FinancialSummaryService : ApplicationService
    {
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;        
        private readonly ILocalEventBus _localEventBus;
        private readonly EmailRecipientStrategyFactory _emailRecipientStrategyFactory;

        public FinancialSummaryService (
            IPaymentRequestRepository paymentRequestsRepository,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            ILocalEventBus localEventBus,
            EmailRecipientStrategyFactory emailRecipientStrategyFactory)
        {
            _paymentRequestsRepository = paymentRequestsRepository;
            _tenantRepository = tenantRepository;
            _currentTenant = currentTenant;
            _localEventBus = localEventBus;
            _emailRecipientStrategyFactory = emailRecipientStrategyFactory;
        }

        public async Task NotifyFailedPayments()
        {
            var tenants = await _tenantRepository.GetListAsync();
            foreach (var tenantId in tenants.Select(tenant => tenant.Id))
            {
                using (_currentTenant.Change(tenantId))
                {
                    List<PaymentRequest> failedPaymentList = await GetFailedPayments();
                    if (failedPaymentList != null && failedPaymentList.Count > 0)
                    {
                        string failedContent = GetFailedPaymentContent(failedPaymentList);                        
                        if (!failedContent.IsNullOrEmpty())
                        {
                            // Use strategy pattern to collect emails from all registered strategies
                            // Each strategy is responsible for obtaining its own data sources
                            var strategies = _emailRecipientStrategyFactory.GetAllStrategies();
                            HashSet<string> recipientEmails = new(StringComparer.OrdinalIgnoreCase);
                            
                            foreach (var strategy in strategies)
                            {
                                try
                                {
                                    var emails = await strategy.GetEmailRecipientsAsync();
                                    recipientEmails.UnionWith(emails);
                                    Logger.LogDebug("NotifyFailedPayments: Strategy '{StrategyName}' contributed email addresses", 
                                        strategy.StrategyName);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogWarning(ex, "NotifyFailedPayments: Strategy '{StrategyName}' failed to get email recipients", 
                                        strategy.StrategyName);
                                }
                            }

                            if (recipientEmails.Count == 0)
                            {
                                Logger.LogWarning("NotifyFailedPayments: No recipients found from any strategy for tenant {TenantId}", tenantId);
                                continue;
                            }

                            var defaultFromAddress = await SettingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
                            string fromAddress = defaultFromAddress ?? "NoReply@gov.bc.ca";
                            List<string> recipientList = [.. recipientEmails];

                            await _localEventBus.PublishAsync(
                                new EmailNotificationEvent
                                {
                                    Action = EmailAction.SendFailedSummary,  
                                    RetryAttempts = 0,
                                    Body = failedContent,
                                    EmailFrom = fromAddress,                                    
                                    EmailAddressList = recipientList
                                }
                            );
                        }                       
                    }
                    
                }
            }
        }

        private static string GetFailedPaymentContent(List<PaymentRequest> failedPaymentRequests)
        {

            StringBuilder sb = new();
            sb.Append("<b>Failed CAS Payment Requests</b>\n");
            using (Table table = new(sb))
            {
                Row header = new(sb, true);

                header.AddCell("Payment Id");
                header.AddCell("Amount");
                header.AddCell("Applicant Name");
                header.AddCell("CAS Response");
                header.Dispose();

                foreach (var paymentRequest in failedPaymentRequests)
                {
                    using Row row = table.AddRow();
                    row.AddCell(paymentRequest.ReferenceNumber ?? string.Empty);
                    row.AddCell(paymentRequest.Amount.ToString());
                    row.AddCell(paymentRequest.PayeeName);
                    row.AddCell(paymentRequest.CasResponse ?? string.Empty);
                }
            }
            return sb.ToString();
        }

        
        public async Task<List<PaymentRequest>> GetFailedPayments()
        {
            List <PaymentRequest> failedPaymentList = [];

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
