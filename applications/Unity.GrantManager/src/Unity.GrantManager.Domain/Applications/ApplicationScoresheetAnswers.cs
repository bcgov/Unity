using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

/// <summary>
/// AI-generated scoresheet answers for a single application. One row per application,
/// holding the whole answer set as JSON keyed by scoresheet question id.
/// <para>
/// Previously stored as the Applications.AIScoresheetAnswers jsonb column. Moved to its own
/// table in the AI schema so the Applications table stays legible to report builders, and so
/// AI output is not written back onto the Application aggregate.
/// </para>
/// </summary>
public class ApplicationScoresheetAnswers : AuditedAggregateRoot<Guid>, IMultiTenant
{
    protected ApplicationScoresheetAnswers()
    {
        Answers = "{}";
    }

    public ApplicationScoresheetAnswers(
        Guid id,
        Guid applicationId,
        string answers)
        : base(id)
    {
        ApplicationId = applicationId;
        Answers = answers;
    }

    public Guid ApplicationId { get; private set; }

    [Column(TypeName = "jsonb")]
    public string Answers { get; private set; }

    public Guid? TenantId { get; set; }

    public void SetAnswers(string answers)
    {
        Answers = answers;
    }
}
