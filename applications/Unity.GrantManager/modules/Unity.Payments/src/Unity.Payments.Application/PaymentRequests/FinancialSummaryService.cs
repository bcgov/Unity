using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using System;
using Volo.Abp.Application.Services;
using Volo.Abp.TenantManagement;
using Volo.Abp.MultiTenancy;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Unity.Payments.PaymentRequests
{
    public class FinancialSummaryService : ApplicationService
    {
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;

        public FinancialSummaryService (
            IPaymentRequestRepository paymentRequestsRepository,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant)
        {
            _paymentRequestsRepository = paymentRequestsRepository;
            _tenantRepository = tenantRepository;
            _currentTenant = currentTenant;
        }

        public async Task NotifyFinancialAdvisorsOfNightlyFailedPayments()
        {
            var tenants = await _tenantRepository.GetListAsync();
            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id))
                {
                    //await GetFailedPayments(tenantId);
                }
            }
        }

        //public async Task<List<Users>> GetFinancialAnalysts()
        //{
        //}
        
        public async Task<List<PaymentRequest>> GetFailedPayments()
        {
            List <PaymentRequest> failedPaymentList = new List<PaymentRequest>();

            try
            {
                failedPaymentList = await _paymentRequestsRepository.GetListAsync();
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
