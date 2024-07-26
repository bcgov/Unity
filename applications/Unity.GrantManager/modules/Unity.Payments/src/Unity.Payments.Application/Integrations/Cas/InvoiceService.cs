using Volo.Abp;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using Unity.Payments.Integrations.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Unity.Payments.Enums;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Volo.Abp.DependencyInjection;
using Unity.Payments.Codes;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Volo.Abp.Uow;

namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(InvoiceService), typeof(IInvoiceService))]
    public class InvoiceService : ApplicationService, IInvoiceService
    {
        private readonly ITokenService _iTokenService;
        private readonly IPaymentRequestRepository _iPaymentRequestRepository;
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly ISiteRepository _iSiteRepository;
        private readonly ISupplierRepository _iSupplierRepository;
        private readonly IOptions<CasClientOptions> _casClientOptions;
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        private const string CFS_APINVOICE = "cfs/apinvoice";

        private readonly Dictionary<int, string> CASPaymentGroup = new Dictionary<int, string>
        {
            { (int)PaymentGroup.EFT, "GEN EFT" },
            { (int)PaymentGroup.Cheque, "GEN CHQ" }
        };

        public InvoiceService(
            ITokenService iTokenService,
            IPaymentRequestRepository paymentRequestRepository,
            IPaymentConfigurationAppService paymentConfigurationAppService,
            IResilientHttpRequest resilientHttpRequest,
            IOptions<CasClientOptions> casClientOptions,
            ISupplierRepository iSupplierRepository,
            ISiteRepository iSiteRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _iTokenService = iTokenService;
            _iPaymentRequestRepository = paymentRequestRepository;
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _resilientRestClient = resilientHttpRequest;
            _casClientOptions = casClientOptions;
            _iSupplierRepository = iSupplierRepository;
            _iSiteRepository = iSiteRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        protected virtual async Task<Invoice?> InitializeCASInvoice(PaymentRequest paymentRequest,
                                                          string? accountDistributionCode)
        {
            Invoice? casInvoice = new Invoice();
            Site? site = await GetSiteByPaymentRequestAsync(paymentRequest);

            if (site != null && site.Supplier != null && site.Supplier.Number != null && accountDistributionCode != null)
            {
                var currentMonth = DateTime.UtcNow.ToString("MMM").Trim('.');
                var currentDay = DateTime.UtcNow.ToString("dd");
                var currentYear = DateTime.UtcNow.ToString("yyyy");
                var dateStringDayMonYear = $"{currentDay}-{currentMonth}-{currentYear}";

                casInvoice.SupplierNumber = site.Supplier.Number; // This is from each Applicant
                casInvoice.SupplierName = site.Supplier.Name;
                casInvoice.SupplierSiteNumber = site.Number;
                casInvoice.PayGroup = CASPaymentGroup[(int)site.PaymentGroup]; // GEN CHQ - other options
                casInvoice.InvoiceNumber = paymentRequest.InvoiceNumber;
                casInvoice.InvoiceDate = dateStringDayMonYear; //DD-MMM-YYYY
                casInvoice.DateInvoiceReceived = dateStringDayMonYear;
                casInvoice.GlDate = dateStringDayMonYear;
                casInvoice.InvoiceAmount = paymentRequest.Amount;

                InvoiceLineDetail invoiceLineDetail = new InvoiceLineDetail();
                invoiceLineDetail.InvoiceLineNumber = 1;
                invoiceLineDetail.InvoiceLineAmount = paymentRequest.Amount;
                invoiceLineDetail.DefaultDistributionAccount = accountDistributionCode; // This will be at the tenant level
                casInvoice.InvoiceLineDetails = new List<InvoiceLineDetail> { invoiceLineDetail };
            }

            return casInvoice;
        }

        public async Task<Site?> GetSiteByPaymentRequestAsync(PaymentRequest paymentRequest)
        {
            Site? site = await _iSiteRepository.GetAsync(paymentRequest.SiteId, true);
            Supplier supplier = await _iSupplierRepository.GetAsync(site.SupplierId);
            site.Supplier = supplier;
            return site;
        }

        public async Task<InvoiceResponse?> CreateInvoiceByPaymentRequestAsync(string invoiceNumber)
        {
            InvoiceResponse invoiceResponse = new();
            try
            {
                PaymentRequest? paymentRequest = await _iPaymentRequestRepository.GetPaymentRequestByInvoiceNumber(invoiceNumber);
                if (paymentRequest == null)
                {
                    throw new UserFriendlyException("CreateInvoiceByPaymentRequestAsync: Payment Request not found");
                }

                string? accountDistributionCode = await _paymentConfigurationAppService.GetAccountDistributionCodeAsync();
                if (accountDistributionCode != null)
                {
                    Invoice? invoice = await InitializeCASInvoice(paymentRequest, accountDistributionCode);

                    if (invoice != null)
                    {
                        invoiceResponse = await CreateInvoiceAsync(invoice);
                        if (invoiceResponse != null)
                        {
                            await UpdatePaymentRequestWithInvoice(paymentRequest.Id, invoiceResponse);
                        }
                    }
                }
            } catch (Exception ex) {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "CreateInvoiceByPaymentRequestAsync Exception: {ExceptionMessage}", ExceptionMessage);
            }

            return invoiceResponse;
        }

        private async Task UpdatePaymentRequestWithInvoice(Guid paymentRequestId, InvoiceResponse invoiceResponse)
        {
            try
            {
                using var uow = _unitOfWorkManager.Begin();
                PaymentRequest? paymentRequest = await _iPaymentRequestRepository.GetAsync(paymentRequestId);
                paymentRequest.SetCasHttpStatusCode((int)invoiceResponse.CASHttpStatusCode);
                paymentRequest.SetCasResponse(invoiceResponse.CASReturnedMessages);
                // Set the status - for the payment request
                if (invoiceResponse.IsSuccess())
                {
                    paymentRequest.SetInvoiceStatus(CasPaymentRequestStatus.SentToCas);
                }
                else
                {
                    paymentRequest.SetInvoiceStatus(CasPaymentRequestStatus.ErrorFromCas);
                }
                
                await _iPaymentRequestRepository.UpdateAsync(paymentRequest, autoSave: false);
                await uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "CreateInvoiceByPaymentRequestAsync Exception: {ExceptionMessage}", ExceptionMessage);
            }
        }

        public async Task<InvoiceResponse> CreateInvoiceAsync(Invoice casAPInvoice)
        {
            string jsonString = JsonSerializer.Serialize(casAPInvoice);
            var authToken = await _iTokenService.GetAuthTokenAsync();
            var resource = $"{_casClientOptions.Value.CasBaseUrl}/{CFS_APINVOICE}/";						
            var response = await _resilientRestClient.HttpAsyncWithBody(HttpMethod.Post, resource, jsonString, authToken);

            if (response != null)
            {
                if(response.Content != null && response.StatusCode != HttpStatusCode.NotFound)
                {
                    var contentString = ResilientHttpRequest.ContentToString(response.Content);
                    var result = JsonSerializer.Deserialize<InvoiceResponse>(contentString)
                        ?? throw new UserFriendlyException("CAS InvoiceService CreateInvoiceAsync Exception: " + response);
                    result.CASHttpStatusCode = response.StatusCode;
                    return result;
                }
                else if (response.RequestMessage != null)
                {
                    throw new UserFriendlyException("CAS InvoiceService CreateInvoiceAsync Exception: " + response.RequestMessage);
                } else
                {
                    throw new UserFriendlyException("CAS InvoiceService CreateInvoiceAsync Exception: " + response);
                }
            }
            else
            {
                throw new UserFriendlyException("CAS InvoiceService CreateInvoiceAsync: Null response");
            }
        }

        public async Task<CasPaymentSearchResult> GetCasInvoiceAsync(string invoiceNumber, string supplierNumber, string supplierSiteCode)
        {
            var authToken = await _iTokenService.GetAuthTokenAsync();
			var resource = $"{_casClientOptions.Value.CasBaseUrl}/{CFS_APINVOICE}/{invoiceNumber}/{supplierNumber}/{supplierSiteCode}";
            var response = await _resilientRestClient.HttpAsync(HttpMethod.Get, resource, authToken);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string contentString = ResilientHttpRequest.ContentToString(response.Content);
                var result = JsonSerializer.Deserialize<CasPaymentSearchResult>(contentString);
                return result ?? new CasPaymentSearchResult();
            }
            else
            {
                return new CasPaymentSearchResult() {};
            }
        }

        public async Task<CasPaymentSearchResult> GetCasPaymentAsync(string invoiceNumber, string supplierNumber, string siteNumber)
        {
            var authToken = await _iTokenService.GetAuthTokenAsync();
            var resource = $"{_casClientOptions.Value.CasBaseUrl}/{CFS_APINVOICE}/{invoiceNumber}/{supplierNumber}/{siteNumber}";
            var response = await _resilientRestClient.HttpAsync(HttpMethod.Get, resource, authToken);
            CasPaymentSearchResult casPaymentSearchResult = new CasPaymentSearchResult();

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CasPaymentSearchResult>(content.Result);
                return result ?? casPaymentSearchResult;
            }
            else if(response != null)
            {
                casPaymentSearchResult.InvoiceStatus = response.StatusCode.ToString();
            }

            return casPaymentSearchResult;
        }
    }

