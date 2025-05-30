using System.Threading.Tasks;
using Unity.Payments.Events;
using Unity.Payments.PaymentTags;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Payments.Handlers;
public class RenameTagHandler(PaymentTagAppService paymentTagAppService) :
    ILocalEventHandler<RenameTagEto>,
    ITransientDependency
{
    public async Task HandleEventAsync(RenameTagEto eventData)
    {
        await paymentTagAppService.RenameTagAsync(eventData.originalTagName, eventData.replacementTagName);
    }
}
