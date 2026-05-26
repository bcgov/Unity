using Volo.Abp;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using Unity.Payments.Integrations.Http;
using Volo.Abp.Application.Services;
using System.Collections.Generic;
using Unity.Payments.Enums;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.PaymentRequests;
using Volo.Abp.DependencyInjection;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Unity.Modules.Shared.Http;
using Unity.GrantManager.Integrations;
using Unity.Payments.Domain.Services;
using Volo.Abp.MultiTenancy;
using System.Linq;
using Volo.Abp.Domain.Repositories;
using Unity.SharedKernel.Utilities;
using Volo.Abp.Identity;

namespace Unity.Payments.Integrations.Cas
{
    [RemoteService(false)]
    [AllowAnonymous]
    [IntegrationService]
    [ExposeServices(typeof(InvoiceService), typeof(IInvoiceService))]
    public class InvoiceService(
                    IEndpointManagementAppService endpointManagementAppService,
                    ICasTokenService iTokenService,
                    IResilientHttpRequest resilientHttpRequest,
                    IInvoiceManager invoiceManager,
                    IRepository<ExpenseApproval, Guid> expenseApprovalRepository,
                    IRepository<IdentityUser, Guid> identityUserRepository) : ApplicationService, IInvoiceService
    {
        private const string CFS_APINVOICE = "cfs/apinvoice";

        protected new ICurrentTenant CurrentTenant =>
            LazyServiceProvider.LazyGetRequiredService<ICurrentTenant>();

        private readonly Dictionary<int, string> CASPaymentGroup = new()
        {
            [(int)PaymentGroup.EFT] = "GEN EFT",
            [(int)PaymentGroup.Cheque] = "GEN CHQ"
        };

        protected virtual async Task<Invoice?> InitializeCASInvoice(
            PaymentRequest paymentRequest,
            string? accountDistributionCode)
        {
            Site? site = await invoiceManager.GetSiteByPaymentRequestAsync(paymentRequest);

            if (site == null ||
                site.Supplier == null ||
                string.IsNullOrWhiteSpace(site.Supplier.Number) ||
                string.IsNullOrWhiteSpace(accountDistributionCode))
            {
                return null;
            }


            // This can not be UTC Now it is sent to cas and can not be in the future - this is not being stored in Unity as a date
            var vancouverTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vancouverTimeZone);
            var currentMonth = localDateTime.ToString("MMM").Trim('.');
            var currentDay = localDateTime.ToString("dd");
            var currentYear = localDateTime.ToString("yyyy");
            var dateStringDayMonYear = $"{currentDay}-{currentMonth}-{currentYear}";

            if (!CASPaymentGroup.TryGetValue((int)site.PaymentGroup, out var payGroup))
            {
                throw new UserFriendlyException(
                    $"Unsupported payment group: {site.PaymentGroup}");
            }

            var casInvoice = new Invoice
            {
                SupplierNumber = site.Supplier.Number,
                SupplierName = site.Supplier.Name,
                SupplierSiteNumber = site.Number,
                PayGroup = payGroup,
                InvoiceNumber = paymentRequest.InvoiceNumber,
                InvoiceDate = dateStringDayMonYear,
                DateInvoiceReceived = dateStringDayMonYear,
                GlDate = dateStringDayMonYear,
                InvoiceAmount = paymentRequest.Amount,
                InvoiceBatchName = paymentRequest.BatchName,

                // Payment description: build or use existing
                PaymentAdviceComments =
                await BuildPaymentDescriptionAsync(paymentRequest.Description),

                // Level1 approver username
                QualifiedReceiver =
                await GetLevel1DecisionUserNameAsync(paymentRequest),

                InvoiceLineDetails = new List<InvoiceLineDetail>
                {
                    new()
                    {
                        InvoiceLineNumber = 1,
                        InvoiceLineAmount = paymentRequest.Amount,
                        DefaultDistributionAccount = accountDistributionCode
                    }
                }
            };

            return casInvoice;
        }

        private async Task<string> GetLevel1DecisionUserNameAsync(
            PaymentRequest? paymentRequest)
        {
            if (paymentRequest == null)
            {
                return string.Empty;
            }

            Guid? decisionUserId = null;

            try
            {
                if (paymentRequest.ExpenseApprovals == null ||
                    paymentRequest.ExpenseApprovals.Count == 0)
                {
                    var approvals = await expenseApprovalRepository.GetListAsync(
                        a => a.PaymentRequestId == paymentRequest.Id &&
                             a.Type == ExpenseApprovalType.Level1);

                    decisionUserId = approvals
                        .FirstOrDefault()?
                        .DecisionUserId;
                }
                else
                {
                    decisionUserId = paymentRequest.ExpenseApprovals
                        .FirstOrDefault(x => x.Type == ExpenseApprovalType.Level1)?
                        .DecisionUserId;
                }

                if (decisionUserId == null || decisionUserId == Guid.Empty)
                {
                    return string.Empty;
                }

                var user = await identityUserRepository.FindAsync(
                    (Guid)decisionUserId);

                if (user == null)
                {
                    return string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(user.UserName))
                {
                    return user.UserName;
                }

                var fullName = $"{user.Name} {user.Surname}".Trim();

                return string.IsNullOrWhiteSpace(fullName)
                    ? string.Empty
                    : fullName;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(
                    ex,
                    "Failed resolving Level1 approver for payment request {PaymentRequestId}",
                    paymentRequest.Id);

                return string.Empty;
            }
        }

