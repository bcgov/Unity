using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.Suppliers.ValueObjects;
using Unity.Payments.Enums;
using Unity.Payments.Integrations.Cas;
using Volo.Abp.Features;
using Unity.Modules.Shared.Correlation;

namespace Unity.Payments.Suppliers
{
    [RequiresFeature("Unity.Payments")]
    public class SupplierAppService(ISupplierRepository supplierRepository,
                                    ISupplierService supplierService,
                                    ISiteAppService siteAppService,
                                    IApplicationRepository applicationRepository) : PaymentsAppService, ISupplierAppService
    {                
        public virtual async Task<SupplierDto> CreateAsync(CreateSupplierDto createSupplierDto)
        {
            var basicInfo = new SupplierBasicInfo(
                createSupplierDto.Name, 
                createSupplierDto.Number, 
                createSupplierDto.Subcategory);
            
            var providerInfo = new ProviderInfo(
                createSupplierDto.ProviderId, 
                createSupplierDto.BusinessNumber);
            
            var supplierStatus = new SupplierStatus(
                createSupplierDto.Status, 
                createSupplierDto.SupplierProtected, 
                createSupplierDto.StandardIndustryClassification);
            
            var casMetadata = new CasMetadata(createSupplierDto.LastUpdatedInCAS);
            
            var correlation = new Correlation(createSupplierDto.CorrelationId, createSupplierDto.CorrelationProvider);
            
            var mailingAddress = new MailingAddress(
                createSupplierDto.MailingAddress,
                createSupplierDto.City,
                createSupplierDto.Province,
                createSupplierDto.PostalCode);

            Supplier supplier = new(Guid.NewGuid(),
                basicInfo,
                correlation,
                providerInfo,
                supplierStatus,
                casMetadata,
                mailingAddress);

            var result = await supplierRepository.InsertAsync(supplier);
            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }

        public virtual async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto updateSupplierDto)
        {
            var supplier = await supplierRepository.GetAsync(id);
            
            // Use the new value object methods for better encapsulation
            supplier.UpdateBasicInfo(new SupplierBasicInfo(
                updateSupplierDto.Name, 
                updateSupplierDto.Number, 
                updateSupplierDto.Subcategory));
            
            supplier.UpdateProviderInfo(new ProviderInfo(
                updateSupplierDto.ProviderId, 
                updateSupplierDto.BusinessNumber));
            
            supplier.UpdateStatus(new SupplierStatus(
                updateSupplierDto.Status, 
                updateSupplierDto.SupplierProtected, 
                updateSupplierDto.StandardIndustryClassification));
            
            supplier.UpdateCasMetadata(new CasMetadata(updateSupplierDto.LastUpdatedInCAS));
            
            supplier.CorrelationId = updateSupplierDto.CorrelationId;
            supplier.CorrelationProvider = updateSupplierDto.CorrelationProvider;

            supplier.SetAddress(updateSupplierDto.MailingAddress,
                updateSupplierDto.City,
                updateSupplierDto.Province,
                updateSupplierDto.PostalCode);

            Supplier result = await supplierRepository.UpdateAsync(supplier);
            return ObjectMapper.Map<Supplier, SupplierDto>(result);
        }

