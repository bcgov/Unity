using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AdjudicationAttachmentService), typeof(IAdjudicationAttachmentService))]
    public class AdjudicationAttachmentService : ApplicationService, IAdjudicationAttachmentService
    {
        private readonly IAdjudicationAttachmentRepository _adjudicationAttachmentRepository;

        public AdjudicationAttachmentService(IAdjudicationAttachmentRepository adjudicationAttachmentRepository)
        {
            _adjudicationAttachmentRepository = adjudicationAttachmentRepository;
        }

        public async Task<IList<AdjudicationAttachmentDto>> GetListAsync(Guid assessmentId)
        {
            IQueryable<AdjudicationAttachment> queryableAttachment = _adjudicationAttachmentRepository.GetQueryableAsync().Result; 
            var attachments  = queryableAttachment.Where(c => c.AdjudicationId.Equals(assessmentId)).ToList();
            return await Task.FromResult<IList<AdjudicationAttachmentDto>>(ObjectMapper.Map<List<AdjudicationAttachment>, List<AdjudicationAttachmentDto>>(attachments.OrderByDescending(s => s.Time).ToList()));
        }


    }
}
