using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly ILocalEventBus _localEventBus;
        private readonly ISettingProvider _settingProvider;
        private readonly FsbApEmailGroupStrategy _fsbApEmailGroupStrategy;
        private readonly ILogger<FsbPaymentNotifier> _logger;

        public FsbPaymentNotifier(            
            IIdentityUserRepository identityUserRepository,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            ILocalEventBus localEventBus,
            ISettingProvider settingProvider,            
            FsbApEmailGroupStrategy fsbApEmailGroupStrategy,
            ILogger<FsbPaymentNotifier> logger)
        {
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
                // Get recipients from FSB-AP email group
                var recipients = await _fsbApEmailGroupStrategy.GetEmailRecipientsAsync();
                if (recipients == null || recipients.Count == 0)
                {
                    _logger.LogWarning("NotifyFsbPayments: No recipients found in FSB-AP email group. Email not sent.");
                    return;
                }

                // Group payments by batch name (treating null/empty as "Unknown")
                var batchGroups = fsbPayments
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.BatchName) ? "Unknown" : p.BatchName)
                    .ToList();

                _logger.LogInformation(
                    "NotifyFsbPayments: Grouped {TotalPayments} payments into {BatchCount} batches",
                    fsbPayments.Count,
                    batchGroups.Count);

                // Get tenant name for email body
                string tenantName = "N/A";
                if (_currentTenant.Id.HasValue)
                {
                    var tenant = await _tenantRepository.GetAsync(_currentTenant.Id.Value);
                    tenantName = tenant?.Name ?? "N/A";
                }

                // Get from address
                var defaultFromAddress = await _settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
                string fromAddress = defaultFromAddress ?? "NoReply@gov.bc.ca";

                // Process each batch
                int successCount = 0;
                int failureCount = 0;

                foreach (var batchGroup in batchGroups)
                {
                    string batchName = batchGroup.Key;
                    var batchPayments = batchGroup.ToList();

                    try
                    {
                        await SendBatchNotification(
                            batchName,
                            batchPayments,
                            recipients,
                            tenantName,
                            fromAddress);

                        successCount++;
                        _logger.LogInformation(
                            "NotifyFsbPayments: Successfully sent notification for batch '{BatchName}' with {PaymentCount} payments",
                            batchName,
                            batchPayments.Count);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(
                            ex,
                            "NotifyFsbPayments: Failed to send notification for batch '{BatchName}' with {PaymentCount} payments. Continuing with other batches.",
                            batchName,
                            batchPayments.Count);
                        // Continue processing other batches (resilient processing)
                    }
                }

                _logger.LogInformation(
                    "NotifyFsbPayments: Completed processing {TotalBatches} batches. Success: {SuccessCount}, Failed: {FailureCount}",
                    batchGroups.Count,
                    successCount,
                    failureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyFsbPayments: Critical error during batch processing");
                throw new InvalidOperationException(
                    $"Failed to process FSB payment notifications. See inner exception for details.",
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
                var userNameDict = await BuildUserNameDictionaryAsync(fsbPayments);

                // Process each payment
                foreach (var payment in fsbPayments)
                {
                    try
                    {
                        var paymentData = CreateBasePaymentData(payment);
                        ApplySiteData(paymentData, payment);
                        ApplyPaymentRequester(paymentData, payment, userNameDict);
                        ApplyApprovals(paymentData, payment, userNameDict);

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

        private async Task<Dictionary<Guid, string>> BuildUserNameDictionaryAsync(List<PaymentRequest> fsbPayments)
        {
            var allApproverIds = fsbPayments
                .SelectMany(p => p.ExpenseApprovals)
                .Where(ea => ea.DecisionUserId.HasValue)
                .Select(ea => ea.DecisionUserId!.Value)
                .Distinct()
                .ToList();

            var allCreatorIds = fsbPayments
                .Select(p => p.CreatorId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var allUserIds = allApproverIds.Concat(allCreatorIds).Distinct().ToList();
            if (allUserIds.Count == 0)
            {
                return [];
            }

            var users = await _identityUserRepository.GetListByIdsAsync(allUserIds);
            return users.ToDictionary(
                u => u.Id,
                u => $"{u.Name} {u.Surname}".Trim()
            );
        }

        private static FsbPaymentData CreateBasePaymentData(PaymentRequest payment)
        {
            return new FsbPaymentData
            {
                // Column 1: Batch # - Use BatchName instead of BatchNumber
                BatchName = payment.BatchName ?? "N/A",

                // Column 2: Contract Number
                ContractNumber = payment.ContractNumber,

                // Column 3: Payee Name
                PayeeName = payment.PayeeName ?? "N/A",

                // Column 7: Invoice Number
                InvoiceNumber = payment.InvoiceNumber,

                // Column 8: Amount
                Amount = payment.Amount,

                // Column 15: CAS Cheque Stub Description
                CasCheckStubDescription = payment.Description,

                // Column 16: Account Coding
                AccountCoding = AccountCodingFormatter.Format(payment.AccountCoding),

                // Column 18: Requested On
                RequestedOn = payment.CreationTime
            };
        }

        private static void ApplySiteData(FsbPaymentData paymentData, PaymentRequest payment)
        {
            if (payment.Site == null)
            {
                paymentData.CasSupplierSiteNumber = "N/A/N/A";
                paymentData.PayeeAddress = "N/A";
                paymentData.PayGroup = "N/A";
                return;
            }

            string supplierNumber = payment.Site.Supplier?.Number ?? "N/A";
            string siteNumber = payment.Site.Number ?? "N/A";

            // Column 4: CAS Supplier/Site Number - Combined format
            paymentData.CasSupplierSiteNumber = $"{supplierNumber}/{siteNumber}";

            // Column 5: Payee Address - Combined address string
            paymentData.PayeeAddress = FormatAddress(
                payment.Site.AddressLine1,
                payment.Site.AddressLine2,
                payment.Site.AddressLine3,
                payment.Site.City,
                payment.Site.Province,
                payment.Site.PostalCode
            );

            // Column 9: Pay Group - Convert enum to string
            paymentData.PayGroup = FormatPayGroup(payment.Site.PaymentGroup);
        }

        private static string FormatPayGroup(PaymentGroup paymentGroup)
        {
            return paymentGroup switch
            {
                PaymentGroup.EFT => "EFT",
                PaymentGroup.Cheque => "Cheque",
                _ => "N/A"
            };
        }

        private static void ApplyPaymentRequester(
            FsbPaymentData paymentData,
            PaymentRequest payment,
            Dictionary<Guid, string> userNameDict)
        {
            // Column 17: Payment Requester
            if (!payment.CreatorId.HasValue)
            {
                return;
            }

            if (userNameDict.TryGetValue(payment.CreatorId.Value, out var requesterName))
            {
                paymentData.PaymentRequester = requesterName;
            }
        }

        private static void ApplyApprovals(
            FsbPaymentData paymentData,
            PaymentRequest payment,
            IReadOnlyDictionary<Guid, string> userNameDict)
        {
            ApplyLevel1Approval(paymentData, payment, userNameDict);
            ApplyLevel2Approval(paymentData, payment, userNameDict);
            ApplyLevel3Approval(paymentData, payment, userNameDict);
        }

        private static void ApplyLevel1Approval(
            FsbPaymentData paymentData,
            PaymentRequest payment,
            IReadOnlyDictionary<Guid, string> userNameDict)
        {
            var l1Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level1);
            if (l1Approval == null)
            {
                return;
            }

            // Columns 6, 10, 12: All use L1 Approval Date
            paymentData.InvoiceDate = l1Approval.DecisionDate;
            paymentData.GoodsServicesReceivedDate = l1Approval.DecisionDate;
            paymentData.QRApprovalDate = l1Approval.DecisionDate;

            // Column 11: Qualifier Receiver (L1 Approver name)
            if (l1Approval.DecisionUserId.HasValue
                && userNameDict.TryGetValue(l1Approval.DecisionUserId.Value, out var l1Name))
            {
                paymentData.QualifierReceiver = l1Name;
            }
        }

        private static void ApplyLevel2Approval(
            FsbPaymentData paymentData,
            PaymentRequest payment,
            IReadOnlyDictionary<Guid, string> userNameDict)
        {
            var l2Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level2);
            if (l2Approval == null)
            {
                return;
            }

            // Column 14: EA-Approval Date
            paymentData.EAApprovalDate = l2Approval.DecisionDate;

            // Column 13: Expense Authority (L2 Approver name)
            if (l2Approval.DecisionUserId.HasValue
                && userNameDict.TryGetValue(l2Approval.DecisionUserId.Value, out var l2Name))
            {
                paymentData.ExpenseAuthority = l2Name;
            }
        }

        private static void ApplyLevel3Approval(
            FsbPaymentData paymentData,
            PaymentRequest payment,
            IReadOnlyDictionary<Guid, string> userNameDict)
        {
            var l3Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level3);
            if (l3Approval == null)
            {
                return;
            }

            // Column 20: L3 Approval Date
            paymentData.L3ApprovalDate = l3Approval.DecisionDate;

            // Column 19: L3 Approver
            if (l3Approval.DecisionUserId.HasValue
                && userNameDict.TryGetValue(l3Approval.DecisionUserId.Value, out var l3Name))
            {
                paymentData.L3Approver = l3Name;
            }
        }

        /// <summary>
        /// Generates HTML email body
        /// </summary>
        private static string GenerateEmailBody(string tenantName)
        {
            return $@"
<html>
<body>
    <p>Hello,</p>
    <p>Please see the attached spreadsheet for the payment processing request from the {tenantName} program. If you have any questions, please contact payment requester.</p>
    <p>Kind regards.</p>
    <br/>
    <br/>
    <p><em>*ATTENTION - Please do not reply to this email as it is an automated notification which is unable to receive replies.</em></p>
</body>
</html>";
        }

        /// <summary>
        /// Formats address components into a single string
        /// </summary>
        private static string FormatAddress(
            string? addressLine1,
            string? addressLine2,
            string? addressLine3,
            string? city,
            string? province,
            string? postalCode)
        {
            var addressParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(addressLine1))
                addressParts.Add(addressLine1);

            if (!string.IsNullOrWhiteSpace(addressLine2))
                addressParts.Add(addressLine2);

            if (!string.IsNullOrWhiteSpace(addressLine3))
                addressParts.Add(addressLine3);

            // Combine city, province, postal code on one line
            var cityProvincePostal = new List<string>();
            if (!string.IsNullOrWhiteSpace(city))
                cityProvincePostal.Add(city);

            if (!string.IsNullOrWhiteSpace(province))
                cityProvincePostal.Add(province);

            if (!string.IsNullOrWhiteSpace(postalCode))
                cityProvincePostal.Add(postalCode);

            if (cityProvincePostal.Count > 0)
                addressParts.Add(string.Join(", ", cityProvincePostal));

            return addressParts.Count > 0 ? string.Join(", ", addressParts) : "N/A";
        }

        /// <summary>
        /// Sends email notification for a single batch of payments
        /// </summary>
        /// <param name="batchName">Name of the batch (already normalized for "Unknown")</param>
        /// <param name="batchPayments">List of payment requests in this batch</param>
        /// <param name="recipients">Email recipients list</param>
        /// <param name="tenantName">Current tenant name for email body</param>
        /// <param name="fromAddress">Email from address</param>
        private async Task SendBatchNotification(
            string batchName,
            List<PaymentRequest> batchPayments,
            List<string> recipients,
            string tenantName,
            string fromAddress)
        {
            // Collect payment data for this batch
            var paymentDataList = await CollectPaymentData(batchPayments);
            if (paymentDataList.Count == 0)
            {
                _logger.LogWarning(
                    "SendBatchNotification: Failed to collect payment data for batch '{BatchName}'. Email not sent.",
                    batchName);
                return;
            }

            // Generate Excel file
            byte[] excelBytes = FsbPaymentExcelGenerator.GenerateExcelFile(paymentDataList);

            // Generate filename with sanitized batch name
            string sanitizedBatchName = SanitizeFileName(batchName);
            string fileName = $"FSB_Payments_{sanitizedBatchName}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.xlsx";

            // Generate email body (reuse existing method)
            string emailBody = GenerateEmailBody(tenantName);

            // Generate email subject per requirement
            string subject = $"Batch # {batchName}";

            // Extract payment IDs for tracking
            var paymentIds = batchPayments.Select(p => p.Id).ToList();

            // Publish email event with attachment
            await _localEventBus.PublishAsync(
                new EmailNotificationEvent
                {
                    Action = EmailAction.SendFsbNotification,
                    TenantId = _currentTenant.Id,
                    RetryAttempts = 0,
                    Body = emailBody,
                    Subject = subject,  // Batch-specific subject
                    EmailFrom = fromAddress,
                    EmailAddressList = recipients,
                    ApplicationId = Guid.Empty,
                    EmailAttachments =
                    [
                        new() {
                            FileName = fileName,  // Batch-specific filename
                            Content = excelBytes,
                            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        }
                    ],
                    PaymentRequestIds = paymentIds  // Track which payments are in this email
                }
            );
        }

        /// <summary>
        /// Sanitizes batch name for use in filenames by removing invalid characters
        /// </summary>
        /// <param name="batchName">Original batch name</param>
        /// <returns>Sanitized batch name safe for filenames</returns>
        private static string SanitizeFileName(string batchName)
        {
            if (string.IsNullOrWhiteSpace(batchName))
            {
                return "Unknown";
            }

            // Get OS-specific invalid filename characters
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // Replace invalid characters with underscore
            string sanitized = batchName;
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Replace spaces with underscores for cleaner filenames
            sanitized = sanitized.Replace(' ', '_');

            // Trim to reasonable length (Windows has 255 char limit)
            // Reserve space for: "FSB_Payments_" (13) + "_yyyyMMdd_HHmmssfff.xlsx" (25) = 38 chars
            const int maxBatchNameLength = 217;  // Conservative limit (255 - 38 = 217)
            if (sanitized.Length > maxBatchNameLength)
            {
                sanitized = sanitized.Substring(0, maxBatchNameLength);
            }

            return sanitized;
        }

    }
}
