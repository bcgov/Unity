using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Unity.GrantManager.Intakes
{
    public class IntakeFormSubmissionMapper : InputComponentProcessor, IIntakeFormSubmissionMapper
    {
        private readonly IApplicationChefsFileAttachmentRepository _iApplicationChefsFileAttachmentRepository;

        public IntakeFormSubmissionMapper(IApplicationChefsFileAttachmentRepository iApplicationChefsFileAttachmentRepository)
        {
            _iApplicationChefsFileAttachmentRepository = iApplicationChefsFileAttachmentRepository;
        }

        public string InitializeAvailableFormFields(dynamic formVersion)
        {
            // Check The Version of the form to make sure it is current
            JToken? tokenComponents = ((JObject)formVersion).SelectToken("schema.components");
            TraverseComponents(tokenComponents);
            return JsonSerializer.Serialize(components);
        }

        public IntakeMapping MapFormSubmissionFields(ApplicationForm applicationForm, dynamic formSubmission, string? mapFormSubmissionFields)
        {
            var submission = formSubmission.submission;
            var data = submission.submission.data;

            if (mapFormSubmissionFields != null)
            {
                try
                {
                    return ApplyConfigurationMapping(mapFormSubmissionFields!, data);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return ApplyDefaultConfigurationMapping(data);
                }
            }
            else
            {
                return ApplyDefaultConfigurationMapping(data);
            }
        }

        public async Task SaveChefsFiles(dynamic formSubmission, Guid applicationId)
        {
            Dictionary<Guid, string> files = ExtractSubmissionFiles(formSubmission);
            var submissionId = formSubmission.submission.id;

            foreach (var file in files)
            {
                await _iApplicationChefsFileAttachmentRepository
                    .InsertAsync(new ApplicationChefsFileAttachment
                    {
                        ApplicationId = applicationId,
                        ChefsFileId = file.Key.ToString(),
                        ChefsSumbissionId = submissionId,
                        FileName = file.Value,
                    });
            }
        }

        private static IntakeMapping ApplyDefaultConfigurationMapping(dynamic data)
        {
            return new IntakeMapping()
            {
                UnityApplicantId = data.unityApplicantId,
                ApplicantName = data.applicantName is string ? data.applicantName : null,
                Sector = data.sector is string ? data.sector : null,
                TotalProjectBudget = data.totalProjectBudget is string ? data.totalProjectBudget : null,
                RequestedAmount = data.requestedAmount is string ? data.requestedAmount : null,
                PhysicalCity = data.city is string ? data.city : null,
                EconomicRegion = data.economicRegion is string ? data.economicRegion : null,
                ApplicantAgent = data.applicantAgent
            };
        }

        private static IntakeMapping ApplyConfigurationMapping(string submissionHeaderMapping, dynamic data)
        {
            var configMap = JsonConvert.DeserializeObject<dynamic>(submissionHeaderMapping)!;
            IntakeMapping intakeMapping = ApplyDefaultConfigurationMapping(data);

            if (configMap != null)
            {
                foreach (JProperty property in configMap.Properties())
                {
                    var dataKey = property.Name;
                    var intakeProperty = property.Value.ToString();
                    var dataValue = data.SelectToken(intakeProperty)?.ToString();

                    if (intakeProperty != null && dataValue != null && dataKey != null)
                    {
                        // Get a type object that represents the IntakeMapping.
                        Type intakeType = typeof(IntakeMapping);
                        PropertyInfo? intakePropInfo = intakeType.GetProperty(dataKey!);
                        intakePropInfo?.SetValue(intakeMapping, dataValue?.ToString());
                    }
                }
            }

            return intakeMapping;
        }

        public Dictionary<Guid, string> ExtractSubmissionFiles(dynamic formSubmission)
        {
            var files = new Dictionary<Guid, string>();

            var submission = formSubmission.submission;
            var data = submission.submission.data;
            var version = formSubmission.version;

            foreach (var fileKey in GetFileKeys(version))
            {
                var nodes = FindNodes(data, fileKey);

                foreach (JToken filesNode in nodes) //object containing array of files
                {
                    foreach (JToken prop in filesNode) //array of files
                    {
                        foreach (var obj in (JArray)prop) //each file in array
                        {
                            dynamic? fileObject = obj;
                            var id = fileObject.data.id;
                            var originalName = fileObject.originalName;
                            var uuid = Guid.Parse(Convert.ToString(id));
                            if (!files.ContainsKey(uuid))
                                files.Add(uuid, Convert.ToString(originalName));
                        }
                    }
                }
            }
            return files;
        }

        private static List<string> GetFileKeys(dynamic version)
        {
            var fileKeys = new List<string>();
            fileKeys.AddRange(FileKeyFinder.FindFileKeys(version, "type", "simplefile"));
            fileKeys.AddRange(FileKeyFinder.FindFileKeys(version, "type", "file"));
            return fileKeys;
        }

        public async Task ResyncSubmissionAttachments(Guid applicationId, dynamic formSubmission)
        {
            await DeleteExistingChefsAttachmentRecords(applicationId);
            await SaveChefsFiles(formSubmission, applicationId);
        }

        private async Task DeleteExistingChefsAttachmentRecords(Guid applicationId)
        {
            var query = from chefsAttachment in await _iApplicationChefsFileAttachmentRepository.GetQueryableAsync()
                        where chefsAttachment.ApplicationId == applicationId
                        select chefsAttachment.Id;
            IList<Guid> chefsAttachmentsGuids = query.ToList();
            foreach (Guid chefsAttachmentGuid in chefsAttachmentsGuids)
            {
                await _iApplicationChefsFileAttachmentRepository.DeleteAsync(chefsAttachmentGuid);
            }
        }
    }
}
