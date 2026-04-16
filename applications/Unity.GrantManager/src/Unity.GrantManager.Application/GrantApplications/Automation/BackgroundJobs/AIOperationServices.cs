using Unity.AI;
using Unity.AI.Operations;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

/// <summary>
/// Aggregates the four AI operation services used by <see cref="RunApplicationAIPipelineJob"/>.
/// Introduced to keep the constructor within SonarQube's 7-parameter limit (S107)
/// while preserving explicit, testable constructor injection for each dependency.
/// Implements <see cref="ITransientDependency"/> so ABP's DI scanning registers it in the container.
/// </summary>
public record AIOperationServices(
    IAttachmentSummaryService AttachmentSummary,
    IApplicationAnalysisService ApplicationAnalysis,
    IApplicationScoringService ApplicationScoring,
    IAIService AI) : ITransientDependency;