#pragma warning disable S125 // Sections of code should not be commented out

    /*
    <INVOICE NUMBER>/<SUPPLIER NUMBER>/<SUPPLIER SITE CODE>
     Example Response for GET:
     {
     "invoice_number": "TESTINVOICE2",
     "invoice_status": "Validated",
     "payment_status": " Paid",
     "payment_number": "009877676",
     "payment_date": "25-Aug-2017"
     }

    Void Payment Webservices Request Format, Type POST
    https://<server>:<port>/ords/cas/cfs/apinvoice/
    Sample JSON File – Regular Standard Invoice -  Web Service
    {
        "invoiceType": "Standard",
        "supplierNumber": "3125635",
        "supplierSiteNumber": "001",
        "invoiceDate": "06-MAR-2023",  --- 
        "invoiceNumber": "CAETEST0B",
        "invoiceAmount": 150.00,
        "payGroup": "GEN CHQ",  -- COULD BE GEN EFT
        "dateInvoiceReceived":"02-MAR-2023", --- 
        "dateGoodsReceived": "01-MAR-2023",
        "remittanceCode": "01", -- Refers to Invoice Number in Remmitance
        "specialHandling": "N",
        "nameLine1": "",
        "nameLine2": "",
        "qualifiedReceiver": "",
        "terms": "Immediate",
        "payAloneFlag": "Y",
        "paymentAdviceComments": "Test",
        "remittanceMessage1": "",
        "remittanceMessage2": "",
        "remittanceMessage3": "",
        "glDate": "06-MAR-2023",
        "invoiceBatchName": "CASAPWEB1",
        "currencyCode": "CAD",
        "invoiceLineDetails": 
            [{
            "invoiceLineNumber": 1,
            "invoiceLineType": "Item",
            "lineCode": "DR",
            "invoiceLineAmount": 150.00,
            "defaultDistributionAccount": "039.15006.10120.5185.1500000.000000.0000",
            "description": "Test Line Description",
            "taxClassificationCode": "",
            "distributionSupplier": "",
            "info1": "",
            "info2": "",
            "info3": ""
            }]
    }
    */
#pragma warning restore S125 // Sections of code should not be commented out
}