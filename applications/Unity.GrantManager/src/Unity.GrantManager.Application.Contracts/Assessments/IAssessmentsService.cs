using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Assessments;

public interface IAssessmentsService : IApplicationService
{
    Task<AssessmentDto> CreateAssessment(CreateAssessmentDto dto);
    Task<IList<AssessmentDto>> GetListAsync(Guid applicationId);
}
