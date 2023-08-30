using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Assessments
{
    [Serializable]
    public class AssessmentCommentDto : EntityDto<Guid>
    {
        public string Comment { get; set; } = string.Empty;
    }
}
