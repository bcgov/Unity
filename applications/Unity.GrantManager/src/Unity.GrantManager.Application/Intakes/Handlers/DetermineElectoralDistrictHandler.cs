using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes.Events;
using Unity.GrantManager.Integrations.Geocoder;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.GrantManager.Intakes.Handlers
{
    public class DetermineElectoralDistrictHandler(IGeocoderApiService geocoderApiService,
        ILogger<DetermineElectoralDistrictHandler> logger)
        : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
    {
        /// <summary>
        /// Determines the Electoral District based on the Address.
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(ApplicationProcessEvent eventData)
        {
            await DetermineElectoralDistrictAsync(eventData.Application, eventData.FormVersion);
        }

        /// <summary>
        /// Core method to determine electoral district that can be called by different handlers.
        /// </summary>
        /// <param name="application">The application to process</param>
        /// <param name="formVersion">The form version associated with the application</param>
        /// <returns></returns>
        public async Task DetermineElectoralDistrictAsync(Application? application, ApplicationFormVersion? formVersion)
        {
            try
            {
                if (application == null)
                {
                    logger.LogWarning("Application data is null in DetermineElectoralDistrictHandler.");
                    return;
                }

                if (!string.IsNullOrEmpty(application.ApplicantElectoralDistrict))
                {
                    logger.LogInformation("Electoral district already set to '{ExistingElectoralDistrict}' for application {ApplicationId}.",
                        application.ApplicantElectoralDistrict, application.Id);
                    return;
                }

                if (formVersion == null)
                {
                    logger.LogWarning("Form version data is null in DetermineElectoralDistrictHandler.");
                    return;
                }

                // Check if the electoral district is already mapped for the form submission, if so then no work to be done
                if (formVersion.HasSubmissionHeaderMapping("ApplicantElectoralDistrict"))
                {
                    logger.LogInformation("Electoral district already determined for application {ApplicationId}. No further action required.",
                        application.Id);
                    return;
                }

                // Use local variable to avoid modifying the entity property
                var addressType = application.ApplicationForm.ElectoralDistrictAddressType ?? GrantApplications.AddressType.PhysicalAddress;
                logger.LogInformation("Using electoral district address type: {AddressType} for electoral determination", addressType);

                var applicantAddresses = application.Applicant.ApplicantAddresses;

                if (applicantAddresses == null || applicantAddresses.Count == 0)
                {
                    logger.LogWarning("Applicant addresses are null or empty in DetermineElectoralDistrictHandler for application {ApplicationId}.",
                        application.Id);
                    return;
                }

                // Find the related address type
                var matchedAddressType = applicantAddresses
                    .FirstOrDefault(a => a.AddressType == addressType);

                if (matchedAddressType == null)
                {
                    logger.LogWarning("No address of type {AddressType} found for application {ApplicationId}.",
                        addressType, application.Id);
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
                    application.ApplicantElectoralDistrict = electoralDistrict.Name;
                    logger.LogInformation("Electoral district '{ElectoralDistrict}' determined for address: {Address}",
                        electoralDistrict.Name, address);
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
