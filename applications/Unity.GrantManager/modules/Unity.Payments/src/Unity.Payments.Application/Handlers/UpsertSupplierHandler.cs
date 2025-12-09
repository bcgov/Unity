using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;
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
                                       ILocalEventBus localEventBus,
                                       IApplicationRepository applicationRepository) : ILocalEventHandler<UpsertSupplierEto>, ITransientDependency
    {        

        public async Task HandleEventAsync(UpsertSupplierEto eventData)
        {
            SupplierDto supplierDto = await GetSupplierFromEvent(eventData);
            var existingSites = await siteAppService.GetSitesBySupplierIdAsync(supplierDto.Id);
            var existingSitesDictionary = existingSites?.ToDictionary(s => s.Number) ?? new Dictionary<string, Site>();

            var defaultPaymentGroup = await ResolveDefaultPaymentGroupAsync(eventData);
            await UpsertSitesFromEventDtoAsync(existingSitesDictionary, supplierDto.Id, eventData, defaultPaymentGroup);

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
                    UpsertSupplierEto upsertSupplierEto,
                    PaymentGroup defaultPaymentGroup)
        {
            foreach (var siteEto in upsertSupplierEto.SiteEtos)
            {
                var siteDto = supplierAppService.GetSiteDtoFromSiteEto(siteEto, supplierId, defaultPaymentGroup);

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

        private async Task<PaymentGroup> ResolveDefaultPaymentGroupAsync(UpsertSupplierEto eventData)
        {
            const PaymentGroup fallbackPaymentGroup = PaymentGroup.EFT;

            try
            {
                var applicationsQueryable = await applicationRepository.GetQueryableAsync();
                IQueryable<Application> query = applicationsQueryable.Include(a => a.ApplicationForm);

                if (eventData.ApplicationId.HasValue && eventData.ApplicationId.Value != Guid.Empty)
                {
                    query = query.Where(a => a.Id == eventData.ApplicationId.Value);
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
                logger.LogWarning(ex, "Unable to resolve default payment group for correlation {CorrelationId}", eventData.CorrelationId);
            }

            return fallbackPaymentGroup;
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
