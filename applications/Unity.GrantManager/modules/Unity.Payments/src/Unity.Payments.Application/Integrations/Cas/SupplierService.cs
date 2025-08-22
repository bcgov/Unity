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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Unity.GrantManager.Integrations;

namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(SupplierService), typeof(ISupplierService))]
    public class SupplierService : ApplicationService, ISupplierService
    {
        protected new ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
        private const string CFS_SUPPLIER = "cfs/supplier";
        private readonly Task<string> casBaseApiTask;
        private readonly ILocalEventBus localEventBus;
        private readonly IResilientHttpRequest resilientHttpRequest;
        private readonly ICasTokenService iTokenService;
        public SupplierService  (ILocalEventBus localEventBus,
                                IEndpointManagementAppService endpointManagementAppService,
                                IResilientHttpRequest resilientHttpRequest,
                                ICasTokenService iTokenService)
        {
                this.localEventBus = localEventBus;
                this.resilientHttpRequest = resilientHttpRequest;
                this.iTokenService = iTokenService;

                // Initialize the base API URL once during construction
                casBaseApiTask = InitializeBaseApiAsync(endpointManagementAppService);
        }
        private static async Task<string> InitializeBaseApiAsync(IEndpointManagementAppService endpointManagementAppService)
        {
            var url = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE);
            return url ?? throw new UserFriendlyException("Payment API base URL is not configured.");
        }


        public virtual async Task UpdateApplicantSupplierInfo(string? supplierNumber, Guid applicantId)
        {
            Logger.LogInformation("SupplierService->UpdateApplicantSupplierInfo: {SupplierNumber}, {ApplicantId}", supplierNumber, applicantId);

            // Integrate with payments module to update / insert supplier
            if (await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature)
                && !string.IsNullOrEmpty(supplierNumber))
            {
                dynamic casSupplierResponse = await GetCasSupplierInformationAsync(supplierNumber);
                await UpdateSupplierInfo(casSupplierResponse, applicantId);
            }
        }

        public async Task<dynamic> UpdateApplicantSupplierInfoByBn9(string? bn9, Guid applicantId)
        {
            Logger.LogInformation("SupplierService->UpdateApplicantSupplierInfo: {Bn9}, {ApplicantId}", bn9, applicantId);
            bool paymentsEnabled = await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature);
            if (!paymentsEnabled || string.IsNullOrEmpty(bn9))
            {
                throw new UserFriendlyException("Feature is disabled or BN9 is null or empty.");
            }

            dynamic casSupplierResponse = await GetCasSupplierInformationByBn9Async(bn9);
            try
            {
                var items = casSupplierResponse.GetProperty("items");
                if (items is JsonElement { ValueKind: JsonValueKind.Array } array && array.GetArrayLength() > 0)
                {
                    casSupplierResponse = array[0];
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
                casSupplierResponse = "An exception occurred updating the supplier for BN9: " + ex.Message;
            }

            return casSupplierResponse;
        }

        private async Task UpdateSupplierInfo(dynamic casSupplierResponse, Guid applicantId)
        {
            try
            {
                if (casSupplierResponse.TryGetProperty("code", out JsonElement codeProp) && codeProp.GetString() == "Unauthorized")
                    throw new UserFriendlyException("Unauthorized access to CAS supplier information.");
                var casSupplierJson = casSupplierResponse is string str ? str : casSupplierResponse.ToString();
                using var doc = JsonDocument.Parse(casSupplierJson);
                UpsertSupplierEto supplierEto = GetEventDtoFromCasResponse(doc.RootElement);
                supplierEto.CorrelationId = applicantId;
                supplierEto.CorrelationProvider = CorrelationConsts.Applicant;
                await localEventBus.PublishAsync(supplierEto);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred updating the supplier: {ExceptionMessage}", ex.Message);
                throw new UserFriendlyException("An exception occurred updating the supplier: " + ex.Message);
            }
        }

        protected virtual UpsertSupplierEto GetEventDtoFromCasResponse(JsonElement casSupplierResponse)
        {
            string GetProp(string name) =>
                casSupplierResponse.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.ToString()
                    : string.Empty;

            string lastUpdated = GetProp("lastupdated");
            string suppliernumber = GetProp("suppliernumber");
            string suppliername = GetProp("suppliername");
            string subcategory = GetProp("subcategory");
            string providerid = GetProp("providerid");
            string businessnumber = GetProp("businessnumber");
            string status = GetProp("status");
            string supplierprotected = GetProp("supplierprotected");
            string standardindustryclassification = GetProp("standardindustryclassification");

            _ = DateTime.TryParse(lastUpdated, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime lastUpdatedDate);

            var siteEtos = new List<SiteEto>();
            if (casSupplierResponse.TryGetProperty("supplieraddress", out var sitesJson) &&
                sitesJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var site in sitesJson.EnumerateArray())
                {
                    siteEtos.Add(GetSiteEto(site));
                }
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
                var casBaseApi = await casBaseApiTask;
                var resource = $"{casBaseApi}/{CFS_SUPPLIER}/{supplierNumber}";
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
                var casBaseApi = await casBaseApiTask;
                var resource = $"{casBaseApi}/{CFS_SUPPLIER}/{bn9}/businessnumber";
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
                try
                {
                    using var response = await resilientHttpRequest.HttpAsync(HttpMethod.Get, resource, authToken);
                    if (response != null)
                    {
                        if (response.Content != null && response.StatusCode != HttpStatusCode.NotFound)
                        {
                            var contentString = await response.Content.ReadAsStringAsync();
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
                catch (Exception ex)
                {
                    string ExceptionMessage = ex.Message;
                    Logger.LogError(ex, "An exception occurred while fetching CAS Supplier Information: {ExceptionMessage}", ExceptionMessage);
                    throw new UserFriendlyException($"Failed to fetch supplier information from CAS. Exception:  {ExceptionMessage}");
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
