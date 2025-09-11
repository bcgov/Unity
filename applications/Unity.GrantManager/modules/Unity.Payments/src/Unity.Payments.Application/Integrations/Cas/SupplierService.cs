using Volo.Abp;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using Volo.Abp.Application.Services;
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
        private const string CFS_SUPPLIER = "cfs/supplier";
        private readonly Task<string> casBaseApiTask;
        private readonly ILocalEventBus localEventBus;
        private readonly IResilientHttpRequest resilientHttpRequest;
        private readonly ICasTokenService iTokenService;
        protected new ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        public SupplierService(
            ILocalEventBus localEventBus,
            IEndpointManagementAppService endpointManagementAppService,
            IResilientHttpRequest resilientHttpRequest,
            ICasTokenService iTokenService)
        {
            this.localEventBus = localEventBus;
            this.resilientHttpRequest = resilientHttpRequest;
            this.iTokenService = iTokenService;
            casBaseApiTask = endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE)
                .ContinueWith(t => t.Result ?? throw new UserFriendlyException("Payment API base URL is not configured."));
        }

        public virtual async Task UpdateApplicantSupplierInfo(string? supplierNumber, Guid applicantId)
        {
            Logger.LogInformation("UpdateApplicantSupplierInfo: {SupplierNumber}, {ApplicantId}", supplierNumber, applicantId);
            if (!await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature) || string.IsNullOrEmpty(supplierNumber))
            {
                return;
            }

            var casSupplierResponse = await GetCasSupplierInformationAsync(supplierNumber);
            await UpdateSupplierInfo(casSupplierResponse, applicantId);
        }

        public async Task<dynamic> UpdateApplicantSupplierInfoByBn9(string? bn9, Guid applicantId)
        {
            Logger.LogInformation("UpdateApplicantSupplierInfoByBn9: {Bn9}, {ApplicantId}", bn9, applicantId);
            if (!await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature) || string.IsNullOrEmpty(bn9))
            {
                throw new UserFriendlyException("Feature is disabled or BN9 is null or empty.");
            }

            var casSupplierResponse = await GetCasSupplierInformationByBn9Async(bn9);
            try
            {
                var items = casSupplierResponse.GetProperty("items");
                if (items is JsonElement { ValueKind: JsonValueKind.Array } array && array.GetArrayLength() > 0)
                {
                    await UpdateSupplierInfo(array[0], applicantId);
                    return array[0];
                }
                throw new UserFriendlyException("No items found in CAS Supplier response.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception updating supplier for BN9: {Message}", ex.Message);
                return $"Exception updating supplier for BN9: {ex.Message}";
            }
        }

        private async Task UpdateSupplierInfo(dynamic casSupplierResponse, Guid applicantId)
        {
            try
            {
                var casSupplierJson = casSupplierResponse is string str ? str : casSupplierResponse.ToString();
                using var doc = JsonDocument.Parse(casSupplierJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("code", out JsonElement codeProp) && codeProp.GetString() == "Unauthorized")
                {
                    throw new UserFriendlyException("Unauthorized access to CAS supplier information.");
                }

                var supplierEto = GetEventDtoFromCasResponse(root);
                supplierEto.CorrelationId = applicantId;
                supplierEto.CorrelationProvider = CorrelationConsts.Applicant;
                await localEventBus.PublishAsync(supplierEto);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception updating supplier: {Message}", ex.Message);
                throw new UserFriendlyException("Exception updating supplier: " + ex.Message);
            }
        }

        protected virtual UpsertSupplierEto GetEventDtoFromCasResponse(JsonElement casSupplierResponse)
        {
            string GetProp(string name) =>
                casSupplierResponse.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.ToString()
                    : string.Empty;

            DateTime lastUpdatedDate = default;
            var lastUpdatedStr = GetProp("lastupdated");
            if (!string.IsNullOrEmpty(lastUpdatedStr))
            {
                if (!DateTime.TryParse(lastUpdatedStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out lastUpdatedDate))
                {
                    Logger.LogWarning("Failed to parse 'lastupdated' date: {LastUpdated}", lastUpdatedStr);
                }
            }

            var siteEtos = new List<SiteEto>();
            if (casSupplierResponse.TryGetProperty("supplieraddress", out var sitesJson) && sitesJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var site in sitesJson.EnumerateArray())
                {
                    siteEtos.Add(GetSiteEto(site));
                }
            }

            return new UpsertSupplierEto
            {
                Number = GetProp("suppliernumber"),
                Name = GetProp("suppliername"),
                Subcategory = GetProp("subcategory"),
                ProviderId = GetProp("providerid"),
                BusinessNumber = GetProp("businessnumber"),
                Status = GetProp("status"),
                SupplierProtected = GetProp("supplierprotected"),
                StandardIndustryClassification = GetProp("standardindustryclassification"),
                LastUpdatedInCAS = lastUpdatedDate,
                SiteEtos = siteEtos
            };
        }

        protected static SiteEto GetSiteEto(JsonElement site)
        {
            string Get(string name) => site.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null ? prop.ToString() : string.Empty;
            string accountNumber = Get("accountnumber");
            string maskedAccountNumber = accountNumber.Length > 4 ? new string('*', accountNumber.Length - 4) + accountNumber[^4..] : accountNumber;
            DateTime siteLastUpdatedDate = default;
            var lastUpdatedStr = Get("lastupdated");
            if (!string.IsNullOrEmpty(lastUpdatedStr))
            {
                if (!DateTime.TryParse(lastUpdatedStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out siteLastUpdatedDate))
                {
                    // Optionally log or handle the failed parse here
                    siteLastUpdatedDate = default;
                }
            }

            return new SiteEto
            {
                SupplierSiteCode = Get("suppliersitecode"),
                AddressLine1 = Get("addressline1"),
                AddressLine2 = Get("addressline2"),
                AddressLine3 = string.Empty,
                City = Get("city"),
                Province = Get("province"),
                Country = Get("country"),
                PostalCode = Get("postalcode"),
                EmailAddress = Get("emailaddress"),
                EFTAdvicePref = Get("eftadvicepref"),
                BankAccount = maskedAccountNumber,
                ProviderId = Get("providerid"),
                Status = Get("status"),
                SiteProtected = Get("siteprotected"),
                LastUpdated = siteLastUpdatedDate
            };
        }

        public async Task<dynamic> GetCasSupplierInformationAsync(string? supplierNumber)
        {
            if (string.IsNullOrEmpty(supplierNumber))
            {
                throw new UserFriendlyException("CAS Supplier Service: No Supplier Number");
            }

            var casBaseApi = await casBaseApiTask;
            var resource = $"{casBaseApi}/{CFS_SUPPLIER}/{supplierNumber}";
            return await GetCasSupplierInformationByResourceAsync<dynamic>(resource);
        }

        public async Task<dynamic> GetCasSupplierInformationByBn9Async(string? bn9)
        {
            if (string.IsNullOrEmpty(bn9))
            {
                throw new UserFriendlyException("CAS Supplier Service: No Supplier Number");
            }

            var casBaseApi = await casBaseApiTask;
            var resource = $"{casBaseApi}/{CFS_SUPPLIER}/{bn9}/businessnumber";
            return await GetCasSupplierInformationByResourceAsync<dynamic>(resource);
        }

        private async Task<TSupplierInfo> GetCasSupplierInformationByResourceAsync<TSupplierInfo>(string resource)
            where TSupplierInfo : class
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new UserFriendlyException("CAS Supplier Service: No Supplier Number provided");
            }

            var authToken = await iTokenService.GetAuthTokenAsync();
            try
            {
                using var response = await resilientHttpRequest.HttpLongLivedAsync(HttpMethod.Get, resource, authToken: authToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new UserFriendlyException("Supplier not found");
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await ResilientHttpRequest.ContentToJsonAsync<TSupplierInfo>(response.Content)
                        ?? throw new UserFriendlyException("Failed to parse supplier information");
                }

                throw new UserFriendlyException($"CAS service returned error: {response.StatusCode}");
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogWarning(ex, "CAS supplier request timed out for resource: {Resource}", resource);
                throw new UserFriendlyException("The supplier information request timed out. Please try again later.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching CAS supplier info for: {Resource}", resource);
                throw new UserFriendlyException($"Failed to fetch supplier information: {ex.Message}");
            }
        }
    }
}
