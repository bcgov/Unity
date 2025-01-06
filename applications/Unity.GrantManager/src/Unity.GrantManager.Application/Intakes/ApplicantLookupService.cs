using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Newtonsoft.Json;
using Volo.Abp;
using System.Collections.Generic;
using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Linq;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Intakes
{
    [RemoteService(true)]
    public class ApplicantLookupService(
                            IApplicantRepository applicantRepository,
                            IApplicantAddressRepository applicantAddressRepository,
                            IApplicantAgentRepository applicantAgentRepository) : GrantManagerAppService, IApplicantLookupService
    {

        public async Task<string> ApplicantLookupByApplicantId(string unityApplicantId)
        {
            if (string.IsNullOrWhiteSpace(unityApplicantId))
            {
                throw new ArgumentNullException(nameof(unityApplicantId), "Unity applicant ID cannot be null or empty.");
            }

            try
            {
                Applicant? applicant = await applicantRepository.GetByUnityApplicantIdAsync(unityApplicantId);
                if (applicant == null)
                {
                    throw new KeyNotFoundException("Applicant not found.");
                }
                return await FormatApplicantJsonAsync(applicant);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ApplicantService->ApplicantLookupByApplicantId Exception: {Message}", ex.Message);
                throw new UserFriendlyException("An error occurred while retrieving the applicant.");
            }
        }

        public async Task<string> ApplicantLookupByApplicantName(string unityApplicantName)
        {
            if (string.IsNullOrWhiteSpace(unityApplicantName))
            {
                throw new ArgumentNullException(nameof(unityApplicantName), "Unity applicant name cannot be null or empty.");
            }

            try
            {
                Applicant? applicant = await applicantRepository.GetByUnityApplicantNameAsync(unityApplicantName);
                if (applicant == null)
                {
                    throw new KeyNotFoundException("Applicant not found.");
                }
                return await FormatApplicantJsonAsync(applicant);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ApplicantService->ApplicantLookupByApplicantName Exception: {Message}", ex.Message);
                throw new UserFriendlyException("An error occurred while retrieving the applicant.");
            }
        }

        private async Task<string> FormatApplicantJsonAsync(Applicant? applicant)
        {
            if (applicant == null)
            {
                throw new KeyNotFoundException("Applicant not found.");
            }

            string operatingDate = applicant.StartedOperatingDate != null
                ? ((DateOnly)applicant.StartedOperatingDate).ToString("o", CultureInfo.InvariantCulture)
                : string.Empty;
            
            string bcSocietyNumber = applicant.OrgNumber?.StartsWith('S') == true ? applicant.OrgNumber : string.Empty;

            // Fetch address and agent information
            // Fetch all applicant addresses at once
            List<ApplicantAddress>? applicantAddresses = await applicantAddressRepository.FindByApplicantIdAsync(applicant.Id);

            // Order by date (for example, DateCreated) in descending order
            applicantAddresses = applicantAddresses?.OrderByDescending(x => x.LastModificationTime).ToList();

            // Filter physical and mailing addresses from the sorted list
            ApplicantAddress? applicantPhysicalAgent = applicantAddresses?.Find(x => x.AddressType == GrantApplications.AddressType.PhysicalAddress);
            ApplicantAddress? applicantMailingAgent = applicantAddresses?.Find(x => x.AddressType == GrantApplications.AddressType.MailingAddress);

            ApplicantAgent? applicantAgent = await applicantAgentRepository.FirstOrDefaultAsync(x => x.ApplicantId == applicant.Id);

            var result = new ApplicantResult
            {
                Id = applicant.Id.ToString(),
                ApplicantName = applicant.ApplicantName,
                UnityApplicantId = applicant.UnityApplicantId,
                BcSocietyNumber = bcSocietyNumber,
                OrgNumber = applicant.OrgNumber,
                Sector = applicant.Sector,
                OperatingStartDate = operatingDate,
                FiscalYearDay = applicant.FiscalDay.ToString(),
                FiscalYearMonth = applicant.FiscalMonth,
                BusinessNumber = applicant.BusinessNumber,
                RedStop = applicant.RedStop,
                PhysicalAddressUnit = applicantPhysicalAgent?.Unit ?? "",
                PhysicalAddressLine1 = applicantPhysicalAgent?.Street ?? "",
                PhysicalAddressLine2 = applicantPhysicalAgent?.Street2 ?? "",
                PhysicalAddressPostal = applicantPhysicalAgent?.Postal ?? "",
                PhysicalAddressCity = applicantPhysicalAgent?.City ?? "",
                PhysicalAddressProvince = applicantPhysicalAgent?.Province ?? "",
                PhysicalAddressCountry = applicantPhysicalAgent?.Country ?? "",
                MailingAddressUnit = applicantMailingAgent?.Unit ?? "",
                MailingAddressLine1 = applicantMailingAgent?.Street ?? "",
                MailingAddressLine2 = applicantMailingAgent?.Street2 ?? "",
                MailingAddressPostal = applicantMailingAgent?.Postal ?? "",
                MailingAddressCity = applicantMailingAgent?.City ?? "",
                MailingAddressProvince = applicantMailingAgent?.Province ?? "",
                MailingAddressCountry = applicantMailingAgent?.Country ?? "",
                PhoneNumber = applicantAgent?.Phone ?? "",
                PhoneExtension = applicantAgent?.PhoneExtension ?? ""
            };

            return JsonConvert.SerializeObject(result);
        }
    }
}
