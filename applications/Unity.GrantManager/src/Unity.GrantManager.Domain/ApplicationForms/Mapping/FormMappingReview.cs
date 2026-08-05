using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class FormMappingReview : AuditedAggregateRoot<Guid>, IMultiTenant
{
    protected FormMappingReview()
    {
        PendingMappingSuggestionsJson = "[]";
        AcceptedWorksheetFieldsJson = "[]";
    }

    public FormMappingReview(Guid id, Guid formVersionId)
        : base(id)
    {
        FormVersionId = formVersionId;
        PendingMappingSuggestionsJson = "[]";
        AcceptedWorksheetFieldsJson = "[]";
        Phase = FormMappingReviewPhase.MappingReview;
    }

    public Guid FormVersionId { get; private set; }
    public FormMappingReviewPhase Phase { get; private set; }

    [Column(TypeName = "jsonb")]
    public string PendingMappingSuggestionsJson { get; private set; }

    [Column(TypeName = "jsonb")]
    public string AcceptedWorksheetFieldsJson { get; private set; }

    public Guid? TenantId { get; set; }

    public void SetPendingMappingSuggestions(string suggestionsJson)
    {
        PendingMappingSuggestionsJson = suggestionsJson;
    }

    public void SetAcceptedWorksheetFields(string fieldsJson)
    {
        AcceptedWorksheetFieldsJson = fieldsJson;
    }

    public void SetPhase(FormMappingReviewPhase phase)
    {
        Phase = phase;
    }
}
