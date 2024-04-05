using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Integration.Cas;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.Settings;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.Payments.BatchPaymentRequests
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class BatchPaymentRequestAppService : PaymentsAppService, IBatchPaymentRequestAppService
    {
        private readonly IBatchPaymentRequestRepository _batchPaymentRequestsRepository;
        private readonly InvoiceService _invoiceService;
        private readonly ICurrentUser _currentUser;

        public BatchPaymentRequestAppService(
            IBatchPaymentRequestRepository batchPaymentRequestsRepository,
            InvoiceService invoiceService,
            ICurrentUser currentUser)
        {
            _batchPaymentRequestsRepository = batchPaymentRequestsRepository;
            _currentUser = currentUser;
            _invoiceService = invoiceService;
        }

        public async Task<BatchPaymentRequestDto> CreateAsync(CreateBatchPaymentRequestDto batchPaymentRequest)
        {
            var paymentThreshold = await GetPaymentThresholdSettingValueAsync();

            var newBatchPaymentRequest = new BatchPaymentRequest(Guid.NewGuid(),
                Guid.NewGuid().ToString(), // Need to implement batch number generator
                Enums.PaymentGroup.EFT,
                batchPaymentRequest.Description,
                GetCurrentRequesterName(),
                batchPaymentRequest.Provider);

            foreach (var payment in batchPaymentRequest.PaymentRequests)
            {
                decimal paymentThresholdAmount = ConvertPaymentThresholdAmount(paymentThreshold);
                PaymentRequest paymentRequest = new PaymentRequest(
                    Guid.NewGuid(),
                    newBatchPaymentRequest,
                    payment.InvoiceNumber,
                    payment.Amount,
                    newBatchPaymentRequest.PaymentGroup,
                    payment.CorrelationId,
                    payment.Description);

                ExampleCASIntegration(paymentRequest);

                newBatchPaymentRequest.AddPaymentRequest(paymentRequest, paymentThresholdAmount);
            }

            var result = await _batchPaymentRequestsRepository.InsertAsync(newBatchPaymentRequest);

            return ObjectMapper.Map<BatchPaymentRequest, BatchPaymentRequestDto>(result);
        }

        private void ExampleCASIntegration(PaymentRequest paymentRequest) {
            var currentMonth = DateTime.Now.ToString("MMM").Trim('.');
            var currentDay = DateTime.Now.ToString("dd");
            var currentYear = DateTime.Now.ToString("yyyy");
            var dateStringDayMonYear =  $"{currentDay}-{currentMonth}-{currentYear}";

            Invoice invoice = new Invoice();
            invoice.supplierNumber = "004696"; // This is from each Applicant
            invoice.supplierSiteNumber = "002";
            invoice.payGroup= "GEN EFT"; // GEN CHQ - other options

            // Invoice Number/?????? Want to use the Submission ID - Confirmation ID + 4 digit sequence
            // The Application ID in unity - Might NEED SEQUENCE IF MULTIPLE
            invoice.invoiceNumber = paymentRequest.InvoiceNumber; 
            invoice.invoiceDate  = dateStringDayMonYear; //DD-MMM-YYYY
            invoice.dateInvoiceReceived = dateStringDayMonYear;
            invoice.glDate = dateStringDayMonYear;
            invoice.invoiceAmount = paymentRequest.Amount;

            InvoiceLineDetail invoiceLineDetail = new InvoiceLineDetail();
            invoiceLineDetail.invoiceLineNumber = 1;
            invoiceLineDetail.invoiceLineAmount = paymentRequest.Amount;
            invoiceLineDetail.defaultDistributionAccount =  "043.80001.01090.6001.8000000.000000.0000"; // This will be at the tenant level
            invoice.invoiceLineDetails = new List<InvoiceLineDetail> { invoiceLineDetail };
            _invoiceService.CreateInvoiceAsync(invoice);
        }

        private async Task<string?> GetPaymentThresholdSettingValueAsync()
        {
            return await SettingProvider.GetOrNullAsync(PaymentsSettings.PaymentThreshold);
        }

        private static decimal ConvertPaymentThresholdAmount(string? paymentThreshold)
        {
            return paymentThreshold == null ? PaymentConsts.DefaultThresholdAmount : decimal.Parse(paymentThreshold);
        }

        private string GetCurrentRequesterName()
        {
            return $"{_currentUser.Name} {_currentUser.SurName}";
        }
    }
}
