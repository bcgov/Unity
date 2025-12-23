using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Settings;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Enums;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;

namespace Unity.Payments.PaymentRequests.Notifications
{
    /// <summary>
    /// Service responsible for sending email notifications when payments reach FSB status
    /// </summary>
    public class FsbPaymentNotifier : ISingletonDependency
    {        
        private readonly IApplicationRepository _applicationRepository;
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly ILocalEventBus _localEventBus;
        private readonly ISettingProvider _settingProvider;
        private readonly FsbApEmailGroupStrategy _fsbApEmailGroupStrategy;
        private readonly ILogger<FsbPaymentNotifier> _logger;

        public FsbPaymentNotifier(
            IPaymentRequestRepository paymentRequestsRepository,
            IApplicationRepository applicationRepository,
            IIdentityUserRepository identityUserRepository,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            ILocalEventBus localEventBus,
            ISettingProvider settingProvider,
            FsbPaymentExcelGenerator excelGenerator,
            FsbApEmailGroupStrategy fsbApEmailGroupStrategy,
            ILogger<FsbPaymentNotifier> logger)
        {
            _applicationRepository = applicationRepository;
            _identityUserRepository = identityUserRepository;
            _tenantRepository = tenantRepository;
            _currentTenant = currentTenant;
            _localEventBus = localEventBus;
            _settingProvider = settingProvider;            
            _fsbApEmailGroupStrategy = fsbApEmailGroupStrategy;
            _logger = logger;
        }

