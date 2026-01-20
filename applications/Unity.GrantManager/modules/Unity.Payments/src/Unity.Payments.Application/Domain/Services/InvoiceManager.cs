using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Payments.Codes;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Integrations.Http;
using Unity.Payments.PaymentConfigurations;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Unity.Payments.Domain.Services
{
    public class InvoiceManager(
        IAccountCodingRepository accountCodingRepository,
        PaymentConfigurationAppService paymentConfigurationAppService,
        IPaymentRequestRepository paymentRequestRepository,
        ISupplierRepository supplierRepository,
        ISiteRepository siteRepository,
        IUnitOfWorkManager unitOfWorkManager) : DomainService, IInvoiceManager
    {
        public async Task<Site?> GetSiteByPaymentRequestAsync(PaymentRequest paymentRequest)
        {
            Site? site = await siteRepository.GetAsync(paymentRequest.SiteId, true);
            if (site?.SupplierId != null)
            {
                Supplier supplier = await supplierRepository.GetAsync(site.SupplierId);
                site.Supplier = supplier;
            }
            return site;
        }

        public async Task<PaymentRequestData> GetPaymentRequestDataAsync(string invoiceNumber)
        {
            var paymentRequest = await paymentRequestRepository.GetPaymentRequestByInvoiceNumber(invoiceNumber)
                ?? throw new UserFriendlyException("CreateInvoiceByPaymentRequestAsync: Payment Request not found");

            if (!paymentRequest.AccountCodingId.HasValue)
                throw new UserFriendlyException("CreateInvoiceByPaymentRequestAsync: Account Coding - Payment Request - not found");

            AccountCoding accountCoding = await accountCodingRepository.GetAsync(paymentRequest.AccountCodingId.Value);
            string accountDistributionCode = await paymentConfigurationAppService.GetAccountDistributionCode(accountCoding);

            return new PaymentRequestData
            {
                PaymentRequest = paymentRequest,
                AccountCoding = accountCoding,
                AccountDistributionCode = accountDistributionCode
            };
        }

        public async Task UpdatePaymentRequestWithInvoiceAsync(Guid paymentRequestId, InvoiceResponse invoiceResponse)
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Each attempt must have a fresh UoW
                    using (var uow = unitOfWorkManager.Begin())
                    {
                        // Load with tracking
                        var paymentRequest = await paymentRequestRepository.GetAsync(paymentRequestId);

                        if (paymentRequest == null)
                        {
                            Logger.LogWarning("PaymentRequest {Id} not found. Skipping update.", paymentRequestId);
                            return;
                        }

                        // Idempotency: do not re-process
                        if (paymentRequest.InvoiceStatus == CasPaymentRequestStatus.SentToCas)
                        {
                            Logger.LogInformation(
                                "PaymentRequest {Id} already invoiced. Skipping update.",
                                paymentRequestId
                            );
                            return;
                        }

                        // Apply CAS response info
                        paymentRequest.SetCasHttpStatusCode((int)invoiceResponse.CASHttpStatusCode);
                        paymentRequest.SetCasResponse(invoiceResponse.CASReturnedMessages);

                        // Set status
                        paymentRequest.SetInvoiceStatus(
                            invoiceResponse.IsSuccess()
                                ? CasPaymentRequestStatus.SentToCas
                                : CasPaymentRequestStatus.ErrorFromCas
                        );

                        await paymentRequestRepository.UpdateAsync(paymentRequest, autoSave: false);

                        // Commit this attempt
                        await uow.CompleteAsync();

                        Logger.LogInformation(
                            "PaymentRequest {Id} updated successfully on attempt {Attempt}.",
                            paymentRequestId,
                            attempt
                        );
                        return; // success
                    }
                }
                catch (Exception ex) when (
                    ex is AbpDbConcurrencyException ||
                    ex is DbUpdateConcurrencyException
                )
                {
                    Logger.LogWarning(
                        ex,
                        "Concurrency conflict when updating PaymentRequest {Id}, attempt {Attempt}",
                        paymentRequestId,
                        attempt
                    );

                    if (attempt == maxRetries)
                    {
                        Logger.LogError(
                            ex,
                            "Max retries reached for PaymentRequest {Id}. Manual intervention may be required.",
                            paymentRequestId
                        );

                        throw new UserFriendlyException(
                            $"Failed to update payment request {paymentRequestId} after {maxRetries} attempts due to concurrency conflicts."
                        );
                    }

                    // Brief pause before retrying to reduce immediate collision
                    await Task.Delay(75);
                }
                catch (Exception ex)
                {
                    Logger.LogError(
                        ex,
                        "Unexpected exception updating PaymentRequest {Id} on attempt {Attempt}",
                        paymentRequestId,
                        attempt
                    );

                    throw new UserFriendlyException(
                        $"Failed to update payment request {paymentRequestId}: {ex.Message}"
                    );
                }
            }
        }
    }
}
