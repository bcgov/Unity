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

namespace Unity.Payments.PaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class PaymentRequestAppService : PaymentsAppService, IPaymentRequestAppService
    {
        private readonly IRepository<PaymentRequest, Guid> _iPaymentRequestRepository;
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly IPaymentConfigurationRepository _paymentConfigurationRepository;
        private readonly ICurrentUser _currentUser;

        public PaymentRequestAppService(
            IRepository<PaymentRequest, Guid> iPaymentRequestRepository,
            IPaymentConfigurationRepository paymentConfigurationRepository,
            IPaymentRequestRepository paymentRequestsRepository,
            ICurrentUser currentUser)
        {
            _iPaymentRequestRepository = iPaymentRequestRepository;
            _paymentConfigurationRepository = paymentConfigurationRepository;
            _paymentRequestsRepository = paymentRequestsRepository;
            _currentUser = currentUser;
        }

        public virtual async Task<List<PaymentRequestDto>> CreateAsync(List<CreatePaymentRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> createdPayments = [];

            var paymentThreshold = await GetPaymentThresholdAsync();

            foreach (var dto in paymentRequests)
            {

                // Confirmation ID + 4 digit sequence NEED SEQUENCE IF MULTIPLE
                string format = "0000";
                int applicationPaymentRequestCount = await paymentRequestsPerApplicationCountAsync(dto.CorrelationId);

                // Create a new Payment entity from the DTO
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

            return createdPayments;
        }
        public virtual async Task<List<PaymentRequestDto>> UpdateStatusAsync(List<UpdatePaymentStatusRequestDto> paymentRequests)
        {
            List<PaymentRequestDto> updatedPayments = [];


            foreach (var dto in paymentRequests)
            {
                var payment = await _paymentRequestsRepository.GetAsync(dto.PaymentRequestId);
              
                payment.SetPaymentRequestStatus(dto.Status);

                if(dto.Status == Enums.PaymentRequestStatus.L1Approved)
                {
                  var index =  payment.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level1);
                    payment.ExpenseApprovals[index].Approve();
                  

                }
                else if(dto.Status == Enums.PaymentRequestStatus.L1Declined)
                {
                    var index = payment.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level1);
                    payment.ExpenseApprovals[index].Decline();
                }

                var result = await _paymentRequestsRepository.UpdateAsync(payment);
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

        private async Task<int> paymentRequestsPerApplicationCountAsync(Guid correlationId)
        {
            var currentPaymentRequests = (await _iPaymentRequestRepository.GetQueryableAsync())
                                            .Where(s => s.CorrelationId == correlationId)
                                            .ToList();

            return currentPaymentRequests.Count + 1;
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