        public virtual async Task<SupplierDto?> GetAsync(Guid id)
        {
            try
            {
                var result = await supplierRepository.GetAsync(id);
                if (result == null) return null;
                return ObjectMapper.Map<Supplier, SupplierDto>(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching supplier");
                return null;
            }
        }

        public virtual async Task<SupplierDto?> GetByCorrelationAsync(GetSupplierByCorrelationDto requestDto)
        {
            var result = await supplierRepository.GetByCorrelationAsync(requestDto.CorrelationId, requestDto.CorrelationProvider, requestDto.IncludeDetails);

            if (result == null) return null;

            return ObjectMapper.Map<Supplier, SupplierDto?>(result);
        }

        public virtual async Task<SupplierDto?> GetBySupplierNumberAsync(string? supplierNumber)
        {
            if (supplierNumber == null) return null;

            bool includeDetails = true;
            var result = await supplierRepository.GetBySupplierNumberAsync(supplierNumber, includeDetails);

            if (result == null) return null;

            return ObjectMapper.Map<Supplier, SupplierDto?>(result);
        }

        public async Task<dynamic> GetSitesBySupplierNumberAsync(string? supplierNumber, Guid applicantId, Guid? applicationId = null)
        {
            // Change this code to get the supplier list of sites - from the datatabase which it is currently doing
            // Then go to CAS and get the list of sites again
            // Compare and update the database if there are any new sites
            var defaultPaymentGroup = await ResolveDefaultPaymentGroupForApplicantAsync(applicantId, applicationId);
            dynamic casSupplierResponse = await supplierService.GetCasSupplierInformationAsync(supplierNumber);
            var casSiteDtos = new List<SiteDto>();
            if (casSupplierResponse.TryGetProperty("supplieraddress", out JsonElement sitesJson) &&
                            sitesJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var site in sitesJson.EnumerateArray())
                {
                    SiteEto siteEto = SupplierService.GetSiteEto(site);
                    SiteDto siteDto = GetSiteDtoFromSiteEto(siteEto, Guid.Empty, defaultPaymentGroup);
                    casSiteDtos.Add(siteDto);
                }
            }

            var supplier = await GetBySupplierNumberAsync(supplierNumber);
            if (supplier == null) return new List<SiteDto>();
            List<Site> sites = await siteAppService.GetSitesBySupplierIdAsync(supplier.Id);
            List<SiteDto> existingSiteDtos = sites.Select(ObjectMapper.Map<Site, SiteDto>).ToList();

            bool hasChanges = false;
            // If the list of CAS sites is different from the existing sites
            if (existingSiteDtos.Count != casSiteDtos.Count)
            {
                // Update the supplier and sites
               hasChanges = true;
            } 
            else if (existingSiteDtos.Count == casSiteDtos.Count)
            {
                // Go through each site and compare
                hasChanges = false;

                // based on the matching number, check if any other fields are different
                foreach (var casSite in casSiteDtos)   
                {
                    var existingSite = existingSiteDtos.FirstOrDefault(s => s.Number == casSite.Number);
                    if (existingSite != null)
                    {
                        // Compare fields and update if necessary
                        if (existingSite.PaymentGroup != casSite.PaymentGroup ||
                            existingSite.Country != casSite.Country ||
                            existingSite.EFTAdvicePref != casSite.EFTAdvicePref ||
                            existingSite.EmailAddress != casSite.EmailAddress ||
                            existingSite.PostalCode != casSite.PostalCode ||
                            existingSite.ProviderId != casSite.ProviderId ||
                            existingSite.Province != casSite.Province ||
                            existingSite.SiteProtected != casSite.SiteProtected ||
                            existingSite.City != casSite.City ||
                            existingSite.AddressLine1 != casSite.AddressLine1 ||
                            existingSite.AddressLine2 != casSite.AddressLine2 ||
                            existingSite.BankAccount != casSite.BankAccount ||
                            existingSite.Status != casSite.Status)
                        {
                            hasChanges = true;
                            break;
                        }
                    }
                    else
                    {
                        hasChanges = true;
                        break;
                    }
                }
            }

            if (hasChanges)
            {
                await supplierService.UpdateSupplierInfo(casSupplierResponse, applicantId, applicationId);

                // Re-fetch sites from database to get proper IDs
                var updatedSupplier = await GetBySupplierNumberAsync(supplierNumber);
                if (updatedSupplier != null)
                {
                    List<Site> updatedSites = await siteAppService.GetSitesBySupplierIdAsync(updatedSupplier.Id);
                    existingSiteDtos = updatedSites.Select(ObjectMapper.Map<Site, SiteDto>).ToList();
                }
            }


            return new { sites = existingSiteDtos, hasChanges };
        }