        /// <summary>
        /// Sends email notification with Excel attachment for FSB payments
        /// </summary>
        /// <param name="fsbPayments">List of payments that have reached FSB status</param>
        public async Task NotifyFsbPayments(List<PaymentRequest> fsbPayments)
        {
            if (fsbPayments == null || fsbPayments.Count == 0)
            {
                _logger.LogDebug("NotifyFsbPayments: No FSB payments to notify");
                return;
            }

            try
            {
                _logger.LogInformation("NotifyFsbPayments: Processing {Count} FSB payments", fsbPayments.Count);

                // Get recipients from FSB-AP email group
                var recipients = await _fsbApEmailGroupStrategy.GetEmailRecipientsAsync();
                if (recipients == null || recipients.Count == 0)
                {
                    _logger.LogWarning("NotifyFsbPayments: No recipients found in FSB-AP email group. Email not sent.");
                    return;
                }

                // Collect payment data for Excel
                var paymentDataList = await CollectPaymentData(fsbPayments);
                if (paymentDataList.Count == 0)
                {
                    _logger.LogWarning("NotifyFsbPayments: Failed to collect payment data. Email not sent.");
                    return;
                }

                // Generate Excel file
                byte[] excelBytes = FsbPaymentExcelGenerator.GenerateExcelFile(paymentDataList);
                string fileName = $"FSB_Payments_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

                // Generate email body
                string emailBody = GenerateEmailBody(paymentDataList.Count);

                // Get from address
                var defaultFromAddress = await _settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
                string fromAddress = defaultFromAddress ?? "NoReply@gov.bc.ca";

                // Publish email event with attachment
                await _localEventBus.PublishAsync(
                    new EmailNotificationEvent
                    {
                        Action = EmailAction.SendFsbNotification,
                        TenantId = _currentTenant.Id,
                        RetryAttempts = 0,
                        Body = emailBody,
                        Subject = "FSB Payment Notification",
                        EmailFrom = fromAddress,
                        EmailAddressList = recipients,
                        ApplicationId = Guid.Empty,  // System-level email, not application-specific
                        EmailAttachments = new List<EmailAttachmentData>
                        {
                            new EmailAttachmentData
                            {
                                FileName = fileName,
                                Content = excelBytes,  // Byte array, not Base64
                                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                            }
                        }
                    }
                );

                _logger.LogInformation("NotifyFsbPayments: Email notification published successfully for {Count} payments", fsbPayments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyFsbPayments: Error sending FSB payment notification");
                throw new InvalidOperationException(
                    $"Failed to send FSB payment notification. See inner exception for details.",
                    ex);
            }
        }

        /// <summary>
        /// Collects detailed payment data from database for Excel export
        /// </summary>
        private async Task<List<FsbPaymentData>> CollectPaymentData(List<PaymentRequest> fsbPayments)
        {
            var paymentDataList = new List<FsbPaymentData>();

            try
            {
                // Get all application IDs to fetch in batch
                var applicationIds = fsbPayments.Select(p => p.CorrelationId).Distinct().ToList();
                var applications = await _applicationRepository.GetListAsync(a => applicationIds.Contains(a.Id));
                var applicationDict = applications.ToDictionary(a => a.Id);

                // Get tenant name
                string tenantName = "N/A";
                if (_currentTenant.Id.HasValue)
                {
                    var tenant = await _tenantRepository.GetAsync(_currentTenant.Id.Value);
                    tenantName = tenant?.Name ?? "N/A";
                }

                // Get all unique approver user IDs
                var allApproverIds = fsbPayments
                    .SelectMany(p => p.ExpenseApprovals)
                    .Where(ea => ea.DecisionUserId.HasValue)
                    .Select(ea => ea.DecisionUserId!.Value)
                    .Distinct()
                    .ToList();

                Dictionary<Guid, string> approverNameDict = new();
                if (allApproverIds.Count > 0)
                {
                    var approvers = await _identityUserRepository.GetListByIdsAsync(allApproverIds);
                    approverNameDict = approvers.ToDictionary(
                        u => u.Id,
                        u => $"{u.Name} {u.Surname}".Trim()
                    );
                }

                // Process each payment
                foreach (var payment in fsbPayments)
                {
                    try
                    {
                        var paymentData = new FsbPaymentData
                        {
                            PaymentId = payment.Id,
                            Amount = payment.Amount,
                            DateApproved = payment.LastModificationTime,
                            TenantName = tenantName,
                            BatchNumber = payment.BatchNumber.ToString(),
                            InvoiceNumber = payment.InvoiceNumber,
                            ContractNumber = payment.ContractNumber,
                            PaymentGroup = "N/A" // Not currently tracked in PaymentRequest
                        };

                        // Get application data
                        if (applicationDict.TryGetValue(payment.CorrelationId, out var application))
                        {
                            //paymentData.ApplicantName = application.ApplicantName ?? "N/A";
                            paymentData.ProjectName = application.ProjectName ?? "N/A";
                        }
                        else
                        {
                            paymentData.ApplicantName = "N/A";
                            paymentData.ProjectName = "N/A";
                        }

                        // Get site data
                        if (payment.Site != null)
                        {
                            paymentData.SiteNumber = payment.Site.Number;
                            if (payment.Site.Supplier != null)
                            {
                                paymentData.SupplierNumber = payment.Site.Supplier.Number;
                            }
                        }

                        // Get approval data
                        var l1Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level1);
                        if (l1Approval != null)
                        {
                            paymentData.L1ApprovalDate = l1Approval.DecisionDate;
                            if (l1Approval.DecisionUserId.HasValue && approverNameDict.TryGetValue(l1Approval.DecisionUserId.Value, out var l1Name))
                            {
                                paymentData.L1Approver = l1Name;
                            }
                        }

                        var l2Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level2);
                        if (l2Approval != null)
                        {
                            paymentData.L2ApprovalDate = l2Approval.DecisionDate;
                            if (l2Approval.DecisionUserId.HasValue && approverNameDict.TryGetValue(l2Approval.DecisionUserId.Value, out var l2Name))
                            {
                                paymentData.L2Approver = l2Name;
                            }
                        }

                        var l3Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level3);
                        if (l3Approval != null)
                        {
                            paymentData.L3ApprovalDate = l3Approval.DecisionDate;
                            if (l3Approval.DecisionUserId.HasValue && approverNameDict.TryGetValue(l3Approval.DecisionUserId.Value, out var l3Name))
                            {
                                paymentData.L3Approver = l3Name;
                            }
                        }

                        paymentDataList.Add(paymentData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "NotifyFsbPayments: Error collecting data for payment {PaymentId}", payment.Id);
                        // Continue processing other payments
                    }
                }

                _logger.LogInformation("NotifyFsbPayments: Successfully collected data for {Count} payments", paymentDataList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyFsbPayments: Error collecting payment data");
            }

            return paymentDataList;
        }

        /// <summary>
        /// Generates HTML email body
        /// </summary>
        private static string GenerateEmailBody(int paymentCount)
        {
            return $@"
<html>
<body>
    <p>Hello,</p>
    <p>This email is to notify you that <strong>{paymentCount}</strong> payment(s) have been approved and sent to FSB (Financial Services Branch) for processing.</p>
    <p>Please find the attached Excel file with detailed payment information.</p>
    <p>Thank you.</p>
    <br/>
    <p><em>This is an automated notification. Please do not reply to this email.</em></p>
</body>
</html>";
        }
    }
}
