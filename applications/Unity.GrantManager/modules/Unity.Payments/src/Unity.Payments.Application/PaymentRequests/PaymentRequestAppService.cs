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
using Volo.Abp.Domain.Repositories;
using System.Linq;
using Unity.Payments.Domain.Shared;
using Unity.Payments.Domain.Services;
using Unity.Payments.Enums;

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

        public PaymentRequestAppService(IPaymentConfigurationRepository paymentConfigurationRepository,
            IPaymentRequestRepository paymentRequestsRepository,
            ICurrentUser currentUser, IPaymentsManager paymentsManager)
        {            
            _paymentConfigurationRepository = paymentConfigurationRepository;
            _paymentRequestsRepository = paymentRequestsRepository;
            _currentUser = currentUser;
            _paymentsManager = paymentsManager;
        }

        public virtual async Task<List<PaymentRequestDto>> CreateAsync(List<CreatePaymentRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> createdPayments = [];

            var paymentThreshold = await GetPaymentThresholdAsync();

            foreach (var dto in paymentRequests)
            {
                // Confirmation ID + 4 digit sequence NEED SEQUENCE IF MULTIPLE
                string format = "0000";
                // Needs to be optimized
                int applicationPaymentRequestCount = await _paymentRequestsRepository.GetCountByCorrelationId(dto.CorrelationId) + 1;
                try
                {
                    var payment = new PaymentRequest(Guid.NewGuid(),
                   dto.InvoiceNumber + $"-{applicationPaymentRequestCount.ToString(format)}",
                   dto.Amount,
                   dto.PayeeName,
                   dto.ContractNumber,
                   dto.SupplierNumber,
                   dto.SiteId,
                   dto.CorrelationId,
                   dto.CorrelationProvider,
                   dto.Description,
                   paymentThreshold);

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
                    });
                }
                catch(Exception ex)
                {
                    //Handle exception
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
                List<PaymentRequestStatus> level1Approvals = new List<PaymentRequestStatus>{
                    PaymentRequestStatus.L1Pending,PaymentRequestStatus.L1Declined};
                List<PaymentRequestStatus> level2Approvals = new List<PaymentRequestStatus>{
                    PaymentRequestStatus.L2Pending,PaymentRequestStatus.L2Declined};
                List<PaymentRequestStatus> level3Approvals = new List<PaymentRequestStatus>{
                    PaymentRequestStatus.L3Pending,PaymentRequestStatus.L3Declined};


                var triggerAction = PaymentApprovalAction.None;

                if (_currentUser.IsInRole("l1_approver") && (payment.Status.IsIn(level1Approvals)))
                {
                    triggerAction = dto.isApprove ? PaymentApprovalAction.L1Approve : PaymentApprovalAction.L1Decline;
                }


                if (_currentUser.IsInRole("l2_approver") && (payment.Status.IsIn(level2Approvals)))
                {
                    if (payment.Amount > paymentThreshold)
                    {
                        triggerAction = dto.isApprove ? (PaymentApprovalAction.L2Approve) : PaymentApprovalAction.L2Decline;
                    }
                    else
                    {
                        triggerAction = dto.isApprove ? (PaymentApprovalAction.Submit) : PaymentApprovalAction.L2Decline;
                    }
                }

                if (_currentUser.IsInRole("l3_approver") && payment.Status.IsIn(level3Approvals))
                {
                    triggerAction = dto.isApprove ? PaymentApprovalAction.Submit : PaymentApprovalAction.L3Decline;
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
            var payments = await _paymentRequestsRepository.GetPagedListAsync(input.SkipCount, input.MaxResultCount, input.Sorting ?? string.Empty, true);
            return new PagedResultDto<PaymentRequestDto>(totalCount, ObjectMapper.Map<List<PaymentRequest>, List<PaymentRequestDto>>(payments));
        }
        public async Task<List<PaymentDetailsDto>> GetListByApplicationIdAsync(Guid applicationId)
        {
            var payments = await _paymentRequestsRepository.GetListAsync();
            var filteredPayments = payments.Where(e => e.CorrelationId == applicationId).ToList();

            return new List<PaymentDetailsDto>(ObjectMapper.Map<List<PaymentRequest>, List<PaymentDetailsDto>>(filteredPayments));
        } 
        public async Task<List<PaymentDetailsDto>> GetListByPaymentIdsAsync (List<Guid> paymentIds)
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
    }
}
