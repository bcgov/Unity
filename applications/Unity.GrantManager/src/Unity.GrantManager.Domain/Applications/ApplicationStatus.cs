using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationStatus : AuditedAggregateRoot<Guid>
{
    public string ExternalStatus { get; set; }

    public string InternalStatus { get; set; }

    public string StatusCode { get; set; }
}
