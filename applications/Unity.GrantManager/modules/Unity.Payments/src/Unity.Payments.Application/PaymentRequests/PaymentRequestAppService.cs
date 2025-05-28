using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payment.Shared;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.PaymentConfigurations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Features;
using Volo.Abp.Users;
using System.Linq;
using Unity.Payments.Domain.Shared;
using Unity.Payments.Domain.Services;
using Unity.Payments.Enums;
using Microsoft.Extensions.Logging;
using Volo.Abp.Authorization.Permissions;
using Unity.Payments.Permissions;
using Volo.Abp.Data;
using Volo.Abp;
using Microsoft.EntityFrameworkCore;

namespace Unity.Payments.PaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class PaymentRequestAppService : PaymentsAppService, IPaymentRequestAppService
    {
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly IPaymentConfigurationRepository _paymentConfigurationRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IPaymentsManager _paymentsManager;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IDataFilter _dataFilter;

        public PaymentRequestAppService(IPaymentConfigurationRepository paymentConfigurationRepository,
            IPaymentRequestRepository paymentRequestsRepository,
            ICurrentUser currentUser,
            IPaymentsManager paymentsManager,
            IPermissionChecker permissionChecker, IDataFilter dataFilter)
        {
            _paymentConfigurationRepository = paymentConfigurationRepository;
            _paymentRequestsRepository = paymentRequestsRepository;
            _currentUser = currentUser;
            _paymentsManager = paymentsManager;
            _permissionChecker = permissionChecker;
            _dataFilter = dataFilter;
        }

        protected virtual async Task<(PaymentConfiguration? Config, decimal Threshold)> GetPaymentConfigurationWithThresholdAsync()
        {
            var paymentConfigs = await _paymentConfigurationRepository.GetListAsync();
            var paymentConfig = paymentConfigs.FirstOrDefault();

            if (paymentConfig == null)
            {
                return (null, PaymentSharedConsts.DefaultThresholdAmount);
            }

            return (paymentConfig, paymentConfig.PaymentThreshold ?? PaymentSharedConsts.DefaultThresholdAmount);
        }

        [Authorize(PaymentsPermissions.Payments.RequestPayment)]
        public virtual async Task<List<PaymentRequestDto>> CreateAsync(List<CreatePaymentRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> createdPayments = [];
            var (paymentConfig, paymentThreshold) = await GetPaymentConfigurationWithThresholdAsync();
            var paymentIdPrefix = string.Empty;

            if (paymentConfig != null)
            {
                if (paymentConfig.PaymentThreshold != null)
                {
                    paymentThreshold = (decimal)paymentConfig.PaymentThreshold;
                }
                if (!paymentConfig.PaymentIdPrefix.IsNullOrEmpty())
                {
                    paymentIdPrefix = paymentConfig.PaymentIdPrefix;
                }
            }

            var batchNumber = await GetMaxBatchNumberAsync();
            var batchName = $"{paymentIdPrefix}_UNITY_BATCH_{batchNumber}";
            var currentYear = DateTime.UtcNow.Year;
            var sequenceNumber = await GetNextSequenceNumberAsync(currentYear);

            foreach (var paymentRequestItem in paymentRequests.Select((value, i) => new { i, value }))
            {
                try
                {
                    // referenceNumber + Chefs Confirmation ID + 4 digit sequence based on invoice number count
                    CreatePaymentRequestDto paymentRequestDto = paymentRequestItem.value;
                    int applicationPaymentRequestCount = await _paymentRequestsRepository.GetCountByCorrelationId(paymentRequestDto.CorrelationId) + 1;
                    var sequenceForInvoice = applicationPaymentRequestCount.ToString("D4");
                    string referenceNumber = GeneratePaymentNumberAsync(sequenceNumber, paymentRequestItem.i, paymentIdPrefix);

                    paymentRequestDto.InvoiceNumber = $"{referenceNumber}-{paymentRequestDto.InvoiceNumber}-{sequenceForInvoice}";
                    paymentRequestDto.ReferenceNumber = referenceNumber;
                    paymentRequestDto.BatchName = batchName;
                    paymentRequestDto.BatchNumber = batchNumber;
                    paymentRequestDto.PaymentThreshold = paymentThreshold;

                    var payment = new PaymentRequest(Guid.NewGuid(), paymentRequestDto);
                    var result = await _paymentRequestsRepository.InsertAsync(payment);
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
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
            return createdPayments;
        }

        private async Task<decimal> GetMaxBatchNumberAsync()
        {
            var paymentRequestList = await _paymentRequestsRepository.GetListAsync();
            decimal batchNumber = 1; // Lookup max plus 1
            if (paymentRequestList != null && paymentRequestList.Count > 0)
            {
                var maxBatchNumber = paymentRequestList.Max(s => s.BatchNumber);

                if (maxBatchNumber > 0)
                {
                    batchNumber = maxBatchNumber + 1;
                }
            }

            return batchNumber;
        }

        public virtual async Task<List<PaymentRequestDto>> UpdateStatusAsync(List<UpdatePaymentStatusRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> updatedPayments = [];

            var paymentThreshold = await GetPaymentThresholdAsync();

            foreach (var dto in paymentRequests)
            {
                try
                {
                    var payment = await _paymentRequestsRepository.GetAsync(dto.PaymentRequestId);
                    var triggerAction = await DetermineTriggerActionAsync(dto, payment, paymentThreshold);

                    if (triggerAction != PaymentApprovalAction.None)
                    {
                        await _paymentsManager.UpdatePaymentStatusAsync(dto.PaymentRequestId, triggerAction);
                        updatedPayments.Add(await CreatePaymentRequestDtoAsync(dto.PaymentRequestId));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }

            return updatedPayments;
        }
        private async Task<PaymentApprovalAction> DetermineTriggerActionAsync(
            UpdatePaymentStatusRequestDto dto,
            PaymentRequest payment,
            decimal paymentThreshold)
        {
            if (await CanPerformLevel1ActionAsync(payment.Status))
            {
                return dto.IsApprove ? PaymentApprovalAction.L1Approve : PaymentApprovalAction.L1Decline;
            }

            if (await CanPerformLevel2ActionAsync(payment))
            {
                if (dto.IsApprove)
                {
                    return payment.Amount > paymentThreshold
                        ? PaymentApprovalAction.L2Approve
                        : PaymentApprovalAction.Submit;
                }
                return PaymentApprovalAction.L2Decline;
            }

            if (await CanPerformLevel3ActionAsync(payment.Status))
            {
                return dto.IsApprove ? PaymentApprovalAction.Submit : PaymentApprovalAction.L3Decline;
            }

            return PaymentApprovalAction.None;
        }

        private async Task<bool> CanPerformLevel1ActionAsync(PaymentRequestStatus status)
        {
            List<PaymentRequestStatus> level1Approvals = new() { PaymentRequestStatus.L1Pending, PaymentRequestStatus.L1Declined };
            return await _permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L1ApproveOrDecline) && level1Approvals.Contains(status);
        }

        private async Task<bool> CanPerformLevel2ActionAsync(PaymentRequest payment)
        {
            List<PaymentRequestStatus> level2Approvals = new() { PaymentRequestStatus.L2Pending, PaymentRequestStatus.L2Declined };
            return await _permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L2ApproveOrDecline) && level2Approvals.Contains(payment.Status);
        }

        private async Task<bool> CanPerformLevel3ActionAsync(PaymentRequestStatus status)
        {
            List<PaymentRequestStatus> level3Approvals = new() { PaymentRequestStatus.L3Pending, PaymentRequestStatus.L3Declined };
            return await _permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L3ApproveOrDecline) && level3Approvals.Contains(status);
        }        

        private async Task<PaymentRequestDto> CreatePaymentRequestDtoAsync(Guid paymentRequestId)
        {
            var payment = await _paymentRequestsRepository.GetAsync(paymentRequestId);
            return new PaymentRequestDto
            {
                Id = payment.Id,
                InvoiceNumber = payment.InvoiceNumber,
                InvoiceStatus = payment.InvoiceStatus,
                Amount = payment.Amount,
                PayeeName = payment.PayeeName,
                SupplierNumber = payment.SupplierNumber,
                ContractNumber = payment.ContractNumber,
                CorrelationId = payment.CorrelationId,
                CorrelationProvider = payment.CorrelationProvider,
                Description = payment.Description,
                CreationTime = payment.CreationTime,
                Status = payment.Status,
                ReferenceNumber = payment.ReferenceNumber,
            };
        }

        public async Task<List<PaymentDetailsDto>> GetListByApplicationIdsAsync(List<Guid> applicationIds)
        {
            var paymentsQueryable = await _paymentRequestsRepository.GetQueryableAsync();
            var payments = await paymentsQueryable.Include(pr => pr.Site).ToListAsync();
            var filteredPayments = payments.Where(pr => applicationIds.Contains(pr.CorrelationId)).ToList();

            return ObjectMapper.Map<List<PaymentRequest>, List<PaymentDetailsDto>>(payments);
        }

        public async Task<PagedResultDto<PaymentRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var totalCount = await _paymentRequestsRepository.GetCountAsync();
            using (_dataFilter.Disable<ISoftDelete>())
            {
                var payments = await _paymentRequestsRepository
                    .GetPagedListAsync(input.SkipCount, input.MaxResultCount, input.Sorting ?? string.Empty, true);

                var mappedPayments = ObjectMapper.Map<List<PaymentRequest>, List<PaymentRequestDto>>(payments);

                ApplyErrorSummary(mappedPayments);

                return new PagedResultDto<PaymentRequestDto>(totalCount, mappedPayments);
            }
        }

        private static void ApplyErrorSummary(List<PaymentRequestDto> mappedPayments)
        {
            mappedPayments.ForEach(mappedPayment =>
            {
                if (!string.IsNullOrWhiteSpace(mappedPayment.CasResponse) &&
                    !mappedPayment.CasResponse.Equals("SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                {
                    mappedPayment.ErrorSummary = mappedPayment.CasResponse;
                }
            });
        }

        public async Task<List<PaymentDetailsDto>> GetListByApplicationIdAsync(Guid applicationId)
        {
            using (_dataFilter.Disable<ISoftDelete>())
            {
                var payments = await _paymentRequestsRepository.GetListAsync();
                var filteredPayments = payments.Where(e => e.CorrelationId == applicationId).ToList();

                return new List<PaymentDetailsDto>(ObjectMapper.Map<List<PaymentRequest>, List<PaymentDetailsDto>>(filteredPayments));
            }
        }
        public async Task<List<PaymentDetailsDto>> GetListByPaymentIdsAsync(List<Guid> paymentIds)
        {
            var payments = await _paymentRequestsRepository.GetListAsync(e => paymentIds.Contains(e.Id));

            return new List<PaymentDetailsDto>(ObjectMapper.Map<List<PaymentRequest>, List<PaymentDetailsDto>>(payments));
        }

        public virtual async Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId)
        {
            return await _paymentRequestsRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);
        }

        protected virtual string GetCurrentRequesterName()
        {
            return $"{_currentUser.Name} {_currentUser.SurName}";
        }

        protected virtual async Task<PaymentConfiguration?> GetPaymentConfigurationAsync()
        {
            var paymentConfigs = await _paymentConfigurationRepository.GetListAsync();

            if (paymentConfigs.Count > 0)
            {
                var paymentConfig = paymentConfigs[0];
                return paymentConfig;
            }

            return null;
        }

        protected virtual async Task<decimal> GetPaymentThresholdAsync()
        {
            var paymentConfigs = await _paymentConfigurationRepository.GetListAsync();

            if (paymentConfigs.Count > 0)
            {
                var paymentConfig = paymentConfigs[0];
                return paymentConfig.PaymentThreshold ?? PaymentSharedConsts.DefaultThresholdAmount;
            }

            return PaymentSharedConsts.DefaultThresholdAmount;
        }

        public static string GeneratePaymentNumberAsync(int sequenceNumber, int index, string paymentIdPrefix)
        {
            var currentYear = DateTime.UtcNow.Year;
            var yearPart = currentYear.ToString();

            sequenceNumber = sequenceNumber + index;
            var sequencePart = sequenceNumber.ToString("D6");

            return $"{paymentIdPrefix}-{yearPart}-{sequencePart}";
        }

        private async Task<int> GetNextSequenceNumberAsync(int currentYear)
        {
            // Retrieve all payment requests
            var payments = await _paymentRequestsRepository.GetListAsync();

            // Filter payments for the current year
            var filteredPayments = payments
                .Where(p => p.CreationTime.Year == currentYear)
                .OrderByDescending(p => p.CreationTime)
                .ToList();

            // Use the first payment in the sorted list (most recent) if available
            if (filteredPayments.Count > 0)
            {
                var latestPayment = filteredPayments[0]; // Access the most recent payment directly
                var referenceParts = latestPayment.ReferenceNumber.Split('-');

                // Extract the sequence number from the reference number safely
                if (referenceParts.Length > 0 && int.TryParse(referenceParts[^1], out int latestSequenceNumber))
                {
                    return latestSequenceNumber + 1;
                }
            }

            // If no payments exist or parsing fails, return the initial sequence number
            return 1;
        }
    }
}
