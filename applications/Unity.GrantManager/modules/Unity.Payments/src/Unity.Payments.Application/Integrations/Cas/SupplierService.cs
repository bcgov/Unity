using Volo.Abp;
using RestSharp;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using Unity.Payments.Integrations.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(SupplierService), typeof(ISupplierService))]
    public class SupplierService : ApplicationService, ISupplierService
    {
        private readonly ITokenService _iTokenService;
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly IOptions<CasClientOptions> _casClientOptions;

        private const string CFS_SUPPLIER = "cfs/supplier";


        public SupplierService(
            ITokenService iTokenService,
            IResilientHttpRequest resilientHttpRequest,
            IOptions<CasClientOptions> casClientOptions)
        {
            _iTokenService = iTokenService;
            _resilientRestClient = resilientHttpRequest;
            _casClientOptions = casClientOptions;
        }
                
        public async Task<dynamic> GetCasSupplierInformationAsync(string? supplierNumber)
        {   
            if(!string.IsNullOrEmpty(supplierNumber))
            {
                var authHeaders = await _iTokenService.GetAuthHeadersAsync();
                var resource = $"{_casClientOptions.Value.CasBaseUrl}/{CFS_SUPPLIER}/{supplierNumber}";
                var response = await _resilientRestClient.HttpAsync(Method.Get, resource, authHeaders);

                if (response != null)
                {
                    if (response.Content != null && response.StatusCode != HttpStatusCode.NotFound)
                    {
                        var result = JsonSerializer.Deserialize<dynamic>(response.Content)
                            ?? throw new UserFriendlyException("CAS SupplierService GetCasSupplierInformationAsync: " + response);
                        return result;
                    }
                    else if (response.ErrorMessage != null)
                    {
                        throw new UserFriendlyException("CAS SupplierService GetCasSupplierInformationAsync Exception: " + response.ErrorMessage);
                    }
                    else
                    {
                        throw new UserFriendlyException("The CAS Supplier Number was not found.");
                    }
                }
                else
                {
                    throw new UserFriendlyException("CAS SupplierService GetCasSupplierInformationAsync: Null response");
                }
            }
            else
            {
                throw new UserFriendlyException("CAS Supplier Service: No Supplier Number");
            }
        }

  
    }

#pragma warning disable S125 // Sections of code should not be commented out

    /*
     * Response:
           {
            "suppliernumber": "2002492",
            "suppliername": "GENSUPPOSP2016, ONE",
            "subcategory": "Individual",
            "sin": null,
            "providerid": null,
            "businessnumber": null,
            "status": "INACTIVE",
            "supplierprotected": null,
            "standardindustryclassification": null,
            "lastupdated": "2024-05-10 12:21:53",
            "supplieraddress": [
                {
                    "suppliersitecode": "001",
                    "addressline1": "100-3350 DOUGLAS ST",
                    "addressline2": null,
                    "addressline3": null,
                    "city": "VICTORIA",
                    "province": "BC",
                    "country": "CA",
                    "postalcode": "V8Z3L1",
                    "emailaddress": null,
                    "accountnumber": null,
                    "branchnumber": null,
                    "banknumber": null,
                    "eftadvicepref": null,
                    "providerid": null,
                    "status": "INACTIVE",
                    "siteprotected": null,
                    "lastupdated": "2021-03-18 14:46:32"
                }
            ]
        }
    */
#pragma warning restore S125 // Sections of code should not be commented out
}