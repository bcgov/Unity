using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Intakes.Events;
using Unity.GrantManager.Integration.Geocoder;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.GrantManager.Intakes.Handlers
{
    public class DetermineElectoralDistrictHandler(IGeocoderApiService geocoderApiService,
        ILogger<DetermineElectoralDistrictHandler> logger)
        : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
    {
        /// <summary>
        /// Determines the Electoral Distrct based on the Address.
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(ApplicationProcessEvent eventData)
        {
            try
            {
                if (eventData.Application == null)
                {
                    logger.LogWarning("Application data is null in DetermineElectoralDistrictHandler.");
                    return;
                }

                if (eventData.FormVersion == null)
                {
                    logger.LogWarning("Application data is null in DetermineElectoralDistrictHandler.");
                    return;
                }

                // Check if the electoral district is already mapped for the form submission, if so then no work to be done
                if (eventData.FormVersion.HasSubmissionHeaderMapping("ApplicantElectoralDistrict"))
                {
                    logger.LogInformation("Electoral district already determined for application {ApplicationId}. No further action required.",
                        eventData.Application.Id);
                    return;
                }

                var electoralDistrictAddressType = eventData.Application.ApplicationForm.ElectoralDistrictAddressType;
                
                electoralDistrictAddressType ??= GrantApplications.AddressType.PhysicalAddress; // default to PhysicalAddress if not set
                logger.LogInformation("Using electoral district address type: {AddressType} for electoral determination", electoralDistrictAddressType);

                var applicantAddresses = eventData.Application.Applicant.ApplicantAddresses;

                if (applicantAddresses == null || applicantAddresses.Count == 0)
                {
                    logger.LogWarning("Application data is null in DetermineElectoralDistrictHandler.");
                    return;
                }

                // Find the related address type
                var matchedAddressType = applicantAddresses
                    .FirstOrDefault(a => a.AddressType == electoralDistrictAddressType);

                if (matchedAddressType == null)
                {
                    logger.LogWarning("No address of type {AddressType} found for application {ApplicationId}.",
                        electoralDistrictAddressType, eventData.Application.Id);
                    return;
                }

                // Extract from geo services
                var address = matchedAddressType.GetFullAddress();
                var geoAddressDetails = await geocoderApiService.GetAddressDetailsAsync(address);

                if (geoAddressDetails == null || geoAddressDetails.Coordinates == null)
                {
                    logger.LogWarning("No coordinates found for address: {Address}", address);
                    return;
                }

                var electoralDistrict = await geocoderApiService.GetElectoralDistrictAsync(geoAddressDetails.Coordinates);

                if (electoralDistrict.Name != null)
                {
                    eventData.Application.Applicant.SetElectoralDistrict(electoralDistrict.Name);
                }
                else
                {
                    logger.LogWarning("Electoral district could not be determined for address: {Address}", address);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while determining the electoral district.");
                // Swallow the exception to ensure best effort and prevent propagation
            }
        }
    }
}
