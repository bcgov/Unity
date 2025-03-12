using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class SiteAppService : PaymentsAppService, ISiteAppService
    {
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
                logger.LogError(ex, "Error fetching site with ID {SiteId}", id);
                throw new BusinessException("Error fetching site").WithData("SiteId", id);
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
                logger.LogError(ex, "Error marking site with ID {SiteId} as deleted in use", id);
                throw new BusinessException("Error marking site as deleted in use").WithData("SiteId", id);
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
                    Site updateSite = site.UpdateSiteBySiteDto(site, siteDto);
                    await siteRepository.UpdateAsync(updateSite, true);
                }

                return site.Id;
            }
            catch (BusinessException ex)
            {
                // Log BusinessExceptions for specific error handling
                logger.LogWarning(ex, "Business exception occurred during site update");
                throw;
            }
            catch (Exception ex)
            {
                // General exception logging
                logger.LogError(ex, "Error updating site with ID {SiteId}", siteDto.Id);
                throw new BusinessException("Error updating site").WithData("SiteId", siteDto.Id);
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
                throw new BusinessException("Error fetching sites by supplier").WithData("SupplierId", supplierId);
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
                throw new BusinessException("Error deleting sites by supplier").WithData("SupplierId", supplierId);
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
                logger.LogError(ex, "Error deleting site with ID {SiteId}", id);
                throw new BusinessException("Error deleting site").WithData("SiteId", id);
            }
        }
    }
}
