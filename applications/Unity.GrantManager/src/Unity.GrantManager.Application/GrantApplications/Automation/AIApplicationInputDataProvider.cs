using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Models;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIApplicationInputDataProvider(
    IApplicationFormRepository applicationFormRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IScoresheetRepository scoresheetRepository) : IAIApplicationInputDataProvider, ITransientDependency
{
    public async Task<ApplicationFormSnapshot?> GetApplicationFormAsync(Guid applicationFormId)
    {
        var form = await applicationFormRepository.FindAsync(applicationFormId);
        return form == null ? null : new ApplicationFormSnapshot { ScoresheetId = form.ScoresheetId };
    }

    public async Task<ApplicationSubmissionSnapshot?> GetApplicationSubmissionAsync(Guid applicationId)
    {
        var submission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        return submission == null
            ? null
            : new ApplicationSubmissionSnapshot
            {
                ApplicationFormVersionId = submission.ApplicationFormVersionId,
                Submission = submission.Submission?.ToString()
            };
    }

    public async Task<ApplicationFormVersionSnapshot?> GetApplicationFormVersionAsync(Guid? formVersionId)
    {
        if (formVersionId == null)
        {
            return null;
        }

        var version = await applicationFormVersionRepository.FindAsync(formVersionId.Value);
        return version == null ? null : new ApplicationFormVersionSnapshot { FormSchema = version.FormSchema };
    }

    public async Task<List<AttachmentSummarySnapshot>> GetAttachmentSummariesAsync(Guid applicationId)
    {
        var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
        return attachments
            .Select(a => new AttachmentSummarySnapshot(
                string.IsNullOrWhiteSpace(a.FileName) ? "attachment" : a.FileName.Trim(),
                string.IsNullOrWhiteSpace(a.AISummary) ? null : a.AISummary.Trim()))
            .Where(a => !string.IsNullOrWhiteSpace(a.Summary))
            .ToList();
    }

    public async Task<ScoresheetSnapshot?> GetScoresheetAsync(Guid scoresheetId)
    {
        var scoresheet = await scoresheetRepository.GetWithChildrenAsync(scoresheetId);
        if (scoresheet == null)
        {
            return null;
        }

        return new ScoresheetSnapshot
        {
            Sections = scoresheet.Sections
                .Select(section => new ScoresheetSectionSnapshot
                {
                    Name = section.Name,
                    Order = (int)section.Order,
                    Fields = section.Fields
                        .Select(field => new ScoresheetFieldSnapshot
                        {
                            Id = field.Id,
                            Label = field.Label,
                            Description = field.Description,
                            Type = field.Type.ToString(),
                            Order = (int)field.Order,
                            Definition = field.Definition
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    public async Task<bool> HasAttachmentsAsync(Guid applicationId)
    {
        var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
        return attachments.Count > 0;
    }

    public async Task<bool> HasSubmissionAsync(Guid applicationId)
    {
        var submission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        return submission != null && !string.IsNullOrWhiteSpace(submission.Submission);
    }
}
