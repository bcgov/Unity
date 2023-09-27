using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.GrantPrograms
{
    public class GrantProgram : AuditedAggregateRoot<Guid>
    {
        public string ProgramName { get; set; } = string.Empty;

        public GrantProgramType Type { get; set; }

        public DateTime PublishDate { get; set; }
    }
}
