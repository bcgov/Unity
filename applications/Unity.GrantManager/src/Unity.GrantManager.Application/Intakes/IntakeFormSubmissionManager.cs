using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes.Mapping;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Intakes
{
    public class IntakeFormSubmissionManager(IUnitOfWorkManager _unitOfWorkManager,
                                             IApplicantsService applicantsService,
                                             IApplicationRepository _applicationRepository,
                                             IApplicationStatusRepository _applicationStatusRepository,
                                             IApplicationFormSubmissionRepository _applicationFormSubmissionRepository,
                                             IIntakeFormSubmissionMapper _intakeFormSubmissionMapper,
                                             IApplicationFormVersionRepository _applicationFormVersionRepository,
                                             CustomFieldsIntakeSubmissionMapper _customFieldsIntakeSubmissionMapper) : DomainService, IIntakeFormSubmissionManager
    {

        public async Task<string?> GetApplicationFormVersionMapping(string chefsFormVersionId)
        {
            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid == chefsFormVersionId)
                    .FirstOrDefault();

            string? formVersionSubmissionHeaderMapping = null;

            if (applicationFormVersion != null)
            {
                formVersionSubmissionHeaderMapping = applicationFormVersion.SubmissionHeaderMapping;
            }

            return formVersionSubmissionHeaderMapping;
        }

        public async Task<Guid> ProcessFormSubmissionAsync(ApplicationForm applicationForm, dynamic formSubmission)
        {
            string? formVersionId = formSubmission.submission.formVersionId;
            string? formVersionSubmissionHeaderMapping = await GetApplicationFormVersionMapping(formVersionId);
            IntakeMapping intakeMap = _intakeFormSubmissionMapper.MapFormSubmissionFields(applicationForm, formSubmission, formVersionSubmissionHeaderMapping);
            intakeMap.SubmissionId = formSubmission.submission.id;
            intakeMap.SubmissionDate = formSubmission.submission.updatedAt;
            intakeMap.ConfirmationId = formSubmission.submission.confirmationId;
            using var uow = _unitOfWorkManager.Begin();
            var application = await CreateNewApplicationAsync(intakeMap, applicationForm);
            _intakeFormSubmissionMapper.SaveChefsFiles(formSubmission, application.Id);

            var newSubmission = new ApplicationFormSubmission
            {
                OidcSub = Guid.Empty.ToString(),
                ApplicantId = application.ApplicantId,
                ApplicationFormId = applicationForm.Id,
                ChefsSubmissionGuid = intakeMap.SubmissionId ?? $"{Guid.Empty}",
                ApplicationId = application.Id,
                Submission = ChefsFormIOReplacement.ReplaceAdvancedFormIoControls(formSubmission)
            };

            _ = await _applicationFormSubmissionRepository.InsertAsync(newSubmission);

            var localFormVersion = await _applicationFormVersionRepository.GetByChefsFormVersionAsync(Guid.Parse(formVersionId));
            await _customFieldsIntakeSubmissionMapper.MapAndPersistCustomFields(application.Id,
                localFormVersion?.Id ?? Guid.Empty,
                formSubmission,
                formVersionSubmissionHeaderMapping);

            newSubmission.ReportData = GenerateReportDataForSubmission(formSubmission, localFormVersion?.ReportKeys);
            newSubmission.ApplicationFormVersionId = localFormVersion?.Id;

            await uow.SaveChangesAsync();
            
            return application.Id;
        }

        private static string? GenerateReportDataForSubmission(dynamic submissionData, string keys)
        {
            var reportResult = new Dictionary<string, List<string>>();

            JObject submission = JObject.Parse(submissionData.ToString());

            // Navigate to the "data" node within the "submission" node
            JToken? dataNode = submission.SelectToken("submission.submission.data");

            List<string> keysToTrack = [.. keys.Split('|')];

            if (dataNode == null) return null;

            // Perform a recursive scan of the data node
            ScanNode(dataNode, keysToTrack, reportResult);

            // Ensure all keys are present in the result, even if no matches were found
            foreach (var key in keysToTrack)
            {
                if (!reportResult.ContainsKey(key))
                {
                    reportResult[key] = [];
                }
            }

            // Clean up the JSON strings
            foreach (var key in reportResult.Keys.ToList())
            {
                reportResult[key] = reportResult[key].Select(CleanJsonString).ToList();
            }

            // Sort the dictionary by keys alphabetically 
            var sortedResult = reportResult.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Create a simple JSON object from the result dictionary
            JObject jsonObject = [];

            foreach (var kvp in sortedResult)
            {
                JArray valuesArray = new(kvp.Value);
                jsonObject[kvp.Key] = valuesArray;
            }

            if (jsonObject == null) return null;

            // Prep value for db            
            return jsonObject.ToString();
        }

        static void ScanNode(JToken node, List<string> keysToTrack, Dictionary<string, List<string>> result)
        {
            if (node.Type == JTokenType.Object)
            {
                foreach (var property in node.Children<JProperty>())
                {
                    // Check if the key is in the list of keys to track
                    if (keysToTrack.Contains(property.Name))
                    {
                        // Initialize the list if the key is not yet in the result dictionary
                        if (!result.TryGetValue(property.Name, out List<string>? value))
                        {
                            value = ([]);
                            result[property.Name] = value;
                        }

                        value.Add(property.Value.ToString());
                    }

                    // Recursively scan the property's value
                    ScanNode(property.Value, keysToTrack, result);
                }
            }
            else if (node.Type == JTokenType.Array)
            {
                foreach (var item in node.Children())
                {
                    // Recursively scan each item in the array
                    ScanNode(item, keysToTrack, result);
                }
            }
        }

        static string CleanJsonString(string jsonString)
        {
            if (IsJson(jsonString))
            {
                try
                {
                    var parsedJson = JsonConvert.DeserializeObject<JObject>(jsonString);

                    // Serialize the JObject back to a JSON string without escape characters
                    var cleaned = parsedJson != null ? parsedJson.ToString(Formatting.None) : jsonString;

                    return cleaned;
                }
                catch
                {
                    return jsonString; // Return as is if parsing fails
                }
            }
            return jsonString; // Return as is if not a JSON object
        }

        static bool IsJson(string str)
        {
            str = str.Trim();
            return (str.StartsWith('{') && str.EndsWith('}')) || (str.StartsWith('[') && str.EndsWith(']'));
        }

        private async Task<Application> CreateNewApplicationAsync(IntakeMapping intakeMap,
            ApplicationForm applicationForm)
        {
            var applicant = await applicantsService.CreateOrRetrieveApplicantAsync(intakeMap);
            var submittedStatus = await _applicationStatusRepository.FirstAsync(s => s.StatusCode.Equals(GrantApplicationState.SUBMITTED));
            var application = await _applicationRepository.InsertAsync(
                new Application
                {
                    ProjectName = MappingUtil.ResolveAndTruncateField(255, string.Empty, intakeMap.ProjectName),
                    ApplicantId = applicant.Id,
                    ApplicationFormId = applicationForm.Id,
                    ApplicationStatusId = submittedStatus.Id,
                    ReferenceNo = intakeMap.ConfirmationId ?? string.Empty,
                    Acquisition = intakeMap.Acquisition,
                    Forestry = intakeMap.Forestry,
                    ForestryFocus = intakeMap.ForestryFocus,
                    City = intakeMap.PhysicalCity, // To be determined from the applicant
                    EconomicRegion = intakeMap.EconomicRegion,
                    CommunityPopulation = MappingUtil.ConvertToIntFromString(intakeMap.CommunityPopulation),
                    RequestedAmount = MappingUtil.ConvertToDecimalFromStringDefaultZero(intakeMap.RequestedAmount),
                    SubmissionDate = MappingUtil.ConvertDateTimeFromStringDefaultNow(intakeMap.SubmissionDate),
                    ProjectStartDate = MappingUtil.ConvertDateTimeNullableFromString(intakeMap.ProjectStartDate),
                    ProjectEndDate = MappingUtil.ConvertDateTimeNullableFromString(intakeMap.ProjectEndDate),
                    TotalProjectBudget = MappingUtil.ConvertToDecimalFromStringDefaultZero(intakeMap.TotalProjectBudget),
                    Community = intakeMap.Community,
                    ElectoralDistrict = intakeMap.ElectoralDistrict,
                    RegionalDistrict = intakeMap.RegionalDistrict,
                    SigningAuthorityFullName = intakeMap.SigningAuthorityFullName,
                    SigningAuthorityTitle = intakeMap.SigningAuthorityTitle,
                    SigningAuthorityEmail = intakeMap.SigningAuthorityEmail,
                    SigningAuthorityBusinessPhone = intakeMap.SigningAuthorityBusinessPhone,
                    SigningAuthorityCellPhone = intakeMap.SigningAuthorityCellPhone,
                    Place = intakeMap.Place,
                    RiskRanking = intakeMap.RiskRanking,
                    ProjectSummary = intakeMap.ProjectSummary,
                }
            );
            ApplicantAgentDto applicantAgentDto = new ApplicantAgentDto
            {
                Applicant = applicant,
                Application = application,
                IntakeMap = intakeMap
            };
            await applicantsService.CreateOrUpdateApplicantAgentAsync(applicantAgentDto);
            return application;
        }

        public async Task ResyncSubmissionAttachments(Guid applicationId)
        {
            var query = from applicationFormSubmission in await _applicationFormSubmissionRepository.GetQueryableAsync()
                        where applicationFormSubmission.ApplicationId == applicationId
                        select applicationFormSubmission;
            ApplicationFormSubmission? applicationFormSubmissionData = await AsyncExecuter.FirstOrDefaultAsync(query);
            if (applicationFormSubmissionData == null) return;
            var formSubmission = JsonConvert.DeserializeObject<dynamic>(applicationFormSubmissionData.Submission)!;
            await _intakeFormSubmissionMapper.ResyncSubmissionAttachments(applicationId, formSubmission);
        }
    }
}
