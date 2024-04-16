using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Integration.Cas;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.PaymentConfigurations;
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
        private IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly IPaymentConfigurationRepository _paymentConfigurationRepository;
        private readonly ICurrentUser _currentUser;

        public BatchPaymentRequestAppService(
            IPaymentConfigurationAppService paymentConfigurationAppService,
            IPaymentConfigurationRepository paymentConfigurationRepository,
            IBatchPaymentRequestRepository batchPaymentRequestsRepository,
            InvoiceService invoiceService,
            ICurrentUser currentUser)
        {
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _batchPaymentRequestsRepository = batchPaymentRequestsRepository;
            _currentUser = currentUser;
            _invoiceService = invoiceService;
        }

        public virtual async Task<BatchPaymentRequestDto> CreateAsync(CreateBatchPaymentRequestDto batchPaymentRequest)
        {
            var newBatchPaymentRequest = new BatchPaymentRequest(Guid.NewGuid(),
                Guid.NewGuid().ToString(), // Need to implement batch number generator
                batchPaymentRequest.Description,
                GetCurrentRequesterName(),
                batchPaymentRequest.Provider);

            foreach (var payment in batchPaymentRequest.PaymentRequests)
            {
                PaymentConfigurationDto? paymentConfigurationDto = await _paymentConfigurationAppService.GetAsync();
				PaymentRequest paymentRequest = new PaymentRequest(
                    Guid.NewGuid(),
                    newBatchPaymentRequest,
                    payment.InvoiceNumber,
                    payment.Amount,
                    payment.SiteId,
                    payment.CorrelationId,
                    payment.Description),
                    await GetPaymentThresholdAsync());
            }

            var result = await _batchPaymentRequestsRepository.InsertAsync(newBatchPaymentRequest);

            return ObjectMapper.Map<BatchPaymentRequest, BatchPaymentRequestDto>(result);
        }

        private async Task ExampleCASIntegrationAsync(PaymentRequest paymentRequest, string accountDistributionCode) {
            var currentMonth = DateTime.Now.ToString("MMM").Trim('.');
            var currentDay = DateTime.Now.ToString("dd");
            var currentYear = DateTime.Now.ToString("yyyy");
            var dateStringDayMonYear =  $"{currentDay}-{currentMonth}-{currentYear}";

            Invoice invoice = new Invoice();

            // Pull from Payment request
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
            invoiceLineDetail.defaultDistributionAccount =  accountDistributionCode; // This will be at the tenant level
            invoice.invoiceLineDetails = new List<InvoiceLineDetail> { invoiceLineDetail };
            _invoiceService.CreateInvoiceAsync(invoice);
        }

        protected virtual async Task<decimal> GetPaymentThresholdAsync()
        {
            var paymentConfigs = await _paymentConfigurationRepository.GetListAsync();

            if (paymentConfigs.Count > 0)
            {
                var paymentConfig = paymentConfigs[0];
                return paymentConfig.PaymentThreshold ?? PaymentConsts.DefaultThresholdAmount;
            }
            return PaymentConsts.DefaultThresholdAmount;
        }

        protected virtual string GetCurrentRequesterName()
        {
            return $"{_currentUser.Name} {_currentUser.SurName}";
        }
    }
}
