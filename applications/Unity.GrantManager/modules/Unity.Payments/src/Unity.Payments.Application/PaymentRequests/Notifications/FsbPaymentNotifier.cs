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
                // Get all unique approver user IDs
                var allApproverIds = fsbPayments
                    .SelectMany(p => p.ExpenseApprovals)
                    .Where(ea => ea.DecisionUserId.HasValue)
                    .Select(ea => ea.DecisionUserId!.Value)
                    .Distinct()
                    .ToList();

                // Get all unique creator user IDs
                var allCreatorIds = fsbPayments
                    .Select(p => p.CreatorId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                // Combine approver and creator IDs for single lookup
                var allUserIds = allApproverIds.Concat(allCreatorIds).Distinct().ToList();

                Dictionary<Guid, string> userNameDict = [];
                if (allUserIds.Count > 0)
                {
                    var users = await _identityUserRepository.GetListByIdsAsync(allUserIds);
                    userNameDict = users.ToDictionary(
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

                        // Column 4: CAS Supplier/Site Number - Combined format
                        if (payment.Site != null)
                        {
                            string supplierNumber = "N/A";
                            string siteNumber = payment.Site.Number ?? "N/A";

                            if (payment.Site.Supplier != null)
                            {
                                supplierNumber = payment.Site.Supplier.Number ?? "N/A";
                            }

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
                            if (payment.Site.PaymentGroup == PaymentGroup.EFT)
                            {
                                paymentData.PayGroup = "EFT";
                            }
                            else if (payment.Site.PaymentGroup == PaymentGroup.Cheque)
                            {
                                paymentData.PayGroup = "Cheque";
                            }
                            else
                            {
                                paymentData.PayGroup = "N/A";
                            }
                        }
                        else
                        {
                            paymentData.CasSupplierSiteNumber = "N/A/N/A";
                            paymentData.PayeeAddress = "N/A";
                            paymentData.PayGroup = "N/A";
                        }

                        // Column 17: Payment Requester
                        if (payment.CreatorId.HasValue && userNameDict.TryGetValue(payment.CreatorId.Value, out var requesterName))
                        {
                            paymentData.PaymentRequester = requesterName;
                        }

                        // Get approval data
                        var l1Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level1);
                        if (l1Approval != null)
                        {
                            // Columns 6, 10, 12: All use L1 Approval Date
                            paymentData.InvoiceDate = l1Approval.DecisionDate;
                            paymentData.GoodsServicesReceivedDate = l1Approval.DecisionDate;
                            paymentData.QRApprovalDate = l1Approval.DecisionDate;

                            // Column 11: Qualifier Receiver (L1 Approver name)
                            if (l1Approval.DecisionUserId.HasValue && userNameDict.TryGetValue(l1Approval.DecisionUserId.Value, out var l1Name))
                            {
                                paymentData.QualifierReceiver = l1Name;
                            }
                        }

                        var l2Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level2);
                        if (l2Approval != null)
                        {
                            // Column 14: EA-Approval Date
                            paymentData.EAApprovalDate = l2Approval.DecisionDate;

                            // Column 13: Expense Authority (L2 Approver name)
                            if (l2Approval.DecisionUserId.HasValue && userNameDict.TryGetValue(l2Approval.DecisionUserId.Value, out var l2Name))
                            {
                                paymentData.ExpenseAuthority = l2Name;
                            }
                        }

                        var l3Approval = payment.ExpenseApprovals.FirstOrDefault(ea => ea.Type == ExpenseApprovalType.Level3);
                        if (l3Approval != null)
                        {
                            // Column 20: L3 Approval Date
                            paymentData.L3ApprovalDate = l3Approval.DecisionDate;

                            // Column 19: L3 Approver
                            if (l3Approval.DecisionUserId.HasValue && userNameDict.TryGetValue(l3Approval.DecisionUserId.Value, out var l3Name))
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
