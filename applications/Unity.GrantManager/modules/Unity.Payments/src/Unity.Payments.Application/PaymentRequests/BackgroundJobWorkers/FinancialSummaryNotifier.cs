using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Settings;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.PaymentRequests.Notifications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;

namespace Unity.Payments.PaymentRequests.BackgroundJobWorkers
{
    public class FinancialSummaryNotifier(
        IPaymentRequestRepository paymentRequestsRepository,
        ISettingProvider settingProvider,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        ILocalEventBus localEventBus,
        ILogger<FinancialSummaryNotifier> logger) : ISingletonDependency
    {
        public async Task NotifyFailedPayments(IEnumerable<IEmailRecipientStrategy> strategies)
        {
            var tenants = await tenantRepository.GetListAsync();
            foreach (var tenantId in tenants.Select(tenant => tenant.Id))
            {
                using (currentTenant.Change(tenantId))
                {                    
                    List<PaymentRequest> failedPaymentList = await GetFailedPayments();
                    if (failedPaymentList != null && failedPaymentList.Count > 0)
                    {
                        string failedContent = GetFailedPaymentContent(failedPaymentList);
                        
                        if (!failedContent.IsNullOrEmpty())
                        {
                            // Use strategy pattern to collect emails from all registered strategies
                            HashSet<string> recipientEmails = new(StringComparer.OrdinalIgnoreCase);
                            
                            foreach (var strategy in strategies)
                            {
                                try
                                {
                                    var emails = await strategy.GetEmailRecipientsAsync();
                                    recipientEmails.UnionWith(emails);
                                    logger.LogDebug("NotifyFailedPayments: Strategy '{StrategyName}' contributed email addresses", 
                                        strategy.StrategyName);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, "NotifyFailedPayments: Strategy '{StrategyName}' failed to get email recipients", 
                                        strategy.StrategyName);
                                }
                            }

                            if (recipientEmails.Count == 0)
                            {
                                logger.LogWarning("NotifyFailedPayments: No recipients found from any strategy for tenant {TenantId}", tenantId);
                                continue;
                            }
                            
                            var defaultFromAddress = await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
                            string fromAddress = defaultFromAddress ?? "NoReply@gov.bc.ca";
                            List<string> recipientList = [.. recipientEmails];

                            await localEventBus.PublishAsync(
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
                failedPaymentList = await paymentRequestsRepository.GetPaymentRequestsByFailedsStatusAsync();
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                logger.LogInformation(ex, "GetFailedPayments: Exception: {ExceptionMessage}", ExceptionMessage);
            }

            return failedPaymentList;
        }
    }
}
