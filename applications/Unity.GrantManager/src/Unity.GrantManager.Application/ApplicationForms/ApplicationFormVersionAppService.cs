using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.AI.Cooldown;
using Unity.AI.Features;
using Unity.AI.Permissions;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.AI.Runtime;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integrations.Chefs;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Reporting.FieldGenerators;
using Unity.Modules.Shared.Features;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp;
using Volo.Abp.Uow;
using Unity.GrantManager.Intakes.Mapping;
using Unity.Flex.Domain.Worksheets;

namespace Unity.GrantManager.ApplicationForms
{
    public class ApplicationFormVersionAppService(
        IRepository<ApplicationFormVersion, Guid> repository,
        IIntakeFormSubmissionMapper formSubmissionMapper,
        IUnitOfWorkManager unitOfWorkManager,
        IFormsApiService formsApiService,
        IApplicationFormVersionRepository formVersionRepository,
        IApplicationFormSubmissionRepository formSubmissionRepository,
        IReportingFieldsGeneratorService reportingFieldsGeneratorService,
        IFeatureChecker featureChecker,
        IApplicationFormVersionMappingReadService mappingReadService,
        IAICooldownService aiCooldownService,
        IFormMappingService aiService,
        IWorksheetRepository worksheetRepository,
        IRepository<CustomField, Guid> customFieldRepository) :
        CrudAppService<
            ApplicationFormVersion,
            ApplicationFormVersionDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateApplicationFormVersionDto>(repository),
        IApplicationFormVersionAppService
    {
        private readonly IApplicationFormVersionMappingReadService _mappingReadService = mappingReadService;
        private readonly IAICooldownService _aiCooldownService = aiCooldownService;
        private readonly IFormMappingService _aiService = aiService;

        public override async Task<ApplicationFormVersionDto> CreateAsync(CreateUpdateApplicationFormVersionDto input) =>
            await base.CreateAsync(input);

        public override async Task<ApplicationFormVersionDto> UpdateAsync(Guid id, CreateUpdateApplicationFormVersionDto input) =>
            await base.UpdateAsync(id, input);

        public override async Task<ApplicationFormVersionDto> GetAsync(Guid id) =>
            await base.GetAsync(id);

        public async Task<bool> InitializePublishedFormVersion(dynamic chefsForm, Guid applicationFormId, bool initializePublishedOnly)
        {
            if (chefsForm == null) return false;

            try
            {
                var versionsToken = GetFormVersionToken(chefsForm);
                if (versionsToken == null) return false;

                var childTokens = ((IEnumerable<JToken>)versionsToken.Children()).Where(t => t.Type == JTokenType.Object);
                foreach (var childToken in childTokens)
                {
                    if (TryParsePublished(childToken, out string? formVersionId, out bool published) &&
                        formVersionId != null &&
                        await FormVersionDoesNotExist(formVersionId) &&
                        (!initializePublishedOnly || published))
                    {
                        var applicationFormVersion = await TryInitializeApplicationFormVersionWithToken(childToken, applicationFormId, formVersionId, published);
                        if (applicationFormVersion != null)
                        {
                            await InsertApplicationFormVersion(applicationFormVersion);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception: {Exception}", ex);
            }

            return false;
        }

        private static JToken? GetFormVersionToken(dynamic chefsForm) =>
            chefsForm == null ? null : JObject.Parse(chefsForm.ToString())?["versions"];

        private static bool TryParsePublished(JToken token, out string? formVersionId, out bool published)
        {
            formVersionId = token.Value<string>("id");
            return bool.TryParse(token.Value<string>("published"), out published);
        }

        private async Task<bool> FormVersionDoesNotExist(string formVersionId) =>
            await GetApplicationFormVersion(formVersionId) == null;

        public async Task<ApplicationFormVersionDto?> TryInitializeApplicationFormVersionWithToken(JToken token, Guid applicationFormId, string formVersionId, bool published)
        {
            try
            {
                var formId = token.Value<string>("formId");
                var version = token.Value<int>("version");
                return await TryInitializeApplicationFormVersion(formId, version, applicationFormId, formVersionId, published);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Initialization Exception: {Exception}", ex);
                return null;
            }
        }

        public async Task<ApplicationFormVersionDto?> TryInitializeApplicationFormVersion(string? formId, int version, Guid applicationFormId, string formVersionId, bool published)
        {
            if (formId == null) return null;

            try
            {
                var applicationFormVersion = new ApplicationFormVersion
                {
                    ApplicationFormId = applicationFormId,
                    ChefsApplicationFormGuid = formId,
                    Version = version,
                    Published = published,
                    ChefsFormVersionGuid = formVersionId
                };

                var formVersion = await formsApiService.GetFormDataAsync(formId, formVersionId);
                if (formVersion == null) // Ensure formVersion is not null
                {
                    Logger.LogWarning("Form version data is null for formId: {FormId}, formVersionId: {FormVersionId}", formId, formVersionId);
                    return null;
                }

                applicationFormVersion.AvailableChefsFields = formSubmissionMapper.InitializeAvailableFormFields(formVersion);

                if (formVersion is JObject formVersionObject)
                {
                    var schema = formVersionObject.SelectToken("schema")?.ToString() ?? string.Empty;
                    applicationFormVersion.FormSchema = ChefsFormIOReplacement.ReplaceAdvancedFormIoControls(schema);
                }

                return ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Initialization Exception: {Exception}", ex);
                return null;
            }
        }

        private async Task InsertApplicationFormVersion(ApplicationFormVersionDto applicationFormVersionDto)
        {
            var applicationFormVersion = ObjectMapper.Map<ApplicationFormVersionDto, ApplicationFormVersion>(applicationFormVersionDto);
            await formVersionRepository.InsertAsync(applicationFormVersion);
        }

        public async Task<string?> GetFormVersionSubmissionMapping(string chefsFormVersionId)
        {
            var applicationFormVersion = (await formVersionRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.ChefsFormVersionGuid == chefsFormVersionId);

            return applicationFormVersion?.SubmissionHeaderMapping;
        }

        private async Task<ApplicationFormVersion?> GetApplicationFormVersion(string chefsFormVersionId) =>
            (await formVersionRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.ChefsFormVersionGuid == chefsFormVersionId);

        public async Task<bool> FormVersionExists(string chefsFormVersionId) =>
            await GetApplicationFormVersion(chefsFormVersionId) != null;

        private async Task<bool> UnPublishFormVersions(Guid applicationFormId, string chefsFormVersionId)
        {
            using var uow = unitOfWorkManager.Begin();
            var applicationFormVersion = (await formVersionRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.ChefsFormVersionGuid != chefsFormVersionId && s.ApplicationFormId == applicationFormId);

            if (applicationFormVersion != null)
            {
                applicationFormVersion.Published = false;
                await formVersionRepository.UpdateAsync(applicationFormVersion);
                await uow.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<ApplicationFormVersionDto> UpdateOrCreateApplicationFormVersion(
            string chefsFormId,
            string chefsFormVersionId,
            Guid applicationFormId,
            dynamic chefsFormVersion)
        {
            var applicationFormVersion = await GetOrCreateApplicationFormVersion(chefsFormId, chefsFormVersionId, applicationFormId);
            await UpdateApplicationFormVersionFields(applicationFormVersion, chefsFormVersion, applicationFormId, chefsFormVersionId);

            if (await featureChecker.IsEnabledAsync(FeatureConsts.Reporting) &&
                string.IsNullOrEmpty(applicationFormVersion.ReportViewName))
            {
                // Should be deprecated with new reporting configuration at some point
                await reportingFieldsGeneratorService.GenerateAndSetAsync(applicationFormVersion);
            }

            return ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
        }

        private async Task<ApplicationFormVersion> GetOrCreateApplicationFormVersion(string chefsFormId, string chefsFormVersionId, Guid applicationFormId)
        {
            var applicationFormVersion = await GetApplicationFormVersion(chefsFormVersionId) ??
                                         (await formVersionRepository.GetQueryableAsync())
                                             .FirstOrDefault(s => s.ChefsApplicationFormGuid == chefsFormId && s.ChefsFormVersionGuid == null) ??
                                         new ApplicationFormVersion
                                         {
                                             ApplicationFormId = applicationFormId,
                                             ChefsApplicationFormGuid = chefsFormId
                                         };

            applicationFormVersion.ChefsFormVersionGuid = chefsFormVersionId;
            return applicationFormVersion;
        }

        private async Task UpdateApplicationFormVersionFields(ApplicationFormVersion applicationFormVersion, dynamic chefsFormVersion, Guid applicationFormId, string chefsFormVersionId)
        {
            if (chefsFormVersion == null)
                throw new EntityNotFoundException("Application Form Not Registered");

            var version = ((JObject)chefsFormVersion).SelectToken("version")?.ToString();
            var published = ((JObject)chefsFormVersion).SelectToken("published")?.ToString();
            var schema = ((JObject)chefsFormVersion).SelectToken("schema")?.ToString();

            applicationFormVersion.AvailableChefsFields = formSubmissionMapper.InitializeAvailableFormFields(chefsFormVersion);
            applicationFormVersion.FormSchema = schema != null ? ChefsFormIOReplacement.ReplaceAdvancedFormIoControls(schema) ?? string.Empty : string.Empty;

            if (version != null)
                applicationFormVersion.Version = int.Parse(version);

            if (published != null && bool.TryParse(published, out var isPublished))
            {
                if (isPublished)
                    await UnPublishFormVersions(applicationFormId, chefsFormVersionId);

                applicationFormVersion.Published = isPublished;
            }

            if (applicationFormVersion.Id == Guid.Empty)
                await formVersionRepository.InsertAsync(applicationFormVersion, true);
            else
                await formVersionRepository.UpdateAsync(applicationFormVersion, true);
        }

        public async Task<ApplicationFormVersionDto?> GetByChefsFormVersionId(Guid chefsFormVersionId)
        {
            var applicationFormVersion = await formVersionRepository.GetByChefsFormVersionAsync(chefsFormVersionId);
            return applicationFormVersion == null ? null : ObjectMapper.Map<ApplicationFormVersion, ApplicationFormVersionDto>(applicationFormVersion);
        }

        public async Task<int> GetFormVersionByApplicationIdAsync(Guid applicationId)
        {
            var formSubmission = await formSubmissionRepository.GetByApplicationAsync(applicationId);

            if (formSubmission == null)
            {
                return 0;
            }
            
            if (formSubmission.FormVersionId == null)
            {
                return await HandleEmptyFormVersionIdAsync(formSubmission!);
            }

            return await GetVersion(formSubmission.FormVersionId ?? Guid.Empty);
        }

        /// <summary>
        /// Handles the case where the form version ID is empty or null in the form submission.
        /// This method is for backward compatibility with legacy submissions that may not have the form version ID set.
        /// This method should be reviewed later as it can be removed once all submissions have been migrated to include the form version ID.
        /// </summary>
        /// <param name="formSubmission"></param>
        /// <returns></returns>
        private async Task<int> HandleEmptyFormVersionIdAsync(ApplicationFormSubmission formSubmission)
        {
            try
            {
                var submissionJson = JObject.Parse(formSubmission.Submission);
                var legacyTokenFormVersionId = submissionJson?.SelectToken("submission.formVersionId")?.ToString();
                var newTokenFormVersionId = submissionJson?.SelectToken("formVersionId")?.ToString();

                var formVersionIdString = legacyTokenFormVersionId ?? newTokenFormVersionId;
                if (formVersionIdString == null)
                    return 0;

                var formVersionId = Guid.Parse(formVersionIdString);
                formSubmission.FormVersionId = formVersionId;
                await formSubmissionRepository.UpdateAsync(formSubmission);
                return await GetVersion(formVersionId);
            }
            catch
            {
                return 0;
            }
        }

        public async Task DeleteWorkSheetMappingByFormName(string formName, Guid formVersionId)
        {
            var applicationFormVersion = await formVersionRepository.GetAsync(formVersionId);
            if (applicationFormVersion?.SubmissionHeaderMapping == null) return;

            var pattern = $"(,\\s*\\\"{formName}.*\\\")|(\\\"{formName}.*\\\",)";
            applicationFormVersion.SubmissionHeaderMapping = Regex.Replace(applicationFormVersion.SubmissionHeaderMapping, pattern, "", RegexOptions.None, TimeSpan.FromSeconds(30));
            await formVersionRepository.UpdateAsync(applicationFormVersion);
        }

        public virtual async Task<ApplicationFormMappingDto> GenerateMappingAsync(Guid id)
        {
            if (!await featureChecker.IsEnabledAsync(AIFeatures.FormMapping))
            {
                throw new UserFriendlyException("AI form mapping is disabled.");
            }

            await CheckPolicyAsync(AIPermissions.Analysis.GenerateFormMapping);
            await _aiCooldownService.EnsureAsync(CurrentUser.Id);

            var readModel = await _mappingReadService.GetAsync(id);
            var response = await _aiService.GenerateFormMappingAsync(new FormMappingRequest
            {
                Data = FormMappingPromptDataBuilder.Build(readModel)
            });
            var submissionHeaderMapping = FormMappingResponseMapper.BuildSubmissionHeaderMapping(response);
            var applicationFormVersion = await repository.GetAsync(id);
            applicationFormVersion.SubmissionHeaderMapping = submissionHeaderMapping;
            await repository.UpdateAsync(applicationFormVersion, true);

            return new ApplicationFormMappingDto
            {
                ApplicationFormVersionId = id
            };
        }

        [HttpGet("api/app/application-form-version/pending-ai-worksheet")]
        public virtual async Task<AiWorksheetReviewDto?> GetPendingAiWorksheetAsync(Guid formVersionId)
        {
            await CheckPolicyAsync(AIPermissions.Analysis.ViewFormWorksheet);

            var worksheet = await GetPendingAiWorksheetEntityAsync(formVersionId);
            return worksheet == null ? null : MapAiWorksheetReview(worksheet);
        }

        [HttpPost("api/app/application-form-version/create-ai-worksheet-draft")]
        public virtual async Task CreateAiWorksheetDraftAsync(Guid formVersionId, CreateAiWorksheetDraftDto input)
        {
            await CheckPolicyAsync(AIPermissions.Analysis.GenerateFormWorksheet);

            var worksheet = await GetPendingAiWorksheetEntityAsync(formVersionId);
            if (worksheet == null || worksheet.Id != input.SessionId)
            {
                throw new UserFriendlyException("The AI worksheet is no longer available for review.");
            }

            var title = input.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new UserFriendlyException("A worksheet title is required.");
            }

            var selectedFieldIds = input.SelectedFieldIds?.ToHashSet() ?? [];
            if (selectedFieldIds.Count == 0)
            {
                throw new UserFriendlyException("Select at least one suggested field.");
            }

            var fields = worksheet.Sections.SelectMany(section => section.Fields).ToList();
            var unknownFieldIds = selectedFieldIds.Except(fields.Select(field => field.Id)).ToList();
            if (unknownFieldIds.Count > 0)
            {
                throw new UserFriendlyException("The AI worksheet selection is invalid.");
            }

            var draftName = await GetNextAiWorksheetDraftNameAsync(title);
            var draft = new Worksheet(Guid.NewGuid(), draftName, title);

            var draftSection = new WorksheetSection(Guid.NewGuid(), "Suggested Fields")
            {
                Worksheet = draft
            }.SetOrder(1);
            draft.AddSection(draftSection);

            foreach (var (field, index) in fields
                .Where(field => selectedFieldIds.Contains(field.Id))
                .OrderBy(field => field.Section.Order)
                .ThenBy(field => field.Order)
                .Select((field, index) => (field, index)))
            {
                var draftField = new CustomField(
                    Guid.NewGuid(),
                    field.Key,
                    draft.Name,
                    field.Label,
                    field.Type,
                    NormalizeCustomFieldDefinition(field.Definition));
                draftField.Section = draftSection;
                draftSection.AddField(draftField);
                draftField.SetOrder((uint)(index + 1)).SetEnabled(true);
            }

            await worksheetRepository.InsertAsync(draft, true);

            foreach (var field in fields.Where(field => selectedFieldIds.Contains(field.Id)))
            {
                field.Section.RemoveField(field);
                await customFieldRepository.DeleteAsync(field.Id);
            }

            if (worksheet.Sections.All(section => section.Fields.Count == 0))
            {
                await worksheetRepository.DeleteAsync(worksheet, true);
                return;
            }

            await worksheetRepository.UpdateAsync(worksheet, true);
        }

        [HttpPost("api/app/application-form-version/discard-ai-worksheet-suggestions")]
        public virtual async Task DiscardAiWorksheetSuggestionsAsync(Guid formVersionId)
        {
            await CheckPolicyAsync(AIPermissions.Analysis.GenerateFormWorksheet);

            var worksheet = await GetPendingAiWorksheetEntityAsync(formVersionId);
            if (worksheet != null)
            {
                await worksheetRepository.DeleteAsync(worksheet, true);
            }
        }

        private async Task<Worksheet?> GetPendingAiWorksheetEntityAsync(Guid formVersionId)
        {
            var formVersion = await formVersionRepository.GetAsync(formVersionId);
            var worksheetName = BuildAiWorksheetName(formVersion.ApplicationFormId, formVersion.Id);
            var worksheet = await worksheetRepository.GetByNameAsync(worksheetName, true);

            if (worksheet == null)
            {
                return null;
            }

            if (worksheet.Published)
            {
                return null;
            }

            return worksheet;
        }

        private static AiWorksheetReviewDto MapAiWorksheetReview(Worksheet worksheet) => new()
        {
            SessionId = worksheet.Id,
            Fields = worksheet.Sections
                .OrderBy(section => section.Order)
                .SelectMany(section => section.Fields.OrderBy(field => field.Order))
                .Select(field => new AiWorksheetReviewFieldDto
                {
                    Id = field.Id,
                    Key = field.Key,
                    Label = field.Label,
                    Type = field.Type.ToString()
                })
                .ToList()
        };

        private async Task<string> GetNextAiWorksheetDraftNameAsync(string title)
        {
            var titlePart = Regex.Replace(title.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            var baseName = $"ai-{(string.IsNullOrEmpty(titlePart) ? "worksheet" : titlePart)}";
            var candidate = baseName;
            var suffix = 2;

            while (await worksheetRepository.GetByNameAsync(candidate, false) != null)
            {
                candidate = $"{baseName}-{suffix++}";
            }

            return candidate;
        }

        private static string NormalizeCustomFieldDefinition(string definition)
        {
            try
            {
                using var document = JsonDocument.Parse(definition);
                if (document.RootElement.ValueKind != JsonValueKind.String)
                {
                    return definition;
                }

                var unwrappedDefinition = document.RootElement.GetString();
                if (string.IsNullOrWhiteSpace(unwrappedDefinition))
                {
                    return definition;
                }

                using var unwrappedDocument = JsonDocument.Parse(unwrappedDefinition);
                return unwrappedDocument.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                    ? unwrappedDefinition
                    : definition;
            }
            catch (JsonException)
            {
                return definition;
            }
        }

        private static string BuildAiWorksheetName(Guid formId, Guid formVersionId) =>
            $"ai-form-{formId}-version-{formVersionId}-worksheet";

        private async Task<int> GetVersion(Guid formVersionId)
        {
            var formVersion = await formVersionRepository.GetByChefsFormVersionAsync(formVersionId);
            return formVersion?.Version ?? 0;
        }
    }
}
