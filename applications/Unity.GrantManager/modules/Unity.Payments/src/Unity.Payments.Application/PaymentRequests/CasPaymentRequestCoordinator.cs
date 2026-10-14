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
using Unity.Payments.Events;
using Unity.Payments.RabbitMQ.QueueMessages;
using Unity.Notifications.Integrations.RabbitMQ;
using Volo.Abp.Data;
using Volo.Abp.EventBus.Local;

namespace Unity.Payments.PaymentRequests
{
    public class CasPaymentRequestCoordinator(PaymentQueueService paymentQueueService,
            IPaymentRequestRepository paymentRequestsRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            ILocalEventBus localEventBus) : ApplicationService
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
                        SiteNumber = paymentRequest.Site?.Number ?? string.Empty,
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
                            SiteNumber = paymentRequest.Site?.Number ?? string.Empty,
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

            const int maxConcurrencyAttempts = 3;
            CasPaymentValues? originalCasValues = null;

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

                    var paymentRequest = await paymentRequestsRepository.GetAsync(PaymentRequestId);
                    var previousPaymentStatus = paymentRequest.PaymentStatus;
                    var previousStatus = paymentRequest.Status;
                    originalCasValues ??= GetCurrentCasValues(paymentRequest);

                    var casValues = GetCasValues(paymentRequest, result);
                    if (attempt > 1)
                    {
                        var currentCasValues = GetCurrentCasValues(paymentRequest);
                        if (currentCasValues == casValues)
                        {
                            return paymentRequest;
                        }

                        if (currentCasValues != originalCasValues)
                        {
                            Logger.LogWarning(
                                "Rejecting stale CAS result for payment request {PaymentRequestId}; the payment has a different CAS state.",
                                PaymentRequestId);
                            return paymentRequest;
                        }
                    }

                    ApplyCasValues(paymentRequest, casValues);

                    await paymentRequestsRepository.UpdateAsync(paymentRequest, autoSave: false);

                    // CompleteAsync commits the transaction and calls SaveChangesAsync,
                    // which triggers AbpDbContext to collect entity changes into the active audit log.
                    // The audit log is then persisted by QueueConsumerHandler after ConsumeAsync returns.
                    await uow.CompleteAsync();

                    if (previousPaymentStatus != paymentRequest.PaymentStatus || previousStatus != paymentRequest.Status)
                    {
                        await localEventBus.PublishAsync(new PaymentStatusChangedEvent
                        {
                            PaymentRequestId = paymentRequest.Id,
                            ApplicationId = paymentRequest.CorrelationId,
                            Status = paymentRequest.Status,
                            CasPaymentStatus = paymentRequest.PaymentStatus,
                            TenantId = TenantId
                        });
                    }

                    return paymentRequest;
                }
                catch (AbpDbConcurrencyException ex) when (attempt < maxConcurrencyAttempts)
                {
                    using var inspectionUow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
                    var currentPaymentRequest = await paymentRequestsRepository.GetAsync(PaymentRequestId);
                    var currentCasValues = GetCurrentCasValues(currentPaymentRequest);
                    var desiredCasValues = GetCasValues(currentPaymentRequest, result);

                    if (currentCasValues == desiredCasValues)
                    {
                        return currentPaymentRequest;
                    }

                    if (currentCasValues != originalCasValues)
                    {
                        Logger.LogWarning(
                            ex,
                            "Rejecting stale CAS result for payment request {PaymentRequestId}; the payment has a different CAS state.",
                            PaymentRequestId);
                        return currentPaymentRequest;
                    }

                    Logger.LogWarning(
                        ex,
                        "Concurrency conflict updating payment request {PaymentRequestId} from CAS; retrying attempt {Attempt} of {MaxAttempts}.",
                        PaymentRequestId,
                        attempt + 1,
                        maxConcurrencyAttempts);
                }
                catch (AbpDbConcurrencyException)
                {
                    throw;
                }
            }
        }

        private static CasPaymentValues GetCasValues(PaymentRequest paymentRequest, CasPaymentSearchResult result)
        {
            var invoiceStatus = result.InvoiceStatus ?? string.Empty;
            if (paymentRequest.InvoiceStatus == CasPaymentRequestStatus.NotFound + "2" &&
                invoiceStatus == CasPaymentRequestStatus.NotFound)
            {
                invoiceStatus = paymentRequest.InvoiceStatus;
            }

            if (paymentRequest.InvoiceStatus == CasPaymentRequestStatus.NotFound &&
                invoiceStatus == CasPaymentRequestStatus.NotFound)
            {
                invoiceStatus = CasPaymentRequestStatus.NotFound + "2";
            }

            return new CasPaymentValues(
                invoiceStatus,
                result.PaymentStatus ?? string.Empty,
                result.PaymentNumber ?? string.Empty,
                result.PaymentDate ?? string.Empty,
                result.InvoiceStatus != null ? (int)System.Net.HttpStatusCode.OK : null,
                result.InvoiceStatus != null ? "SUCCEEDED" : null);
        }

        private static CasPaymentValues GetCurrentCasValues(PaymentRequest paymentRequest)
        {
            return new CasPaymentValues(
                paymentRequest.InvoiceStatus ?? string.Empty,
                paymentRequest.PaymentStatus ?? string.Empty,
                paymentRequest.PaymentNumber ?? string.Empty,
                paymentRequest.PaymentDate ?? string.Empty,
                paymentRequest.CasHttpStatusCode,
                paymentRequest.CasResponse);
        }

        private static void ApplyCasValues(PaymentRequest paymentRequest, CasPaymentValues casValues)
        {
            paymentRequest.SetInvoiceStatus(casValues.InvoiceStatus);
            paymentRequest.SetPaymentStatus(casValues.PaymentStatus);
            paymentRequest.SetPaymentDate(casValues.PaymentDate);
            paymentRequest.SetPaymentNumber(casValues.PaymentNumber);

            if (casValues.CasHttpStatusCode.HasValue)
            {
                paymentRequest.SetCasHttpStatusCode(casValues.CasHttpStatusCode.Value);
                paymentRequest.SetCasResponse(casValues.CasResponse ?? string.Empty);
            }
        }

        private readonly record struct CasPaymentValues(
            string InvoiceStatus,
            string PaymentStatus,
            string PaymentNumber,
            string PaymentDate,
            int? CasHttpStatusCode,
            string? CasResponse);
    }
}
