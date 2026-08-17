using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.AI.Features;
using Unity.AI.Localization;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Runtime.Execution;
using Unity.AI.Settings;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets.Enums;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Mapping;
using Unity.GrantManager.Integrations.Chefs;
using Unity.GrantManager.Reporting.FieldGenerators;
using Unity.Modules.Shared.Features;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Uow;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Permissions;
using Unity.Modules.Shared.Correlation;

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
        AIFeatureGuard aiFeatureGuard,
        IStringLocalizer<AIResource> localizer,
        IAIGenerationAppService aiGenerationAppService,
        IWorksheetRepository worksheetRepository,
        IRepository<CustomField, Guid> customFieldRepository,
        IGenerationReviewRepository generationReviewRepository,
        IWorksheetLinkRepository worksheetLinkRepository,
        IScoresheetRepository scoresheetRepository,
        IWorksheetInstanceRepository worksheetInstanceRepository,
        IScoresheetInstanceRepository scoresheetInstanceRepository) :
        CrudAppService<
            ApplicationFormVersion,
            ApplicationFormVersionDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateApplicationFormVersionDto>(repository),
        IApplicationFormVersionAppService
    {
        private readonly IAIGenerationAppService _aiGenerationAppService = aiGenerationAppService;

        private async Task EnsureAiOperationAccessAsync(string operationType, bool requiresGeneratePermission)
        {
            var operation = AIGenerationOperations.Get(operationType);
            await aiFeatureGuard.EnsureEnabledAsync(operation.FeatureName, operation.DisabledLocalizationKey);
            await CheckPolicyAsync(requiresGeneratePermission ? operation.GeneratePermission : operation.ViewPermission);
        }

        public override async Task<ApplicationFormVersionDto> CreateAsync(CreateUpdateApplicationFormVersionDto input) =>
            await base.CreateAsync(input);

        [RemoteService(false)]
        [Authorize]
        public override async Task<ApplicationFormVersionDto> UpdateAsync(Guid id, CreateUpdateApplicationFormVersionDto input) =>
            await base.UpdateAsync(id, input);

        public override async Task<ApplicationFormVersionDto> GetAsync(Guid id) =>
            await base.GetAsync(id);

        [RemoteService(false)]
        public override Task DeleteAsync(Guid id)
            => base.DeleteAsync(id);

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

        /// <summary>
        /// Queues form mapping generation and returns before the mapping is persisted.
        /// </summary>
        public virtual async Task<ApplicationFormMappingDto> GenerateMappingAsync(Guid id)
        {
            var applicationFormVersion = await Repository.GetAsync(id);
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormMapping,
                id);
            if (review?.Status == GenerationReviewStatus.Active)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormGenerationReviewActive]);
            }
            await _aiGenerationAppService.SubmitAsync(
                AIGenerationOperations.FormMapping,
                new AIGenerationSubmissionDto
            {
                ApplicationId = applicationFormVersion.ApplicationFormId,
                ApplicationFormVersionId = id
            });

            return new ApplicationFormMappingDto
            {
                ApplicationFormVersionId = id
            };
        }

        [HttpGet("api/app/application-form-version/mapping-review")]
        public virtual async Task<FormMappingReviewDto> GetMappingReviewAsync(Guid formVersionId)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormMapping, requiresGeneratePermission: false);
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormMapping,
                formVersionId);
            return await MapMappingReviewAsync(formVersionId, review);
        }

        [HttpPost("api/app/application-form-version/accept-mapping-suggestions")]
        public virtual async Task<AcceptMappingSuggestionsResultDto> AcceptMappingSuggestionsAsync(
            Guid formVersionId,
            AcceptMappingSuggestionsDto input)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormMapping, requiresGeneratePermission: true);
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                    AIGenerationOperations.FormMapping,
                    formVersionId)
                ?? throw new UserFriendlyException(localizer[AILocalizationKeys.MappingReviewPending]);
            if (review.Status != GenerationReviewStatus.Active)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.MappingReviewInactive]);
            }

            var suggestionIds = input?.SuggestionIds?.Distinct().ToHashSet() ?? [];
            if (suggestionIds.Count == 0)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.MappingSelectionRequired]);
            }

            var payload = GetMappingReviewPayload(review);
            var selectedSuggestions = payload.PendingSuggestions
                .Where(suggestion => suggestionIds.Contains(suggestion.Id))
                .ToList();
            if (selectedSuggestions.Count != suggestionIds.Count)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.MappingSelectionInvalid]);
            }

            var formVersion = await Repository.GetAsync(formVersionId);
            formVersion.SubmissionHeaderMapping = FormMappingResponseMapper.MergeSubmissionHeaderMapping(
                formVersion.SubmissionHeaderMapping,
                selectedSuggestions.Select(suggestion => new FormMappingDto
                {
                    SourceField = suggestion.SourceField,
                    TargetField = suggestion.TargetField
                }),
                replaceExisting: review.Sequence > 1 && review.Sequence % 2 == 0);
            await Repository.UpdateAsync(formVersion, true);
            payload.PendingSuggestions.RemoveAll(suggestion => suggestionIds.Contains(suggestion.Id));
            SetMappingReviewPayload(review, payload);
            await generationReviewRepository.UpdateAsync(review, true);

            return new AcceptMappingSuggestionsResultDto
            {
                SubmissionHeaderMapping = formVersion.SubmissionHeaderMapping
            };
        }

        [HttpPost("api/app/application-form-version/discard-mapping-suggestions")]
        public virtual async Task DiscardMappingSuggestionsAsync(Guid formVersionId)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormMapping, requiresGeneratePermission: true);
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormMapping,
                formVersionId);
            if (review == null)
            {
                return;
            }

            var payload = GetMappingReviewPayload(review);
            payload.PendingSuggestions = [];
            review.Discard();
            SetMappingReviewPayload(review, payload);
            await generationReviewRepository.UpdateAsync(review, true);
        }

        [HttpPost("api/app/application-form-version/mapping-review-phase")]
        public virtual async Task SetMappingReviewPhaseAsync(Guid formVersionId, FormMappingReviewPhase phase)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormMapping, requiresGeneratePermission: true);
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormMapping,
                formVersionId);
            if (review == null || review.Status != GenerationReviewStatus.Active)
            {
                if (phase == FormMappingReviewPhase.WorksheetReview &&
                    review?.Sequence == 1)
                {
                    review.Complete();
                    await generationReviewRepository.UpdateAsync(review, true);
                    return;
                }

                if (phase == FormMappingReviewPhase.Completed && review != null)
                {
                    return;
                }

                if (phase != FormMappingReviewPhase.WorksheetReview)
                {
                    throw new UserFriendlyException(localizer[AILocalizationKeys.MappingReviewPending]);
                }

                return;
            }

            var payload = GetMappingReviewPayload(review);
            if (phase == FormMappingReviewPhase.WorksheetReview)
            {
                if (payload.PendingSuggestions.Count > 0)
                {
                    throw new UserFriendlyException(localizer[AILocalizationKeys.MappingReviewPendingSuggestions]);
                }

                review.Complete();
            }
            else if (phase == FormMappingReviewPhase.Completed)
            {
                review.Complete();
            }
            else
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.MappingReviewTransitionInvalid]);
            }

            SetMappingReviewPayload(review, payload);
            await generationReviewRepository.UpdateAsync(review, true);
        }

        [HttpPost("api/app/application-form-version/reset-ai-flow")]
        public virtual async Task ResetAiFlowAsync(Guid formVersionId)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormMapping, requiresGeneratePermission: true);
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormWorksheet, requiresGeneratePermission: true);
            var formVersion = await Repository.GetAsync(formVersionId);
            var mappingReviews = await generationReviewRepository.GetListByOperationAndFormVersionAsync(AIGenerationOperations.FormMapping, formVersionId);
            var worksheetReviews = await generationReviewRepository.GetListByOperationAndFormVersionAsync(AIGenerationOperations.FormWorksheet, formVersionId);
            var worksheetIds = worksheetReviews
                .SelectMany(review => GetWorksheetReviewPayload(review).DraftWorksheetIds)
                .Distinct()
                .ToList();
            var suggestionWorksheet = await worksheetRepository.GetByNameAsync(
                AiWorksheetSuggestionName.Build(formVersion.ApplicationFormId, formVersion.Id), true);
            if (suggestionWorksheet != null && !worksheetIds.Contains(suggestionWorksheet.Id))
            {
                worksheetIds.Add(suggestionWorksheet.Id);
            }

            foreach (var worksheetId in worksheetIds)
            {
                var worksheet = await worksheetRepository.FindAsync(worksheetId);
                if (worksheet == null)
                {
                    continue;
                }

                await DeleteAiWorksheetSuggestionAsync(worksheet, formVersionId);
            }
            await generationReviewRepository.DeleteManyAsync(mappingReviews.Concat(worksheetReviews), true);
            formVersion.SubmissionHeaderMapping = "{}";
            await Repository.UpdateAsync(formVersion, true);
        }

        [HttpPost("api/app/application-form-version/finalize-mapping-review")]
        public virtual async Task FinalizeMappingReviewAsync(Guid formVersionId)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormMapping, requiresGeneratePermission: true);
            var formVersion = await Repository.GetAsync(formVersionId);
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                    AIGenerationOperations.FormMapping,
                    formVersionId)
                ?? throw new UserFriendlyException(localizer[AILocalizationKeys.MappingReviewPending]);
            var worksheetReview = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormWorksheet,
                formVersionId);
            if (review.Sequence % 2 == 0 ||
                review.Status == GenerationReviewStatus.Active ||
                worksheetReview == null ||
                worksheetReview.Status == GenerationReviewStatus.Active ||
                GetWorksheetReviewPayload(worksheetReview).NoSuggestionsGenerated ||
                !await HasNoRemainingDraftsOrAssignedDraftAsync(worksheetReview))
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.WorksheetDraftsMustBePublished]);
            }
            review.Complete();
            await generationReviewRepository.UpdateAsync(review, true);
            await _aiGenerationAppService.SubmitAsync(
                AIGenerationOperations.FormMapping,
                new AIGenerationSubmissionDto
                {
                    ApplicationId = formVersion.ApplicationFormId,
                    ApplicationFormVersionId = formVersionId
                });
        }

        [HttpGet("api/app/application-form-version/pending-ai-worksheet")]
        public virtual async Task<AiWorksheetReviewDto?> GetPendingAiWorksheetAsync(Guid formVersionId)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormWorksheet, requiresGeneratePermission: false);

            var worksheet = await GetPendingAiWorksheetEntityAsync(formVersionId);
            return worksheet == null ? null : MapAiWorksheetReview(worksheet);
        }

        [HttpPost("api/app/application-form-version/create-ai-worksheet-draft")]
        public virtual async Task CreateAiWorksheetDraftAsync(Guid formVersionId, CreateAiWorksheetDraftDto input)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormWorksheet, requiresGeneratePermission: true);
            var worksheet = await GetPendingAiWorksheetEntityAsync(formVersionId);
            if (worksheet == null || worksheet.Id != input.SessionId)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormWorksheetUnavailable]);
            }

            var title = input.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.WorksheetTitleRequired]);
            }

            var selectedFieldIds = input.SelectedFieldIds?.ToHashSet() ?? [];
            if (selectedFieldIds.Count == 0)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.WorksheetSelectionRequired]);
            }

            var fields = worksheet.Sections.SelectMany(section => section.Fields).ToList();
            var unknownFieldIds = selectedFieldIds.Except(fields.Select(field => field.Id)).ToList();
            if (unknownFieldIds.Count > 0)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormWorksheetSelectionInvalid]);
            }

            var draftName = await GetNextAiWorksheetDraftNameAsync(title);
            var draft = new Worksheet(GuidGenerator.Create(), draftName, title);

            var draftSection = new WorksheetSection(GuidGenerator.Create(), "Suggested Fields")
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
                    GuidGenerator.Create(),
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

            var worksheetReview = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormWorksheet,
                formVersionId);
            if (worksheetReview != null)
            {
                var worksheetPayload = GetWorksheetReviewPayload(worksheetReview);
                worksheetPayload.DraftWorksheetIds.Add(draft.Id);
                SetWorksheetReviewPayload(worksheetReview, worksheetPayload);
                await generationReviewRepository.UpdateAsync(worksheetReview);
            }

            foreach (var field in fields.Where(field => selectedFieldIds.Contains(field.Id)))
            {
                field.Section.RemoveField(field);
                await customFieldRepository.DeleteAsync(field.Id);
            }

            if (worksheet.Sections.All(section => section.Fields.Count == 0))
            {
                await DeleteAiWorksheetSuggestionAsync(worksheet, formVersionId);
                if (worksheetReview != null)
                {
                    worksheetReview.Complete();
                    await generationReviewRepository.UpdateAsync(worksheetReview, true);
                }
                return;
            }

            await worksheetRepository.UpdateAsync(worksheet, true);
        }

        [HttpPost("api/app/application-form-version/discard-ai-worksheet-suggestions")]
        public virtual async Task DiscardAiWorksheetSuggestionsAsync(Guid formVersionId)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormWorksheet, requiresGeneratePermission: true);
            var worksheet = await GetPendingAiWorksheetEntityAsync(formVersionId);
            if (worksheet != null)
            {
                await DeleteAiWorksheetSuggestionAsync(worksheet, formVersionId);
                var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                    AIGenerationOperations.FormWorksheet,
                    formVersionId);
                if (review != null)
                {
                    review.Discard();
                    await generationReviewRepository.UpdateAsync(review, true);
                }
            }
        }

        [HttpGet("api/app/application-form-version/pending-ai-scoresheet")]
        public virtual async Task<AiScoresheetReviewDto?> GetPendingAiScoresheetAsync(Guid formVersionId)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormScoresheet, requiresGeneratePermission: false);

            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormScoresheet,
                formVersionId);
            if (review == null || review.Status != GenerationReviewStatus.Active)
            {
                return null;
            }

            var formVersion = await formVersionRepository.GetAsync(formVersionId);
            var scoresheet = await scoresheetRepository.GetByNameAsync(
                AiScoresheetSuggestionName.Build(formVersion.ApplicationFormId, formVersion.Id), true);
            return scoresheet?.Published == false ? MapAiScoresheetReview(scoresheet) : null;
        }

        [HttpPost("api/app/application-form-version/create-ai-scoresheet-draft")]
        public virtual async Task CreateAiScoresheetDraftAsync(Guid formVersionId, CreateAiScoresheetDraftDto input)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormScoresheet, requiresGeneratePermission: true);

            var suggestion = await GetPendingAiScoresheetEntityAsync(formVersionId);
            if (suggestion == null || suggestion.Id != input.SessionId)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetUnavailable]);
            }

            var title = input.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetTitleRequired]);
            }

            var selectedIds = input.SelectedQuestionIds?.ToHashSet() ?? [];
            if (selectedIds.Count == 0)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetSelectionRequired]);
            }

            var questions = suggestion.Sections.SelectMany(section => section.Fields).ToList();
            if (selectedIds.Except(questions.Select(question => question.Id)).Any())
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetSelectionInvalid]);
            }

            var draftName = await GetNextAiScoresheetDraftNameAsync(title);
            var draft = new Scoresheet(GuidGenerator.Create(), title, draftName);
            foreach (var sourceSection in suggestion.Sections.OrderBy(section => section.Order))
            {
                var selectedQuestions = sourceSection.Fields
                    .Where(question => selectedIds.Contains(question.Id))
                    .OrderBy(question => question.Order)
                    .ToList();
                if (selectedQuestions.Count == 0)
                {
                    continue;
                }

                var section = new ScoresheetSection(GuidGenerator.Create(), sourceSection.Name, sourceSection.Order);
                draft.AddSection(section);
                foreach (var sourceQuestion in selectedQuestions)
                {
                    var draftQuestion = new Question(
                        GuidGenerator.Create(),
                        sourceQuestion.Name,
                        sourceQuestion.Label,
                        sourceQuestion.Type,
                        sourceQuestion.Order,
                        sourceQuestion.Description,
                        sourceQuestion.Definition)
                    {
                        SectionId = section.Id
                    };
                    section.Fields.Add(draftQuestion);
                }
            }

            await scoresheetRepository.InsertAsync(draft, true);

            foreach (var question in questions.Where(question => selectedIds.Contains(question.Id)).ToList())
            {
                var sourceSection = suggestion.Sections.First(section => section.Fields.Contains(question));
                sourceSection.Fields.Remove(question);
            }

            if (suggestion.Sections.All(section => section.Fields.Count == 0))
            {
                await DeleteAiScoresheetSuggestionAsync(suggestion);
                var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                    AIGenerationOperations.FormScoresheet,
                    formVersionId);
                review?.Complete();
                if (review != null)
                {
                    await generationReviewRepository.UpdateAsync(review, true);
                }
            }
            else
            {
                await scoresheetRepository.UpdateAsync(suggestion, true);
            }
        }

        [HttpPost("api/app/application-form-version/discard-ai-scoresheet-suggestions")]
        public virtual async Task DiscardAiScoresheetSuggestionsAsync(Guid formVersionId)
        {
            await EnsureAiOperationAccessAsync(AIGenerationOperations.FormScoresheet, requiresGeneratePermission: true);
            var suggestion = await GetPendingAiScoresheetEntityAsync(formVersionId);
            if (suggestion == null)
            {
                return;
            }

            await DeleteAiScoresheetSuggestionAsync(suggestion);
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormScoresheet,
                formVersionId);
            if (review != null)
            {
                review.Discard();
                await generationReviewRepository.UpdateAsync(review, true);
            }
        }

        private async Task<Scoresheet?> GetPendingAiScoresheetEntityAsync(Guid formVersionId)
        {
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormScoresheet,
                formVersionId);
            if (review == null || review.Status != GenerationReviewStatus.Active)
            {
                return null;
            }

            var formVersion = await formVersionRepository.GetAsync(formVersionId);
            var scoresheet = await scoresheetRepository.GetByNameAsync(
                AiScoresheetSuggestionName.Build(formVersion.ApplicationFormId, formVersion.Id), true);
            return scoresheet?.Published == false ? scoresheet : null;
        }

        private static AiScoresheetReviewDto MapAiScoresheetReview(Scoresheet scoresheet) => new()
        {
            SessionId = scoresheet.Id,
            Title = scoresheet.Title,
            Sections = scoresheet.Sections
                .OrderBy(section => section.Order)
                .Select(section => new AiScoresheetReviewSectionDto
                {
                    Id = section.Id,
                    Name = section.Name,
                    Order = section.Order,
                    Questions = section.Fields.OrderBy(question => question.Order)
                        .Select(question => new AiScoresheetReviewQuestionDto
                        {
                            Id = question.Id,
                            SectionId = section.Id,
                            Name = question.Name,
                            Label = question.Label,
                            Description = question.Description,
                            Type = question.Type.ToString(),
                            Selected = true
                        }).ToList()
                }).ToList()
        };

        private async Task DeleteAiWorksheetSuggestionAsync(Worksheet worksheet, Guid formVersionId)
        {
            var links = await worksheetLinkRepository.GetListByWorksheetAsync(worksheet.Id, CorrelationConsts.FormVersion) ?? [];
            if (worksheet.Published ||
                links.Any(link => link.CorrelationId != formVersionId) ||
                await worksheetInstanceRepository.AnyByWorksheetAndFormVersionAsync(worksheet.Id, formVersionId))
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormWorksheetDeleteProtected]);
            }

            foreach (var link in links.Where(link => link.CorrelationId == formVersionId))
            {
                await worksheetLinkRepository.DeleteAsync(link, true);
            }

            await worksheetRepository.DeleteAsync(worksheet, true);
        }

        private async Task DeleteAiScoresheetSuggestionAsync(Scoresheet scoresheet)
        {
            if (scoresheet.Published || scoresheet.IsArchived)
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetDeleteProtected]);
            }
            if (await scoresheetInstanceRepository.AnyByScoresheetAsync(scoresheet.Id))
            {
                throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetHasInstances]);
            }

            await scoresheetRepository.DeleteAsync(scoresheet, true);
        }
        private async Task<Worksheet?> GetPendingAiWorksheetEntityAsync(Guid formVersionId)
        {
            var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormWorksheet,
                formVersionId);
            if (review == null || review.Status != GenerationReviewStatus.Active)
            {
                return null;
            }

            var formVersion = await formVersionRepository.GetAsync(formVersionId);
            var worksheet = await worksheetRepository.GetByNameAsync(
                AiWorksheetSuggestionName.Build(formVersion.ApplicationFormId, formVersion.Id), true);

            if (worksheet?.Published == false)
            {
                return worksheet;
            }

            return null;
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
                    Type = field.Type.ToString(),
                    Selected = true
                })
                .ToList()
        };

        private async Task<FormMappingReviewDto> MapMappingReviewAsync(
            Guid formVersionId,
            GenerationReview? mappingReview)
        {
            var worksheetReview = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
                AIGenerationOperations.FormWorksheet,
                formVersionId);
            var workflow = await DeriveWorkflowAsync(mappingReview, worksheetReview);
            var mappingPayload = mappingReview == null
                ? new FormMappingReviewPayload()
                : GetMappingReviewPayload(mappingReview);
            var worksheetPayload = worksheetReview == null
                ? new FormWorksheetReviewPayload()
                : GetWorksheetReviewPayload(worksheetReview);

            return new FormMappingReviewDto
            {
                FormVersionId = formVersionId,
                Sequence = mappingReview?.Sequence ?? 0,
                Status = mappingReview?.Status ?? GenerationReviewStatus.Completed,
                Phase = GetLegacyPhase(workflow.State),
                WorkflowState = workflow.State,
                WorkflowAction = workflow.Action,
                State = workflow.State.ToString(),
                Action = workflow.Action.ToString(),
                AvailableActions = workflow.AvailableActions,
                ActionEnabled = workflow.ActionEnabled,
                StateLabel = GetWorkflowLabel(workflow.State),
                ActionLabel = GetWorkflowLabel(workflow.Action),
                PendingSuggestions = mappingPayload.PendingSuggestions,
                UnchangedSuggestionCount = mappingPayload.UnchangedSuggestionCount,
                NoSuggestionsGenerated = mappingPayload.NoSuggestionsGenerated,
                NoWorksheetSuggestionsGenerated = worksheetPayload.NoSuggestionsGenerated,
                DraftWorksheetIds = worksheetPayload.DraftWorksheetIds,
                CanGenerateFinalMapping = workflow.State == FormGenerationWorkflowState.GenerateFinalMapping
            };
        }

        private async Task<FormWorkflowResult> DeriveWorkflowAsync(
            GenerationReview? mappingReview,
            GenerationReview? worksheetReview)
        {
            if (mappingReview == null)
            {
                return FormWorkflowResult.Single(
                    FormGenerationWorkflowState.GenerateInitialMapping,
                    FormGenerationWorkflowAction.GenerateInitialMapping,
                    true);
            }

            if (mappingReview.Status == GenerationReviewStatus.Active)
            {
                var isFinalMapping = mappingReview.Sequence > 1 && mappingReview.Sequence % 2 == 0;
                var state = !isFinalMapping
                    ? FormGenerationWorkflowState.ReviewInitialMapping
                    : FormGenerationWorkflowState.ReviewFinalMapping;
                var action = !isFinalMapping
                    ? FormGenerationWorkflowAction.ReviewInitialMapping
                    : FormGenerationWorkflowAction.ReviewFinalMapping;
                return FormWorkflowResult.Single(state, action, true);
            }

            if (mappingReview.Sequence > 1 &&
                mappingReview.Sequence % 2 == 0 &&
                worksheetReview?.Status == GenerationReviewStatus.Active)
            {
                return FormWorkflowResult.Single(
                    FormGenerationWorkflowState.ReviewWorksheets,
                    FormGenerationWorkflowAction.ReviewWorksheets,
                    true);
            }

            if (mappingReview.Sequence > 1 && mappingReview.Sequence % 2 == 0)
            {
                return new FormWorkflowResult(
                    FormGenerationWorkflowState.Completed,
                    FormGenerationWorkflowAction.GenerateMapping,
                    true,
                    [
                        FormGenerationWorkflowAction.GenerateMapping,
                        FormGenerationWorkflowAction.GenerateWorksheetsNextCycle
                    ]);
            }

            if (worksheetReview == null)
            {
                return FormWorkflowResult.Single(
                    FormGenerationWorkflowState.GenerateWorksheets,
                    FormGenerationWorkflowAction.GenerateWorksheets,
                    true);
            }

            if (worksheetReview.Status == GenerationReviewStatus.Active)
            {
                return FormWorkflowResult.Single(
                    FormGenerationWorkflowState.ReviewWorksheets,
                    FormGenerationWorkflowAction.ReviewWorksheets,
                    true);
            }

            if (GetWorksheetReviewPayload(worksheetReview).NoSuggestionsGenerated)
            {
                return FormWorkflowResult.Single(
                    FormGenerationWorkflowState.Completed,
                    FormGenerationWorkflowAction.GenerateMapping,
                    true);
            }

            if (worksheetReview.Status == GenerationReviewStatus.Discarded)
            {
                return FormWorkflowResult.Single(
                    FormGenerationWorkflowState.Completed,
                    FormGenerationWorkflowAction.GenerateMapping,
                    true);
            }

            return await HasNoRemainingDraftsOrAssignedDraftAsync(worksheetReview)
                ? FormWorkflowResult.Single(
                    FormGenerationWorkflowState.GenerateFinalMapping,
                    FormGenerationWorkflowAction.GenerateFinalMapping,
                    true)
                : FormWorkflowResult.Single(
                    FormGenerationWorkflowState.PublishAndAssignWorksheets,
                    FormGenerationWorkflowAction.PublishAndAssignWorksheets,
                    false);
        }

        private async Task<bool> HasNoRemainingDraftsOrAssignedDraftAsync(GenerationReview review)
        {
            var draftWorksheetIds = GetWorksheetReviewPayload(review).DraftWorksheetIds;
            if (draftWorksheetIds.Count == 0)
            {
                return true;
            }

            var linkedWorksheetIds = (await worksheetLinkRepository.GetListByCorrelationAsync(
                review.ContextId,
                CorrelationConsts.FormVersion))
                .Select(link => link.WorksheetId)
                .ToHashSet();

            var hasRemainingDraft = false;

            foreach (var worksheetId in draftWorksheetIds)
            {
                var worksheet = await worksheetRepository.FindAsync(worksheetId);
                if (worksheet == null)
                {
                    continue;
                }

                hasRemainingDraft = true;
                if (worksheet.Published && linkedWorksheetIds.Contains(worksheetId))
                {
                    return true;
                }
            }

            return !hasRemainingDraft;
        }

        private static FormMappingReviewPhase GetLegacyPhase(FormGenerationWorkflowState state) =>
            state switch
            {
                FormGenerationWorkflowState.ReviewInitialMapping => FormMappingReviewPhase.MappingReview,
                FormGenerationWorkflowState.GenerateWorksheets or
                    FormGenerationWorkflowState.ReviewWorksheets => FormMappingReviewPhase.WorksheetReview,
                FormGenerationWorkflowState.PublishAndAssignWorksheets or
                    FormGenerationWorkflowState.GenerateFinalMapping => FormMappingReviewPhase.PublishAndAssignWorksheets,
                FormGenerationWorkflowState.ReviewFinalMapping => FormMappingReviewPhase.FinalMappingReview,
                _ => FormMappingReviewPhase.Completed
            };

        private string GetWorkflowLabel(FormGenerationWorkflowState state) =>
            state switch
            {
                FormGenerationWorkflowState.GenerateInitialMapping => localizer[AILocalizationKeys.WorkflowGenerateInitialMapping],
                FormGenerationWorkflowState.ReviewInitialMapping => localizer[AILocalizationKeys.WorkflowReviewInitialMapping],
                FormGenerationWorkflowState.GenerateWorksheets => localizer[AILocalizationKeys.WorkflowGenerateWorksheets],
                FormGenerationWorkflowState.ReviewWorksheets => localizer[AILocalizationKeys.WorkflowReviewWorksheets],
                FormGenerationWorkflowState.PublishAndAssignWorksheets => localizer[AILocalizationKeys.WorkflowPublishAssignWorksheets],
                FormGenerationWorkflowState.GenerateFinalMapping => localizer[AILocalizationKeys.WorkflowGenerateFinalMapping],
                FormGenerationWorkflowState.ReviewFinalMapping => localizer[AILocalizationKeys.WorkflowReviewFinalMapping],
                _ => localizer[AILocalizationKeys.WorkflowCompleted]
            };

        private string GetWorkflowLabel(FormGenerationWorkflowAction action) =>
            action switch
            {
                FormGenerationWorkflowAction.GenerateInitialMapping => localizer[AILocalizationKeys.WorkflowGenerateInitialMapping],
                FormGenerationWorkflowAction.ReviewInitialMapping => localizer[AILocalizationKeys.WorkflowReviewInitialMapping],
                FormGenerationWorkflowAction.GenerateWorksheets or
                    FormGenerationWorkflowAction.GenerateWorksheetsNextCycle => localizer[AILocalizationKeys.WorkflowGenerateWorksheets],
                FormGenerationWorkflowAction.ReviewWorksheets => localizer[AILocalizationKeys.WorkflowReviewWorksheets],
                FormGenerationWorkflowAction.PublishAndAssignWorksheets => localizer[AILocalizationKeys.WorkflowPublishAssignWorksheets],
                FormGenerationWorkflowAction.GenerateFinalMapping => localizer[AILocalizationKeys.WorkflowGenerateFinalMapping],
                FormGenerationWorkflowAction.GenerateMapping => localizer[AILocalizationKeys.WorkflowGenerateMapping],
                FormGenerationWorkflowAction.ReviewFinalMapping => localizer[AILocalizationKeys.WorkflowReviewFinalMapping],
                _ => localizer[AILocalizationKeys.WorkflowCompleted]
            };

        private sealed record FormWorkflowResult(
            FormGenerationWorkflowState State,
            FormGenerationWorkflowAction Action,
            bool ActionEnabled,
            List<FormGenerationWorkflowAction> AvailableActions)
        {
            public static FormWorkflowResult Single(
                FormGenerationWorkflowState state,
                FormGenerationWorkflowAction action,
                bool enabled) =>
                new(state, action, enabled, [action]);
        }

        private static FormMappingReviewPayload GetMappingReviewPayload(GenerationReview review) =>
            string.IsNullOrWhiteSpace(review.ReviewData)
                ? new FormMappingReviewPayload()
                : JsonSerializer.Deserialize<FormMappingReviewPayload>(review.ReviewData)
                    ?? new FormMappingReviewPayload();

        private static void SetMappingReviewPayload(
            GenerationReview review,
            FormMappingReviewPayload payload) =>
            review.SetReviewData(JsonSerializer.Serialize(payload));

        private static FormWorksheetReviewPayload GetWorksheetReviewPayload(GenerationReview review) =>
            string.IsNullOrWhiteSpace(review.ReviewData)
                ? new FormWorksheetReviewPayload()
                : JsonSerializer.Deserialize<FormWorksheetReviewPayload>(review.ReviewData)
                    ?? new FormWorksheetReviewPayload();

        private static void SetWorksheetReviewPayload(
            GenerationReview review,
            FormWorksheetReviewPayload payload) =>
            review.SetReviewData(JsonSerializer.Serialize(payload));

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

        private async Task<string> GetNextAiScoresheetDraftNameAsync(string title)
        {
            var titlePart = Regex.Replace(title.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            var baseName = $"ai-{(string.IsNullOrEmpty(titlePart) ? "scoresheet" : titlePart)}";
            var candidate = baseName;
            var suffix = 2;

            while (await scoresheetRepository.GetByNameAsync(candidate, false) != null)
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

        private async Task<int> GetVersion(Guid formVersionId)
        {
            var formVersion = await formVersionRepository.GetByChefsFormVersionAsync(formVersionId);
            return formVersion?.Version ?? 0;
        }
    }
}
