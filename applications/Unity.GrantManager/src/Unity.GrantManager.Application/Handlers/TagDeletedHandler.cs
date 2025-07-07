using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Volo.Abp.EventBus;
using Unity.Payments.Events;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.GlobalTag;

namespace Unity.GrantManager.Handlers
{
    public class TagDeletedHandler : ILocalEventHandler<TagDeletedEto>,
    ITransientDependency
    {
        private readonly ITagsService _tagsService;

        public TagDeletedHandler(ITagsService tagsService)
        {

            _tagsService = tagsService;
        }
        public async Task HandleEventAsync(TagDeletedEto eventData)
        {
            
            await _tagsService.DeleteTagAsync(eventData.TagId);
            
        }
    }
}
