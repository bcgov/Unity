using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.AI;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Events;
using Unity.Modules.Shared.Features;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Features;

namespace Unity.GrantManager.Intakes.Handlers
{
    public class GenerateAiSummaryHandler : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
    {
        private readonly IAIService _aiService;
        private readonly ISubmissionAppService _submissionAppService;
        private readonly IApplicationChefsFileAttachmentRepository _attachmentRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly ILogger<GenerateAiSummaryHandler> _logger;
        private readonly IFeatureChecker _featureChecker;

        public GenerateAiSummaryHandler(
            IAIService aiService,
            ISubmissionAppService submissionAppService,
            IApplicationChefsFileAttachmentRepository attachmentRepository,
            IApplicationRepository applicationRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            ILogger<GenerateAiSummaryHandler> logger,
            IFeatureChecker featureChecker)
        {
            _aiService = aiService;
            _submissionAppService = submissionAppService;
            _attachmentRepository = attachmentRepository;
            _applicationRepository = applicationRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _logger = logger;
            _featureChecker = featureChecker;
        }

        /// <summary>
        /// Generate AI summaries for attachments when a new application is processed
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(ApplicationProcessEvent eventData)
        {
            if (eventData?.Application == null)
            {
                _logger.LogWarning("Event data or application is null in GenerateAiSummaryHandler.");
                return;
            }

            // Check if AI Reporting feature is enabled
            if (!await _featureChecker.IsEnabledAsync(FeatureConsts.AIReporting))
            {
                _logger.LogDebug("AI Reporting feature is disabled, skipping AI summary generation for application {ApplicationId}.", eventData.Application.Id);
                return;
            }

            // Check if AI service is available
            if (!await _aiService.IsAvailableAsync())
            {
                _logger.LogWarning("AI service is not available, skipping AI summary generation for application {ApplicationId}.", eventData.Application.Id);
                return;
            }

            _logger.LogInformation("Generating AI summaries for attachments in application {ApplicationId}.", eventData.Application.Id);

            try
            {
                // Get all CHEFS attachments for this application
                var attachments = await _attachmentRepository.GetListAsync(a => a.ApplicationId == eventData.Application.Id);

                foreach (var attachment in attachments)
                {
                    try
                    {
                        // Skip if already has an AI summary (don't regenerate)
                        if (!string.IsNullOrEmpty(attachment.AISummary))
                        {
                            _logger.LogDebug("Skipping AI summary for attachment {FileName} - already has summary", attachment.FileName);
                            continue;
                        }

                        _logger.LogDebug("Generating AI summary for attachment {FileName}", attachment.FileName);

                        try
                        {
                            // Get the file content from CHEFS (now accessible via [AllowAnonymous])
                            var fileDto = await _submissionAppService.GetChefsFileAttachment(
                                Guid.Parse(attachment.ChefsSumbissionId), 
                                Guid.Parse(attachment.ChefsFileId), 
                                attachment.FileName);

                            if (fileDto?.Content != null)
                            {
                                _logger.LogDebug("Processing {FileName} ({ContentType}, {Size} bytes) for AI summary generation", 
                                    attachment.FileName, fileDto.ContentType, fileDto.Content.Length);

                                // Generate AI summary with text extraction and file content analysis
                                var summary = await _aiService.GenerateAttachmentSummaryAsync(
                                    attachment.FileName, 
                                    fileDto.Content, 
                                    fileDto.ContentType);

                                // Update the attachment with the AI summary
                                attachment.AISummary = summary;
                                await _attachmentRepository.UpdateAsync(attachment);

                                _logger.LogDebug("Successfully generated AI summary for attachment {FileName}: {SummaryPreview}", 
                                    attachment.FileName, summary?.Substring(0, Math.Min(100, summary?.Length ?? 0)) + "...");
                            }
                            else
                            {
                                _logger.LogWarning("Could not retrieve content for attachment {FileName}", attachment.FileName);
                                
                                // Generate summary from filename only as fallback
                                var summary = await _aiService.GenerateAttachmentSummaryAsync(
                                    attachment.FileName, 
                                    new byte[0], 
                                    "application/octet-stream");

                                attachment.AISummary = summary;
                                await _attachmentRepository.UpdateAsync(attachment);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not access CHEFS file {FileName}. Generating summary from filename only.", attachment.FileName);
                            
                            // Fallback: Generate summary from filename only
                            var summary = await _aiService.GenerateAttachmentSummaryAsync(
                                attachment.FileName, 
                                new byte[0], 
                                "application/octet-stream");

                            attachment.AISummary = summary;
                            await _attachmentRepository.UpdateAsync(attachment);

                            _logger.LogDebug("Generated fallback AI summary for attachment {FileName} from filename only", attachment.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating AI summary for attachment {FileName} in application {ApplicationId}", 
                            attachment.FileName, eventData.Application.Id);
                        // Continue processing other attachments even if one fails
                    }
                }

                // After processing all attachments, perform application analysis
                await GenerateApplicationAnalysisAsync(eventData.Application, attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summaries for application {ApplicationId}", eventData.Application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }

        private async Task GenerateApplicationAnalysisAsync(Application application, List<ApplicationChefsFileAttachment> attachments)
        {
            try
            {
                _logger.LogDebug("Starting application analysis for {ApplicationId}", application.Id);

                // Skip if application already has analysis
                if (!string.IsNullOrEmpty(application.AIAnalysis))
                {
                    _logger.LogDebug("Skipping application analysis for {ApplicationId} - already has analysis", application.Id);
                    return;
                }

                // Collect all attachment summaries that were generated
                var attachmentSummaries = attachments
                    .Where(a => !string.IsNullOrEmpty(a.AISummary))
                    .Select(a => $"{a.FileName}: {a.AISummary}")
                    .ToList();

                // Get form submission content including rendered HTML
                var formSubmission = await _applicationFormSubmissionRepository
                    .GetByApplicationAsync(application.Id);

                // Get application content including the full form submission
                var applicationContent = $@"
Project Name: {application.ProjectName}
Reference Number: {application.ReferenceNo}
Requested Amount: ${application.RequestedAmount:N2}
Total Project Budget: ${application.TotalProjectBudget:N2}
Project Summary: {application.ProjectSummary ?? "Not provided"}
City: {application.City ?? "Not specified"}
Economic Region: {application.EconomicRegion ?? "Not specified"}
Community: {application.Community ?? "Not specified"}
Project Start Date: {application.ProjectStartDate?.ToShortDateString() ?? "Not specified"}
Project End Date: {application.ProjectEndDate?.ToShortDateString() ?? "Not specified"}
Submission Date: {application.SubmissionDate.ToShortDateString()}

FULL APPLICATION FORM SUBMISSION:
{formSubmission?.RenderedHTML ?? "Form submission content not available"}
";
                _logger.LogInformation("Generating analysis for following application:", applicationContent);

                // Hardcoded rubric for now
                var rubric = @"
BC GOVERNMENT GRANT EVALUATION RUBRIC:

1. ELIGIBILITY REQUIREMENTS:
   - Project must align with program objectives
   - Applicant must be eligible entity type
   - Budget must be reasonable and well-justified
   - Project timeline must be realistic

2. COMPLETENESS CHECKS:
   - All required fields completed
   - Necessary supporting documents provided
   - Budget breakdown detailed and accurate
   - Project description clear and comprehensive

3. FINANCIAL REVIEW:
   - Requested amount is within program limits
   - Budget is reasonable for scope of work
   - Matching funds or in-kind contributions identified
   - Cost per outcome/beneficiary is reasonable

4. RISK ASSESSMENT:
   - Applicant capacity to deliver project
   - Technical feasibility of proposed work
   - Environmental or regulatory compliance
   - Potential for cost overruns or delays

5. QUALITY INDICATORS:
   - Clear project objectives and outcomes
   - Well-defined target audience/beneficiaries
   - Appropriate project methodology
   - Sustainability plan for long-term impact

EVALUATION CRITERIA:
- HIGH: Meets all requirements, well-prepared application, low risk
- MEDIUM: Meets most requirements, minor issues or missing elements
- LOW: Missing key requirements, significant concerns, high risk
";

                _logger.LogDebug("Generating AI analysis for application {ApplicationId} with {AttachmentCount} attachment summaries", 
                    application.Id, attachmentSummaries.Count);

                // Generate the analysis
                var analysis = await _aiService.AnalyzeApplicationAsync(applicationContent, attachmentSummaries, rubric);

                // Update the application with the analysis
                application.AIAnalysis = analysis;
                await _applicationRepository.UpdateAsync(application);

                _logger.LogInformation("Successfully generated AI analysis: {AIAnalysis}", application.AIAnalysis);
                _logger.LogInformation("Successfully generated AI analysis for application {ApplicationId}", application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating application analysis for {ApplicationId}", application.Id);
                // Don't throw - this should not break the main submission processing
            }
        }
    }
}