using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantPrograms;

public class GrantProgramDto : AuditedEntityDto<Guid>
{
    public string ProgramName { get; set; }

    public GrantProgramType Type { get; set; }

    public DateTime PublishDate { get; set; }
}