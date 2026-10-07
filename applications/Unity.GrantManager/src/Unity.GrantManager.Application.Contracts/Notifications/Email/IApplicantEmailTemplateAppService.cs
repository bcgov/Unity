using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Notifications.Email;

public interface IApplicantEmailTemplateAppService : IApplicationService
{
    Task<ApplicantEmailTemplatePreviewDto> GetPreviewAsync(Guid applicantId, Guid templateId);
}

public class ApplicantEmailTemplatePreviewDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? SendFrom { get; set; }
    public string EmailTo { get; set; } = string.Empty;
    public string TemplateType { get; set; } = "Applicant";
}
