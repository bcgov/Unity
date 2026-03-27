using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using System;
using System.Linq;
using Volo.Abp.Application.Services;
using System.Collections.Generic;
using Volo.Abp.TenantManagement;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Microsoft.Extensions.Logging;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.Codes;
using Unity.Payments.RabbitMQ.QueueMessages;
using Unity.Notifications.Integrations.RabbitMQ;

namespace Unity.Payments.PaymentRequests
{
    public class CasPaymentRequestCoordinator(PaymentQueueService paymentQueueService,
            IPaymentRequestRepository paymentRequestsRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant) : ApplicationService
    {

        private static int TenMinutes = 10;


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
                if (!string.IsNullOrEmpty(paymentRequest.InvoiceNumber) && currentTenant != null && currentTenant.Id != null)
                {                    
                    InvoiceMessages message = new InvoiceMessages
                    {
                        TimeToLive = TimeSpan.FromMinutes(TenMinutes),
                        PaymentRequestId = paymentRequest.Id,
                        InvoiceNumber = paymentRequest.InvoiceNumber,
                        SupplierNumber = paymentRequest.SupplierNumber,
                        SiteNumber = paymentRequest.Site.Number,
                        TenantId = (Guid)currentTenant.Id
                    };

                    await paymentQueueService.SendPaymentToInvoiceQueueAsync(message);
                }
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "AddPaymentRequestsToInvoiceQueue Exception: {ExceptionMessage}", ExceptionMessage);
            }
        }

        public async Task ManuallyAddPaymentRequestsToReconciliationQueue(List<PaymentRequestDto>paymentRequests)
        {
            foreach (PaymentRequestDto paymentRequest in paymentRequests)
            {
                ReconcilePaymentMessages reconcilePaymentMessage = new ReconcilePaymentMessages
                {
                    TimeToLive = TimeSpan.FromMinutes(TenMinutes),
                    PaymentRequestId = paymentRequest.Id,
                    InvoiceNumber = paymentRequest.InvoiceNumber,
                    SupplierNumber = paymentRequest.SupplierNumber,
                    SiteNumber = paymentRequest.Site?.Number ?? string.Empty,
                    TenantId = paymentRequest.TenantId ?? currentTenant.Id!.Value
                };

                await paymentQueueService.SendPaymentToReconciliationQueueAsync(reconcilePaymentMessage);
            }
        }

        public async Task AddPaymentRequestsToReconciliationQueue()
        {
            var tenants = await tenantRepository.GetListAsync();
            foreach (var tenantId in tenants.Select(tenant => tenant.Id))
            {
                using (currentTenant.Change(tenantId))
                {
                    List<PaymentRequest> paymentRequests = await paymentRequestsRepository.GetPaymentRequestsBySentToCasStatusAsync();
                    foreach (PaymentRequest paymentRequest in paymentRequests)
                    {
                        ReconcilePaymentMessages reconcilePaymentMessage = new ReconcilePaymentMessages
                        {
                            TimeToLive = TimeSpan.FromMinutes(TenMinutes),
                            PaymentRequestId = paymentRequest.Id,
                            InvoiceNumber = paymentRequest.InvoiceNumber,
                            SupplierNumber = paymentRequest.SupplierNumber,
                            SiteNumber = paymentRequest.Site.Number,
                            TenantId = tenantId
                        };

                        await paymentQueueService.SendPaymentToReconciliationQueueAsync(reconcilePaymentMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Updates payment request status from CAS integration results.
        /// Tenant context and audit scope are already established by the caller
        /// (via <see cref="QueueConsumerHandler{TMessageConsumer,TQueueMessage}"/>);
        /// this method only needs to own its unit of work.
        /// </summary>
        public async Task<PaymentRequest?> UpdatePaymentRequestStatus(Guid TenantId, Guid PaymentRequestId, CasPaymentSearchResult result)
        {
            if (TenantId == Guid.Empty)
            {
                return null;
            }

            using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

            var paymentRequest = await paymentRequestsRepository.GetAsync(PaymentRequestId);

            UpdatePaymentRequestFromCasResult(paymentRequest, result);

            await paymentRequestsRepository.UpdateAsync(paymentRequest, autoSave: false);

            // CompleteAsync commits the transaction and calls SaveChangesAsync,
            // which triggers AbpDbContext to collect entity changes into the active audit log.
            // The audit log is then persisted by QueueConsumerHandler after ConsumeAsync returns.
            await uow.CompleteAsync();

            return paymentRequest;
        }

        private static void UpdatePaymentRequestFromCasResult(PaymentRequest paymentRequest, CasPaymentSearchResult result)
        {
            // Handle duplicate NotFound status by appending "2"
            if (paymentRequest.InvoiceStatus == CasPaymentRequestStatus.NotFound && 
                result.InvoiceStatus == CasPaymentRequestStatus.NotFound)
            {
                result.InvoiceStatus = CasPaymentRequestStatus.NotFound + "2";
            }

            paymentRequest.SetInvoiceStatus(result.InvoiceStatus ?? "");
            paymentRequest.SetPaymentStatus(result.PaymentStatus ?? "");
            paymentRequest.SetPaymentDate(result.PaymentDate ?? "");
            paymentRequest.SetPaymentNumber(result.PaymentNumber ?? "");
            
            if (result.InvoiceStatus != null)
            {
                paymentRequest.SetCasHttpStatusCode((int)System.Net.HttpStatusCode.OK);
                paymentRequest.SetCasResponse("SUCCEEDED");
            }
        }
    }
}
