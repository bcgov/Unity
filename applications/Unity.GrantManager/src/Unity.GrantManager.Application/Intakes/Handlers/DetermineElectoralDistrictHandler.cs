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

        /// This method mutates the provided <paramref name="application"/> instance by setting
        /// its <c>ApplicantElectoralDistrict</c> property when appropriate but does not persist
        /// changes or manage transaction boundaries.
        /// </summary>
        /// <param name="application">The application to process. Must be tracked within an active unit of work so that changes are persisted by the caller.</param>
        /// <param name="formVersion">The form version associated with the application</param>
        /// <remarks>
        /// This method assumes it is executed within a valid unit of work or transactional context.
        /// Callers are responsible for managing transaction boundaries and saving any changes made
        /// to the <paramref name="application"/> entity.
        /// </remarks>
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
                var address = matchedAddressType.GetSearchAddress();
                var geoAddressDetails = await geocoderApiService.GetAddressDetailsAsync(address);

                if (geoAddressDetails == null || geoAddressDetails.Coordinates == null)
                {
                    logger.LogWarning("No coordinates found for address: {Address} for application {ApplicationId}.",
                        address, application.Id);
                    return;
                }

                logger.LogInformation(
                    "Geocoder resolved address for application {ApplicationId}: " +
                    "Input={Address}, Resolved={ResolvedAddress}, Score={GeocoderScore}, " +
                    "Coordinates=({Latitude}, {Longitude})",
                    application.Id,
                    address,
                    geoAddressDetails.FullAddress,
                    geoAddressDetails.Score,
                    geoAddressDetails.Coordinates.Latitude,
                    geoAddressDetails.Coordinates.Longitude);

                if (geoAddressDetails.Score < 60)
                {
                    application.ApplicantElectoralDistrict = null;
                    logger.LogWarning(
                        "Low geocoder confidence score {GeocoderScore} for application {ApplicationId}. " +
                        "Input={Address}, Resolved={ResolvedAddress}. " +
                        "Electoral district set to null due to unreliable geocoding.",
                        geoAddressDetails.Score, application.Id, address, geoAddressDetails.FullAddress);
                    return;
                }

                var electoralDistrict = await geocoderApiService.GetElectoralDistrictAsync(geoAddressDetails.Coordinates);

                if (electoralDistrict.Name != null)
                {
                    application.ApplicantElectoralDistrict = electoralDistrict.Name;
                    logger.LogInformation(
                        "Electoral district '{ElectoralDistrict}' determined for application {ApplicationId}. " +
                        "Address={Address}, GeocoderScore={GeocoderScore}",
                        electoralDistrict.Name, application.Id, address, geoAddressDetails.Score);
                }
                else
                {
                    logger.LogWarning(
                        "Electoral district could not be determined for application {ApplicationId}. " +
                        "Address={Address}, GeocoderScore={GeocoderScore}",
                        application.Id, address, geoAddressDetails.Score);
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
