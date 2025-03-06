using System;
using System.Collections.Generic;
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
            await InsertSitesFromEventDtoAsync(supplierDto.Id, eventData);

            // Send event notification to application module
            await localEventBus.PublishAsync(
                new ApplicantSupplierEto
                {
                    SupplierId = supplierDto.Id,
                    ApplicantId = eventData.CorrelationId
                }
            );
        }

        private async Task<SupplierDto> GetSupplierFromEvent(UpsertSupplierEto eventData) {
            var existing = await supplierAppService.GetBySupplierNumberAsync(eventData.Number);
            logger.LogInformation("Upserting supplier from event data: {Existing}", existing);

            // This is subject to some business rules and a domain implementation
            if (existing != null)
            {
                existing.Number = eventData.Number;
                SupplierDto updatedSupplierDto = await supplierAppService.UpdateAsync(existing.Id, new UpdateSupplierDto()
                {
                    Name = eventData.Name,
                    Number = existing.Number,
                    Subcategory = eventData.Subcategory,
                    ProviderId = eventData.ProviderId,
                    BusinessNumber = eventData.BusinessNumber,
                    Status = eventData.Status,
                    SupplierProtected = eventData.SupplierProtected,
                    StandardIndustryClassification = eventData.StandardIndustryClassification,
                    LastUpdatedInCAS = eventData.LastUpdatedInCAS,
                    CorrelationId = eventData.CorrelationId,
                    CorrelationProvider = eventData.CorrelationProvider,
                    MailingAddress = existing.MailingAddress,
                    PostalCode = existing.PostalCode,
                    Province = existing.Province,
                    City = existing.City,
                });

                return updatedSupplierDto;
            }

            SupplierDto supplierDto = await supplierAppService.CreateAsync(new CreateSupplierDto()
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
                    });
            
            return supplierDto;
        }

        protected virtual async Task InsertSitesFromEventDtoAsync(Guid supplierId, UpsertSupplierEto upsertSupplierEto)
        {
            // If sites are being inserted from a lookup then potentially the CAS data has brought back different data
            // Did any sites already exist for the current supplier?
            List<Site>? existingSites = await siteAppService.GetSitesBySupplierIdAsync(supplierId);
            if(existingSites != null && existingSites.Count > 0)
            {
                // Where any defaulted in the applicants?
                // If so then we need - re-associate ? or delete the existing site from the applicant?

                // Delete the current sites
                await siteAppService.DeleteBySupplierIdAsync(supplierId);
            }

            foreach(SiteEto siteEto in upsertSupplierEto.SiteEtos)
            {                
                SiteDto siteDto = new()
                {
                    Number = siteEto.SupplierSiteCode,
                    PaymentGroup = siteEto.EFTAdvicePref == "E" ? Enums.PaymentGroup.EFT : Enums.PaymentGroup.Cheque,
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

                await siteAppService.InsertAsync(siteDto);
            }
        }
    }
}
