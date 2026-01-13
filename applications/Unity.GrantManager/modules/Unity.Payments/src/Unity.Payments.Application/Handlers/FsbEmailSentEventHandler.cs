using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Notifications.Events;
using Unity.Payments.Domain.PaymentRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.Payments.Handlers
{
    /// <summary>
    /// Handles FSB email sent events to update payment request tracking
    /// </summary>
    public class FsbEmailSentEventHandler :
        ILocalEventHandler<FsbEmailSentEto>,
        ITransientDependency
    {
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<FsbEmailSentEventHandler> _logger;

        public FsbEmailSentEventHandler(
            IPaymentRequestRepository paymentRequestRepository,
            ICurrentTenant currentTenant,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<FsbEmailSentEventHandler> logger)
        {
            _paymentRequestRepository = paymentRequestRepository;
            _currentTenant = currentTenant;
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
        }

        public async Task HandleEventAsync(FsbEmailSentEto eventData)
        {
            using (_currentTenant.Change(eventData.TenantId))
            {
                using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

                try
                {
                    foreach (var paymentId in eventData.PaymentRequestIds)
                    {
                        try
                        {
                            var payment = await _paymentRequestRepository.GetAsync(paymentId, includeDetails: false);
                            payment.SetFsbNotificationEmailLog(eventData.EmailLogId, eventData.SentDate);
                            await _paymentRequestRepository.UpdateAsync(payment, autoSave: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Failed to update FSB notification tracking for payment {PaymentId}",
                                paymentId);
                            // Continue processing other payments
                        }
                    }

                    await uow.SaveChangesAsync();
                    await uow.CompleteAsync();

                    _logger.LogInformation(
                        "Updated FSB notification tracking for {Count} payments. EmailLogId: {EmailLogId}",
                        eventData.PaymentRequestIds.Count,
                        eventData.EmailLogId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to update FSB notification tracking for batch.");
                    throw new InvalidOperationException(
                        $"Failed to update FSB notification tracking for batch.",
                        ex);
                }
            }
        }
    }
}
