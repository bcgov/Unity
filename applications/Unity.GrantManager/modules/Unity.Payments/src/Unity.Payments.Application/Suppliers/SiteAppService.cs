using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Domain.Suppliers;
using Volo.Abp.Features;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Unity.Payments.Suppliers
{
    [RequiresFeature("Unity.Payments")]
    public class SiteAppService : PaymentsAppService, ISiteAppService
    {
        private const string SITE_ID_KEY = "SiteId";
        private const string SUPLIER_ID_KEY = "SupplierId";
        private readonly ISiteRepository siteRepository;
        private readonly ILogger<SiteAppService> logger;

        public SiteAppService(ISiteRepository siteRepository, ILogger<SiteAppService> logger)
        {
            this.siteRepository = siteRepository;
            this.logger = logger;
        }

        public virtual async Task<SiteDto> GetAsync(Guid id)
        {
            try
            {
                var site = await siteRepository.GetAsync(id);
                return ObjectMapper.Map<Site, SiteDto>(site);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching site");
                throw new BusinessException("Error fetching site").WithData(SITE_ID_KEY, id);
            }
        }

        public virtual async Task<Guid> InsertAsync(SiteDto siteDto)
        {
            try
            {
                Site site = new Site(siteDto);
                await siteRepository.InsertAsync(site, true);
                return site.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inserting site");
                throw new BusinessException("Error inserting site");
            }
        }

        public virtual async Task<Guid> MarkDeletedInUseAsync(Guid id)
        {
            try
            {
                Site site = await siteRepository.GetAsync(id);
                site.MarkDeletedInUse = true;
                await siteRepository.UpdateAsync(site, true);
                return site.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error marking site as deleted in use");
                throw new BusinessException("Error marking site as deleted in use").WithData(SITE_ID_KEY, id);
            }
        }

        public virtual async Task<Guid> UpdatePaygroupAsync(Enums.PaymentGroup paymentGroup, Guid siteId)
        {
            try{
                Site updateSite = await siteRepository.GetAsync(siteId);
                updateSite.PaymentGroup = paymentGroup;
                await siteRepository.UpdateAsync(updateSite, true);            
                return updateSite.Id;
            }
            catch (BusinessException ex)
            {
                // Log BusinessExceptions with context and rethrow with additional data
                logger.LogWarning(ex, "Business exception occurred while updating site {SiteId}", siteId);
                throw new BusinessException("Error updating site: " + ex.Message)
                    .WithData(SITE_ID_KEY, siteId)
                    .WithData("OriginalError", ex.Message);
            }
            catch (Exception ex)
            {
                // General exception logging
                logger.LogError(ex, "Error updating site {SiteId}", siteId);
                throw new BusinessException("Error updating site").WithData(SITE_ID_KEY, siteId);
            }
        }
  
        public virtual async Task<Guid> UpdateAsync(SiteDto siteDto)
        {
            try
            {
                Site site = await siteRepository.GetAsync(siteDto.Id);

                // Only update the site if the values have changed
                if (!Site.SiteMatchesSiteDto(site, siteDto))
                {
                    siteDto.MarkDeletedInUse = false; // Reset the MarkDeletedInUse flag
                    Site updateSite = Site.UpdateSiteBySiteDto(site, siteDto);
                    await siteRepository.UpdateAsync(updateSite, true);
                }

                return site.Id;
            }
            catch (BusinessException ex)
            {
                // Log BusinessExceptions with context and rethrow with additional data
                logger.LogWarning(ex, "Business exception occurred while updating site {SiteId}", siteDto.Id);
                throw new BusinessException("Error updating site: " + ex.Message)
                    .WithData(SITE_ID_KEY, siteDto.Id)
                    .WithData("OriginalError", ex.Message);
            }
            catch (Exception ex)
            {
                // General exception logging
                logger.LogError(ex, "Error updating site {SiteId}", siteDto.Id);
                throw new BusinessException("Error updating site").WithData(SITE_ID_KEY, siteDto.Id);
            }
        }

        public virtual async Task<List<Site>> GetSitesBySupplierIdAsync(Guid supplierId)
        {
            try
            {
                return await siteRepository.GetBySupplierAsync(supplierId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching sites for supplier with ID {SupplierId}", supplierId);
                throw new BusinessException("Error fetching sites by supplier").WithData(SUPLIER_ID_KEY, supplierId);
            }
        }

        public virtual async Task DeleteBySupplierIdAsync(Guid supplierId)
        {
            try
            {
                var sites = await siteRepository.GetBySupplierAsync(supplierId);
                if (sites == null || sites.Count == 0)
                {
                    throw new BusinessException("No sites found for the given supplier.");
                }

                await siteRepository.DeleteManyAsync(sites, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting sites for supplier with ID {SupplierId}", supplierId);
                throw new BusinessException("Error deleting sites by supplier").WithData(SUPLIER_ID_KEY, supplierId);
            }
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            try
            {
                await siteRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting site {SiteId}", id);
                throw new BusinessException("Error deleting site").WithData(SITE_ID_KEY, id);            }
        }
    }
}
