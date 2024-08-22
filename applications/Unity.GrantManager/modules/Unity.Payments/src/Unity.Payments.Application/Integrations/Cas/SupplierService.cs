using Volo.Abp;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using Unity.Payments.Integrations.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using System.Net.Http;

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
                var authToken = await _iTokenService.GetAuthTokenAsync();
                var resource = $"{_casClientOptions.Value.CasBaseUrl}/{CFS_SUPPLIER}/{supplierNumber}";
                var response = await _resilientRestClient.HttpAsync(HttpMethod.Get, resource, authToken);

                if (response != null)
                {
                    if (response.Content != null && response.StatusCode != HttpStatusCode.NotFound)
                    {
                        var contentString = ResilientHttpRequest.ContentToString(response.Content);                        
                        var result = JsonSerializer.Deserialize<dynamic>(contentString)
                            ?? throw new UserFriendlyException("CAS SupplierService GetCasSupplierInformationAsync: " + response);
                        return result;
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new UserFriendlyException("You have entered an invalid Supplier #.");
                    }
                    else if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new UserFriendlyException("CAS SupplierService GetCasSupplierInformationAsync Status Code: " + response.StatusCode);
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