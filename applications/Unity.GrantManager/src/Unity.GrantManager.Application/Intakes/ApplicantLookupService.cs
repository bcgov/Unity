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
            List<ApplicantAddress> applicantAddresses = await applicantAddressRepository.FindByApplicantIdAsync(applicant.Id);
            ApplicantAddress? applicantAddress = applicantAddresses?.FirstOrDefault();
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
                PyhsicalAddressUnit = applicantAddress?.Unit ?? "",
                PyhsicalAddressLine1 = applicantAddress?.Street ?? "",
                PyhsicalAddressLine2 = applicantAddress?.Street2 ?? "",
                PyhsicalAddressPostal = applicantAddress?.Postal ?? "",
                PyhsicalAddressCity = applicantAddress?.City ?? "",
                PyhsicalAddressProvince = applicantAddress?.Province ?? "",
                PyhsicalAddressCountry = applicantAddress?.Country ?? "",
                PhoneNumber = applicantAgent?.Phone ?? "",
                PhoneExtension = applicantAgent?.PhoneExtension ?? ""
            };

            return JsonConvert.SerializeObject(result);
        }
    }
}
