using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationStatus : AuditedAggregateRoot<Guid>
{
    public string ExternalStatus { get; set; }

    public string InternalStatus { get; set; }

    public string StatusCode { get; set; }

    public ApplicationStatus(string statusCode, string externalStatus, string internalStatus)
    {
        StatusCode = statusCode;
        ExternalStatus = externalStatus;
        InternalStatus = internalStatus;
    }
}