        private async Task<string> BuildPaymentDescriptionAsync(
            string? existingDescription)
        {
            if (!string.IsNullOrWhiteSpace(existingDescription))
            {
                var trimmed = existingDescription.Trim();

                return trimmed.Length > 50
                    ? trimmed[..50]
                    : trimmed;
            }

            var serviceProvider =
                LazyServiceProvider.LazyGetRequiredService<IServiceProvider>();

            var tenantDesc =
                await AbpUserTenantAccessor.GetCurrentTenantNameAsync(serviceProvider)
                ?? string.Empty;

            var generated = string.IsNullOrWhiteSpace(tenantDesc)
                ? "Grant Payment"
                : $"{tenantDesc} – Grant Payment";

            if (generated.Length > 50)
            {
                generated = generated[..50];
            }

            return generated;
        }

        public async Task<InvoiceResponse?> CreateInvoiceByPaymentRequestAsync(
            string invoiceNumber)
        {
            InvoiceResponse invoiceResponse = new();

            try
            {
                var paymentRequestData =
                    await invoiceManager.GetPaymentRequestDataAsync(invoiceNumber);

                if (!string.IsNullOrWhiteSpace(
                        paymentRequestData.AccountDistributionCode))
                {
                    var invoice = await InitializeCASInvoice(
                        paymentRequestData.PaymentRequest,
                        paymentRequestData.AccountDistributionCode);

                    if (invoice != null)
                    {
                        invoiceResponse = await CreateInvoiceAsync(invoice);

                        if (invoiceResponse != null)
                        {
                            await invoiceManager.UpdatePaymentRequestWithInvoiceAsync(
                                paymentRequestData.PaymentRequest.Id,
                                invoiceResponse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "CreateInvoiceByPaymentRequestAsync Exception: {ExceptionMessage}", ExceptionMessage);
            }

            return invoiceResponse;
        }

        public async Task<InvoiceResponse> CreateInvoiceAsync(Invoice casAPInvoice)
        {
            string jsonString = JsonSerializer.Serialize(casAPInvoice);
            var authToken = await iTokenService.GetAuthTokenAsync(CurrentTenant.Id ?? Guid.Empty);
            string casBaseUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE);
            var resource = $"{casBaseUrl}/{CFS_APINVOICE}/";
            var response = await resilientHttpRequest.HttpAsync(HttpMethod.Post, resource, jsonString, authToken);

            if (response == null)
            {
                throw new UserFriendlyException(
                    "CAS InvoiceService CreateInvoiceAsync: Null response");
            }

            if (response.Content != null &&
                response.StatusCode != HttpStatusCode.NotFound)
            {
                var contentString =
                    await ResilientHttpRequest.ContentToStringAsync(
                        response.Content);

                var result =
                    JsonSerializer.Deserialize<InvoiceResponse>(contentString)
                    ?? throw new UserFriendlyException(
                        $"CAS InvoiceService CreateInvoiceAsync Exception: {response}");

                result.CASHttpStatusCode = response.StatusCode;

                return result;
            }

            if (response.RequestMessage != null)
            {
                throw new UserFriendlyException(
                    $"CAS InvoiceService CreateInvoiceAsync Exception: {response.RequestMessage}");
            }

            throw new UserFriendlyException(
                $"CAS InvoiceService CreateInvoiceAsync Exception: {response}");
        }

        public async Task<CasPaymentSearchResult> GetCasInvoiceAsync(
            string invoiceNumber,
            string supplierNumber,
            string supplierSiteCode)
        {
            var authToken =
                await iTokenService.GetAuthTokenAsync(
                    CurrentTenant.Id ?? Guid.Empty);

            var casBaseUrl =
                await endpointManagementAppService.GetUgmUrlByKeyNameAsync(
                    DynamicUrlKeyNames.PAYMENT_API_BASE);

            var resource =
                $"{casBaseUrl}/{CFS_APINVOICE}/{invoiceNumber}/{supplierNumber}/{supplierSiteCode}";

            var response = await resilientHttpRequest.HttpAsync(
                HttpMethod.Get,
                resource,
                body: null,
                authToken);

            if (response != null &&
                response.Content != null &&
                response.IsSuccessStatusCode)
            {
                string contentString =
                    await ResilientHttpRequest.ContentToStringAsync(
                        response.Content);

                var result =
                    JsonSerializer.Deserialize<CasPaymentSearchResult>(
                        contentString);

                return result ?? new CasPaymentSearchResult();
            }

            return new CasPaymentSearchResult();
        }

        public async Task<CasPaymentSearchResult> GetCasPaymentAsync(
            Guid tenantId,
            string invoiceNumber,
            string supplierNumber,
            string siteNumber)
        {
            Logger.LogInformation("GetCasPaymentAsync for Invoice: {InvoiceNumber}, SupplierNumber: {SupplierNumber}, SiteNumber: {SiteNumber}, TenantId: {TenantId}", invoiceNumber, supplierNumber, siteNumber, tenantId);
            var authToken = await iTokenService.GetAuthTokenAsync(tenantId);
            var casBaseUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE);
            var resource = $"{casBaseUrl}/{CFS_APINVOICE}/{invoiceNumber}/{supplierNumber}/{siteNumber}";
            var response = await resilientHttpRequest.HttpAsync(HttpMethod.Get, resource, body: null, authToken);
            CasPaymentSearchResult casPaymentSearchResult = new();

            if (response != null &&
                response.Content != null &&
                response.IsSuccessStatusCode)
            {
                var content =
                    await response.Content.ReadAsStringAsync();

                var result =
                    JsonSerializer.Deserialize<CasPaymentSearchResult>(content);

                return result ?? casPaymentSearchResult;
            }

            if (response != null)
            {
                casPaymentSearchResult.InvoiceStatus = response.StatusCode.ToString();
            }

            return casPaymentSearchResult;
        }
    }

#pragma warning disable S125

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

    Sample JSON File – Regular Standard Invoice - Web Service

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

#pragma warning restore S125
}