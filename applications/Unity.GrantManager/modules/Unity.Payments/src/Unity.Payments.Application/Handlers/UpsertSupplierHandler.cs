using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Events;
using Unity.Payments.Suppliers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace Unity.Payments.Handlers
{
    public class UpsertSupplierHandler(ISupplierAppService supplierAppService,
                                       SiteAppService siteAppService,
                                       ILogger<UpsertSupplierHandler> logger,
                                       ILocalEventBus localEventBus) : ILocalEventHandler<UpsertSupplierEto>, ITransientDependency
    {

        public async Task HandleEventAsync(UpsertSupplierEto eventData)
        {
            SupplierDto supplierDto = await GetSupplierFromEvent(eventData);
            var existingSites = await siteAppService.GetSitesBySupplierIdAsync(supplierDto.Id);
            var existingSitesDictionary = existingSites?.ToDictionary(s => s.Number) ?? new Dictionary<string, Site>();

            await UpsertSitesFromEventDtoAsync(existingSitesDictionary, supplierDto.Id, eventData);

            // Send event notification to application module
            await localEventBus.PublishAsync(
                new ApplicantSupplierEto
                {
                    SupplierId = supplierDto.Id,
                    ApplicantId = eventData.CorrelationId,
                    ExistingSitesDictionary = existingSitesDictionary,
                    SiteEtos = eventData.SiteEtos
                }
            );
        }

        private async Task<Dictionary<string, Site>> UpsertSitesFromEventDtoAsync(
                    Dictionary<string, Site> existingSitesDictionary,
                    Guid supplierId, 
                    UpsertSupplierEto upsertSupplierEto)
        {
            foreach (var siteEto in upsertSupplierEto.SiteEtos)
            {
                var siteDto = GetSiteDtoFromSiteEto(siteEto, supplierId);

                if (existingSitesDictionary.TryGetValue(siteDto.Number, out var existingSite))
                {
                    siteDto.Id = existingSite.Id;
                    await siteAppService.UpdateAsync(siteDto);
                }
                else
                {
                    await siteAppService.InsertAsync(siteDto);
                }
            }

            return existingSitesDictionary;
        }

        private async Task<SupplierDto> GetSupplierFromEvent(UpsertSupplierEto eventData)
        {
            var existing = await supplierAppService.GetBySupplierNumberAsync(eventData.Number);
            logger.LogInformation("Upserting supplier from event data: {Existing}", existing);

            // This is subject to some business rules and a domain implementation
            if (existing != null)
            {
                existing.Number = eventData.Number;
                UpdateSupplierDto updateSupplierDto = GetUpdateSupplierDtoFromEvent(eventData);
                SupplierDto updatedSupplierDto = await supplierAppService.UpdateAsync(existing.Id, updateSupplierDto);
                return updatedSupplierDto;
            }

            CreateSupplierDto createSupplierDto = GetCreateSupplierDtoFromEvent(eventData);
            SupplierDto supplierDto = await supplierAppService.CreateAsync(createSupplierDto);

            return supplierDto;
        }

        private static SiteDto GetSiteDtoFromSiteEto(SiteEto siteEto, Guid supplierId)
        {
            return new()
                {
                    Number = siteEto.SupplierSiteCode,
                    PaymentGroup = Enums.PaymentGroup.EFT, // Defaulting to EFT based on conversations with CGG/CAS
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

        private static UpdateSupplierDto GetUpdateSupplierDtoFromEvent(UpsertSupplierEto eventData)
        {
            return new UpdateSupplierDto()
            {
                Name = eventData.Name,
                Number = eventData.Number,
                Subcategory = eventData.Subcategory,
                ProviderId = eventData.ProviderId,
                BusinessNumber = eventData.BusinessNumber,
                Status = eventData.Status,
                SupplierProtected = eventData.SupplierProtected,
                StandardIndustryClassification = eventData.StandardIndustryClassification,
                LastUpdatedInCAS = eventData.LastUpdatedInCAS,
                CorrelationId = eventData.CorrelationId,
                CorrelationProvider = eventData.CorrelationProvider,
            };
        }

        private static CreateSupplierDto GetCreateSupplierDtoFromEvent(UpsertSupplierEto eventData)
        {
            return new CreateSupplierDto()
            {
                Name = eventData.Name,
                Number = eventData.Number,
                Subcategory = eventData.Subcategory,
                ProviderId = eventData.ProviderId,
                BusinessNumber = eventData.BusinessNumber,
                Status = eventData.Status,
                SupplierProtected = eventData.SupplierProtected,
                StandardIndustryClassification = eventData.StandardIndustryClassification,
                LastUpdatedInCAS = eventData.LastUpdatedInCAS,
                CorrelationId = eventData.CorrelationId,
                CorrelationProvider = eventData.CorrelationProvider,
            };
        }
    }
}
