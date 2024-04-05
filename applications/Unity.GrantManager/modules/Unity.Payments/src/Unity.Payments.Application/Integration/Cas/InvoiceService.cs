using Volo.Abp;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.Integrations.Http;
using Unity.Payments.Integration.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;

namespace Unity.Payments.Integration.Cas
{
    [IntegrationService]
    public class InvoiceService : ApplicationService, IInvoiceService
    {
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly IOptions<CasClientOptions> _casClientOptions;
        private readonly RestClient _restClient;

		// MOVE THESE - SHOULD BE ROUTES???
		private const string OAUTH_PATH = "oauth/token";
		private const string CFS_APINVOICE = "cfs/apinvoice";
        // "CasBaseUrl": "https://<server>:<port>/ords/cas/",

        public InvoiceService(
			IResilientHttpRequest resilientHttpRequest,
            IOptions<CasClientOptions> casClientOptions,
            RestClient restClient)
        {
            _resilientRestClient = resilientHttpRequest;
            _casClientOptions = casClientOptions;
            _restClient = restClient;
        }

		public async Task<InvoiceResponse> CreateInvoiceAsync(Invoice casAPInvoice)
        {
            var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(casAPInvoice);
            HttpContent postContent = new StringContent(jsonString);

            var authHeaders = await GetAuthHeadersAsync();
            var resource = $"{_casClientOptions.Value.CasBaseUrl}{CFS_APINVOICE}";
			
            //var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(casAPInvoice);
			
            var response = await _resilientRestClient.HttpAsync(Method.Post, resource, authHeaders, postContent);

            if (response != null && response.Content != null)
            {
                var result = JsonSerializer.Deserialize<InvoiceResponse>(response.Content) ?? throw new Exception();
                return result;
            }
            else if (response != null && response.ErrorMessage != null)
            {
                throw new UserFriendlyException("CAS InvoiceService CreateInvoiceAsync Exception: " + response.ErrorMessage);
            } else
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
                var result = JsonSerializer.Deserialize<CasPaymentSearchResult>(content) ?? throw new Exception();

                return result;
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
                var result = JsonSerializer.Deserialize<CasPaymentSearchResult>(content) ?? throw new Exception();
                return result;
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

		// Concept of routes?
		// What are all the routes

	    // Create Invoice Request Format, Type POST
		//https://<server>:<port>/ords/cas/cfs/apinvoice/
		
		//Create Invoice Response Format
		//Lookup Invoice Request Format, Type GET
		//<INVOICE NUMBER>/<SUPPLIER NUMBER>/<SUPPLIER SITE CODE>

		// Example Response for GET:
			// {
			// "invoice_number": "TESTINVOICE2",
			// "invoice_status": "Validated",
			// "payment_status": " Paid",
			// "payment_number": "009877676",
			// "payment_date": "25-Aug-2017"
			// }

		// Void Payment Webservices Request Format, Type POST

/*
Sample JSON File – Regular Standard Invoice -  Web Service
{
	"invoiceType": "Standard",
	"supplierNumber": "3125635",
	"supplierSiteNumber": "001",
	"invoiceDate": "06-MAR-2023",  --- 
	"invoiceNumber": "CAETEST0B",
	"invoiceAmount": 150.00,
	"payGroup": "GEN CHQ",  -- SHOULD BE EFT
	"dateInvoiceReceived":"02-MAR-2023", --- 
	"dateGoodsReceived": "01-MAR-2023",
	"remittanceCode": "01",
	"specialHandling": "N",
	"nameLine1": "",
	"nameLine2": "",
	"addressLine1": "",
	"addressLine2": "",
	"addressLine3": "",
	"city": "",
	"country": "",
	"province": "",
	"postalCode": "",
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