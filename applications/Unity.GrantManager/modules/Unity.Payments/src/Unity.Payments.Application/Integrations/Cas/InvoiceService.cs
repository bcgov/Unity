using Volo.Abp;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using Unity.Payments.Integrations.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Unity.Payments.Enums;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Volo.Abp.DependencyInjection;


namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(InvoiceService), typeof(IInvoiceService))]
    public class InvoiceService : ApplicationService, IInvoiceService
    {

        private readonly IPaymentRequestRepository _iPaymentRequestRepository;
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly ISiteRepository _iSiteRepository;
        private readonly ISupplierRepository _iSupplierRepository;
        private readonly IOptions<CasClientOptions> _casClientOptions;
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly RestClient _restClient;
        private const string OAUTH_PATH = "oauth/token";
        private const string CFS_APINVOICE = "cfs/apinvoice";

        private readonly Dictionary<int, string> CASPaymentGroup = new Dictionary<int, string>
        {
            { (int)PaymentGroup.EFT, "GEN EFT" },
            { (int)PaymentGroup.Cheque, "GEN CHQ" }
        };

        public InvoiceService(
            IPaymentRequestRepository paymentRequestRepository,
            IPaymentConfigurationAppService paymentConfigurationAppService,
            IResilientHttpRequest resilientHttpRequest,
            IOptions<CasClientOptions> casClientOptions,
            ISupplierRepository iSupplierRepository,
            ISiteRepository iSiteRepository,
            RestClient restClient)
        {
            _iPaymentRequestRepository = paymentRequestRepository;
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _resilientRestClient = resilientHttpRequest;
            _casClientOptions = casClientOptions;
            _iSupplierRepository = iSupplierRepository;
            _iSiteRepository = iSiteRepository;
            _restClient = restClient;
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

        public async Task<InvoiceResponse?> CreateInvoiceByPaymentRequestAsync(PaymentRequest paymentRequest)
        {
            InvoiceResponse invoiceResponse = new();
            string? accountDistributionCode = await _paymentConfigurationAppService.GetAccountDistributionCodeAsync();

            if (accountDistributionCode != null)
            {
                Invoice? invoice = await InitializeCASInvoice(paymentRequest, accountDistributionCode);
                
                if (invoice != null)
                {
                    invoiceResponse = await CreateInvoiceAsync(invoice);
                    if(invoiceResponse != null)
                    {
                        paymentRequest.SetCasHttpStatusCode((int)invoiceResponse.CASHttpStatusCode);
                        paymentRequest.SetCasResponse(invoiceResponse.CASReturnedMessages);
                        // Set the status - for the payment request
                        if (invoiceResponse.IsSuccess())
                        {
                            paymentRequest.SetInvoiceStatus("SentToCas");
                        } else
                        {
                            paymentRequest.SetInvoiceStatus("ErrorFromCas");
                        }
                        await _iPaymentRequestRepository.UpdateAsync(paymentRequest, autoSave: true);
                    }
                }
            }

            return invoiceResponse;
        }

        public async Task<InvoiceResponse> CreateInvoiceAsync(Invoice casAPInvoice)
        {
            var jsonString = JsonSerializer.Serialize(casAPInvoice);
            var authHeaders = await GetAuthHeadersAsync();
            var resource = $"{_casClientOptions.Value.CasBaseUrl}/{CFS_APINVOICE}/";						
            var response = await _resilientRestClient.HttpAsync(Method.Post, resource, authHeaders, jsonString);

            if (response != null)
            {
                if(response.Content != null && response.StatusCode != HttpStatusCode.NotFound)
                {
                    var result = JsonSerializer.Deserialize<InvoiceResponse>(response.Content)
                        ?? throw new UserFriendlyException("CAS InvoiceService CreateInvoiceAsync Exception: " + response);
                    result.CASHttpStatusCode = response.StatusCode;
                    return result;
                }
                else if (response.ErrorMessage != null)
                {
                    throw new UserFriendlyException("CAS InvoiceService CreateInvoiceAsync Exception: " + response.ErrorMessage);
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
            var authHeaders = await GetAuthHeadersAsync();
			var resource = $"{_casClientOptions.Value.CasBaseUrl}{CFS_APINVOICE}/{invoiceNumber}/{supplierNumber}/{supplierSiteCode}";
            var response = await _resilientRestClient.HttpAsync(Method.Get, resource, authHeaders);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                var result = JsonSerializer.Deserialize<CasPaymentSearchResult>(content);
                return result ?? new CasPaymentSearchResult();
            }
            else
            {
                return new CasPaymentSearchResult() {};
            }
        }

        public async Task<CasPaymentSearchResult> GetCasPaymentAsync(string paymentId)
        {
            var authHeaders = await GetAuthHeadersAsync();
            var resource = $"{_casClientOptions.Value.CasBaseUrl}{CFS_APINVOICE}/payment/{paymentId}";
            var response = await _resilientRestClient.HttpAsync(Method.Get, resource, authHeaders);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                var result = JsonSerializer.Deserialize<CasPaymentSearchResult>(content);
                return result ?? new CasPaymentSearchResult();
            }
            else
            {
                return new CasPaymentSearchResult() { };
            }
        }

        private async Task<Dictionary<string, string>> GetAuthHeadersAsync() {
            var tokenResponse = await GetAccessTokenAsync();

            Dictionary<string, string> authHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {tokenResponse.AccessToken}" }
            };
            return authHeaders;
        }

 		private async Task<TokenValidationResponse> GetAccessTokenAsync()
        {
            var grantType = "client_credentials";

            var request = new RestRequest($"{_casClientOptions.Value.CasBaseUrl}/{OAUTH_PATH}")
            {
                Authenticator = new HttpBasicAuthenticator(_casClientOptions.Value.CasClientId, _casClientOptions.Value.CasClientSecret)
            };

            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"grant_type={grantType}", ParameterType.RequestBody);

            var response = await _restClient.ExecuteAsync(request, Method.Post);

            if (response.Content == null)
            {
                throw new UserFriendlyException($"Error fetching CAS API token - content empty {response.StatusCode} {response.ErrorMessage}");
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError("Error fetching CAS API token {statusCode} {errorMessage} {errorException}", response.StatusCode, response.ErrorMessage, response.ErrorException);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException(response.ErrorMessage);
                }
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(response.Content) ?? throw new UserFriendlyException($"Error deserializing token response {response.StatusCode} {response.ErrorMessage}");
            return tokenResponse;
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