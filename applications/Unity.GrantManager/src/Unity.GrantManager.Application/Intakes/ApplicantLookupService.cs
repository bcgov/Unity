using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Newtonsoft.Json;
using Volo.Abp;
using System.Collections.Generic;
using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Intakes
{
    [RemoteService(true)]
    public class ApplicantLookupService(IApplicantRepository applicantRepository) : GrantManagerAppService, IApplicantLookupService
    {

        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);


        public async Task<string> ApplicantLookupByApplicantId(string? unityApplicantId)
        {
            if (applicantRepository == null)
            {
                throw new InvalidOperationException("Applicant repository is not initialized.");
            }

            if (string.IsNullOrWhiteSpace(unityApplicantId))
            {
                throw new ArgumentNullException(nameof(unityApplicantId), "Unity applicant ID cannot be null or empty.");
            }

            try
            {
                Applicant? applicant = await applicantRepository.GetByUnityApplicantId(unityApplicantId);

                if (applicant == null)
                {
                    throw new KeyNotFoundException("Applicant not found.");
                } 

                if (applicant.OrgNumber == null)
                {
                    throw new KeyNotFoundException("Applicant Org Number not found.");
                }

                string operatingDate = applicant.StartedOperatingDate != null ? ((DateOnly)applicant.StartedOperatingDate).ToString("o", CultureInfo.InvariantCulture) : ""; // Specify a format
                string bcSocietyNumber = applicant.OrgNumber.StartsWith('S') ? applicant.OrgNumber : string.Empty;
                var result = new ApplicantResult
                {
                    id = applicant.Id.ToString(),
                    applicant_name = applicant.ApplicantName,
                    unity_applicant_id = applicant.UnityApplicantId,
                    bc_society_number = bcSocietyNumber,
                    org_number = applicant.OrgNumber,
                    sector = applicant.Sector,
                    operating_start_date = operatingDate,
                    fiscal_year_day = applicant.FiscalDay.ToString(),
                    fiscal_year_month = applicant.FiscalMonth
                };

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                var ExceptionMessage = ex.Message;
                logger.LogError(ex, "ApplicantService->ApplicantLookupByApplicantLookup Exception: {ExceptionMessage}", ExceptionMessage);
                throw new UserFriendlyException("An Exception Occured in retreving the Applicant"); 
            }
        }
    }
}
