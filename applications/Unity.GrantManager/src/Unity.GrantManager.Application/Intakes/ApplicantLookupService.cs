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
using Unity.GrantManager.Applicants;
using Unity.Payments.Integrations.Cas;

namespace Unity.GrantManager.Intakes
{
    [RemoteService(true)]
    public class ApplicantLookupService(
                            ISupplierService supplierService,
                            IApplicantAppService applicantAppService,
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

        public async Task<string> ApplicantLookupByBceidBusinesName(string bceidBusinessName, bool createIfNotExists = false)
        {
            if (string.IsNullOrWhiteSpace(bceidBusinessName))
            {
                throw new ArgumentNullException(nameof(bceidBusinessName), "Unity applicant name cannot be null or empty.");
            }

            try
            {
                Applicant? applicant = await applicantRepository.GetByUnityApplicantNameAsync(bceidBusinessName);
                if (applicant == null)
                {
                    if (!createIfNotExists)
                    {
                        throw new KeyNotFoundException("Applicant not found.");
                    }

                    int unityApplicantId = await applicantAppService.GetNextUnityApplicantIdAsync();
                    applicant = await applicantRepository.InsertAsync(new Applicant
                    {
                        ApplicantName = bceidBusinessName,
                        UnityApplicantId = unityApplicantId.ToString(),
                        RedStop = false
                    });
                }

                if (applicant.OrgNumber.IsNullOrEmpty() || applicant.BusinessNumber.IsNullOrEmpty())
                {
                    applicant = await applicantAppService.UpdateApplicantOrgMatchAsync(applicant);
                    if (applicant?.OrgNumber != null && applicant?.BusinessNumber != null)
                    {
                        try
                        {
                            await supplierService.UpdateApplicantSupplierInfoByBn9(applicant.BusinessNumber, applicant.Id);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "ApplicantService->ApplicantLookupByBceidBusinesName Exception: {Message}", ex.Message);
                        }
                    }
                }

                return await FormatApplicantJsonAsync(applicant);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ApplicantService->ApplicantLookupByBceidBusinesName Exception: {Message}", ex.Message);
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
            applicantAddresses = applicantAddresses?.OrderByDescending(x => x.CreationTime).ToList();

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
                OrgName = applicant.OrgName,
                OrgNumber = applicant.OrgNumber,
                BusinessNumber = applicant.BusinessNumber,
                Sector = applicant.Sector,
                OperatingStartDate = operatingDate,
                FiscalYearDay = applicant.FiscalDay.ToString(),
                FiscalYearMonth = applicant.FiscalMonth,
                RedStop = applicant.RedStop,
                IndigenousOrgInd = applicant.IndigenousOrgInd,
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
