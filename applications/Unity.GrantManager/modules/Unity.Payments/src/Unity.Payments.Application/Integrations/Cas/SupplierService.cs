using Volo.Abp;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using System.Net.Http;
using Unity.Modules.Shared.Http;
using Volo.Abp.EventBus.Local;
using Unity.GrantManager.Payments;
using Unity.Payments.Suppliers;
using Unity.Modules.Shared.Correlation;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(SupplierService), typeof(ISupplierService))]
    public class SupplierService(ILocalEventBus localEventBus,
                                IResilientHttpRequest resilientHttpRequest,
                                IOptions<CasClientOptions> casClientOptions,
                                ICasTokenService iTokenService) : ApplicationService, ISupplierService
    {
        protected new ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        private const string CFS_SUPPLIER = "cfs/supplier";

        public virtual async Task UpdateApplicantSupplierInfo(string? supplierNumber, Guid applicantId)
        {
            Logger.LogWarning("SupplierService->UpdateApplicantSupplierInfo: {SupplierNumber}, {ApplicantId}", supplierNumber, applicantId);
            
            // Integrate with payments module to update / insert supplier
            if (await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature)
                && !string.IsNullOrEmpty(supplierNumber))
            {
                dynamic casSupplierResponse = await GetCasSupplierInformationAsync(supplierNumber);
                Logger.LogWarning("SupplierService->UpdateApplicantSupplierInfo: Response {CasSupplierResponse}", (string)casSupplierResponse.ToString());
                await UpdateSupplierInfo(casSupplierResponse, applicantId);
            }
        }

        public async Task<dynamic> UpdateApplicantSupplierInfoByBn9(string? bn9, Guid applicantId)
        {
            Logger.LogWarning("SupplierService->UpdateApplicantSupplierInfo: {Bn9}, {ApplicantId}", bn9, applicantId);
            bool paymentsEnabled = await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature);
            if (!paymentsEnabled || string.IsNullOrEmpty(bn9))
            {
                throw new UserFriendlyException("Feature is disabled or BN9 is null or empty.");
            }


            string casRespString = "{\"items\":[{\"suppliernumber\":\"2184411\",\"suppliername\":\"BLUE BRIDGE THEATRE SOCIETY\",\"subcategory\":\"Business\",\"sin\":null,\"providerid\":null,\"businessnumber\":\"853184497\",\"status\":\"ACTIVE\",\"supplierprotected\":null,\"standardindustryclassification\":null,\"lastupdated\":\"2024-05-16 16:58:21\",\"supplieraddress\":[{\"suppliersitecode\":\"001\",\"addressline1\":\"2657 QUADRA ST\",\"addressline2\":null,\"addressline3\":null,\"city\":\"VICTORIA\",\"province\":\"BC\",\"country\":\"CA\",\"postalcode\":\"V8T4E3\",\"emailaddress\":null,\"accountnumber\":null,\"branchnumber\":null,\"banknumber\":null,\"eftadvicepref\":null,\"providerid\":null,\"status\":\"ACTIVE\",\"siteprotected\":null,\"lastupdated\":\"2017-04-24 18:17:37\"}]}],\"hasMore\":false,\"limit\":0,\"offset\":0,\"count\":1,\"links\":[{\"rel\":\"self\",\"href\":\"https://cfs-prodws.cas.gov.bc.ca:7121/ords/cas/cfs/supplier/853184497/businessnumber\"},{\"rel\":\"describedby\",\"href\":\"https://cfs-prodws.cas.gov.bc.ca:7121/ords/cas/metadata-catalog/cfs/supplier/853184497/item\"}]}";
            dynamic casSupplierResponse = JsonSerializer.Deserialize<dynamic>(casRespString) ?? throw new UserFriendlyException("Deserialization resulted in null.");
            try
            {
                var items = casSupplierResponse.GetProperty("items");
                if (items is JsonElement { ValueKind: JsonValueKind.Array } array && array.GetArrayLength() > 0)
                {
                    casSupplierResponse = array[0];
                    Logger.LogWarning("SupplierService->UpdateApplicantSupplierInfo: {CasSupplierResponse}", (string)casSupplierResponse.ToString());
                    await UpdateSupplierInfo(casSupplierResponse, applicantId);
                }
                else
                {
                    throw new UserFriendlyException("No items found in CAS Supplier response.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred updating the supplier for BN9: {ExceptionMessage}", ex.Message);
                casSupplierResponse  = "An exception occurred updating the supplier for BN9: " + ex.Message;
            }

            return casSupplierResponse;
        }

        private async Task UpdateSupplierInfo(dynamic casSupplierResponse, Guid applicantId)
        {
            UpsertSupplierEto supplierEto = GetEventDtoFromCasResponse(casSupplierResponse);
            supplierEto.CorrelationId = applicantId;
            supplierEto.CorrelationProvider = CorrelationConsts.Applicant;
            string supEt = JsonSerializer.Serialize(supplierEto);
            Logger.LogWarning("SupplierService->UpdateApplicantSupplierInfo: Publishing supplier: {SupEt}", supEt);
            await localEventBus.PublishAsync(supplierEto);
        }

        protected virtual UpsertSupplierEto GetEventDtoFromCasResponse(dynamic casSupplierResponse)
        {
            string lastUpdated = casSupplierResponse.GetProperty("lastupdated").ToString();
            string suppliernumber = casSupplierResponse.GetProperty("suppliernumber").ToString();
            string suppliername = casSupplierResponse.GetProperty("suppliername").ToString();
            string subcategory = casSupplierResponse.GetProperty("subcategory").ToString();
            string providerid = casSupplierResponse.GetProperty("providerid").ToString();
            string businessnumber = casSupplierResponse.GetProperty("businessnumber").ToString();
            string status = casSupplierResponse.GetProperty("status").ToString();
            string supplierprotected = casSupplierResponse.GetProperty("supplierprotected").ToString();
            string standardindustryclassification = casSupplierResponse.GetProperty("standardindustryclassification").ToString();

            _ = DateTime.TryParse(lastUpdated, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime lastUpdatedDate);
            List<SiteEto> siteEtos = new List<SiteEto>();
            JArray siteArray = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(casSupplierResponse.GetProperty("supplieraddress").ToString());
            foreach (dynamic site in siteArray)
            {
                siteEtos.Add(GetSiteEto(site));
            }

            return new UpsertSupplierEto
            {
                Number = suppliernumber,
                Name = suppliername,
                Subcategory = subcategory,
                ProviderId = providerid,
                BusinessNumber = businessnumber,
                Status = status,
                SupplierProtected = supplierprotected,
                StandardIndustryClassification = standardindustryclassification,
                LastUpdatedInCAS = lastUpdatedDate,
                SiteEtos = siteEtos
            };
        }

        protected static SiteEto GetSiteEto(dynamic site)
        {
            string supplierSiteCode = site["suppliersitecode"].ToString();
            string addressLine1 = site["addressline1"].ToString();
            string addressLine2 = site["addressline2"].ToString();
            string city = site["city"].ToString();
            string province = site["province"].ToString();
            string country = site["country"].ToString();
            string postalCode = site["postalcode"].ToString();
            string emailAddress = site["emailaddress"].ToString();
            string eftAdvicePref = site["eftadvicepref"].ToString();
            string accountNumber = site["accountnumber"].ToString();
            string maskedAccountNumber = accountNumber.Length > 4
                ? new string('*', accountNumber.Length - 4) + accountNumber[^4..]
                : accountNumber;
            string bankAccount = maskedAccountNumber;
            string providerId = site["providerid"].ToString();
            string siteStatus = site["status"].ToString();
            string siteProtected = site["siteprotected"].ToString();
            string siteLastUpdated = site["lastupdated"].ToString();

            _ = DateTime.TryParse(siteLastUpdated, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime siteLastUpdatedDate);
            return new SiteEto
            {
                SupplierSiteCode = supplierSiteCode,
                AddressLine1 = addressLine1,
                AddressLine2 = addressLine2,
                AddressLine3 = addressLine2,
                City = city,
                Province = province,
                Country = country,
                PostalCode = postalCode,
                EmailAddress = emailAddress,
                EFTAdvicePref = eftAdvicePref,
                BankAccount = bankAccount,
                ProviderId = providerId,
                Status = siteStatus,
                SiteProtected = siteProtected,
                LastUpdated = siteLastUpdatedDate
            };
        }
        public async Task<dynamic> GetCasSupplierInformationAsync(string? supplierNumber)
        {
            if (!string.IsNullOrEmpty(supplierNumber))
            {
                var resource = $"{casClientOptions.Value.CasBaseUrl}/{CFS_SUPPLIER}/{supplierNumber}";
                return await GetCasSupplierInformationByResourceAsync(resource);
            }
            else
            {
                throw new UserFriendlyException("CAS Supplier Service: No Supplier Number");
            }
        }

        public async Task<dynamic> GetCasSupplierInformationByBn9Async(string? bn9)
        {
            if (!string.IsNullOrEmpty(bn9))
            {
                var resource = $"{casClientOptions.Value.CasBaseUrl}/{CFS_SUPPLIER}/{bn9}/businessnumber";
                return await GetCasSupplierInformationByResourceAsync(resource);
            }
            else
            {
                throw new UserFriendlyException("CAS Supplier Service: No Supplier Number");
            }
        }


        private async Task<dynamic> GetCasSupplierInformationByResourceAsync(string? resource)
        {
            if (!string.IsNullOrEmpty(resource))
            {
                var authToken = await iTokenService.GetAuthTokenAsync();
                var response = await resilientHttpRequest.HttpAsync(HttpMethod.Get, resource, authToken);

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
                        throw new UserFriendlyException("Supplier not Found.");
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
