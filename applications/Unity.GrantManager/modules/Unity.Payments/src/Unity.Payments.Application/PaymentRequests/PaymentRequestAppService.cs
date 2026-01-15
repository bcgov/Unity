using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.Exceptions;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Services;
using Unity.Payments.Domain.Shared;
using Unity.Payments.Enums;
using Unity.Payments.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.Features;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Users;
using Unity.Payments.PaymentRequests.Notifications;

namespace Unity.Payments.PaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class PaymentRequestAppService(
                ICurrentUser currentUser,
                IDataFilter dataFilter,
                IPermissionChecker permissionChecker,
                IPaymentsManager paymentsManager,
                FsbPaymentNotifier fsbPaymentNotifier,
                IPaymentRequestManager paymentRequestManager) : PaymentsAppService, IPaymentRequestAppService

    {
        public async Task<Guid?> GetDefaultAccountCodingId()
        {
            return await paymentRequestManager.GetDefaultAccountCodingIdAsync();
        }

        [Authorize(PaymentsPermissions.Payments.RequestPayment)]
        public virtual async Task<List<PaymentRequestDto>> CreateAsync(List<CreatePaymentRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> createdPayments = [];
            var paymentConfig = await paymentRequestManager.GetPaymentConfigurationAsync();
            var paymentIdPrefix = string.Empty;

            if (paymentConfig != null && !paymentConfig.PaymentIdPrefix.IsNullOrEmpty())
            {
                paymentIdPrefix = paymentConfig.PaymentIdPrefix;
            }

            var batchNumber = await paymentRequestManager.GetMaxBatchNumberAsync();
            var batchName = $"{paymentIdPrefix}_UNITY_BATCH_{batchNumber}";
            var currentYear = DateTime.UtcNow.Year;
            var nextSequenceNumber = await paymentRequestManager.GetNextSequenceNumberAsync(currentYear);

            foreach (var paymentRequestItem in paymentRequests.Select((value, i) => new { i, value }))
            {
                try
                {
                    // referenceNumber + Chefs Confirmation ID + 6 digit sequence based on sequence number and index
                    CreatePaymentRequestDto paymentRequestDto = paymentRequestItem.value;
                    string referenceNumberPrefix = paymentRequestManager.GenerateReferenceNumberPrefix(paymentIdPrefix);
                    string sequenceNumber = paymentRequestManager.GenerateSequenceNumber(nextSequenceNumber, paymentRequestItem.i);
                    string referenceNumber = paymentRequestManager.GenerateReferenceNumber(referenceNumberPrefix, sequenceNumber);
                    string invoiceNumber = paymentRequestManager.GenerateInvoiceNumber(referenceNumberPrefix, paymentRequestDto.InvoiceNumber, sequenceNumber);

                    paymentRequestDto.InvoiceNumber = invoiceNumber;
                    paymentRequestDto.ReferenceNumber = referenceNumber;
                    paymentRequestDto.BatchName = batchName;
                    paymentRequestDto.BatchNumber = batchNumber;

                    var payment = new PaymentRequest(Guid.NewGuid(), paymentRequestDto);
                    var result = await paymentRequestManager.InsertPaymentRequestAsync(payment);
                    createdPayments.Add(new PaymentRequestDto()
                    {
                        Id = result.Id,
                        InvoiceNumber = result.InvoiceNumber,
                        InvoiceStatus = result.InvoiceStatus,
                        Amount = result.Amount,
                        PayeeName = result.PayeeName,
                        SupplierNumber = result.SupplierNumber,
                        ContractNumber = result.ContractNumber,
                        CorrelationId = result.CorrelationId,
                        CorrelationProvider = result.CorrelationProvider,
                        Description = result.Description,
                        CreationTime = result.CreationTime,
                        Status = result.Status,
                        ReferenceNumber = result.ReferenceNumber,
                        SubmissionConfirmationCode = result.SubmissionConfirmationCode
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
            return createdPayments;
        }

        public async Task<string> GetNextBatchInfoAsync()
        {
            return await paymentRequestManager.GetNextBatchInfoAsync();
        }

        public Task<int> GetPaymentRequestCountBySiteIdAsync(Guid siteId)
        {
            return paymentRequestManager.GetPaymentRequestCountBySiteIdAsync(siteId);
        }

        public virtual async Task<List<PaymentRequestDto>> UpdateStatusAsync(List<UpdatePaymentStatusRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> updatedPayments = [];
            List<Guid> fsbPaymentIds = []; // Track FSB payments

            // Check approval batches
            var approvalRequests = paymentRequests.Where(r => r.IsApprove).Select(x => x.PaymentRequestId).ToList();
            var approvalList = await paymentRequestManager.GetPaymentRequestsByIdsAsync(approvalRequests, includeDetails: true);

            // Rule AB#26693: Reject Payment Request update batch if violates L1 and L2 separation of duties
            if (approvalList.Exists(
                x => x.Status == PaymentRequestStatus.L2Pending
                && CurrentUser.Id == x.ExpenseApprovals.FirstOrDefault(y => y.Type == ExpenseApprovalType.Level1)?.DecisionUserId))
            {
                throw new BusinessException(
                    code: ErrorConsts.L2ApproverRestriction,
                    message: L[ErrorConsts.L2ApproverRestriction]);
            }

            foreach (var dto in paymentRequests)
            {
                try
                {
                    var payment = await paymentRequestManager.GetPaymentRequestByIdAsync(dto.PaymentRequestId);
                    if (payment == null)
                        continue;

                    var previousStatus = payment.Status; // Capture previous status

                    if (!string.IsNullOrWhiteSpace(payment.Note) && !string.IsNullOrWhiteSpace(dto.Note))
                    {
                        payment.SetNote($"{payment.Note}; {dto.Note}");
                    }
                    else
                    {
                        payment.SetNote(dto.Note);
                    }

                    var triggerAction = await DetermineTriggerActionAsync(dto, payment);

                    if (triggerAction != PaymentApprovalAction.None)
                    {
                        await paymentsManager.UpdatePaymentStatusAsync(dto.PaymentRequestId, triggerAction);

                        // Check if payment transitioned to FSB status
                        var updatedPayment = await paymentRequestManager.GetPaymentRequestByIdAsync(dto.PaymentRequestId);
                        if (updatedPayment?.Status == PaymentRequestStatus.FSB &&
                            previousStatus != PaymentRequestStatus.FSB)
                        {
                            fsbPaymentIds.Add(dto.PaymentRequestId);
                        }

                        updatedPayments.Add(await paymentRequestManager.CreatePaymentRequestDtoAsync(dto.PaymentRequestId));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }

            // Send FSB notification if payments reached FSB status
            if (fsbPaymentIds.Count > 0)
            {
                try
                {
                    var fsbPayments = await paymentRequestManager.GetPaymentRequestsByIdsAsync(fsbPaymentIds, includeDetails: true);

                    await fsbPaymentNotifier.NotifyFsbPayments(fsbPayments);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to send FSB payment notification");
                    // Don't throw - email failure shouldn't fail approval process
                }
            }

            return updatedPayments;
        }

        private async Task<PaymentApprovalAction> DetermineTriggerActionAsync(
            UpdatePaymentStatusRequestDto dto,
            PaymentRequest payment)
        {
            if (payment == null)
            {
                Logger.LogWarning("Payment is null in DetermineTriggerActionAsync.");
                return PaymentApprovalAction.None;
            }

            try
            {
                if (await CanPerformLevel1ActionAsync(payment.Status))
                    return dto.IsApprove ? PaymentApprovalAction.L1Approve : PaymentApprovalAction.L1Decline;

                if (await CanPerformLevel2ActionAsync(payment, dto.IsApprove))
                    return await GetLevel2ApprovalActionAsync(dto, payment);

                if (await CanPerformLevel3ActionAsync(payment.Status))
                    return dto.IsApprove ? PaymentApprovalAction.Submit : PaymentApprovalAction.L3Decline;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return PaymentApprovalAction.None;
        }

        private async Task<PaymentApprovalAction> GetLevel2ApprovalActionAsync(UpdatePaymentStatusRequestDto dto, PaymentRequest payment)
        {
            if (!dto.IsApprove)
                return PaymentApprovalAction.L2Decline;

            decimal? threshold = null;
            try
            {
                decimal? userPaymentThreshold = await GetUserPaymentThresholdAsync();
                threshold = await paymentRequestManager.GetPaymentRequestThresholdByApplicationIdAsync(payment.CorrelationId, userPaymentThreshold);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get payment threshold for applicationId: {CorrelationId}", payment.CorrelationId);
            }

            if (threshold.HasValue && payment.Amount > threshold.Value)
                return PaymentApprovalAction.L2Approve;

            return PaymentApprovalAction.Submit;
        }        
    
        private async Task<bool> CanPerformLevel1ActionAsync(PaymentRequestStatus status)
        {
            List<PaymentRequestStatus> level1Approvals = [PaymentRequestStatus.L1Pending, PaymentRequestStatus.L1Declined];
            return await permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L1ApproveOrDecline) && level1Approvals.Contains(status);
        }

        private async Task<bool> CanPerformLevel2ActionAsync(PaymentRequest payment, bool IsApprove)
        {
            List<PaymentRequestStatus> level2Approvals = [PaymentRequestStatus.L2Pending, PaymentRequestStatus.L2Declined];

            // Rule AB#26693: Reject Payment Request update if violates L1 and L2 separation of duties
            var IsSameApprover = CurrentUser.Id == payment.ExpenseApprovals.FirstOrDefault(x => x.Type == ExpenseApprovalType.Level1)?.DecisionUserId;
            if (IsSameApprover && IsApprove)
            {
                throw new BusinessException(
                    code: ErrorConsts.L2ApproverRestriction,
                    message: L[ErrorConsts.L2ApproverRestriction]);
            }
            return await permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L2ApproveOrDecline) && level2Approvals.Contains(payment.Status);
        }

        private async Task<bool> CanPerformLevel3ActionAsync(PaymentRequestStatus status)
        {
            List<PaymentRequestStatus> level3Approvals = [PaymentRequestStatus.L3Pending, PaymentRequestStatus.L3Declined];
            return await permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L3ApproveOrDecline) && level3Approvals.Contains(status);
        }

        public async Task<List<PaymentDetailsDto>> GetListByApplicationIdsAsync(List<Guid> applicationIds)
        {
            return await paymentRequestManager.GetListByApplicationIdsAsync(applicationIds);
        }

        public async Task<PagedResultDto<PaymentRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var totalCount = await paymentRequestManager.GetPaymentRequestCountAsync();
            using (dataFilter.Disable<ISoftDelete>())
            {
                var paymentWithIncludes = await paymentRequestManager.GetPagedPaymentRequestsWithIncludesAsync(input.SkipCount, input.MaxResultCount, input.Sorting ?? string.Empty);

                var mappedPayments = await paymentRequestManager.MapToDtoAndLoadDetailsAsync(paymentWithIncludes);

                paymentRequestManager.ApplyErrorSummary(mappedPayments);

                return new PagedResultDto<PaymentRequestDto>(totalCount, mappedPayments);
            }
        }

        public async Task<List<PaymentDetailsDto>> GetListByApplicationIdAsync(Guid applicationId)
        {
            using (dataFilter.Disable<ISoftDelete>())
            {
                return await paymentRequestManager.GetListByApplicationIdAsync(applicationId);
            }
        }

        public async Task<List<PaymentDetailsDto>> GetListByPaymentIdsAsync(List<Guid> paymentIds)
        {
            return await paymentRequestManager.GetListByPaymentIdsAsync(paymentIds);
        }

        public virtual async Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId)
        {
            return await paymentRequestManager.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);
        }

        public async Task<decimal?> GetUserPaymentThresholdAsync()
        {
            return await paymentRequestManager.GetUserPaymentThresholdAsync(currentUser.Id);
        }

        protected virtual string GetCurrentRequesterName()
        {
            return $"{currentUser.Name} {currentUser.SurName}";
        }

        public async Task ManuallyAddPaymentRequestsToReconciliationQueue(List<Guid> paymentRequestIds)
        {
            await paymentRequestManager.ManuallyAddPaymentRequestsToReconciliationQueueAsync(paymentRequestIds);
        }
    }
}