        public virtual async Task<SiteDto> CreateSiteAsync(Guid id, CreateSiteDto createSiteDto)
        {
            var supplier = await supplierRepository.GetAsync(id, true);

            var newId = Guid.NewGuid();
            var updateSupplier = supplier.AddSite(new Site(
                newId,
                createSiteDto.Number,
                createSiteDto.PaymentGroup,
                new Address(
                createSiteDto.AddressLine1,
                createSiteDto.AddressLine2,
                createSiteDto.AddressLine3,
                string.Empty,
                createSiteDto.City,
                createSiteDto.Province,
                createSiteDto.PostalCode)));

            return ObjectMapper.Map<Site, SiteDto>(updateSupplier.Sites.First(s => s.Id == newId));
        }

        public virtual async Task<SiteDto> UpdateSiteAsync(Guid id, Guid siteId, UpdateSiteDto updateSiteDto)
        {
            var supplier = await supplierRepository.GetAsync(id, true);

            var updateSupplier = supplier.UpdateSite(siteId,
                updateSiteDto.Number,
                updateSiteDto.PaymentGroup,
                updateSiteDto.AddressLine1,
                updateSiteDto.AddressLine2,
                updateSiteDto.AddressLine3,
                updateSiteDto.City,
                updateSiteDto.Province,
                updateSiteDto.PostalCode);

            return ObjectMapper.Map<Site, SiteDto>(updateSupplier.Sites.First(s => s.Id == siteId));
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await supplierRepository.DeleteAsync(id);
        }

        public SiteDto GetSiteDtoFromSiteEto(SiteEto siteEto, Guid supplierId, PaymentGroup? defaultPaymentGroup = null)
        {
            var resolvedPaymentGroup = defaultPaymentGroup ?? PaymentGroup.EFT;
            return new()
                {
                    Number = siteEto.SupplierSiteCode,
                    PaymentGroup = resolvedPaymentGroup,
                    AddressLine1 = siteEto.AddressLine1,
                    AddressLine2 = siteEto.AddressLine2,
                    AddressLine3 = siteEto.AddressLine3,
                    City = siteEto.City,
                    Province = siteEto.Province,
                    PostalCode = siteEto.PostalCode,
                    SupplierId = supplierId,
                    Country = siteEto.Country,
                    EmailAddress = siteEto.EmailAddress,
                    EFTAdvicePref = siteEto.EFTAdvicePref,
                    BankAccount = siteEto.BankAccount,
                    ProviderId = siteEto.ProviderId,
                    Status = siteEto.Status,
                    SiteProtected = siteEto.SiteProtected,
                    LastUpdatedInCas = siteEto.LastUpdated
                };
        }

        private async Task<PaymentGroup> ResolveDefaultPaymentGroupForApplicantAsync(Guid applicantId, Guid? applicationId = null)
        {
            const PaymentGroup fallbackPaymentGroup = PaymentGroup.EFT;
            try
            {
                var applicationsQueryable = await applicationRepository.GetQueryableAsync();
                var query = applicationsQueryable.Include(a => a.ApplicationForm).Where(a => a.ApplicantId == applicantId);

                if (applicationId.HasValue && applicationId.Value != Guid.Empty)
                {
                    query = query.Where(a => a.Id == applicationId.Value);
                }
                else
                {
                    return fallbackPaymentGroup;
                }

                var application = await query
                        .OrderByDescending(a => a.CreationTime)
                        .FirstOrDefaultAsync();

                var formPaymentGroup = application?.ApplicationForm?.DefaultPaymentGroup;
                if (formPaymentGroup.HasValue && Enum.IsDefined(typeof(PaymentGroup), formPaymentGroup.Value))
                {
                    return (PaymentGroup)formPaymentGroup.Value;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Unable to resolve default payment group for applicant {ApplicantId}", applicantId);
            }

            return fallbackPaymentGroup;
        }
    }
}
