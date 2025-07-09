using System.Threading.Tasks;
using Volo.Abp.EventBus;
using Unity.Payments.Events;
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
