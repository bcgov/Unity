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
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Users;
using Unity.GrantManager.Identity;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Settings;

namespace Unity.Payments.PaymentRequests
{
    public class FinancialSummaryService : ApplicationService
    {
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;
        private readonly ILocalEventBus _localEventBus;
        private readonly IEmailGroupsRepository _emailGroupsRepository;
        private readonly IEmailGroupUsersRepository _emailGroupUsersRepository;
        private const string PaymentsEmailGroupName = "Payments";

        public FinancialSummaryService (
            IIdentityUserIntegrationService identityUserIntegrationService,
            IPaymentRequestRepository paymentRequestsRepository,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            ILocalEventBus localEventBus,
            IEmailGroupsRepository emailGroupsRepository,
            IEmailGroupUsersRepository emailGroupUsersRepository)
        {          
            _identityUserLookupAppService = identityUserIntegrationService;
            _paymentRequestsRepository = paymentRequestsRepository;
            _tenantRepository = tenantRepository;
            _currentTenant = currentTenant;
            _localEventBus = localEventBus;
            _emailGroupsRepository = emailGroupsRepository;
            _emailGroupUsersRepository = emailGroupUsersRepository;
        }

        public async Task NotifyFinancialAdvisorsAndPaymentGroupOfFailedPayments()
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
                            var usersResult = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
                            var users = usersResult.Items?.Cast<IUserData>() ?? [];
                            List<string> financialAnalystEmails = await GetFinancialAnalystEmails(users);
                            List<string> paymentsEmailGroupAddresses = await GetPaymentsEmailGroupEmailsAsync(users);

                            if (financialAnalystEmails.Count == 0 && paymentsEmailGroupAddresses.Count == 0)
                            {
                                Logger.LogWarning("NotifyFinancialAdvisorsAndPaymentGroupsOfFailedPayments: no recipients found for tenant {TenantId}", tenant.Id);
                                continue;
                            }

                            HashSet<string> recipientEmails = new(financialAnalystEmails, StringComparer.OrdinalIgnoreCase);
                            recipientEmails.UnionWith(paymentsEmailGroupAddresses);

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

        private async Task<List<string>> GetFinancialAnalystEmails(IEnumerable<IUserData> users)
        {
            List<string> financialAnalystEmails = [];
            if (users != null)
            {
                foreach (var user in users)
                {
                    var roles = await _identityUserLookupAppService.GetRoleNamesAsync(user.Id);
                    if(roles != null && roles.Contains(UnityRoles.FinancialAnalyst) && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        financialAnalystEmails.Add(user.Email);
                    }
                }
            }
            return financialAnalystEmails;
        }

        private async Task<List<string>> GetPaymentsEmailGroupEmailsAsync(IEnumerable<IUserData> users)
        {
            List<string> paymentsEmails = [];
            string normalizedPaymentsGroupName = PaymentsEmailGroupName.ToUpperInvariant();
            var paymentsGroup = (await _emailGroupsRepository.GetListAsync(group => group.Name != null && group.Name.ToUpper() == normalizedPaymentsGroupName)).FirstOrDefault();
            if (paymentsGroup == null)
            {
                return paymentsEmails;
            }

            var groupUsers = await _emailGroupUsersRepository.GetListAsync(groupUser => groupUser.GroupId == paymentsGroup.Id);
            if (groupUsers == null || groupUsers.Count == 0)
            {
                return paymentsEmails;
            }

            Dictionary<Guid, string> userEmailLookup = users?
                .Where(user => !string.IsNullOrWhiteSpace(user.Email))
                .GroupBy(user => user.Id)
                .Select(group => group.First())
                .ToDictionary(user => user.Id, user => user.Email)
                ?? [];

            foreach (Guid userId in groupUsers.Select(groupUser => groupUser.UserId).Distinct())
            {
                if (userEmailLookup.TryGetValue(userId, out string? email) && !string.IsNullOrWhiteSpace(email))
                {
                    paymentsEmails.Add(email);
                }
                else
                {
                    Logger.LogWarning("NotifyFinancialAdvisorsOfNightlyFailedPayments: no email found for user {UserId} in Payments email group", userId);
                }
            }

            return paymentsEmails;
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
