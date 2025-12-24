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
using Unity.Payments.PaymentRequests;
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

                // Get tenant name for email body
                string tenantName = "N/A";
                if (_currentTenant.Id.HasValue)
                {
                    var tenant = await _tenantRepository.GetAsync(_currentTenant.Id.Value);
                    tenantName = tenant?.Name ?? "N/A";
                }

                // Generate email body
                string emailBody = GenerateEmailBody(tenantName);

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
                        EmailAttachments =
                        [
                            new() {
                                FileName = fileName,
                                Content = excelBytes,  // Byte array, not Base64
                                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                            }
                        ]
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
            IReadOnlyDictionary<Guid, string> userNameDict)
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

    }
}
