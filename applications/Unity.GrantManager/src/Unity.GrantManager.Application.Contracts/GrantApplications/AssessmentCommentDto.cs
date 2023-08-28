using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;


namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class AssessmentCommentDto : EntityDto<Guid>
{
    public string Comment { get; set; } = string.Empty;
}
