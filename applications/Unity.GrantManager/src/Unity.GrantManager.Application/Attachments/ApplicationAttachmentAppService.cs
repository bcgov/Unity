using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
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
    [ExposeServices(typeof(ApplicationAttachmentService), typeof(IApplicationAttachmentService))]
    public class ApplicationAttachmentService : ApplicationService, IApplicationAttachmentService
    {
        private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;

        public ApplicationAttachmentService(IApplicationAttachmentRepository applicationAttachmentRepository)
        {
            _applicationAttachmentRepository = applicationAttachmentRepository;
        }

        public async Task<IList<ApplicationAttachmentDto>> GetListAsync(Guid applicationId)
        {
            IQueryable<ApplicationAttachment> queryableAttachment = _applicationAttachmentRepository.GetQueryableAsync().Result;
            var attachments  = queryableAttachment.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<ApplicationAttachmentDto>>(ObjectMapper.Map<List<ApplicationAttachment>, List<ApplicationAttachmentDto>>(attachments.OrderByDescending(s => s.Time).ToList()));
        }


    }
}
