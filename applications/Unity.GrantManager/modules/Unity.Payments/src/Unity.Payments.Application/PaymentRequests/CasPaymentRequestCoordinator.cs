using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using RabbitMQ.Client;
using System.Text;
using Unity.RabbitMQ;
using System;
using Volo.Abp.Application.Services;
using System.Collections.Generic;
using Volo.Abp.TenantManagement;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Microsoft.Extensions.Logging;
using Unity.Payments.Integrations.Cas;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Linq;
using Unity.Payments.RabbitMQ.QueueMessages;
using Unity.Notifications.Integrations.RabbitMQ;

namespace Unity.Payments.PaymentRequests
{
    public class CasPaymentRequestCoordinator : ApplicationService
    {
        private readonly IPaymentRequestRepository _paymentRequestsRepository;        
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly PaymentQueueService _paymentQueueService;
        private readonly InvoiceService _invoiceService;


        private static int FiveMinutes = 5;

        public CasPaymentRequestCoordinator(
            InvoiceService invoiceService,
            PaymentQueueService paymentQueueService,
            IPaymentRequestRepository paymentRequestsRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant)
        {
            _invoiceService = invoiceService;
            _paymentQueueService = paymentQueueService;
            _paymentRequestsRepository = paymentRequestsRepository;
            _tenantRepository = tenantRepository;
            _currentTenant = currentTenant;
            _unitOfWorkManager = unitOfWorkManager;
        }

        protected virtual dynamic GetPaymentRequestObject(
            Guid paymentRequestId,
            string invoiceNumber,
            string supplierNumber,
            string siteNumber,
            Guid tenantId)
        {
            var paymentRequestObject = new
            {
                paymentRequestId,
                invoiceNumber,
                supplierNumber,
                siteNumber,
                tenantId
            };
            return paymentRequestObject;
        }

        public async Task AddPaymentRequestsToInvoiceQueue(PaymentRequest paymentRequest)
        {
            try
            {
                if (!string.IsNullOrEmpty(paymentRequest.InvoiceNumber) && (Guid)_currentTenant.Id != Guid.Empty)
                {
                    InvoiceMessages message = new InvoiceMessages
                    {
                        TimeToLive = TimeSpan.FromMinutes(FiveMinutes),
                        PaymentRequestId = paymentRequest.Id,
                        InvoiceNumber = paymentRequest.InvoiceNumber,
                        SupplierNumber = paymentRequest.SupplierNumber,
                        SiteNumber = paymentRequest.Site.Number,
                        TenantId = (Guid)_currentTenant.Id
                    };

                    await _paymentQueueService.SendPaymentToInvoiceQueueAsync(message);
                }
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "AddPaymentRequestsToInvoiceQueue Exception: {ExceptionMessage}", ExceptionMessage);
            }
        }

        public async Task AddPaymentRequestsToReconciliationQueue()
        {
            var tenants = await _tenantRepository.GetListAsync();
            foreach (var tenantId in tenants.Select(tenant => tenant.Id))
            {
                using (_currentTenant.Change(tenantId))
                {
                    List<PaymentRequest> paymentRequests = await _paymentRequestsRepository.GetPaymentRequestsBySentToCasStatusAsync();
                    foreach (PaymentRequest paymentRequest in paymentRequests)
                    {
                        ReconcilePaymentMessages reconcilePaymentMessage = new ReconcilePaymentMessages
                        {
                            TimeToLive = TimeSpan.FromMinutes(FiveMinutes),
                            PaymentRequestId = paymentRequest.Id,
                            InvoiceNumber = paymentRequest.InvoiceNumber,
                            SupplierNumber = paymentRequest.SupplierNumber,
                            SiteNumber = paymentRequest.Site.Number,
                            TenantId = (Guid)_currentTenant.Id
                        };

                        await _paymentQueueService.SendPaymentToReconciliationQueueAsync(reconcilePaymentMessage);
                    }
                }
            }
        }


        public async Task<PaymentRequest?> UpdatePaymentRequestStatus(ReconcilePaymentMessages reconcilePayment, CasPaymentSearchResult result)
        {
            PaymentRequest? paymentReqeust = null;
            if (reconcilePayment.TenantId != Guid.Empty)
            {
                using (_currentTenant.Change(reconcilePayment.TenantId))
                {
                    try
                    {
                        using var uow = _unitOfWorkManager.Begin(true, false);
                        paymentReqeust = await _paymentRequestsRepository.GetAsync(reconcilePayment.PaymentRequestId);
                        if (paymentReqeust != null)
                        {
                            paymentReqeust.SetInvoiceStatus(result.InvoiceStatus ?? "");
                            paymentReqeust.SetPaymentStatus(result.PaymentStatus ?? "");
                            paymentReqeust.SetPaymentDate(result.PaymentDate ?? "");
                            paymentReqeust.SetPaymentNumber(result.PaymentNumber ?? "");

                            await _paymentRequestsRepository.UpdateAsync(paymentReqeust, autoSave: false);
                            await uow.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        string ExceptionMessage = ex.Message;
                        Logger.LogInformation(ex, "UpdatePaymentRequestStatus: Error updating payment request: {ExceptionMessage}", ExceptionMessage);
                    }
                }
            }
            return paymentReqeust;
        }
    }
}
