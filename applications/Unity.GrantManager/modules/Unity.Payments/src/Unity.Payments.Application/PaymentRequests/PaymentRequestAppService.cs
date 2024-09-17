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

        public virtual async Task<List<PaymentRequestDto>> CreateAsync(List<CreatePaymentRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> createdPayments = [];
            var paymentConfig = await GetPaymentConfigurationAsync();
            var paymentThreshold = PaymentSharedConsts.DefaultThresholdAmount;
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

            var currentYear = DateTime.UtcNow.Year;
            var sequenceNumber = await GetNextSequenceNumberAsync(currentYear);

            foreach (var item in paymentRequests.Select((value, i) => new { i, value }))
            {
                // referenceNumber + Chefs Confirmation ID + 4 digit sequence based on invoice number count
                var dto = item.value;
                int applicationPaymentRequestCount = await _paymentRequestsRepository.GetCountByCorrelationId(dto.CorrelationId) + 1;
                var sequenceForInvoice = applicationPaymentRequestCount.ToString("D4");
                var referenceNumber = GeneratePaymentNumberAsync(sequenceNumber, item.i, paymentIdPrefix);

                try
                {
                    var payment = new PaymentRequest(Guid.NewGuid(),
                         $"{referenceNumber}-{dto.InvoiceNumber}-{sequenceForInvoice}",
                         dto.Amount,
                         dto.PayeeName,
                         dto.ContractNumber,
                         dto.SupplierNumber,
                         dto.SiteId,
                         dto.CorrelationId,
                         dto.CorrelationProvider,
                         referenceNumber,
                         dto.Description,
                         paymentThreshold
                     );

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

        public virtual async Task<List<PaymentRequestDto>> UpdateStatusAsync(List<UpdatePaymentStatusRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> updatedPayments = [];

            var paymentThreshold = await GetPaymentThresholdAsync();

            foreach (var dto in paymentRequests)
            {
                var payment = await _paymentRequestsRepository.GetAsync(dto.PaymentRequestId);

                List<PaymentRequestStatus> level1Approvals = [PaymentRequestStatus.L1Pending, PaymentRequestStatus.L1Declined];
                List<PaymentRequestStatus> level2Approvals = [PaymentRequestStatus.L2Pending, PaymentRequestStatus.L2Declined];
                List<PaymentRequestStatus> level3Approvals = [PaymentRequestStatus.L3Pending, PaymentRequestStatus.L3Declined];

                var triggerAction = PaymentApprovalAction.None;

                if (await _permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L1ApproveOrDecline) && (payment.Status.IsIn(level1Approvals)))
                {
                    triggerAction = dto.IsApprove ? PaymentApprovalAction.L1Approve : PaymentApprovalAction.L1Decline;
                }


                if (await _permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L2ApproveOrDecline) && (payment.Status.IsIn(level2Approvals)))
                {
                    if (payment.Amount > paymentThreshold)
                    {
                        triggerAction = dto.IsApprove ? (PaymentApprovalAction.L2Approve) : PaymentApprovalAction.L2Decline;
                    }
                    else
                    {
                        triggerAction = dto.IsApprove ? (PaymentApprovalAction.Submit) : PaymentApprovalAction.L2Decline;
                    }
                }

                if (await _permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.L3ApproveOrDecline) && payment.Status.IsIn(level3Approvals))
                {
                    triggerAction = dto.IsApprove ? PaymentApprovalAction.Submit : PaymentApprovalAction.L3Decline;
                }

                await _paymentsManager.UpdatePaymentStatusAsync(dto.PaymentRequestId, triggerAction);

                var result = await _paymentRequestsRepository.GetAsync(dto.PaymentRequestId);
                updatedPayments.Add(new PaymentRequestDto()
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
                });
            }

            return updatedPayments;
        }

        public async Task<PagedResultDto<PaymentRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var totalCount = await _paymentRequestsRepository.GetCountAsync();
            using (_dataFilter.Disable<ISoftDelete>())
            {
                var payments = await _paymentRequestsRepository.GetPagedListAsync(input.SkipCount, input.MaxResultCount, input.Sorting ?? string.Empty, true);
                return new PagedResultDto<PaymentRequestDto>(totalCount, ObjectMapper.Map<List<PaymentRequest>, List<PaymentRequestDto>>(payments));
            }
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
            // Retrieve all payment requests and filter for the current year
            var payments = await _paymentRequestsRepository.GetListAsync();
            var latestPayment = payments
                .Where(p => p.CreationTime.Year == currentYear)
                .OrderByDescending(p => p.CreationTime)
                .FirstOrDefault();

            if (latestPayment != null)
            {
                var latestSequenceNumber = int.Parse(latestPayment.ReferenceNumber.Split('-').Last());
                return latestSequenceNumber + 1;
            }
            else
            {
                return 1;
            }
        }
    }
}
