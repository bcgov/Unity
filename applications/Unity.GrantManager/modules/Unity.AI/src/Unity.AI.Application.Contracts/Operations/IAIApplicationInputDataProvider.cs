using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Models;

namespace Unity.AI.Operations;

public interface IAIApplicationInputDataProvider
{
    Task<ApplicationFormSnapshot?> GetApplicationFormAsync(Guid applicationId);

    Task<ApplicationSubmissionSnapshot?> GetApplicationSubmissionAsync(Guid applicationId);

    Task<ApplicationFormVersionSnapshot?> GetApplicationFormVersionAsync(Guid? formVersionId);

    Task<List<AttachmentSummarySnapshot>> GetAttachmentSummariesAsync(Guid applicationId);

    Task<ScoresheetSnapshot?> GetScoresheetAsync(Guid scoresheetId);

    Task<bool> HasAttachmentsAsync(Guid applicationId);

    Task<bool> HasSubmissionAsync(Guid applicationId);
}

public sealed class ApplicationFormSnapshot
{
    public Guid? ScoresheetId { get; set; }
}

public sealed class ApplicationSubmissionSnapshot
{
    public Guid? ApplicationFormVersionId { get; set; }

    public string? Submission { get; set; }
}

public sealed class ApplicationFormVersionSnapshot
{
    public string? FormSchema { get; set; }
}

public sealed record AttachmentSummarySnapshot(
    string? FileName,
    string? Summary);

public sealed class ScoresheetSnapshot
{
    public List<ScoresheetSectionSnapshot> Sections { get; set; } = [];
}

public sealed class ScoresheetSectionSnapshot
{
    public string Name { get; set; } = string.Empty;

    public int Order { get; set; }

    public List<ScoresheetFieldSnapshot> Fields { get; set; } = [];
}

public sealed class ScoresheetFieldSnapshot
{
    public Guid Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Type { get; set; } = string.Empty;

    public int Order { get; set; }

    public string? Definition { get; set; }
}
