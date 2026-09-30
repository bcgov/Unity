using System;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.Data;

namespace Unity.Notifications.Emails;

/// <summary>Ownership invariants for emails composed against an application or applicant.</summary>
public static class EmailOwnership
{
    // Use the existing JSON metadata column so draft template identity needs no schema migration.
    public static Guid? GetTemplateId(EmailLog email)
    {
        return Guid.TryParse(email.GetProperty<string>("EmailTemplateId"), out var id) ? id : null;
    }

    public static void SetTemplateId(EmailLog email, Guid templateId)
    {
        email.SetProperty("EmailTemplateId", templateId.ToString());
    }

    public static void EnsureSingleOwner(Guid applicationId, Guid applicantId)
    {
        if ((applicationId == Guid.Empty) == (applicantId == Guid.Empty))
        {
            throw new BusinessException("Notifications:EmailOwnerRequired");
        }
    }

    public static void EnsureDraftOwner(EmailLog email, Guid applicationId, Guid applicantId)
    {
        EnsureSingleOwner(applicationId, applicantId);
        if (email.ApplicationId != applicationId || email.ApplicantId != applicantId)
        {
            throw new BusinessException("Notifications:EmailOwnerMismatch");
        }
        EnsureDraft(email);
    }

    public static void EnsureDraft(EmailLog email)
    {
        if (email.Status != EmailStatus.Draft)
        {
            throw new BusinessException("Notifications:EmailMustBeDraft");
        }
    }

    public static void EnsureTemplateType(EmailTemplate template, Guid applicantId)
    {
        var expectedType = applicantId == Guid.Empty ? TemplateTypes.Application : TemplateTypes.Applicant;
        if (!string.Equals(template.TemplateType, expectedType, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Notifications:EmailTemplateTypeMismatch");
        }
    }
}
