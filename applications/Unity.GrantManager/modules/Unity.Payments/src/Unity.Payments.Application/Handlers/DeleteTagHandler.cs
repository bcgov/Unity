using System.Threading.Tasks;
using Unity.Payments.Events;
using Unity.Payments.PaymentTags;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Payments.Handlers;
public class DeleteTagHandler(PaymentTagAppService paymentTagAppService) :
    ILocalEventHandler<DeleteTagEto>,
    ITransientDependency
{
    public async Task HandleEventAsync(DeleteTagEto eventData)
    {
        await paymentTagAppService.DeleteTagAsync(eventData.TagName);
    }
}
