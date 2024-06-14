using System;
using System.Threading.Tasks;
using Unity.Payments.Suppliers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Payments.Handlers
{
    public class UpsertSupplierHandler : ILocalEventHandler<UpsertSupplierEto>, ITransientDependency
    {
        private readonly ISupplierAppService _supplierAppService;
        private readonly ISiteAppService _siteAppService;

        public UpsertSupplierHandler(
            ISupplierAppService supplierAppService,
            ISiteAppService siteAppService
            )
        {
            _supplierAppService = supplierAppService;
            _siteAppService = siteAppService;
        }

        public async Task HandleEventAsync(UpsertSupplierEto eventData)
        {
            var existing = await _supplierAppService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
            {
                CorrelationId = eventData.CorrelationId,
                CorrelationProvider = eventData.CorrelationProvider,
            });

            SupplierDto supplierDto;

            // This is subject to some business rules and a domain implmentation
            if (existing != null)
            {
                existing.Number = eventData.Number;

                supplierDto = await _supplierAppService.UpdateAsync(existing.Id, new UpdateSupplierDto()
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

                // Delete the current sites
                await _siteAppService.DeleteBySupplierIdAsync(supplierDto.Id);
            }
            else
            {
                supplierDto = await _supplierAppService.CreateAsync(new CreateSupplierDto()
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
            }

            await InsertSitesFromEventDtoAsync(supplierDto.Id, eventData);

        }

        protected virtual async Task InsertSitesFromEventDtoAsync(Guid supplierId, UpsertSupplierEto upsertSupplierEto)
        {
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
                    ProviderId = siteEto.ProviderId,
                    Status = siteEto.Status,
                    SiteProtected = siteEto.SiteProtected,
                    LastUpdatedInCas = siteEto.LastUpdated
                };

                await _siteAppService.InsertAsync(siteDto);
            }            
        }
    }
}
